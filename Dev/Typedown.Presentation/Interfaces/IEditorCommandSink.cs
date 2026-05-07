namespace Typedown.Presentation.Interfaces
{
    public interface IEditorCommandSink
    {
        bool Send(string name, object? args);
    }
}
