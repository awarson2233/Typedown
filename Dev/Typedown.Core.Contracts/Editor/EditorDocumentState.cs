namespace Typedown.Core.Contracts.Editor
{
    public sealed record EditorDocumentState
    {
        public string Text { get; init; } = string.Empty;

        public string? FilePath { get; init; }

        public string BasePath { get; init; } = string.Empty;

        public string FileHash { get; init; } = string.Empty;

        public string CurrentHash { get; init; } = string.Empty;

        public bool IsLoaded { get; init; }

        public bool IsSaved { get; init; }

        public string LastEventName { get; init; } = "Waiting";
    }
}
