using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Editor
{
    public sealed record EditorExportRequest
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "pdf";

        [JsonPropertyName("context")]
        public object? Context { get; init; }

        [JsonPropertyName("basePath")]
        public string? BasePath { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("options")]
        public object? Options { get; init; }
    }
}
