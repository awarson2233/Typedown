using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorWordCount
    {
        [JsonPropertyName("word")]
        public int Word { get; init; }

        [JsonPropertyName("character")]
        public int Character { get; init; }
    }
}
