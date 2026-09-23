using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Typedown.Core.Models
{
    public class HistoryModel
    {
        public string? Text { get; set; }
        public CursorState? Cursor { get; set; }
    }

    /// <summary>
    /// 整篇快照式的撤销历史：正文变化先落在待提交快照里，3 秒无新变化、光标换行或撤销时提交成一步，最多保留 100 步。
    /// 提交计时器在线程池上触发，所有状态变更都在锁内完成，<see cref="PropertyChanged"/> 在锁外、可能在线程池线程上引发。
    /// </summary>
    public class ContentHistory : INotifyPropertyChanged, IDisposable
    {
        public static readonly TimeSpan CommitDelay = TimeSpan.FromSeconds(3);

        const int deep = 100;
        readonly object gate = new();
        readonly List<HistoryModel> histories = new();
        HistoryModel pending = new();
        int index = -1;
        private readonly ITimer commitTimer;

        private bool undoable;
        private bool redoable;

        public bool Undoable
        {
            get
            {
                lock (gate)
                {
                    return undoable;
                }
            }
        }

        public bool Redoable
        {
            get
            {
                lock (gate)
                {
                    return redoable;
                }
            }
        }

        public bool IsPending
        {
            get
            {
                lock (gate)
                {
                    return IsPendingCore;
                }
            }
        }

        private bool IsPendingCore => pending.Text != null && pending.Cursor != null;

        public ContentHistory(TimeProvider? timeProvider = null)
        {
            commitTimer = (timeProvider ?? TimeProvider.System).CreateTimer(
                _ => CommitPending(),
                null,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);
        }

        [return: MaybeNull]
        public HistoryModel Undo()
        {
            return Mutate(() =>
            {
                if (index > 0 || (index == 0 && IsPendingCore))
                {
                    CommitPendingCore();
                    index--;
                    SetRedoable(true);
                    SetUndoable(index > 0);
                    return histories[index];
                }

                return null;
            });
        }

        [return: MaybeNull]
        public HistoryModel Redo()
        {
            return Mutate(() =>
            {
                if (index < histories.Count - 1)
                {
                    StopTimer();
                    pending = new();
                    index++;
                    SetRedoable(index < histories.Count - 1);
                    SetUndoable(true);
                    return histories[index];
                }

                return null;
            });
        }

        public void ClearHistory() => Mutate(ClearHistoryCore);

        public void CommitPending() => Mutate(CommitPendingCore);

        public void CursorChange(CursorState cursor)
        {
            Mutate(() =>
            {
                if (pending.Text == null && index > -1)
                {
                    histories[index].Cursor = cursor;
                    return;
                }
                if (cursor == null)
                {
                    return;
                }
                if (IsPendingCore && pending.Cursor!.Focus.Line != cursor.Focus.Line)
                {
                    pending.Cursor = cursor;
                    CommitPendingCore();
                    return;
                }
                pending.Cursor = cursor;
                if (pending.Text != null && histories.Count == 0)
                {
                    CommitPendingCore();
                    return;
                }
                StateChange();
            });
        }

        public void ContentChange(string content)
        {
            Mutate(() => ContentChangeCore(content));
        }

        public void InitHistory(string content)
        {
            Mutate(() =>
            {
                ClearHistoryCore();
                pending.Cursor = new(Focus: new(Line: 0, Ch: 0), Anchor: new(Line: 0, Ch: 0));
                StateChange();
                ContentChangeCore(content);
            });
        }

        private void ContentChangeCore(string content)
        {
            content = content.TrimEnd('\r', '\n');
            if ((pending.Text != null && pending.Text == content) ||
                (pending.Text == null && index > -1 && histories[index].Text!.Trim('\r', '\n') == content.Trim('\r', '\n')))
            {
                return;
            }
            pending.Text = content;
            if (pending.Cursor != null && histories.Count == 0)
            {
                CommitPendingCore();
                return;
            }
            StateChange();
            commitTimer.Change(CommitDelay, Timeout.InfiniteTimeSpan);
        }

        private void ClearHistoryCore()
        {
            histories.Clear();
            StopTimer();
            pending = new();
            index = -1;
            SetRedoable(false);
            SetUndoable(false);
        }

        private void CommitPendingCore()
        {
            if (!IsPendingCore) return;
            StopTimer();
            histories.RemoveRange(index + 1, histories.Count - (index + 1));
            histories.Add(pending);
            if (histories.Count > deep)
            {
                histories.RemoveAt(0);
            }
            else
            {
                index++;
            }
            pending = new();
            SetRedoable(false);
            SetUndoable(index > 0);
        }

        private void StateChange()
        {
            SetRedoable(index < histories.Count - 1);
            SetUndoable(index > 0 || (index == 0 && pending.Text != null && pending.Cursor != null));
        }

        private void StopTimer() => commitTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        // ── 变更与通知 ─────────────────────────────────────────────────────

        private readonly List<string> changedProperties = new();

        private void SetUndoable(bool value)
        {
            if (undoable != value)
            {
                undoable = value;
                changedProperties.Add(nameof(Undoable));
            }
        }

        private void SetRedoable(bool value)
        {
            if (redoable != value)
            {
                redoable = value;
                changedProperties.Add(nameof(Redoable));
            }
        }

        private void Mutate(Action action) => Mutate<object?>(() =>
        {
            action();
            return null;
        });

        private T? Mutate<T>(Func<T?> action)
        {
            T? result = default;
            string[] changes;
            lock (gate)
            {
                try
                {
                    result = action();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }

                changes = changedProperties.ToArray();
                changedProperties.Clear();
            }

            foreach (var propertyName in changes)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            return result;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose()
        {
            commitTimer.Dispose();
        }
    }
}
