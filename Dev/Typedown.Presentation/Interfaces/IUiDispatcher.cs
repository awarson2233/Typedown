using System;
using System.Threading.Tasks;

namespace Typedown.Presentation.Interfaces
{
    public interface IUiDispatcher
    {
        Task RunAsync(Action action);

        Task<T> RunAsync<T>(Func<T> action);

        Task RunIdleAsync(Action action);

        Task<T> RunIdleAsync<T>(Func<T> action);
    }
}
