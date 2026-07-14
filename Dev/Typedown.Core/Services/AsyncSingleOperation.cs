using System;
using System.Threading;
using System.Threading.Tasks;

namespace Typedown.Core.Services
{
    public sealed class AsyncSingleOperation
    {
        private int running;

        public bool IsRunning => Volatile.Read(ref running) != 0;

        public async Task<bool> TryRunAsync(Func<Task> operation)
        {
            ArgumentNullException.ThrowIfNull(operation);
            if (Interlocked.CompareExchange(ref running, 1, 0) != 0) return false;
            try
            {
                await operation();
                return true;
            }
            finally
            {
                Volatile.Write(ref running, 0);
            }
        }
    }
}
