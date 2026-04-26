using System.Threading.Tasks;

namespace Typedown.Core.Interfaces
{
    public interface IEditorBridge
    {
        bool Send(string name, object args);

        Task ReceiveAsync(string json);
    }
}
