using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.WinUI.Utilities;

namespace Typedown.ArchitectureTests;

[TestClass]
public class StartupTraceTests
{
    [TestMethod]
    public void EventSource_EmitsStableStartupContractWithoutUriPayload()
    {
        using var listener = new StartupEventListener();

        StartupTrace.ProgramMain();
        StartupTrace.AppInitializeComponentStart();
        StartupTrace.AppInitializeComponentStop();
        StartupTrace.AppOnLaunchedStart();
        StartupTrace.SQLiteInitializeStart();
        StartupTrace.SQLiteInitializeStop();
        StartupTrace.WindowActivateStart();
        StartupTrace.WindowActivateStop();
        StartupTrace.PersistedLanguageReadStart();
        StartupTrace.PersistedLanguageReadStop();
        StartupTrace.PersistedLanguageReadFailure();
        StartupTrace.ShellBindingsStart();
        StartupTrace.ShellBindingsStop();
        StartupTrace.ShellBindingsFailure();
        StartupTrace.AppOnLaunchedStop();
        StartupTrace.CoreWebView2EnvironmentCreateStart();
        StartupTrace.CoreWebView2EnvironmentCreateStop();
        StartupTrace.EnsureCoreWebView2Start();
        StartupTrace.EnsureCoreWebView2Stop();
        StartupTrace.NavigationStarting(42, "file:///private/document.md");
        StartupTrace.ContentLoading(42);
        StartupTrace.DomContentLoaded(42);
        StartupTrace.NavigationCompleted(42, true, 0);
        StartupTrace.BridgeContentLoaded(42);
        StartupTrace.BridgeFileLoaded(42);
        StartupTrace.BridgeDocumentRendered(42);

        var events = listener.Events.ToArray();
        var expectedContract = new (int Id, string Name)[]
        {
            (1, "ProgramMain"),
            (2, "AppInitializeComponentStart"),
            (3, "AppInitializeComponentStop"),
            (4, "AppOnLaunchedStart"),
            (5, "SQLiteInitializeStart"),
            (6, "SQLiteInitializeStop"),
            (7, "WindowActivateStart"),
            (8, "WindowActivateStop"),
            (21, "PersistedLanguageReadStart"),
            (22, "PersistedLanguageReadStop"),
            (23, "PersistedLanguageReadFailure"),
            (24, "ShellBindingsStart"),
            (25, "ShellBindingsStop"),
            (26, "ShellBindingsFailure"),
            (9, "AppOnLaunchedStop"),
            (10, "CoreWebView2EnvironmentCreateStart"),
            (11, "CoreWebView2EnvironmentCreateStop"),
            (12, "EnsureCoreWebView2Start"),
            (13, "EnsureCoreWebView2Stop"),
            (14, "NavigationStarting"),
            (15, "ContentLoading"),
            (16, "DomContentLoaded"),
            (17, "NavigationCompleted"),
            (18, "BridgeContentLoaded"),
            (19, "BridgeFileLoaded"),
            (20, "BridgeDocumentRendered")
        };

        CollectionAssert.AreEqual(
            expectedContract,
            events.Select(e => (e.Id, e.Name)).ToArray(),
            string.Join(", ", events.Select(e => $"{e.Id}:{e.Name}[{string.Join("|", e.Payload)}]")));
        Assert.IsTrue(events.All(e => e.ProviderName == StartupEventSource.ProviderName));

        var navigationStarting = events.Single(e => e.Id == StartupEventSource.EventIds.NavigationStarting);
        CollectionAssert.AreEqual(new object?[] { 42L, 1L }, navigationStarting.Payload);
        Assert.IsFalse(events.SelectMany(e => e.Payload).OfType<string>().Any());
    }

    [TestMethod]
    public void BridgeState_RecordsFirstStartupNavigationInRequiredOrder()
    {
        var state = new StartupNavigationTraceState();
        state.NavigationStarting(101);

        var milestones = new[]
        {
            state.RecordBridgeMilestone("ContentLoaded"),
            state.RecordBridgeMilestone("FileLoaded"),
            state.RecordBridgeMilestone("DocumentRendered")
        };

        CollectionAssert.AreEqual(
            new[]
            {
                StartupBridgeMilestone.ContentLoaded,
                StartupBridgeMilestone.FileLoaded,
                StartupBridgeMilestone.DocumentRendered
            },
            milestones);
        Assert.AreEqual(101UL, state.StartupNavigationId);
    }

    [TestMethod]
    public void BridgeState_RecordsContentLoadedWithoutDomContentLoadedGate()
    {
        var state = new StartupNavigationTraceState();
        state.NavigationStarting(102);

        var milestone = state.RecordBridgeMilestone("ContentLoaded");

        Assert.AreEqual(StartupBridgeMilestone.ContentLoaded, milestone);
    }

    [TestMethod]
    public void BridgeState_DropsDocumentRenderedUntilFileLoadedWasRecorded()
    {
        var state = new StartupNavigationTraceState();
        state.NavigationStarting(103);

        Assert.AreEqual(
            StartupBridgeMilestone.ContentLoaded,
            state.RecordBridgeMilestone("ContentLoaded"));
        Assert.AreEqual(
            StartupBridgeMilestone.None,
            state.RecordBridgeMilestone("DocumentRendered"));
        Assert.AreEqual(
            StartupBridgeMilestone.FileLoaded,
            state.RecordBridgeMilestone("FileLoaded"));
        Assert.AreEqual(
            StartupBridgeMilestone.DocumentRendered,
            state.RecordBridgeMilestone("DocumentRendered"));
    }

    [TestMethod]
    public void BridgeState_DeduplicatesEveryStartupMilestone()
    {
        var state = new StartupNavigationTraceState();
        state.NavigationStarting(104);

        Assert.AreEqual(StartupBridgeMilestone.ContentLoaded, state.RecordBridgeMilestone("ContentLoaded"));
        Assert.AreEqual(StartupBridgeMilestone.None, state.RecordBridgeMilestone("ContentLoaded"));
        Assert.AreEqual(StartupBridgeMilestone.FileLoaded, state.RecordBridgeMilestone("FileLoaded"));
        Assert.AreEqual(StartupBridgeMilestone.None, state.RecordBridgeMilestone("FileLoaded"));
        Assert.AreEqual(StartupBridgeMilestone.DocumentRendered, state.RecordBridgeMilestone("DocumentRendered"));
        Assert.AreEqual(StartupBridgeMilestone.None, state.RecordBridgeMilestone("DocumentRendered"));
    }

    [TestMethod]
    public void BridgeState_InvalidatesStartupMilestonesWhenAnotherNavigationStarts()
    {
        var state = new StartupNavigationTraceState();
        state.NavigationStarting(105);
        Assert.AreEqual(StartupBridgeMilestone.ContentLoaded, state.RecordBridgeMilestone("ContentLoaded"));

        state.NavigationStarting(106);

        Assert.AreEqual(StartupBridgeMilestone.None, state.RecordBridgeMilestone("FileLoaded"));
        Assert.AreEqual(StartupBridgeMilestone.None, state.RecordBridgeMilestone("DocumentRendered"));
        Assert.AreEqual(StartupBridgeMilestone.None, state.RecordBridgeMilestone("ContentLoaded"));
        Assert.AreEqual(105UL, state.StartupNavigationId);
    }

    [TestMethod]
    public void StartupIntervals_PairFallbackEnvironmentAndWholeOnLaunchedBody()
    {
        var repoRoot = FindRepoRoot();
        var hostSource = File.ReadAllText(Path.Combine(
            repoRoot,
            "Dev",
            "Typedown.WinUI",
            "Controls",
            "EditorControls",
            "Hosting",
            "WinUIEditorHost.cs"));
        var fallbackEnvironment = ExtractBetween(
            hostSource,
            "private async Task<CoreWebView2Environment> GetEnvironmentAsync()",
            "private WinUIEditorDocumentSession CreateDocumentSession");

        StringAssert.Contains(fallbackEnvironment, "StartupTrace.CoreWebView2EnvironmentCreateStart();");
        StringAssert.Contains(fallbackEnvironment, "return await CoreWebView2Environment.CreateAsync();");
        StringAssert.Contains(fallbackEnvironment, "finally");
        StringAssert.Contains(fallbackEnvironment, "StartupTrace.CoreWebView2EnvironmentCreateStop();");

        var webMessageHandler = ExtractBetween(
            hostSource,
            "private async void OnWebMessageReceived",
            "private void OnNavigationStarting");
        var receiveCall = webMessageHandler.IndexOf("var receiveTask = bridgeAdapter.ReceiveAsync", StringComparison.Ordinal);
        var eventNameCapture = webMessageHandler.IndexOf("var bridgeMilestoneName =", StringComparison.Ordinal);
        var firstAwait = webMessageHandler.IndexOf("await receiveTask;", StringComparison.Ordinal);
        var milestoneRecord = webMessageHandler.IndexOf("RecordBridgeMilestone(bridgeMilestoneName);", StringComparison.Ordinal);
        Assert.IsTrue(receiveCall >= 0 && receiveCall < eventNameCapture);
        Assert.IsTrue(eventNameCapture < firstAwait && firstAwait < milestoneRecord);
        Assert.IsFalse(webMessageHandler.Contains("RecordBridgeMilestone(bridgeAdapter.LastEventName)", StringComparison.Ordinal));

        var appSource = File.ReadAllText(Path.Combine(repoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));
        var onLaunched = ExtractBetween(
            appSource,
            "protected override void OnLaunched(LaunchActivatedEventArgs e)",
            "private void OnLaunchedCore()");

        StringAssert.Contains(onLaunched, "StartupTrace.AppOnLaunchedStart();");
        StringAssert.Contains(onLaunched, "try");
        StringAssert.Contains(onLaunched, "OnLaunchedCore();");
        StringAssert.Contains(onLaunched, "finally");
        StringAssert.Contains(onLaunched, "StartupTrace.AppOnLaunchedStop();");
    }

    private static string ExtractBetween(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, $"Missing source marker: {startMarker}");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.IsTrue(end > start, $"Missing source marker after '{startMarker}': {endMarker}");
        return source[start..end];
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Typedown.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Typedown repository root.");
    }

    private sealed class StartupEventListener : EventListener
    {
        public ConcurrentQueue<CapturedEvent> Events { get; } = new();

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == StartupEventSource.ProviderName)
            {
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            Events.Enqueue(new CapturedEvent(
                eventData.EventId,
                eventData.EventName ?? string.Empty,
                eventData.EventSource.Name,
                eventData.Payload?.ToArray() ?? Array.Empty<object?>()));
        }
    }

    private sealed record CapturedEvent(int Id, string Name, string ProviderName, object?[] Payload);
}
