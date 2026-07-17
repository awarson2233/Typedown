using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace Typedown.WinUI.Utilities;

internal static class StartupTrace
{
    public static bool IsEnabled => StartupEventSource.Log.IsEnabled();

#if DEBUG
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
#endif

    public static void ProgramMain()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.ProgramMain();
        }

        Mark("Program.Main entered");
    }

    public static void AppInitializeComponentStart()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.AppInitializeComponentStart();
        }

        Mark("App.InitializeComponent start");
    }

    public static void AppInitializeComponentStop()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.AppInitializeComponentStop();
        }

        Mark("App.InitializeComponent end");
    }

    public static void AppOnLaunchedStart()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.AppOnLaunchedStart();
        }

        Mark("App.OnLaunched start");
    }

    public static void AppOnLaunchedStop()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.AppOnLaunchedStop();
        }

        Mark("App.OnLaunched end");
    }

    public static void SQLiteInitializeStart()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.SQLiteInitializeStart();
        }

        Mark("SQLite Batteries.Init start");
    }

    public static void SQLiteInitializeStop()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.SQLiteInitializeStop();
        }

        Mark("SQLite Batteries.Init end");
    }

    public static void WindowActivateStart()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.WindowActivateStart();
        }

        Mark("Window.Activate start");
    }

    public static void WindowActivateStop()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.WindowActivateStop();
        }

        Mark("Window.Activate end");
    }

    public static void CoreWebView2EnvironmentCreateStart()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.CoreWebView2EnvironmentCreateStart();
        }

        Mark("CoreWebView2Environment.CreateAsync start");
    }

    public static void CoreWebView2EnvironmentCreateStop()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.CoreWebView2EnvironmentCreateStop();
        }

        Mark("CoreWebView2Environment.CreateAsync end");
    }

    public static void EnsureCoreWebView2Start()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.EnsureCoreWebView2Start();
        }

        Mark("WebView2.EnsureCoreWebView2Async start");
    }

    public static void EnsureCoreWebView2Stop()
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.EnsureCoreWebView2Stop();
        }

        Mark("WebView2.EnsureCoreWebView2Async end");
    }

    public static void NavigationStarting(ulong navigationId, string? uri)
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.NavigationStarting(
                unchecked((long)navigationId),
                (long)GetNavigationKind(uri));
        }

        MarkNavigation("NavigationStarting", navigationId);
    }

    public static void ContentLoading(ulong navigationId)
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.ContentLoading(unchecked((long)navigationId));
        }

        MarkNavigation("ContentLoading", navigationId);
    }

    public static void DomContentLoaded(ulong navigationId)
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.DomContentLoaded(unchecked((long)navigationId));
        }

        MarkNavigation("DOMContentLoaded", navigationId);
    }

    public static void NavigationCompleted(ulong navigationId, bool isSuccess, int webErrorStatus)
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.NavigationCompleted(
                unchecked((long)navigationId),
                isSuccess ? 1L : 0L,
                webErrorStatus);
        }

        MarkNavigation("NavigationCompleted", navigationId);
    }

    public static void BridgeContentLoaded(ulong navigationId)
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.BridgeContentLoaded(unchecked((long)navigationId));
        }

        MarkNavigation("Bridge.ContentLoaded", navigationId);
    }

    public static void BridgeFileLoaded(ulong navigationId)
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.BridgeFileLoaded(unchecked((long)navigationId));
        }

        MarkNavigation("Bridge.FileLoaded", navigationId);
    }

    public static void BridgeDocumentRendered(ulong navigationId)
    {
        if (IsEnabled)
        {
            StartupEventSource.Log.BridgeDocumentRendered(unchecked((long)navigationId));
        }

        MarkNavigation("Bridge.DocumentRendered", navigationId);
    }

    [Conditional("DEBUG")]
    public static void Mark(string name)
    {
#if DEBUG
        Debug.WriteLine($"[Typedown startup] {Stopwatch.ElapsedMilliseconds,5} ms | {name}");
#endif
    }

    public static IDisposable Phase(string name)
    {
#if DEBUG
        return new StartupTracePhase(name);
#else
        return NoopPhase.Instance;
#endif
    }

    [Conditional("DEBUG")]
    private static void MarkNavigation(string name, ulong navigationId)
    {
        Mark($"{name} (NavigationId={navigationId})");
    }

    private static StartupNavigationKind GetNavigationKind(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return StartupNavigationKind.Unknown;
        }

        if (uri.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return StartupNavigationKind.File;
        }

        if (uri.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
        {
            return StartupNavigationKind.Https;
        }

        if (uri.StartsWith("http:", StringComparison.OrdinalIgnoreCase))
        {
            return StartupNavigationKind.Http;
        }

        return StartupNavigationKind.Other;
    }

#if DEBUG
    private sealed class StartupTracePhase : IDisposable
    {
        private readonly string name;
        private readonly long startedAt;

        public StartupTracePhase(string name)
        {
            this.name = name;
            startedAt = Stopwatch.ElapsedMilliseconds;
            Mark($"{name} start");
        }

        public void Dispose()
        {
            Mark($"{name} end (+{Stopwatch.ElapsedMilliseconds - startedAt} ms)");
        }
    }
#endif

    private sealed class NoopPhase : IDisposable
    {
        public static readonly NoopPhase Instance = new();

        public void Dispose()
        {
        }
    }

    private enum StartupNavigationKind : long
    {
        Unknown = 0,
        File = 1,
        Http = 2,
        Https = 3,
        Other = 4
    }
}

[EventSource(Name = ProviderName)]
internal sealed class StartupEventSource : EventSource
{
    public const string ProviderName = "Typedown-Startup";

    public static readonly StartupEventSource Log = new();

    [Event(EventIds.ProgramMain, Level = EventLevel.Informational, Keywords = Keywords.Startup)]
    public void ProgramMain() => WriteEvent(EventIds.ProgramMain);

    [Event(EventIds.AppInitializeComponentStart, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Start, Task = Tasks.AppInitializeComponent)]
    public void AppInitializeComponentStart() => WriteEvent(EventIds.AppInitializeComponentStart);

    [Event(EventIds.AppInitializeComponentStop, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Stop, Task = Tasks.AppInitializeComponent)]
    public void AppInitializeComponentStop() => WriteEvent(EventIds.AppInitializeComponentStop);

    [Event(EventIds.AppOnLaunchedStart, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Start, Task = Tasks.AppOnLaunched)]
    public void AppOnLaunchedStart() => WriteEvent(EventIds.AppOnLaunchedStart);

    [Event(EventIds.SQLiteInitializeStart, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Start, Task = Tasks.SQLiteInitialize)]
    public void SQLiteInitializeStart() => WriteEvent(EventIds.SQLiteInitializeStart);

    [Event(EventIds.SQLiteInitializeStop, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Stop, Task = Tasks.SQLiteInitialize)]
    public void SQLiteInitializeStop() => WriteEvent(EventIds.SQLiteInitializeStop);

    [Event(EventIds.WindowActivateStart, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Start, Task = Tasks.WindowActivate)]
    public void WindowActivateStart() => WriteEvent(EventIds.WindowActivateStart);

    [Event(EventIds.WindowActivateStop, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Stop, Task = Tasks.WindowActivate)]
    public void WindowActivateStop() => WriteEvent(EventIds.WindowActivateStop);

    [Event(EventIds.AppOnLaunchedStop, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Stop, Task = Tasks.AppOnLaunched)]
    public void AppOnLaunchedStop() => WriteEvent(EventIds.AppOnLaunchedStop);

    [Event(EventIds.CoreWebView2EnvironmentCreateStart, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Start, Task = Tasks.CoreWebView2EnvironmentCreate)]
    public void CoreWebView2EnvironmentCreateStart() => WriteEvent(EventIds.CoreWebView2EnvironmentCreateStart);

    [Event(EventIds.CoreWebView2EnvironmentCreateStop, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Stop, Task = Tasks.CoreWebView2EnvironmentCreate)]
    public void CoreWebView2EnvironmentCreateStop() => WriteEvent(EventIds.CoreWebView2EnvironmentCreateStop);

    [Event(EventIds.EnsureCoreWebView2Start, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Start, Task = Tasks.EnsureCoreWebView2)]
    public void EnsureCoreWebView2Start() => WriteEvent(EventIds.EnsureCoreWebView2Start);

    [Event(EventIds.EnsureCoreWebView2Stop, Level = EventLevel.Informational, Keywords = Keywords.Startup, Opcode = EventOpcode.Stop, Task = Tasks.EnsureCoreWebView2)]
    public void EnsureCoreWebView2Stop() => WriteEvent(EventIds.EnsureCoreWebView2Stop);

    [Event(EventIds.NavigationStarting, Level = EventLevel.Informational, Keywords = Keywords.Startup)]
    public void NavigationStarting(long navigationId, long navigationKind) =>
        WriteEvent(EventIds.NavigationStarting, navigationId, navigationKind);

    [Event(EventIds.ContentLoading, Level = EventLevel.Informational, Keywords = Keywords.Startup)]
    public void ContentLoading(long navigationId) => WriteEvent(EventIds.ContentLoading, navigationId);

    [Event(EventIds.DomContentLoaded, Level = EventLevel.Informational, Keywords = Keywords.Startup)]
    public void DomContentLoaded(long navigationId) => WriteEvent(EventIds.DomContentLoaded, navigationId);

    [Event(EventIds.NavigationCompleted, Level = EventLevel.Informational, Keywords = Keywords.Startup)]
    public void NavigationCompleted(long navigationId, long isSuccess, long webErrorStatus) =>
        WriteEvent(EventIds.NavigationCompleted, navigationId, isSuccess, webErrorStatus);

    [Event(EventIds.BridgeContentLoaded, Level = EventLevel.Informational, Keywords = Keywords.Startup)]
    public void BridgeContentLoaded(long navigationId) => WriteEvent(EventIds.BridgeContentLoaded, navigationId);

    [Event(EventIds.BridgeFileLoaded, Level = EventLevel.Informational, Keywords = Keywords.Startup)]
    public void BridgeFileLoaded(long navigationId) => WriteEvent(EventIds.BridgeFileLoaded, navigationId);

    [Event(EventIds.BridgeDocumentRendered, Level = EventLevel.Informational, Keywords = Keywords.Startup)]
    public void BridgeDocumentRendered(long navigationId) => WriteEvent(EventIds.BridgeDocumentRendered, navigationId);

    internal static class EventIds
    {
        public const int ProgramMain = 1;
        public const int AppInitializeComponentStart = 2;
        public const int AppInitializeComponentStop = 3;
        public const int AppOnLaunchedStart = 4;
        public const int SQLiteInitializeStart = 5;
        public const int SQLiteInitializeStop = 6;
        public const int WindowActivateStart = 7;
        public const int WindowActivateStop = 8;
        public const int AppOnLaunchedStop = 9;
        public const int CoreWebView2EnvironmentCreateStart = 10;
        public const int CoreWebView2EnvironmentCreateStop = 11;
        public const int EnsureCoreWebView2Start = 12;
        public const int EnsureCoreWebView2Stop = 13;
        public const int NavigationStarting = 14;
        public const int ContentLoading = 15;
        public const int DomContentLoaded = 16;
        public const int NavigationCompleted = 17;
        public const int BridgeContentLoaded = 18;
        public const int BridgeFileLoaded = 19;
        public const int BridgeDocumentRendered = 20;
    }

    public static class Keywords
    {
        public const EventKeywords Startup = (EventKeywords)0x1;
    }

    public static class Tasks
    {
        public const EventTask AppInitializeComponent = (EventTask)1;
        public const EventTask AppOnLaunched = (EventTask)2;
        public const EventTask SQLiteInitialize = (EventTask)3;
        public const EventTask WindowActivate = (EventTask)4;
        public const EventTask CoreWebView2EnvironmentCreate = (EventTask)5;
        public const EventTask EnsureCoreWebView2 = (EventTask)6;
    }
}
