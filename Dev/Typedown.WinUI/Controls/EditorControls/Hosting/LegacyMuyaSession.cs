using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Typedown.Core.Editor;
using Typedown.Core.Editor.Legacy;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Utilities;

namespace Typedown.WinUI.Controls
{
    /// <summary>编辑会话与承载页面的 WebView2 宿主之间的最小通道。</summary>
    internal interface ILegacyMuyaChannel
    {
        /// <summary>向页面投递一条报文；CoreWebView2 不可用时返回 <c>false</c>。</summary>
        bool TryPost(string message);

        /// <summary>重新加载页面（页面自报未处理异常时自愈）。</summary>
        void Reload();

        /// <summary>页面启动时索取的当前主题。</summary>
        EditorTheme GetCurrentTheme();
    }

    /// <summary>
    /// 旧 Muya 页面协议的 <see cref="IEditorSession"/> 适配器。旧协议的一切都收在这里：
    /// 字符串消息名、diffmsg 重组、invoke 应答、就绪门、页面消息与类型化事件/命令的互译，
    /// 以及整篇快照式撤销历史（<see cref="ContentHistory"/> + SetMarkdown 回灌）。
    /// </summary>
    public sealed class LegacyMuyaSession : IEditorSession, IDisposable
    {
        private static readonly HashSet<ulong> reportedEditorExceptions = new();

        private readonly IServiceProvider services;
        private readonly IUiDispatcher uiDispatcher;
        private readonly int uiThreadId;
        private readonly BehaviorSubject<EditorSessionState> state = new(EditorSessionState.Detached);
        private readonly Subject<EditorEvent> events = new();
        private readonly ContentHistory history = new();
        private readonly LegacyDiffChannel diffChannel = new();
        private readonly LegacyClipboardCoalescer clipboard = new();
        private readonly EditorCrashRecovery crashRecovery = new();
        private CursorState? lastCursor;
        private CursorState? restoreCursor;
        private bool faulted;
        /// <summary>
        /// 宿主卸载（例如打开设置页，MainPage 整体卸载）期间命令照样排队、设置照样合并，重新挂载时整体重放，
        /// 否则在设置页里改的编辑器设置永远到不了页面。
        /// </summary>
        private readonly EditorCommandGate<string> gate = new(retainWhileDetached: true);
        private readonly Dictionary<long, TaskCompletionSource<string>> exportRequests = new();
        private readonly CancellationTokenSource lifetime = new();

        private ILegacyMuyaChannel? channel;
        private EditorDocument document = EditorDocument.Empty;
        private string basePath = string.Empty;
        private int startupRequests;
        private long nextRequestId;
        private bool disposed;

        /// <summary>正文正由宿主整篇回灌（撤销/重做），此间页面回声不压入历史栈；StateChange 或源码模式选区变化时解除。</summary>
        private bool contentUpdating;

        /// <summary>当前是否源码模式，决定查找时带回哪一份页面选区。</summary>
        private bool sourceMode;

        private JsonElement muyaSelection;
        private JsonElement codeMirrorSelection;
        private string? imageStyle;
        private RichSelection? lastRichSelection;

        public LegacyMuyaSession(IServiceProvider services, IUiDispatcher uiDispatcher)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            this.uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
            uiThreadId = Environment.CurrentManagedThreadId;
            history.PropertyChanged += OnHistoryPropertyChanged;
        }

        // ── IEditorSession ────────────────────────────────────────────────

        public EditorSessionState State => state.Value;

        public IObservable<EditorSessionState> StateChanged => state.DistinctUntilChanged().Skip(1);

        public EditorDocument Document => document;

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
                case Undo:
                    ApplyHistoryStep(history.Undo());
                    return;
                case Redo:
                    ApplyHistoryStep(history.Redo());
                    return;
                case ClearUndoHistory:
                    history.InitHistory(document.Text);
                    return;
                case LoadDocument load:
                    UpdateDocument(load.Text);
                    basePath = load.BasePath;
                    history.InitHistory(load.Text);
                    if (startupRequests > 0)
                    {
                        // 页面正在启动握手里等正文，应答会带上刚装载的这一篇，不再另发 LoadFile。
                        return;
                    }

                    break;
                case ApplySettings settings when settings.Changes.SourceCode is bool isSourceCode:
                    sourceMode = isSourceCode;
                    break;
            }

            if (command is ApplyTheme or SetKeymap or ApplySettings or RefreshViewport)
            {
                if (gate.Submit(command))
                {
                    Send(LegacyMuyaProtocol.EncodeCommand(command));
                }

                return;
            }

            SubmitMessage(LegacyMuyaProtocol.EncodeCommand(command, sourceMode ? codeMirrorSelection : muyaSelection, imageStyle));
        }

        public async Task<TResult> RequestAsync<TResult>(EditorRequest<TResult> request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            switch (request)
            {
                case RenderExportHtml export:
                    var html = await RenderExportHtmlAsync(export, cancellationToken);
                    return (TResult)(object)html;
                case FlushDocument:
                    // 旧页面每次变化都立即上报 MarkdownChange，镜像本来就是最新的。
                    return (TResult)(object)document.Version;
                case ContextAt:
                    // 旧页面不能按坐标取上下文；右键时光标已落到点击处，最后一次选区的上下文就是它。
                    return (TResult)(object?)lastRichSelection!;
                default:
                    throw new NotSupportedException($"Editor request {request.GetType().Name} is not supported by the legacy Muya session.");
            }
        }

        // ── 宿主侧接口 ───────────────────────────────────────────────────

        internal void Attach(ILegacyMuyaChannel host)
        {
            channel = host;
            Replay(gate.Attach());
            UpdateState();
        }

        internal void Detach(ILegacyMuyaChannel host)
        {
            if (!ReferenceEquals(channel, host))
            {
                return;
            }

            // 通道本身保留：宿主卸载期间页面仍可能 invoke，应答必须照常送达。
            gate.Detach();
            UpdateState();
        }

        /// <summary>页面开始（重新）导航：旧页面的排队消息与挂起请求一并作废。</summary>
        internal void OnNavigationStarting()
        {
            gate.MarkNotReady();
            diffChannel.Reset();
            CancelExportRequests();
            UpdateState();
        }

        /// <summary>
        /// 页面渲染进程崩溃。返回 <c>true</c> 表示宿主应当重载页面；重载后页面在启动握手里拿回正文镜像，
        /// 会话在应答之前先下发一次 SetMarkdown 把光标放回崩溃前的位置。
        /// </summary>
        internal bool OnRenderProcessFailed()
        {
            var decision = crashRecovery.OnCrash();
            faulted = true;
            restoreCursor = decision.RestoreCursor ? lastCursor : null;
            gate.MarkNotReady();
            diffChannel.Reset();
            CancelExportRequests();
            UpdateState();
            return decision.Reload;
        }

        internal async Task ReceiveAsync(string? message)
        {
            if (disposed || !LegacyMuyaProtocol.TryParseEnvelope(message, out var envelope))
            {
                return;
            }

            try
            {
                if (envelope.Name != "SetClipboard")
                {
                    await FlushClipboardAsync();
                }

                switch (envelope.Type)
                {
                    case "invoke":
                        await HandleInvokeAsync(envelope);
                        break;
                    case "message":
                        Dispatch(envelope.Name, envelope.Args);
                        break;
                    case "diffmsg":
                        if (diffChannel.TryApply(envelope.Name, envelope.Args, envelope.Diff, envelope.Start, envelope.End, out var text)
                            && LegacyMuyaProtocol.TryParseArgs(text, out var args))
                        {
                            Dispatch(envelope.Name, args);
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                // 残缺的页面报文不能冒泡到 WebView2 的事件处理器。
                Trace.WriteLine(ex);
            }
        }

        // ── 页面消息 ─────────────────────────────────────────────────────

        private void Dispatch(string name, JsonElement args)
        {
            switch (LegacyMuyaProtocol.DecodeMessage(name, args))
            {
                case LegacyTextChanged changed:
                    UpdateDocument(changed.Text);
                    if (!contentUpdating)
                    {
                        history.ContentChange(changed.Text);
                    }

                    Emit(new DocumentChanged(document.Version));
                    break;
                case LegacyFileLoaded loaded:
                    UpdateDocument(loaded.Text);
                    Emit(new DocumentLoaded(document.Version));
                    break;
                case LegacyCursorChanged cursor:
                    lastCursor = cursor.Cursor;
                    history.CursorChange(cursor.Cursor);
                    break;
                case LegacyStateChanged stateChanged:
                    contentUpdating = false;
                    foreach (var editorEvent in stateChanged.Events)
                    {
                        Emit(editorEvent);
                    }

                    break;
                case LegacySelectionChanged selection:
                    if (selection.SourceMode)
                    {
                        contentUpdating = false;
                        codeMirrorSelection = selection.PageSelection;
                    }
                    else
                    {
                        muyaSelection = selection.PageSelection;
                    }

                    lastRichSelection = selection.Event.Rich;
                    Emit(selection.Event);
                    break;
                case LegacyImageToolbarOpened imageToolbar:
                    imageStyle = imageToolbar.Style;
                    Emit(imageToolbar.Event);
                    break;
                case LegacyTypedEvent typed:
                    Emit(typed.Event);
                    break;
            }
        }

        /// <summary>换一份正文镜像：旧页面每次都报全文，版本号逐次加一即可，不需要算差异。</summary>
        private void UpdateDocument(string text) => document = new EditorDocument(text, document.Version + 1);

        // ── 页面 invoke ─────────────────────────────────────────────────

        private async Task HandleInvokeAsync(LegacyEnvelope envelope)
        {
            string reply;
            try
            {
                reply = await InvokeAsync(envelope);
            }
            catch (Exception ex)
            {
                reply = LegacyMuyaProtocol.EncodeErrorReply(envelope.Id, ex.Message);
            }

            SendReply(reply);
        }

        private async Task<string> InvokeAsync(LegacyEnvelope envelope)
        {
            var id = envelope.Id;
            var args = envelope.Args;
            switch (envelope.Name)
            {
                case "ContentLoaded":
                    OnContentLoaded();
                    return LegacyMuyaProtocol.EncodeNullReply(id);
                case "GetCurrentTheme":
                    return LegacyMuyaProtocol.EncodeThemeReply(id, RequireChannel().GetCurrentTheme());
                case "GetSettings":
                    return await PrepareStartupAsync(id);
                case "GetStringResources":
                    return LegacyMuyaProtocol.EncodeStringResourcesReply(id, GetStringResources(LegacyMuyaProtocol.ReadStringResourceNames(args)));
                case "ResizeTable":
                    return LegacyMuyaProtocol.EncodeTableSizeReply(id, await Callbacks.PickTableSizeAsync(lifetime.Token));
                case "SetClipboard":
                    await WriteClipboardAsync(args);
                    return LegacyMuyaProtocol.EncodeNullReply(id);
                case "ExportCallback":
                case "PrintHTML":
                    return LegacyMuyaProtocol.EncodeBoolReply(id, CompleteExportRequest(args));
                case "OpenNewWindow":
                    var uri = LegacyMuyaProtocol.ReadStringArgument(args)
                        ?? throw new InvalidOperationException("function [OpenNewWindow] requires a valid argument payload");
                    if (!string.IsNullOrWhiteSpace(uri))
                    {
                        Emit(new LinkOpenRequested(uri));
                    }

                    return LegacyMuyaProtocol.EncodeNullReply(id);
                case "UnhandledException":
                    var error = LegacyMuyaProtocol.ReadStringArgument(args)
                        ?? throw new InvalidOperationException("function [UnhandledException] requires a valid argument payload");
                    OnPageUnhandledException(error);
                    return LegacyMuyaProtocol.EncodeNullReply(id);
                default:
                    throw new InvalidOperationException($"function [{envelope.Name}] does not exist");
            }
        }

        private void OnContentLoaded()
        {
            faulted = false;
            Replay(gate.MarkReady());
            UpdateState();
        }

        private async Task<string> PrepareStartupAsync(string id)
        {
            startupRequests++;
            try
            {
                var callbacks = Callbacks;
                var settings = await callbacks.PrepareStartupAsync(lifetime.Token);
                basePath = callbacks.BasePath;
                sourceMode = settings.SourceCode ?? false;

                // 应答里是全量设置，门里积压的设置增量作废。
                gate.ClearPendingSettings();

                if (restoreCursor is { } cursor)
                {
                    // 崩溃恢复：页面在收到应答之前先收到这条 SetMarkdown，编辑器挂载时就带上了原来的光标。
                    // 它与应答一样不经过就绪门；回声不入撤销栈。
                    restoreCursor = null;
                    contentUpdating = true;
                    SendReply(LegacyMuyaProtocol.EncodeSetMarkdown(document.Text, cursor, basePath));
                }
                return LegacyMuyaProtocol.EncodeStartupReply(id, settings, document.Text, basePath);
            }
            finally
            {
                startupRequests--;
            }
        }

        private static IReadOnlyDictionary<string, string> GetStringResources(IReadOnlyList<string> names)
        {
            try
            {
                return names.ToDictionary(x => x, x => Locale.GetString(x));
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        private Task WriteClipboardAsync(JsonElement args)
        {
            var (type, data) = LegacyMuyaProtocol.ReadClipboardWrite(args);
            return WriteClipboardAsync(clipboard.Accept(type, data));
        }

        /// <summary>页面发来 SetClipboard 以外的任何报文，都说明积压的 HTML 等不到配对的纯文本了。</summary>
        private Task FlushClipboardAsync() =>
            clipboard.HasPending ? WriteClipboardAsync(clipboard.Flush()) : Task.CompletedTask;

        private async Task WriteClipboardAsync(IReadOnlyList<ClipboardContent> contents)
        {
            foreach (var content in contents)
            {
                await Callbacks.WriteClipboardAsync(content, lifetime.Token);
            }
        }

        private void OnPageUnhandledException(string error)
        {
            channel?.Reload();
            try
            {
                var hash = Common.SimpleHash(error);
                if (reportedEditorExceptions.Add(hash))
                {
                    _ = Log.Report("WebViewUnhandledException", error);
                }
            }
            catch
            {
                // 上报失败不应影响自愈重载。
            }
        }

        // ── 撤销 / 重做 ──────────────────────────────────────────────────

        private void ApplyHistoryStep(HistoryModel? step)
        {
            if (step is null)
            {
                return;
            }

            UpdateDocument(step.Text ?? string.Empty);
            Emit(new DocumentChanged(document.Version));
            contentUpdating = true;
            basePath = TryGetCallbacks()?.BasePath ?? basePath;
            SubmitMessage(LegacyMuyaProtocol.EncodeSetMarkdown(step.Text, step.Cursor, basePath));
        }

        private void OnHistoryPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not (nameof(ContentHistory.Undoable) or nameof(ContentHistory.Redoable)))
            {
                return;
            }

            RunOnUiThread(() => Emit(new HistoryChanged(history.Undoable, history.Redoable)));
        }

        // ── 导出 ─────────────────────────────────────────────────────────

        private async Task<string> RenderExportHtmlAsync(RenderExportHtml request, CancellationToken cancellationToken)
        {
            var requestId = ++nextRequestId;
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (exportRequests)
            {
                exportRequests[requestId] = completion;
            }

            using var registration = cancellationToken.Register(() =>
            {
                RemoveExportRequest(requestId);
                completion.TrySetCanceled(cancellationToken);
            });
            SubmitMessage(LegacyMuyaProtocol.EncodeExportRequest(request, requestId));
            return await completion.Task;
        }

        private bool CompleteExportRequest(JsonElement args)
        {
            var (html, requestId) = LegacyMuyaProtocol.ReadExportCallback(args);
            if (requestId is not long id || RemoveExportRequest(id) is not { } completion)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(html))
            {
                completion.TrySetException(new InvalidOperationException("Editor callback payload is missing valid string 'html'."));
                return false;
            }

            completion.TrySetResult(html);
            return true;
        }

        private TaskCompletionSource<string>? RemoveExportRequest(long requestId)
        {
            lock (exportRequests)
            {
                return exportRequests.Remove(requestId, out var completion) ? completion : null;
            }
        }

        private void CancelExportRequests()
        {
            TaskCompletionSource<string>[] pending;
            lock (exportRequests)
            {
                pending = exportRequests.Values.ToArray();
                exportRequests.Clear();
            }

            foreach (var completion in pending)
            {
                completion.TrySetCanceled();
            }
        }

        // ── 发送 ─────────────────────────────────────────────────────────

        private void SubmitMessage(string? message)
        {
            if (message is not null && gate.Submit(message))
            {
                Send(message);
            }
        }

        private void Replay(IReadOnlyList<EditorCommandGateItem<string>> items)
        {
            foreach (var item in items)
            {
                Send(item.Command is { } command ? LegacyMuyaProtocol.EncodeCommand(command) : item.Message);
            }
        }

        private void Send(string? message)
        {
            if (message is not null)
            {
                channel?.TryPost(message);
            }
        }

        /// <summary>invoke 应答不经过就绪门：页面正等着它，门的开关与此无关。</summary>
        private void SendReply(string reply) => channel?.TryPost(reply);

        // ── 状态与事件 ───────────────────────────────────────────────────

        private void UpdateState()
        {
            var next = gate.IsOpen
                ? EditorSessionState.Ready
                : faulted ? EditorSessionState.Faulted
                : gate.IsAttached ? EditorSessionState.Loading : EditorSessionState.Detached;
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

        private void RunOnUiThread(Action action)
        {
            if (Environment.CurrentManagedThreadId == uiThreadId)
            {
                action();
                return;
            }

            try
            {
                _ = uiDispatcher.RunAsync(action);
            }
            catch (UiDispatcherUnavailableException ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private IEditorHostCallbacks Callbacks => services.GetRequiredService<IEditorHostCallbacks>();

        private IEditorHostCallbacks? TryGetCallbacks() => services.GetService<IEditorHostCallbacks>();

        private ILegacyMuyaChannel RequireChannel() =>
            channel ?? throw new InvalidOperationException("The editor host is not attached.");

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            history.PropertyChanged -= OnHistoryPropertyChanged;
            history.Dispose();
            lifetime.Cancel();
            lifetime.Dispose();
            CancelExportRequests();
            events.OnCompleted();
            state.OnCompleted();
            channel = null;
        }
    }
}
