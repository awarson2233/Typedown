using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Typedown.Core.Interfaces;

namespace Typedown.Core.Editor.Wire
{
    /// <summary>会话与承载新引擎页面的宿主之间的通道；WinUI 里由 WebView2 宿主实现，单测里是假通道。</summary>
    public interface IEditorWireChannel
    {
        /// <summary>向页面投递一条报文；页面不可用时返回 <c>false</c>。</summary>
        bool TryPost(string message);

        /// <summary>移除上一次注入的初始态脚本，注入 <paramref name="initScript"/>，再（重新）导航到编辑器页面。</summary>
        Task NavigateAsync(string initScript);

        /// <summary>宿主当前的主题；会话还没收到过 <see cref="ApplyTheme"/> 时，初始态取它。</summary>
        EditorTheme GetCurrentTheme();
    }

    /// <summary>
    /// 新引擎线协议（docs/editor-protocol.md）的 <see cref="IEditorSession"/> 实现，不含任何 WebView2 代码：
    /// 启动握手与就绪门、正文镜像与重同步、双向请求与超时、致命错误与渲染进程退出后的重载恢复都在这里，
    /// 宿主只经 <see cref="IEditorWireChannel"/> 收发报文字符串、注入初始态并导航。
    /// 除计时器回调经 <see cref="IUiDispatcher"/> 切回 UI 线程外，所有入口都在 UI 线程上调用，非线程安全。
    /// </summary>
    public class EditorWireSession : IEditorSession, IDisposable
    {
        /// <summary>宿主→页面请求的默认超时。</summary>
        public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

        /// <summary><c>doc.flush</c> 的超时；超时后按当前镜像继续，不阻塞保存。</summary>
        public static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(1);

        /// <summary><c>export.renderHtml</c> 的超时。</summary>
        public static readonly TimeSpan ExportTimeout = TimeSpan.FromSeconds(60);

        private readonly Func<IEditorHostCallbacks> callbacks;
        private readonly IUiDispatcher dispatcher;
        private readonly TimeProvider timeProvider;
        private readonly BehaviorSubject<EditorSessionState> state = new(EditorSessionState.Detached);
        private readonly Subject<EditorEvent> events = new();
        private readonly DocumentMirror mirror = new();
        private readonly EditorCrashRecovery crashRecovery;
        private readonly Dictionary<long, PendingCall> pending = new();
        private readonly CancellationTokenSource lifetime = new();

        /// <summary>宿主卸载（打开设置页时 MainPage 整体卸载）期间命令照样排队、设置照样合并，重新挂载时重放。</summary>
        private readonly EditorCommandGate<string> gate = new(retainWhileDetached: true);

        private IEditorWireChannel? channel;
        private string basePath = string.Empty;
        private long nextRequestId;
        private bool disposed;

        /// <summary>页面报了致命错误或渲染进程退出，重载完成（下一次 ready）前为真。</summary>
        private bool faulted;

        /// <summary>页面的协议版本与宿主不一致：开发时的混合构建，不重试。</summary>
        private bool protocolMismatch;

        /// <summary>每次（重新）装载页面加一；页面回问宿主的请求跨代后不再应答，免得撞上新页面同号的请求。</summary>
        private long pageGeneration;

        /// <summary>并发的装载只让最后一次导航。</summary>
        private long loadSequence;

        /// <summary>会话自己发起了导航，下一次 NavigationStarting 是预期中的。</summary>
        private bool navigationExpected;

        /// <summary>页面还活着但宿主卸载期间装载的文档，重新挂载时先补发。</summary>
        private DocLoad? unsentLoad;

        private long? resyncCallId;
        private readonly List<TaskCompletionSource<long>> resyncWaiters = new();

        // 崩溃恢复与初始态用到的最后一份状态。
        private DocSelection? lastSelection;
        private double? lastScrollTop;
        private (DocSelection? Selection, double? ScrollTop)? restore;
        private EditorTheme? lastTheme;
        private IReadOnlyList<KeyChord>? lastKeymap;

        // 页面当前手里的主题与快捷键表：就绪门重放时相同的就不再发。
        private EditorTheme? pageTheme;
        private IReadOnlyList<KeyChord>? pageKeymap;

        // 宿主侧最后一次看到的「变化才发」状态；页面重载后从初始值起算，这里据此补一次复位。
        private bool lastCanUndo;
        private bool lastCanRedo;
        private bool lastHasMarks;

        public EditorWireSession(Func<IEditorHostCallbacks> callbacks, IUiDispatcher dispatcher, TimeProvider? timeProvider = null)
        {
            this.callbacks = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.timeProvider = timeProvider ?? TimeProvider.System;
            crashRecovery = new EditorCrashRecovery(this.timeProvider);
        }

        // ── IEditorSession ────────────────────────────────────────────────

        public EditorSessionState State => state.Value;

        public IObservable<EditorSessionState> StateChanged => state.DistinctUntilChanged().Skip(1);

        public EditorDocument Document => mirror.Document;

        public IObservable<EditorEvent> Events => events.AsObservable();

        public void Post(EditorCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            if (disposed)
            {
                return;
            }

            switch (command)
            {
                case LoadDocument load:
                    PostLoad(load);
                    return;
                case ApplyTheme theme:
                    lastTheme = theme.Theme;
                    break;
                case SetKeymap keymap:
                    lastKeymap = keymap.Chords.ToArray();
                    break;
            }

            if (command is ApplyTheme or SetKeymap or ApplySettings or RefreshViewport)
            {
                if (gate.Submit(command))
                {
                    SendRetained(command);
                }

                return;
            }

            // 页面还没实现的命令照常发出：页面只记日志，不会有副作用。
            var message = EditorWireCodec.EncodeCommand(command);
            if (gate.Submit(message))
            {
                Send(message);
            }
        }

        public async Task<TResult> RequestAsync<TResult>(EditorRequest<TResult> request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ObjectDisposedException.ThrowIf(disposed, this);
            if (request is FlushDocument)
            {
                return (TResult)(object)await FlushAsync(cancellationToken);
            }

            var timeout = request is RenderExportHtml ? ExportTimeout : RequestTimeout;
            return await Call(EditorWireCodec.EncodeRequest(NextRequestId(), request), timeout, cancellationToken);
        }

        // ── 宿主侧接口 ───────────────────────────────────────────────────

        /// <summary>宿主挂载。页面还活着时（例如从设置页回来）先补发卸载期间装载的文档，再重放就绪门。</summary>
        public void Attach(IEditorWireChannel host)
        {
            ArgumentNullException.ThrowIfNull(host);
            if (disposed)
            {
                return;
            }

            channel = host;
            var pageAlive = gate.IsReady && !gate.IsOpen;
            var replay = gate.Attach();
            if (gate.IsOpen && unsentLoad is { } load)
            {
                unsentLoad = null;
                SendLoad(load);
            }

            Replay(replay);
            SendHeldCalls();
            UpdateState();
            if (gate.IsOpen && pageAlive)
            {
                // 卸载期间宿主收不到页面的报文，挂载前最后一帧的增量可能丢了；flush 应答的版本号会暴露出来并触发重同步。
                _ = ReconcileAsync();
            }
        }

        /// <summary>宿主卸载：宿主不再收页面的报文，挂起的请求随之作废；命令继续排队。</summary>
        public void Detach(IEditorWireChannel host)
        {
            if (!ReferenceEquals(channel, host))
            {
                return;
            }

            gate.Detach();
            AbortAll("编辑器宿主已卸载，请求作废。");
            UpdateState();
        }

        /// <summary>
        /// 取启动设置（首次调用时装载启动文档）、生成初始态，交给宿主注入并导航。宿主挂载后首次加载与崩溃后的重载都走这里。
        /// </summary>
        public async Task LoadPageAsync()
        {
            if (disposed || channel is not { } target)
            {
                return;
            }

            var sequence = ++loadSequence;
            BeginPageLoad();
            EditorSettings settings;
            try
            {
                settings = await callbacks().PrepareStartupAsync(lifetime.Token);
            }
            catch (Exception ex) when (!disposed)
            {
                Log($"PrepareStartupAsync 失败，按默认设置启动页面：{ex}");
                settings = EditorSettings.None;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (disposed || sequence != loadSequence)
            {
                return;
            }

            // 初始态已含全量设置，积压的设置增量作废；主题与快捷键表记下页面手里的这份，重放时相同的不再发。
            gate.ClearPendingSettings();
            pageTheme = lastTheme ?? target.GetCurrentTheme();
            pageKeymap = lastKeymap ?? [];
            var init = new EditorInitState(EditorWireTypes.ProtocolVersion, settings, pageTheme, pageKeymap, CultureInfo.CurrentUICulture.Name);
            navigationExpected = true;
            try
            {
                await target.NavigateAsync(EditorWireCodec.EncodeInitScript(init));
            }
            catch (Exception ex)
            {
                navigationExpected = false;
                Log($"导航到编辑器页面失败：{ex}");
            }
        }

        /// <summary>页面开始导航。会话自己发起的导航在 <see cref="LoadPageAsync"/> 里已经收拾过旧页面；其余（页面自己跳走、被外部重载）在这里作废旧页面。</summary>
        public void OnNavigationStarting()
        {
            if (navigationExpected)
            {
                navigationExpected = false;
                return;
            }

            Log("页面发生了非会话发起的导航，旧页面的状态作废。");
            BeginPageLoad();
        }

        /// <summary>渲染进程退出：与致命 fault 走同一条恢复路径。</summary>
        public void OnRenderProcessExited()
        {
            Log("渲染进程退出。");
            Recover();
        }

        /// <summary>处理页面发来的一条报文字符串。</summary>
        public void Receive(string? message)
        {
            if (disposed)
            {
                return;
            }

            switch (EditorWireCodec.Decode(message))
            {
                case WireEvent { Event: var editorEvent }:
                    OnEvent(editorEvent);
                    break;
                case WireSignal { Signal: LifecycleReady ready }:
                    OnReady(ready);
                    break;
                case WireSignal { Signal: LifecycleFault fault }:
                    OnFault(fault);
                    break;
                case WireSignal { Signal: DocChanged changed }:
                    OnDocChanged(changed);
                    break;
                case WireSignal { Signal: SelectionReport selection }:
                    lastSelection = selection.ToSelection();
                    Emit(selection.ToEvent());
                    break;
                case WireHostRequest request:
                    _ = AnswerAsync(request);
                    break;
                case EditorWireReply reply:
                    OnReply(reply);
                    break;
                case WireRejected rejected:
                    Log($"拒收页面报文（{rejected.Error}）：{rejected.Detail}");
                    if (rejected.Kind == EditorWireKind.Request && rejected.Id is { } id)
                    {
                        Send(EditorWireCodec.EncodeFailure(id, rejected.Error, rejected.Detail));
                    }

                    break;
            }
        }

        /// <summary>页面的 <c>lifecycle.fault</c>；派生类可以在这里上报，基类只记日志。</summary>
        protected virtual void OnPageFault(LifecycleFault fault)
        {
        }

        /// <summary>会话日志，默认写 <see cref="Trace"/>（OutputDebugString）。</summary>
        protected virtual void Log(string message) => Trace.WriteLine($"[editor] {message}");

        // ── 入站 ─────────────────────────────────────────────────────────

        private void OnEvent(EditorEvent editorEvent)
        {
            switch (editorEvent)
            {
                case DocumentLoaded loaded:
                    if (!mirror.IsCurrentLoad(loaded.Version))
                    {
                        Log($"丢弃过期的 doc.rendered（版本 {loaded.Version}，当前装载 {mirror.LoadedVersion}）。");
                        return;
                    }

                    Log($"doc.rendered 版本 {loaded.Version}");
                    break;
                case ViewportChanged viewport:
                    lastScrollTop = viewport.ScrollY;
                    break;
                case HistoryChanged history:
                    (lastCanUndo, lastCanRedo) = (history.CanUndo, history.CanRedo);
                    break;
                case MarksChanged marks:
                    lastHasMarks = marks.Marks.Count > 0;
                    break;
            }

            Emit(editorEvent);
        }

        private void OnReady(LifecycleReady ready)
        {
            Log($"lifecycle.ready 协议 {ready.Protocol}，引擎 {ready.Engine}");
            if (ready.Protocol != EditorWireTypes.ProtocolVersion)
            {
                Log($"页面协议版本 {ready.Protocol} 与宿主的 {EditorWireTypes.ProtocolVersion} 不一致，会话进入 Faulted，不再重试。");
                protocolMismatch = true;
                UpdateState();
                return;
            }

            if (gate.IsReady)
            {
                Log("重复的 lifecycle.ready，忽略。");
                return;
            }

            // 先发 doc.load 再开门：WebView2 的消息通道保序，排队命令一定在装载之后到达。
            var recovery = restore;
            restore = null;
            unsentLoad = null;
            SendLoad(mirror.Load(mirror.Document.Text, CurrentBasePath(), recovery?.Selection, recovery?.ScrollTop));
            SettleResync();
            faulted = false;
            Replay(gate.MarkReady());
            SendHeldCalls();
            UpdateState();
        }

        private void OnFault(LifecycleFault fault)
        {
            Log($"lifecycle.fault{(fault.Fatal ? "（致命）" : string.Empty)}：{fault.Message}{Environment.NewLine}{fault.Stack}");
            try
            {
                OnPageFault(fault);
            }
            catch (Exception ex)
            {
                Log($"上报页面错误失败：{ex.Message}");
            }

            if (fault.Fatal)
            {
                Recover();
            }
        }

        private void OnDocChanged(DocChanged changed)
        {
            switch (mirror.Apply(changed))
            {
                case DocumentMirrorOutcome.Applied:
                    Emit(new DocumentChanged(mirror.Document.Version));
                    break;
                case DocumentMirrorOutcome.ResyncRequired:
                    Log($"正文镜像失步（镜像 {mirror.Document.Version}，增量 {changed.BaseVersion}→{changed.Version}），整体重同步。");
                    StartResync();
                    break;
            }
        }

        private void OnReply(EditorWireReply reply)
        {
            if (!pending.Remove(reply.Id, out var call))
            {
                Log($"没有挂起的请求 {reply.Id}（已超时、作废或重复应答）。");
                return;
            }

            call.Release();
            call.Complete(reply);
        }

        private async Task AnswerAsync(WireHostRequest request)
        {
            var generation = pageGeneration;
            string reply;
            try
            {
                var host = callbacks();
                reply = request.Request switch
                {
                    TablePickSize => EditorWireCodec.EncodeTableSizeReply(request.Id, await host.PickTableSizeAsync(lifetime.Token)),
                    ClipboardWrite write => await WriteClipboardAsync(host, request.Id, write),
                    ImageResolve image => EditorWireCodec.EncodeImageResolveReply(request.Id, await host.ResolveImageAsync(image.Source, lifetime.Token)),
                    _ => EditorWireCodec.EncodeFailure(request.Id, EditorWireError.UnknownType, request.Request.GetType().Name),
                };
            }
            catch (OperationCanceledException ex)
            {
                reply = EditorWireCodec.EncodeFailure(request.Id, EditorWireError.Canceled, ex.Message);
            }
            catch (Exception ex)
            {
                reply = EditorWireCodec.EncodeFailure(request.Id, EditorWireError.Failed, ex.Message);
            }

            if (disposed || generation != pageGeneration)
            {
                // 页面已重载，这个 id 属于旧页面。
                return;
            }

            Send(reply);
        }

        private async Task<string> WriteClipboardAsync(IEditorHostCallbacks host, long id, ClipboardWrite write)
        {
            await host.WriteClipboardAsync(write.ToContent(), lifetime.Token);
            return EditorWireCodec.EncodeClipboardWriteReply(id);
        }

        // ── 正文 ─────────────────────────────────────────────────────────

        private void PostLoad(LoadDocument load)
        {
            basePath = load.BasePath;
            var docLoad = mirror.Load(load.Text, load.BasePath);
            SettleResync();
            if (gate.IsOpen)
            {
                unsentLoad = null;
                SendLoad(docLoad);
            }
            else if (gate.IsReady)
            {
                unsentLoad = docLoad;
            }

            // 页面还没就绪：ready 时按镜像发 doc.load，这里只更新镜像。
        }

        private void SendLoad(DocLoad load)
        {
            Log($"doc.load 版本 {load.Version}，{load.Text.Length} 个字符");
            Send(EditorWireCodec.EncodeDocLoad(load));
        }

        /// <summary>
        /// <c>doc.flush</c>：页面先发出挂起的增量再应答版本号，应答到达时镜像已经跟上；应答的版本比镜像新就整体重同步。
        /// 不在 Ready 时页面上没有未上报的改动，不进就绪门、直接应答镜像。两种情况下重同步进行中都等 getText 的应答落进镜像。
        /// 结果在处理应答的那次调用里就地给出，不经过 await 续体，镜像只在 UI 线程上读写。
        /// </summary>
        private Task<long> FlushAsync(CancellationToken cancellationToken)
        {
            var result = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!gate.IsOpen)
            {
                CompleteAfterResync(result);
                return result.Task;
            }

            var call = EditorWireCodec.EncodeRequest(NextRequestId(), new FlushDocument());
            Register(new PendingCall(
                call.Id,
                call.Message,
                reply =>
                {
                    try
                    {
                        Reconcile(call.ReadReply(reply));
                    }
                    catch (Exception ex)
                    {
                        result.TrySetException(ex);
                        return;
                    }

                    CompleteAfterResync(result);
                },
                error =>
                {
                    if (error is TimeoutException)
                    {
                        Log("doc.flush 超时，按当前镜像继续。");
                        CompleteAfterResync(result);
                        return;
                    }

                    result.TrySetException(error);
                }),
                FlushTimeout,
                cancellationToken);
            return result.Task;
        }

        private void CompleteAfterResync(TaskCompletionSource<long> result)
        {
            if (mirror.IsResyncing)
            {
                resyncWaiters.Add(result);
                return;
            }

            result.TrySetResult(mirror.Document.Version);
        }

        private async Task ReconcileAsync()
        {
            try
            {
                await FlushAsync(CancellationToken.None);
            }
            catch (Exception ex) when (ex is OperationCanceledException or NotSupportedException or EditorRequestFailedException or TimeoutException)
            {
                Log($"重新挂载后的对齐没有完成：{ex.Message}");
            }
        }

        private void Reconcile(long pageVersion)
        {
            if (mirror.Reconcile(pageVersion) == DocumentMirrorOutcome.ResyncRequired)
            {
                Log($"页面版本 {pageVersion} 比镜像 {mirror.Document.Version} 新，整体重同步。");
                StartResync();
            }
        }

        private void StartResync()
        {
            var id = NextRequestId();
            var call = EditorWireCodec.EncodeGetText(id);
            resyncCallId = id;
            Register(new PendingCall(
                id,
                call.Message,
                reply =>
                {
                    DocText text;
                    try
                    {
                        text = call.ReadReply(reply);
                    }
                    catch (Exception ex)
                    {
                        OnResyncFailed(id, ex);
                        return;
                    }

                    OnResyncReply(id, text);
                },
                error => OnResyncFailed(id, error)),
                RequestTimeout,
                CancellationToken.None);
        }

        private void OnResyncReply(long id, DocText text)
        {
            var before = mirror.Document.Version;
            var outcome = mirror.CompleteResync(text);
            if (mirror.Document.Version != before)
            {
                Emit(new DocumentChanged(mirror.Document.Version));
            }

            if (outcome == DocumentMirrorOutcome.ResyncRequired
                || (outcome == DocumentMirrorOutcome.Stale && mirror.IsResyncing && id == resyncCallId))
            {
                StartResync();
                return;
            }

            SettleResync();
        }

        private void OnResyncFailed(long id, Exception error)
        {
            Log($"doc.getText 失败：{error.Message}");
            if (id == resyncCallId)
            {
                mirror.AbandonResync();
            }

            SettleResync();
        }

        /// <summary>重同步结束（成功、放弃或被整篇装载取代）后，等它的 flush 按当时的镜像版本应答。</summary>
        private void SettleResync()
        {
            if (mirror.IsResyncing || resyncWaiters.Count == 0)
            {
                return;
            }

            var waiters = resyncWaiters.ToArray();
            resyncWaiters.Clear();
            foreach (var waiter in waiters)
            {
                waiter.TrySetResult(mirror.Document.Version);
            }
        }

        // ── 页面生命周期 ─────────────────────────────────────────────────

        /// <summary>旧页面作废：就绪门关上、排队命令与挂起请求作废、放弃进行中的重同步，页面报过的状态复位到初始值。</summary>
        private void BeginPageLoad()
        {
            pageGeneration++;
            unsentLoad = null;
            gate.MarkNotReady();
            AbortAll("编辑器页面重新加载，请求作废。");
            mirror.AbandonResync();
            SettleResync();
            if (lastCanUndo || lastCanRedo)
            {
                (lastCanUndo, lastCanRedo) = (false, false);
                Emit(new HistoryChanged(false, false));
            }

            if (lastHasMarks)
            {
                lastHasMarks = false;
                Emit(new MarksChanged([]));
            }

            UpdateState();
        }

        /// <summary>致命 fault 与渲染进程退出：交给 <see cref="EditorCrashRecovery"/> 决定是否重载、是否带回选区与滚动位置。</summary>
        private void Recover()
        {
            var decision = crashRecovery.OnCrash();
            faulted = true;
            restore = decision.RestoreCursor ? (lastSelection, lastScrollTop) : null;
            if (decision.Reload)
            {
                Log(decision.RestoreCursor ? "按正文镜像重载页面，恢复选区与滚动位置。" : "按正文镜像重载页面。");
                _ = LoadPageAsync();
                return;
            }

            Log("一分钟内崩溃过多，不再自动重载页面。");
            BeginPageLoad();
        }

        // ── 请求 ─────────────────────────────────────────────────────────

        private long NextRequestId() => ++nextRequestId;

        private Task<TResult> Call<TResult>(EditorWireCall<TResult> call, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            Register(new PendingCall(
                call.Id,
                call.Message,
                reply =>
                {
                    TResult result;
                    try
                    {
                        result = call.ReadReply(reply);
                    }
                    catch (Exception ex)
                    {
                        if (ex is NotSupportedException)
                        {
                            Log(ex.Message);
                        }

                        completion.TrySetException(ex);
                        return;
                    }

                    completion.TrySetResult(result);
                },
                error => completion.TrySetException(error)),
                timeout,
                cancellationToken);
            return completion.Task;
        }

        /// <summary>登记挂起请求并起算超时；页面就绪且宿主挂载时立即发出，否则等门开时发。</summary>
        private void Register(PendingCall call, TimeSpan timeout, CancellationToken cancellationToken)
        {
            pending[call.Id] = call;
            call.Timer = timeProvider.CreateTimer(
                _ => Dispatch(() => Abort(call.Id, new TimeoutException($"编辑器页面在 {timeout.TotalSeconds:0.#} 秒内没有应答请求 {call.Id}。"))),
                null,
                timeout,
                Timeout.InfiniteTimeSpan);
            if (cancellationToken.CanBeCanceled)
            {
                call.Cancellation = cancellationToken.Register(() => Dispatch(() => Abort(call.Id, new OperationCanceledException(cancellationToken))));
            }

            if (gate.IsOpen)
            {
                call.Sent = true;
                Send(call.Message);
            }
        }

        private void SendHeldCalls()
        {
            if (!gate.IsOpen)
            {
                return;
            }

            foreach (var call in pending.Values.Where(x => !x.Sent).OrderBy(x => x.Id).ToList())
            {
                call.Sent = true;
                Send(call.Message);
            }
        }

        private void Abort(long id, Exception error)
        {
            if (pending.Remove(id, out var call))
            {
                call.Release();
                call.Fail(error);
            }
        }

        private void AbortAll(string reason)
        {
            foreach (var id in pending.Keys.ToList())
            {
                Abort(id, new OperationCanceledException(reason));
            }
        }

        // ── 发送 ─────────────────────────────────────────────────────────

        private void Replay(IReadOnlyList<EditorCommandGateItem<string>> items)
        {
            foreach (var item in items)
            {
                if (item.Command is { } command)
                {
                    SendRetained(command);
                }
                else if (item.Message is { } message)
                {
                    Send(message);
                }
            }
        }

        /// <summary>发一条状态类命令；页面手里已经是同一份主题或快捷键表时跳过。</summary>
        private void SendRetained(EditorCommand command)
        {
            switch (command)
            {
                case ApplyTheme theme when theme.Theme == pageTheme:
                    return;
                case ApplyTheme theme:
                    pageTheme = theme.Theme;
                    break;
                case SetKeymap keymap when pageKeymap is not null && keymap.Chords.SequenceEqual(pageKeymap):
                    return;
                case SetKeymap keymap:
                    pageKeymap = keymap.Chords.ToArray();
                    break;
            }

            Send(EditorWireCodec.EncodeCommand(command));
        }

        private void Send(string message)
        {
            if (channel?.TryPost(message) != true)
            {
                Log("编辑器页面不可用，报文未送达。");
            }
        }

        // ── 状态与事件 ───────────────────────────────────────────────────

        private void UpdateState()
        {
            var next = gate.IsOpen && !protocolMismatch
                ? EditorSessionState.Ready
                : faulted || protocolMismatch ? EditorSessionState.Faulted
                : gate.IsAttached ? EditorSessionState.Loading
                : EditorSessionState.Detached;
            if (state.Value != next)
            {
                state.OnNext(next);
            }
        }

        private void Emit(EditorEvent editorEvent)
        {
            if (!disposed)
            {
                events.OnNext(editorEvent);
            }
        }

        private string CurrentBasePath()
        {
            try
            {
                return callbacks().BasePath ?? basePath;
            }
            catch (Exception ex)
            {
                Log($"取基准目录失败：{ex.Message}");
                return basePath;
            }
        }

        private void Dispatch(Action action)
        {
            try
            {
                _ = dispatcher.RunAsync(() =>
                {
                    if (!disposed)
                    {
                        action();
                    }
                });
            }
            catch (UiDispatcherUnavailableException ex)
            {
                Log(ex.Message);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            AbortAll("编辑会话已释放。");
            disposed = true;
            lifetime.Cancel();
            lifetime.Dispose();
            mirror.AbandonResync();
            SettleResync();
            events.OnCompleted();
            state.OnCompleted();
            channel = null;
            GC.SuppressFinalize(this);
        }

        /// <summary>一个宿主→页面的挂起请求。</summary>
        private sealed class PendingCall(long id, string message, Action<EditorWireReply> complete, Action<Exception> fail)
        {
            public long Id { get; } = id;

            public string Message { get; } = message;

            public bool Sent { get; set; }

            public ITimer? Timer { get; set; }

            public CancellationTokenRegistration Cancellation { get; set; }

            public void Complete(EditorWireReply reply) => complete(reply);

            public void Fail(Exception error) => fail(error);

            public void Release()
            {
                Timer?.Dispose();
                Cancellation.Dispose();
            }
        }
    }
}
