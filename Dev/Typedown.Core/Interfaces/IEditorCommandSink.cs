namespace Typedown.Core.Interfaces
{
    public interface IEditorCommandSink
    {
        bool Send(string name, object? args);
    }
}
