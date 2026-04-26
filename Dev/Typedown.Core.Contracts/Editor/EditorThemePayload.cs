using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Editor
{
    public sealed record EditorThemePayload
    {
        [JsonPropertyName("theme")]
        public string Theme { get; init; } = "Light";

        [JsonPropertyName("accentColor")]
        public EditorColorPayload AccentColor { get; init; } = new(27, 102, 107, 1);

        [JsonPropertyName("background")]
        public EditorColorPayload Background { get; init; } = new(249, 249, 249, 1);
    }
}
