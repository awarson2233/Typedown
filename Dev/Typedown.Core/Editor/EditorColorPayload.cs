using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Editor
{
    public sealed record EditorColorPayload(
        [property: JsonPropertyName("r")] int R,
        [property: JsonPropertyName("g")] int G,
        [property: JsonPropertyName("b")] int B,
        [property: JsonPropertyName("a")] double A)
    {
        [JsonPropertyName("R")]
        public int LegacyR => R;

        [JsonPropertyName("G")]
        public int LegacyG => G;

        [JsonPropertyName("B")]
        public int LegacyB => B;

        [JsonPropertyName("A")]
        public double LegacyA => A;
    }
}
