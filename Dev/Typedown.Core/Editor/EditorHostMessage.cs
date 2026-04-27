namespace Typedown.Core.Contracts.Editor
{
    using System.Text.Json.Serialization;

    public sealed record EditorHostMessage(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("args")] object Args);
}
