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
