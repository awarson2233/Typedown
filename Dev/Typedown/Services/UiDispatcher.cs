using System;
using System.Threading.Tasks;
using Typedown.Core.Interfaces;
using Typedown.Core.Utilities;
using Windows.UI.Core;

namespace Typedown.Services
{
    public class UiDispatcher : IUiDispatcher
    {
        private CoreDispatcher dispatcher;

        public void Attach(CoreDispatcher value)
        {
            dispatcher = value ?? throw new ArgumentNullException(nameof(value));
        }

        public Task RunAsync(Action action) => EnsureDispatcher().RunAsync(action);

        public Task<T> RunAsync<T>(Func<T> action) => EnsureDispatcher().RunAsync(action);

        public Task RunIdleAsync(Action action) => EnsureDispatcher().RunIdleAsync(action);

        public Task<T> RunIdleAsync<T>(Func<T> action) => EnsureDispatcher().RunIdleAsync(action);

        private CoreDispatcher EnsureDispatcher()
        {
            if (dispatcher == null)
                throw new InvalidOperationException("UI dispatcher is not available.");
            return dispatcher;
        }
    }
}
