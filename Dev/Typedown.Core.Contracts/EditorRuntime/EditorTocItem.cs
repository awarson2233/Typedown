using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorTocItem
    {
        public string Slug { get; init; } = string.Empty;

        [JsonPropertyName("lvl")]
        public int Level { get; init; }

        [JsonIgnore]
        public int Lvl => Level;

        public string Content { get; init; } = string.Empty;

        public bool IsSelected { get; init; }
    }
}
