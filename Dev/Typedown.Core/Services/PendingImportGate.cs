using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Typedown.Core.Services
{
    public sealed class PendingImportGate
    {
        private readonly SemaphoreSlim boundary = new(1, 1);
        private string? revision;
        private TaskCompletionSource<bool>? completion;
        private long generation;

        public bool IsPending => completion is not null;
        public string? Revision => revision;

        public void Begin(string nextRevision)
        {
            ArgumentException.ThrowIfNullOrEmpty(nextRevision);
            boundary.Wait();
            try
            {
                completion?.TrySetResult(false);
                generation++;
                revision = nextRevision;
                completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            finally
            {
                boundary.Release();
            }
        }

        public bool Complete(string completedRevision)
        {
            boundary.Wait();
            try
            {
                if (completion is null || !StringComparer.Ordinal.Equals(revision, completedRevision)) return false;
                completion.TrySetResult(true);
                completion = null;
                revision = null;
                return true;
            }
            finally
            {
                boundary.Release();
            }
        }

        public void Cancel()
        {
            boundary.Wait();
            try
            {
                completion?.TrySetResult(false);
                generation++;
                completion = null;
                revision = null;
            }
            finally
            {
                boundary.Release();
            }
        }

        public async Task<PersistenceLease?> AcquireAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                Task<bool>? pending = null;
                long observedGeneration;
                await boundary.WaitAsync(cancellationToken);
                try
                {
                    pending = completion?.Task;
                    observedGeneration = generation;
                    if (pending is null) return new PersistenceLease(this, observedGeneration);
                }
                finally
                {
                    if (pending is not null) boundary.Release();
                }

                var remaining = timeout - stopwatch.Elapsed;
                if (remaining <= TimeSpan.Zero) return null;
                try
                {
                    if (!await pending.WaitAsync(remaining, cancellationToken)) continue;
                }
                catch (TimeoutException)
                {
                    return null;
                }

                remaining = timeout - stopwatch.Elapsed;
                if (remaining <= TimeSpan.Zero) return null;
                if (!await boundary.WaitAsync(remaining, cancellationToken)) return null;
                if (completion is null && generation == observedGeneration)
                    return new PersistenceLease(this, observedGeneration);
                boundary.Release();
            }
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
            public bool IsValid => owner is { } gate && gate.completion is null && gate.generation == Generation;

            public void Dispose()
            {
                Interlocked.Exchange(ref owner, null)?.boundary.Release();
            }
        }
    }
}
