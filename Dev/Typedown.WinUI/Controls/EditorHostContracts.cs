using System.Text.Json;
using System.Text.Json.Serialization;

namespace Typedown.WinUI.Controls
{
    internal interface IEditorDocumentSession
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

    internal interface IEditorHostSink
    {
        bool Send(EditorHostMessage message);
    }

    internal sealed record EditorEventMessage(string Name, JsonElement? Args);

    public sealed record EditorHostMessage(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("args")] object Args);

    public static class EditorHostCommands
    {
        public static EditorHostMessage CreateLoadFile(EditorDocumentState state)
        {
            return new EditorHostMessage("LoadFile", new
            {
                text = state.Text,
                filePath = state.FilePath,
                basePath = state.BasePath
            });
        }

        public static EditorHostMessage CreateThemeChanged(EditorThemePayload payload)
        {
            return new EditorHostMessage("ThemeChanged", payload);
        }

        public static EditorHostMessage CreateSearch(EditorSearchRequest request)
        {
            return new EditorHostMessage("Search", new
            {
                value = request.Value,
                opt = new
                {
                    searchIsCaseSensitive = request.SearchIsCaseSensitive,
                    searchIsWholeWord = request.SearchIsWholeWord,
                    searchIsRegexp = request.SearchIsRegexp
                }
            });
        }

        public static EditorHostMessage CreateReplace(EditorReplaceRequest request)
        {
            return new EditorHostMessage("Replace", new
            {
                searchValue = request.SearchValue,
                value = request.Value,
                isSingle = request.IsSingle
            });
        }

        public static EditorHostMessage CreateSearchOpenChange(EditorSearchPanelState state)
        {
            return new EditorHostMessage("SearchOpenChange", new { open = (int)state });
        }

        public static EditorHostMessage CreateSettingsChanged(EditorSettingsChange change)
        {
            return new EditorHostMessage("SettingsChanged", new Dictionary<string, object?> { [change.Name] = change.Value });
        }

        public static EditorHostMessage CreateExport(EditorExportRequest request)
        {
            return new EditorHostMessage("Export", new
            {
                type = request.Type,
                context = request.Context,
                basePath = request.BasePath,
                title = request.Title,
                options = request.Options
            });
        }
    }

    public sealed record EditorPersistenceResult(
        bool Success,
        EditorDocumentState State,
        string? ErrorMessage = null,
        string? PersistedFilePath = null,
        bool IsCopy = false)
    {
        public string? Message => ErrorMessage;
    }

    public sealed record EditorDocumentState
    {
        public string Text { get; init; } = string.Empty;

        public string? FilePath { get; init; }

        public string BasePath { get; init; } = string.Empty;

        public string FileHash { get; init; } = string.Empty;

        public string CurrentHash { get; init; } = string.Empty;

        public bool IsLoaded { get; init; }

        public bool IsSaved { get; init; }

        public string LastEventName { get; init; } = string.Empty;
    }

    public sealed record EditorSettingsSnapshot(EditorSettingsPayload Payload);

    public sealed record EditorSettingsPayload
    {
        [JsonPropertyName("markdown")]
        public string Markdown { get; init; } = string.Empty;

        [JsonPropertyName("basePath")]
        public string BasePath { get; init; } = string.Empty;

        [JsonPropertyName("fontSize")]
        public int FontSize { get; init; } = 16;

        [JsonPropertyName("sequenceTheme")]
        public string SequenceTheme { get; init; } = "default";
    }

    public sealed record EditorThemePayload
    {
        [JsonPropertyName("theme")]
        public string Theme { get; init; } = "Light";

        [JsonPropertyName("accentColor")]
        public EditorColorPayload AccentColor { get; init; } = new(27, 102, 107, 1);

        [JsonPropertyName("background")]
        public EditorColorPayload Background { get; init; } = new(249, 249, 249, 1);
    }

    public sealed record EditorColorPayload(int R, int G, int B, double A);

    public sealed class EditorSearchRequest
    {
        public string Value { get; init; } = string.Empty;

        public bool SearchIsCaseSensitive { get; init; }

        public bool SearchIsWholeWord { get; init; }

        public bool SearchIsRegexp { get; init; }
    }

    public sealed class EditorReplaceRequest
    {
        public string SearchValue { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;

        public bool IsSingle { get; init; }
    }

    public enum EditorSearchPanelState
    {
        Closed = 0,
        Search = 1,
        Replace = 2
    }

    public sealed record EditorSettingsChange(string Name, object? Value);

    public sealed class EditorExportRequest
    {
        public string Type { get; init; } = string.Empty;

        public object? Context { get; init; }

        public string BasePath { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public object? Options { get; init; }
    }
}
