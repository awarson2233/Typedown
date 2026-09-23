using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core.Editor;
using Typedown.Core.Editor.Wire;
using Typedown.Core.Interfaces;

namespace Typedown.CoreTests.Editor;

/// <summary>
/// 新引擎会话的协议逻辑（docs/editor-protocol.md 第 3、4、9 节），用假通道代替 WebView2：
/// 启动握手与就绪门、flush 与重同步、请求超时、重载时作废挂起请求、致命错误与渲染进程退出后的重载恢复。
/// </summary>
[TestClass]
public sealed class EditorWireSessionTests
{
    private static readonly EditorTheme Light = new(false, new EditorColor(1, 2, 3, 1), new EditorColor(250, 250, 250, 1));
    private static readonly EditorTheme Dark = new(true, new EditorColor(4, 5, 6, 1), new EditorColor(40, 40, 40, 1));

    private ManualTimeProvider clock = null!;
    private FakeChannel channel = null!;
    private FakeCallbacks callbacks = null!;
    private EditorWireSession session = null!;
    private List<EditorEvent> events = null!;

    [TestInitialize]
    public void Setup()
    {
        clock = new ManualTimeProvider();
        channel = new FakeChannel();
        callbacks = new FakeCallbacks();
        session = new EditorWireSession(() => callbacks, new InlineDispatcher(), clock);
        events = new List<EditorEvent>();
        session.Events.Subscribe(events.Add);
    }

    [TestCleanup]
    public void Cleanup() => session.Dispose();

    // ── 启动握手 ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task Handshake_InjectsInitStateLoadsDocumentThenOpensTheGate()
    {
        session.Post(new SetKeymap([new KeyChord(Typedown.Core.Models.KeyboardKey.Z, Typedown.Core.Models.KeyboardModifiers.Control)]));
        session.Attach(channel);
        await session.LoadPageAsync();

        Assert.AreEqual(EditorSessionState.Loading, session.State);
        Assert.AreEqual(1, callbacks.StartupCalls);
        var init = channel.LastInit();
        Assert.AreEqual(EditorWireTypes.ProtocolVersion, init.GetProperty("protocol").GetInt32());
        Assert.AreEqual(Light.IsDark, init.GetProperty("theme").GetProperty("isDark").GetBoolean());
        Assert.AreEqual(1, init.GetProperty("keymap").GetArrayLength());
        Assert.AreEqual(16d, init.GetProperty("settings").GetProperty("fontSize").GetDouble());

        // ready 之前：装载只更新镜像，命令排队，什么都不发。
        session.Post(new LoadDocument("# hi", "C:/docs"));
        session.Post(new SelectAll());
        Assert.AreEqual("# hi", session.Document.Text);
        Assert.AreEqual(0, channel.Sent.Count);

        session.Receive(Ready());

        Assert.AreEqual(EditorSessionState.Ready, session.State);
        CollectionAssert.AreEqual(new[] { "doc.load", "selection.selectAll" }, channel.Sent.Select(x => x.T).ToArray(), "doc.load first, then the queue; the keymap already went in the init state");
        var load = channel.Sent[0].P;
        Assert.AreEqual("# hi", load.GetProperty("text").GetString());
        Assert.AreEqual(callbacks.BasePath, load.GetProperty("basePath").GetString());
        Assert.AreEqual(session.Document.Version, load.GetProperty("version").GetInt64());

        session.Receive(Evt("doc.rendered", $$"""{"version":{{session.Document.Version - 1}}}"""));
        Assert.AreEqual(0, events.OfType<DocumentLoaded>().Count(), "stale echo");
        session.Receive(Evt("doc.rendered", $$"""{"version":{{session.Document.Version}}}"""));
        Assert.AreEqual(session.Document.Version, events.OfType<DocumentLoaded>().Single().Version);
    }

    [TestMethod]
    public async Task Handshake_ProtocolMismatchFaultsWithoutLoading()
    {
        session.Attach(channel);
        await session.LoadPageAsync();
        session.Receive(Ready(protocol: 99));

        Assert.AreEqual(EditorSessionState.Faulted, session.State);
        Assert.AreEqual(0, channel.Sent.Count);
    }

    [TestMethod]
    public async Task Gate_ReplaysOnlyStateChangedSinceInjection()
    {
        session.Post(new ApplyTheme(Light));
        session.Attach(channel);
        await session.LoadPageAsync();
        session.Post(new ApplyTheme(Light));
        session.Post(new ApplySettings(new EditorSettings { FontSize = 20 }));
        session.Receive(Ready());
        CollectionAssert.AreEqual(new[] { "doc.load", "view.settings" }, channel.Sent.Select(x => x.T).ToArray(), "the page already has this theme");

        session.Post(new ApplyTheme(Dark));
        Assert.AreEqual("view.theme", channel.Sent.Last().T);
    }

    [TestMethod]
    public async Task Detach_CancelsRequestsAndReattachSendsTheDocumentLoadedMeanwhile()
    {
        await StartReady("a");
        var context = session.RequestAsync(new ContextAt(1, 2));

        session.Detach(channel);
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => context);
        Assert.AreEqual(EditorSessionState.Detached, session.State);

        session.Post(new LoadDocument("b", "C:/b"));
        session.Post(new Undo());
        channel.Sent.Clear();
        session.Attach(channel);

        CollectionAssert.AreEqual(new[] { "doc.load", "history.undo", "doc.flush" }, channel.Sent.Select(x => x.T).ToArray(), "then a flush to catch increments lost while detached");
        Assert.AreEqual("b", channel.Sent[0].P.GetProperty("text").GetString());
        Assert.AreEqual(EditorSessionState.Ready, session.State);
    }

    [TestMethod]
    public async Task Requests_AreHeldUntilReadyThenSentAfterTheLoad()
    {
        session.Attach(channel);
        await session.LoadPageAsync();
        var context = session.RequestAsync(new ContextAt(3, 4));
        Assert.AreEqual(0, channel.Sent.Count);

        session.Receive(Ready());
        CollectionAssert.AreEqual(new[] { "doc.load", "selection.contextAt" }, channel.Sent.Select(x => x.T).ToArray());

        session.Receive(Res(channel.Sent[1].Id!.Value, "null"));
        Assert.IsNull(await context);
    }

    // ── flush 与重同步 ───────────────────────────────────────────────

    [TestMethod]
    public async Task Flush_AppliesPendingIncrementsBeforeAnswering()
    {
        var version = await StartReady("abc");
        var flush = session.RequestAsync(new FlushDocument());
        var request = channel.Sent.Last();
        Assert.AreEqual("doc.flush", request.T);

        session.Receive(Changed(version, version + 1, 3, 3, "d"));
        session.Receive(Res(request.Id!.Value, $$"""{"version":{{version + 1}}}"""));

        Assert.AreEqual(version + 1, await flush);
        Assert.AreEqual(new EditorDocument("abcd", version + 1), session.Document);
        Assert.AreEqual(version + 1, events.OfType<DocumentChanged>().Single().Version);
    }

    [TestMethod]
    public async Task Flush_TimesOutAfterOneSecondAndUsesTheMirror()
    {
        var version = await StartReady("abc");
        var flush = session.RequestAsync(new FlushDocument());
        clock.Advance(TimeSpan.FromMilliseconds(999));
        Assert.IsFalse(flush.IsCompleted);

        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.AreEqual(version, await flush);
    }

    [TestMethod]
    public async Task Flush_WhenNotReadyAnswersTheMirrorWithoutAsking()
    {
        session.Attach(channel);
        await session.LoadPageAsync();
        session.Post(new LoadDocument("x", ""));

        Assert.AreEqual(session.Document.Version, await session.RequestAsync(new FlushDocument()));
        Assert.AreEqual(0, channel.Sent.Count);
    }

    [TestMethod]
    public async Task Resync_OnGapFetchesTheWholeTextAndBuffersLaterIncrements()
    {
        var version = await StartReady("abc");
        session.Receive(Changed(version + 1, version + 2, 0, 0, "?"));
        var getText = channel.Sent.Last();
        Assert.AreEqual("doc.getText", getText.T);

        session.Receive(Changed(version + 3, version + 4, 0, 0, "Z"));
        Assert.AreEqual("abc", session.Document.Text, "buffered while resyncing");

        session.Receive(Res(getText.Id!.Value, $$"""{"version":{{version + 3}},"text":"xyabc"}"""));
        Assert.AreEqual(new EditorDocument("Zxyabc", version + 4), session.Document);
        Assert.AreEqual(version + 4, events.OfType<DocumentChanged>().Last().Version);
    }

    [TestMethod]
    public async Task Flush_ReportingANewerVersionTriggersResyncAndWaitsForIt()
    {
        var version = await StartReady("abc");
        var flush = session.RequestAsync(new FlushDocument());
        session.Receive(Res(channel.Sent.Last().Id!.Value, $$"""{"version":{{version + 2}}}"""));

        var getText = channel.Sent.Last();
        Assert.AreEqual("doc.getText", getText.T);
        Assert.IsFalse(flush.IsCompleted, "flush waits for the resync");

        session.Receive(Res(getText.Id!.Value, $$"""{"version":{{version + 2}},"text":"abcde"}"""));
        Assert.AreEqual(version + 2, await flush);
        Assert.AreEqual("abcde", session.Document.Text);
    }

    [TestMethod]
    public async Task Resync_TimeoutAbandonsItSoTheNextGapRetries()
    {
        var version = await StartReady("abc");
        session.Receive(Changed(version + 1, version + 2, 0, 0, "?"));
        clock.Advance(EditorWireSession.RequestTimeout);

        session.Receive(Changed(version + 5, version + 6, 0, 0, "?"));
        Assert.AreEqual(2, channel.Sent.Count(x => x.T == "doc.getText"));
    }

    // ── 请求 ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Request_TimesOutAfterFiveSeconds()
    {
        await StartReady("abc");
        var context = session.RequestAsync(new ContextAt(1, 1));
        clock.Advance(TimeSpan.FromSeconds(4.9));
        Assert.IsFalse(context.IsCompleted);

        clock.Advance(TimeSpan.FromSeconds(0.1));
        await Assert.ThrowsExceptionAsync<TimeoutException>(() => context);
    }

    [TestMethod]
    public async Task Request_ExportHasASixtySecondTimeoutAndUnknownTypeIsNotSupported()
    {
        await StartReady("abc");
        var export = session.RequestAsync(new RenderExportHtml(ExportPurpose.Export, "t", null, null));
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.IsFalse(export.IsCompleted);

        session.Receive(Fail(channel.Sent.Last().Id!.Value, "unknownType"));
        await Assert.ThrowsExceptionAsync<NotSupportedException>(() => export);
    }

    [TestMethod]
    public async Task Request_CallerCancellationEndsIt()
    {
        await StartReady("abc");
        using var cancellation = new CancellationTokenSource();
        var context = session.RequestAsync(new ContextAt(1, 1), cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => context);
    }

    [TestMethod]
    public async Task HostRequests_AreAnsweredThroughTheCallbacks()
    {
        await StartReady("abc");
        callbacks.TableSize = new TableSize(3, 4);
        session.Receive("""{"k":"req","id":7,"t":"table.pickSize","p":{}}""");
        session.Receive("""{"k":"req","id":8,"t":"clipboard.write","p":{"plainText":"p","html":"<b>h</b>"}}""");
        session.Receive("""{"k":"req","id":9,"t":"no.such","p":{}}""");
        await Task.Yield();

        var replies = channel.Sent.Where(x => x.K == "res").ToDictionary(x => x.Id!.Value);
        Assert.AreEqual(3, replies[7].P.GetProperty("rows").GetInt32());
        Assert.AreEqual(4, replies[7].P.GetProperty("columns").GetInt32());
        Assert.AreEqual(new ClipboardContent("p", "<b>h</b>"), callbacks.Clipboard.Single());
        Assert.IsTrue(replies[8].Ok);
        Assert.AreEqual("unknownType", replies[9].Error);
    }

    // ── 重载恢复 ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task FatalFault_CancelsPendingRequestsAndReloadsFromTheMirror()
    {
        var version = await StartReady("abc");
        session.Receive(Changed(version, version + 1, 3, 3, "d"));
        session.Receive(Evt("selection.changed", """{"hasText":false,"text":"","rich":null,"anchor":2,"head":4}"""));
        session.Receive(Evt("view.viewport", """{"viewportWidth":800,"viewportHeight":600,"maximumX":0,"maximumY":900,"scrollX":0,"scrollY":120}"""));
        session.Receive(Evt("history.changed", """{"canUndo":true,"canRedo":false}"""));
        var context = session.RequestAsync(new ContextAt(1, 1));

        session.Receive(Evt("lifecycle.fault", """{"message":"boom","stack":"at x","fatal":true}"""));
        await Task.Yield();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => context);
        Assert.AreEqual(EditorSessionState.Faulted, session.State);
        Assert.AreEqual(2, channel.Inits.Count, "init state injected again before the reload");
        Assert.AreEqual(new HistoryChanged(false, false), events.OfType<HistoryChanged>().Last(), "the reloaded page starts without history");

        channel.Sent.Clear();
        session.Receive(Ready());
        var load = channel.Sent.Single(x => x.T == "doc.load").P;
        Assert.AreEqual("abcd", load.GetProperty("text").GetString());
        Assert.AreEqual(2, load.GetProperty("selection").GetProperty("anchor").GetInt32());
        Assert.AreEqual(4, load.GetProperty("selection").GetProperty("head").GetInt32());
        Assert.AreEqual(120d, load.GetProperty("scrollTop").GetDouble());
        Assert.AreEqual(EditorSessionState.Ready, session.State);
    }

    [TestMethod]
    public async Task NonFatalFault_OnlyLogs()
    {
        await StartReady("abc");
        session.Receive(Evt("lifecycle.fault", """{"message":"meh","fatal":false}"""));
        Assert.AreEqual(EditorSessionState.Ready, session.State);
        Assert.AreEqual(1, channel.Inits.Count);
    }

    [TestMethod]
    public async Task RenderProcessExit_QuarantinesTheCursorAndStopsReloadLoops()
    {
        await StartReady("abc");
        session.Receive(Evt("selection.changed", """{"hasText":false,"text":"","rich":null,"anchor":1,"head":1}"""));

        session.OnRenderProcessExited();
        await Task.Yield();
        Assert.AreEqual(2, channel.Inits.Count);
        session.Receive(Ready());
        Assert.IsTrue(channel.Sent.Last(x => x.T == "doc.load").P.TryGetProperty("selection", out _), "first crash restores the cursor");

        clock.Advance(TimeSpan.FromSeconds(5));
        session.OnRenderProcessExited();
        await Task.Yield();
        session.Receive(Ready());
        Assert.IsFalse(channel.Sent.Last(x => x.T == "doc.load").P.TryGetProperty("selection", out _), "second crash soon after: no cursor");

        clock.Advance(TimeSpan.FromSeconds(5));
        session.OnRenderProcessExited();
        await Task.Yield();
        session.Receive(Ready());
        clock.Advance(TimeSpan.FromSeconds(5));
        session.OnRenderProcessExited();
        await Task.Yield();

        Assert.AreEqual(4, channel.Inits.Count, "the fourth crash within a minute does not reload");
        Assert.AreEqual(EditorSessionState.Faulted, session.State);
    }

    [TestMethod]
    public async Task UnexpectedNavigation_ClosesTheGateAndCancelsRequests()
    {
        await StartReady("abc");
        var context = session.RequestAsync(new ContextAt(1, 1));
        session.OnNavigationStarting();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => context);
        Assert.AreEqual(EditorSessionState.Loading, session.State);
        session.Post(new SelectAll());
        Assert.AreEqual("selection.contextAt", channel.Sent.Last().T, "commands queue until the next ready");
    }

    // ── 辅助 ─────────────────────────────────────────────────────────

    /// <summary>挂载、加载、ready，返回装载版本号；之后清空已发报文。</summary>
    private async Task<long> StartReady(string text)
    {
        session.Attach(channel);
        await session.LoadPageAsync();
        session.Post(new LoadDocument(text, "C:/docs"));
        session.OnNavigationStarting();
        session.Receive(Ready());
        channel.Sent.Clear();
        return session.Document.Version;
    }

    private static string Ready(int protocol = EditorWireTypes.ProtocolVersion) =>
        Evt("lifecycle.ready", $$"""{"protocol":{{protocol}},"engine":"test"}""");

    private static string Evt(string type, string payload) => $$"""{"k":"evt","t":"{{type}}","p":{{payload}}}""";

    private static string Changed(long baseVersion, long version, int from, int to, string insert) =>
        Evt("doc.changed", $$"""{"baseVersion":{{baseVersion}},"version":{{version}},"changes":[{"from":{{from}},"to":{{to}},"insert":{{JsonSerializer.Serialize(insert)}}}]}""");

    private static string Res(long id, string payload) => $$"""{"k":"res","id":{{id}},"ok":true,"p":{{payload}}}""";

    private static string Fail(long id, string code) => $$"""{"k":"res","id":{{id}},"ok":false,"err":{"code":"{{code}}","message":"x"} """ + "}";

    private sealed record SentMessage(string K, string? T, long? Id, JsonElement P, bool Ok, string? Error);

    private sealed class FakeChannel : IEditorWireChannel
    {
        public List<SentMessage> Sent { get; } = new();

        public List<string> Inits { get; } = new();

        public bool TryPost(string message)
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement.Clone();
            Sent.Add(new SentMessage(
                root.GetProperty("k").GetString()!,
                root.TryGetProperty("t", out var t) ? t.GetString() : null,
                root.TryGetProperty("id", out var id) ? id.GetInt64() : null,
                root.TryGetProperty("p", out var p) ? p : default,
                root.TryGetProperty("ok", out var ok) && ok.GetBoolean(),
                root.TryGetProperty("err", out var err) ? err.GetProperty("code").GetString() : null));
            return true;
        }

        public Task NavigateAsync(string initScript)
        {
            Inits.Add(initScript);
            return Task.CompletedTask;
        }

        public EditorTheme GetCurrentTheme() => Light;

        public JsonElement LastInit()
        {
            const string prefix = "window.__typedownInit = ";
            var script = Inits.Last();
            Assert.IsTrue(script.StartsWith(prefix, StringComparison.Ordinal));
            return JsonDocument.Parse(script[prefix.Length..].TrimEnd(';')).RootElement.Clone();
        }
    }

    private sealed class FakeCallbacks : IEditorHostCallbacks
    {
        public string BasePath => "C:/base";

        public int StartupCalls { get; private set; }

        public TableSize? TableSize { get; set; }

        public List<ClipboardContent> Clipboard { get; } = new();

        public Task<EditorSettings> PrepareStartupAsync(CancellationToken cancellationToken)
        {
            StartupCalls++;
            return Task.FromResult(new EditorSettings { FontSize = 16 });
        }

        public Task<TableSize?> PickTableSizeAsync(CancellationToken cancellationToken) => Task.FromResult(TableSize);

        public Task WriteClipboardAsync(ClipboardContent content, CancellationToken cancellationToken)
        {
            Clipboard.Add(content);
            return Task.CompletedTask;
        }

        public Task<string?> ResolveImageAsync(ImageSource source, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    }

    /// <summary>计时器回调直接在推进时钟的线程上执行。</summary>
    private sealed class InlineDispatcher : IUiDispatcher
    {
        public Task RunAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        public Task<T> RunAsync<T>(Func<T> action) => Task.FromResult(action());

        public Task RunIdleAsync(Action action) => RunAsync(action);

        public Task<T> RunIdleAsync<T>(Func<T> action) => RunAsync(action);
    }
}
