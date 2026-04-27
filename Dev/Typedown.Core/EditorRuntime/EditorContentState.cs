using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorContentState
    {
        [JsonPropertyName("wordCount")]
        public EditorWordCount WordCount { get; init; } = new();

        [JsonPropertyName("toc")]
        public List<EditorTocItem> Toc { get; init; } = [];

        [JsonPropertyName("cur")]
        public EditorTocItem? Current { get; init; }

        [JsonIgnore]
        public EditorTocItem? Cur => Current;
    }
}
