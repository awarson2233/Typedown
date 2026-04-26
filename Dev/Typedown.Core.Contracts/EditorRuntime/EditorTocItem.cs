using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorTocItem
    {
        [JsonConverter(typeof(EditorSlugJsonConverter))]
        [JsonPropertyName("slug")]
        public string Slug { get; init; } = string.Empty;

        [JsonPropertyName("lvl")]
        public int Level { get; init; }

        [JsonIgnore]
        public int Lvl => Level;

        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;

        [JsonPropertyName("isSelected")]
        public bool IsSelected { get; init; }
    }
}
