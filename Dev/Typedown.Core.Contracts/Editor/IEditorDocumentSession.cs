using System.Text.Json;

namespace Typedown.Core.Contracts.Editor
{
    public interface IEditorDocumentSession
    {
        EditorDocumentState State { get; }

        EditorSettingsSnapshot SettingsSnapshot { get; }

        void HandleEditorEvent(EditorEventMessage message);

        object? HandleRemoteInvoke(string name, JsonElement? args);
    }
}
