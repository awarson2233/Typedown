using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorSelectionState
    {
        [JsonPropertyName("isTextSelected")]
        public bool IsTextSelected { get; init; }

        [JsonPropertyName("selectionText")]
        public string SelectionText { get; init; } = string.Empty;

        [JsonPropertyName("selectedImage")]
        public EditorSelectedImageState? SelectedImage { get; init; }

        [JsonIgnore]
        public bool HasSelectedImage => SelectedImage is not null;
    }
}
