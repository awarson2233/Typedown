using System;
using System.Collections.Generic;

namespace Typedown.Core.Editor
{
    /// <summary>
    /// 编辑会话的就绪门：宿主已挂载（<see cref="IsAttached"/>）且引擎已就绪（<see cref="IsReady"/>）时门开，命令直接放行；
    /// 门关时普通命令按到达顺序排队（超出容量丢最旧的），主题、快捷键表、设置、视口刷新这几类状态只保留最新值，
    /// 在门重新打开时按「主题 → 快捷键 → 设置 → 视口 → 队列」的顺序整体重放。
    /// </summary>
    /// <typeparam name="T">排队的普通消息（适配器已编码好的报文）。</typeparam>
    public sealed class EditorCommandGate<T>
    {
        public const int DefaultCapacity = 128;

        private readonly Queue<T> queue = new();
        private readonly int capacity;
        private ApplyTheme? theme;
        private SetKeymap? keymap;
        private EditorSettings? pendingSettings;
        private bool refreshPending;

        public EditorCommandGate(int capacity = DefaultCapacity, bool retainWhileDetached = false)
        {
            this.capacity = Math.Max(1, capacity);
            RetainWhileDetached = retainWhileDetached;
        }

        /// <summary>宿主卸载期间是否也保留命令；否则卸载期间的普通命令与设置变更直接丢弃。</summary>
        public bool RetainWhileDetached { get; }

        public bool IsAttached { get; private set; }

        public bool IsReady { get; private set; }

        public bool IsOpen => IsAttached && IsReady;

        public int QueuedCount => queue.Count;

        private bool Retaining => IsAttached || RetainWhileDetached;

        /// <summary>
        /// 提交一条状态类命令（主题、快捷键表、设置、视口刷新）。返回 <c>true</c> 表示门开，调用方应立即发送。
        /// </summary>
        public bool Submit(EditorCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);
            switch (command)
            {
                case ApplyTheme applyTheme:
                    theme = applyTheme;
                    return IsOpen;
                case SetKeymap setKeymap:
                    keymap = setKeymap;
                    return IsOpen;
                case ApplySettings applySettings:
                    if (IsOpen)
                    {
                        return true;
                    }

                    if (Retaining)
                    {
                        pendingSettings = pendingSettings?.Merge(applySettings.Changes) ?? applySettings.Changes;
                    }

                    return false;
                case RefreshViewport:
                    if (IsOpen)
                    {
                        return true;
                    }

                    refreshPending |= Retaining;
                    return false;
                default:
                    throw new ArgumentException($"{command.GetType().Name} is not a retained editor command.", nameof(command));
            }
        }

        /// <summary>提交一条普通消息。返回 <c>true</c> 表示门开，调用方应立即发送。</summary>
        public bool Submit(T message)
        {
            if (IsOpen)
            {
                return true;
            }

            if (Retaining)
            {
                if (queue.Count >= capacity)
                {
                    queue.Dequeue();
                }

                queue.Enqueue(message);
            }

            return false;
        }

        public IReadOnlyList<EditorCommandGateItem<T>> Attach() => Transition(() => IsAttached = true);

        public void Detach()
        {
            IsAttached = false;
            if (!RetainWhileDetached)
            {
                queue.Clear();
                pendingSettings = null;
                refreshPending = false;
            }
        }

        public IReadOnlyList<EditorCommandGateItem<T>> MarkReady() => Transition(() => IsReady = true);

        /// <summary>
        /// 引擎开始重新加载：已排队的普通消息属于旧页面，丢弃；它会通过启动握手重新拿到全量状态。
        /// </summary>
        public void MarkNotReady()
        {
            IsReady = false;
            queue.Clear();
            pendingSettings = null;
            refreshPending = false;
        }

        /// <summary>页面在启动握手里拿到了全量设置，此前积压的设置增量作废。</summary>
        public void ClearPendingSettings() => pendingSettings = null;

        private IReadOnlyList<EditorCommandGateItem<T>> Transition(Action change)
        {
            var wasOpen = IsOpen;
            change();
            if (wasOpen || !IsOpen)
            {
                return [];
            }

            var items = new List<EditorCommandGateItem<T>>();
            if (theme is not null)
            {
                items.Add(EditorCommandGateItem<T>.ForCommand(theme));
            }

            if (keymap is not null)
            {
                items.Add(EditorCommandGateItem<T>.ForCommand(keymap));
            }

            if (pendingSettings is not null)
            {
                items.Add(EditorCommandGateItem<T>.ForCommand(new ApplySettings(pendingSettings)));
                pendingSettings = null;
            }

            if (refreshPending)
            {
                items.Add(EditorCommandGateItem<T>.ForCommand(new RefreshViewport()));
                refreshPending = false;
            }

            while (queue.Count > 0)
            {
                items.Add(EditorCommandGateItem<T>.ForMessage(queue.Dequeue()));
            }

            return items;
        }
    }

    /// <summary>门打开时要重放的一项：状态类命令或排队的普通消息，二者取其一。</summary>
    public readonly record struct EditorCommandGateItem<T>(EditorCommand? Command, T? Message)
    {
        public static EditorCommandGateItem<T> ForCommand(EditorCommand command) => new(command, default);

        public static EditorCommandGateItem<T> ForMessage(T message) => new(null, message);
    }
}
