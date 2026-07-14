using System;
using System.Threading;
using System.Threading.Tasks;

namespace Typedown.Core.Services
{
    public sealed class PendingImportGate
    {
        private readonly object syncRoot = new();
        private string? revision;
        private TaskCompletionSource<bool>? completion;

        public bool IsPending
        {
            get
            {
                lock (syncRoot) return completion is not null;
            }
        }

        public string? Revision
        {
            get
            {
                lock (syncRoot) return revision;
            }
        }

        public void Begin(string nextRevision)
        {
            ArgumentException.ThrowIfNullOrEmpty(nextRevision);
            lock (syncRoot)
            {
                completion?.TrySetResult(false);
                revision = nextRevision;
                completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        public bool Complete(string completedRevision)
        {
            lock (syncRoot)
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
            lock (syncRoot)
            {
                completion?.TrySetResult(false);
                completion = null;
                revision = null;
            }
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            Task<bool>? pending;
            lock (syncRoot) pending = completion?.Task;
            if (pending is null) return true;

            try
            {
                return await pending.WaitAsync(timeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                return false;
            }
        }
    }
}
