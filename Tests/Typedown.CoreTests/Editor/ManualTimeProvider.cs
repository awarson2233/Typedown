namespace Typedown.CoreTests.Editor;

/// <summary>手动推进的时钟：<see cref="Advance"/> 时同步触发到期的计时器。</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private readonly List<ManualTimer> timers = new();
    private DateTimeOffset now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => now;

    public void Advance(TimeSpan delta)
    {
        var target = now + delta;
        while (true)
        {
            var next = timers
                .Where(x => x.DueAt is { } due && due <= target)
                .OrderBy(x => x.DueAt)
                .FirstOrDefault();
            if (next is null)
            {
                break;
            }

            now = next.DueAt!.Value;
            next.Fire();
        }

        now = target;
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period);
        timers.Add(timer);
        return timer;
    }

    private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
    {
        private TimeSpan period = Timeout.InfiniteTimeSpan;

        public DateTimeOffset? DueAt { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            this.period = period;
            DueAt = dueTime == Timeout.InfiniteTimeSpan ? null : owner.now + dueTime;
            return true;
        }

        public void Fire()
        {
            DueAt = period == Timeout.InfiniteTimeSpan || period == TimeSpan.Zero ? null : DueAt + period;
            callback(state);
        }

        public void Dispose()
        {
            DueAt = null;
            owner.timers.Remove(this);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
