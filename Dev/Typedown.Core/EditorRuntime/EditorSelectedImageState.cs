using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorSelectedImageState
    {
        [JsonPropertyName("src")]
        public string Src { get; init; } = string.Empty;

        [JsonPropertyName("alt")]
        public string Alt { get; init; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;
    }
}
