using System.Diagnostics;

namespace Typedown.WinUI.Utilities;

internal static class StartupTrace
{
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    [Conditional("DEBUG")]
    public static void Mark(string name)
    {
        Debug.WriteLine($"[Typedown startup] {Stopwatch.ElapsedMilliseconds,5} ms | {name}");
    }

    public static IDisposable Phase(string name) => new StartupTracePhase(name);

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
}
