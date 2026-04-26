using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Editor
{
    public sealed record EditorUiCommand(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("args")] object? Args);
}
