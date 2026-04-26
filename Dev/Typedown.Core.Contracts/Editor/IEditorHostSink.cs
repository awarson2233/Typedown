namespace Typedown.Core.Contracts.Editor
{
    public interface IEditorHostSink
    {
        bool Send(EditorHostMessage message);
    }
}
