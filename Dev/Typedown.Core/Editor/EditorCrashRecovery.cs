using System;
using System.Collections.Generic;

namespace Typedown.Core.Editor
{
    /// <summary>
    /// 引擎进程崩溃后的恢复策略：崩溃即重载并恢复正文与光标，但要防住「一恢复就崩」的死循环——
    /// 两次崩溃间隔过近时不再恢复光标（光标位置可能正是诱因），一分钟内崩溃过多时停止自动重载。
    /// </summary>
    public sealed class EditorCrashRecovery
    {
        public const int MaxReloadsPerWindow = 3;

        public static readonly TimeSpan ReloadWindow = TimeSpan.FromMinutes(1);

        public static readonly TimeSpan CursorQuarantine = TimeSpan.FromSeconds(15);

        private readonly TimeProvider timeProvider;
        private readonly Queue<DateTimeOffset> crashes = new();

        public EditorCrashRecovery(TimeProvider? timeProvider = null)
        {
            this.timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <summary>记录一次崩溃并给出处置。</summary>
        public EditorCrashDecision OnCrash()
        {
            var now = timeProvider.GetUtcNow();
            DateTimeOffset? previous = crashes.Count > 0 ? LastCrash : null;
            while (crashes.Count > 0 && now - crashes.Peek() >= ReloadWindow)
            {
                crashes.Dequeue();
            }

            crashes.Enqueue(now);
            LastCrash = now;
            var reload = crashes.Count <= MaxReloadsPerWindow;
            var restoreCursor = reload && (previous is null || now - previous.Value >= CursorQuarantine);
            return new EditorCrashDecision(reload, restoreCursor);
        }

        private DateTimeOffset LastCrash { get; set; }
    }

    public readonly record struct EditorCrashDecision(bool Reload, bool RestoreCursor);
}
