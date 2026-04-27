using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorMenuItemState
    {
        [JsonPropertyName("isEnable")]
        public bool IsEnable { get; set; }

        [JsonPropertyName("isChecked")]
        public bool IsChecked { get; set; }
    }
}
