using System.Text.Json;
using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Editor
{
    public sealed record EditorEventMessage(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("args")] JsonElement? Args);
}
