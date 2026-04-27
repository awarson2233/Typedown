using System.Text.Json;

namespace Typedown.Core.Contracts.Editor
{
    public interface IEditorDocumentSession
    {
        EditorDocumentState State { get; }

        EditorSettingsSnapshot SettingsSnapshot { get; }

        EditorPersistenceResult LoadFile(string filePath);

        EditorPersistenceResult ReplaceFileText(string text, string? filePath = null, string? basePath = null);

        EditorPersistenceResult Save();

        EditorPersistenceResult SaveAs(string filePath, bool saveCopy = false);

        void HandleEditorEvent(EditorEventMessage message);

        object? HandleRemoteInvoke(string name, JsonElement? args);
    }
}
