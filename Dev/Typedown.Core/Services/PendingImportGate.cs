using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Typedown.Core.Services
{
    public sealed class PendingImportGate
    {
        private readonly object stateLock = new();
        private readonly SemaphoreSlim persistenceLock = new(1, 1);
        private string? revision;
        private TaskCompletionSource<bool>? completion;
        private long generation;

        public bool IsPending { get { lock (stateLock) return completion is not null; } }
        public string? Revision { get { lock (stateLock) return revision; } }

        public void Begin(string nextRevision)
        {
            ArgumentException.ThrowIfNullOrEmpty(nextRevision);
            lock (stateLock)
            {
                completion?.TrySetResult(false);
                generation++;
                revision = nextRevision;
                completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        public bool Complete(string completedRevision)
        {
            lock (stateLock)
            {
                if (completion is null || !StringComparer.Ordinal.Equals(revision, completedRevision)) return false;
                completion.TrySetResult(true);
                completion = null;
                revision = null;
                return true;
            }
        }

        public void Cancel()
        {
            lock (stateLock)
            {
                completion?.TrySetResult(false);
                generation++;
                completion = null;
                revision = null;
            }
        }

        public async Task<PersistenceLease?> AcquireAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                Task<bool>? pending;
                long observedGeneration;
                lock (stateLock)
                {
                    pending = completion?.Task;
                    observedGeneration = generation;
                }

                if (pending is not null)
                {
                    var pendingBudget = Remaining(timeout, stopwatch.Elapsed);
                    if (pendingBudget == TimeSpan.Zero) return null;
                    try
                    {
                        if (!await pending.WaitAsync(pendingBudget, cancellationToken)) continue;
                    }
                    catch (TimeoutException)
                    {
                        return null;
                    }
                }

                var lockBudget = Remaining(timeout, stopwatch.Elapsed);
                var acquired = lockBudget == TimeSpan.Zero
                    ? persistenceLock.Wait(0)
                    : await persistenceLock.WaitAsync(lockBudget, cancellationToken);
                if (!acquired) return null;

                lock (stateLock)
                {
                    if (completion is null && generation == observedGeneration)
                        return new PersistenceLease(this, observedGeneration);
                }
                persistenceLock.Release();
            }
        }

        private static TimeSpan Remaining(TimeSpan timeout, TimeSpan elapsed)
        {
            if (timeout == Timeout.InfiniteTimeSpan) return timeout;
            var remaining = timeout - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public sealed class PersistenceLease : IDisposable
        {
            private PendingImportGate? owner;
            internal PersistenceLease(PendingImportGate owner, long generation)
            {
                this.owner = owner;
                Generation = generation;
            }

            public long Generation { get; }
            public bool IsValid
            {
                get
                {
                    if (owner is not { } gate) return false;
                    lock (gate.stateLock)
                        return gate.completion is null && gate.generation == Generation;
                }
            }

            public bool TryCommit(Func<bool> snapshotMatches, Action commit)
            {
                ArgumentNullException.ThrowIfNull(snapshotMatches);
                ArgumentNullException.ThrowIfNull(commit);
                if (owner is not { } gate) return false;
                lock (gate.stateLock)
                {
                    if (gate.completion is not null || gate.generation != Generation || !snapshotMatches()) return false;
                    commit();
                    return true;
                }
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref owner, null)?.persistenceLock.Release();
            }
        }
    }
}
