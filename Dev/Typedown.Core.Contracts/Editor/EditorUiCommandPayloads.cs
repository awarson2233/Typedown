using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Editor
{
    public sealed record EditorFindActionRequest(
        [property: JsonPropertyName("action")] string Action);

    public sealed record EditorParagraphCommandRequest(
        [property: JsonPropertyName("type")] string Type);

    public sealed record EditorClipboardCommandRequest(
        [property: JsonPropertyName("type")] string Type);

    public sealed record EditorPasteCommandRequest(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("html")] string Html);

    public sealed record EditorInsertTableRequest(
        [property: JsonPropertyName("rows")] int Rows,
        [property: JsonPropertyName("columns")] int Columns);

    public sealed record EditorSearchOpenChangeRequest(
        [property: JsonPropertyName("open")] int Open);

    public sealed record EditorScrollToRequest(
        [property: JsonPropertyName("slug")] string Slug);
}
