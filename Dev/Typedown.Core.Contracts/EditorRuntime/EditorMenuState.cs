using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorMenuState
    {
        [JsonPropertyName("isDisabled")]
        public bool IsDisabled { get; set; }

        [JsonPropertyName("isMultiline")]
        public bool IsMultiline { get; set; }

        [JsonPropertyName("isLooseListItem")]
        public bool IsLooseListItem { get; set; }

        [JsonPropertyName("isTaskList")]
        public bool IsTaskList { get; set; }

        [JsonPropertyName("isCodeFences")]
        public bool IsCodeFences { get; set; }

        [JsonPropertyName("isCodeContent")]
        public bool IsCodeContent { get; set; }

        [JsonPropertyName("isTable")]
        public bool IsTable { get; set; }

        [JsonPropertyName("isFootnote")]
        public bool IsFootnote { get; set; }

        [JsonPropertyName("affiliation")]
        public Dictionary<string, bool> Affiliation { get; set; } = [];
    }
}
