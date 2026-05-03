using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Typedown.Presentation.Interfaces;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIUiDispatcher : IUiDispatcher
    {
        private readonly DispatcherQueue dispatcherQueue;

        public WinUIUiDispatcher(DispatcherQueue dispatcherQueue)
        {
            this.dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        }

        public Task RunAsync(Action action)
        {
            return EnqueueAsync(action, DispatcherQueuePriority.Normal);
        }

        public Task<T> RunAsync<T>(Func<T> action)
        {
            return EnqueueAsync(action, DispatcherQueuePriority.Normal);
        }

        public Task RunIdleAsync(Action action)
        {
            return EnqueueAsync(action, DispatcherQueuePriority.Low);
        }

        public Task<T> RunIdleAsync<T>(Func<T> action)
        {
            return EnqueueAsync(action, DispatcherQueuePriority.Low);
        }

        private Task EnqueueAsync(Action action, DispatcherQueuePriority priority)
        {
            if (dispatcherQueue.HasThreadAccess)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!dispatcherQueue.TryEnqueue(priority, () => Execute(action, tcs)))
            {
                tcs.SetException(new InvalidOperationException("Failed to enqueue work on the WinUI dispatcher."));
            }

            return tcs.Task;
        }

        private Task<T> EnqueueAsync<T>(Func<T> action, DispatcherQueuePriority priority)
        {
            if (dispatcherQueue.HasThreadAccess)
            {
                return Task.FromResult(action());
            }

            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!dispatcherQueue.TryEnqueue(priority, () => Execute(action, tcs)))
            {
                tcs.SetException(new InvalidOperationException("Failed to enqueue work on the WinUI dispatcher."));
            }

            return tcs.Task;
        }

        private static void Execute(Action action, TaskCompletionSource<object?> tcs)
        {
            try
            {
                action();
                tcs.SetResult(null);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }

        private static void Execute<T>(Func<T> action, TaskCompletionSource<T> tcs)
        {
            try
            {
                tcs.SetResult(action());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }
    }
}
