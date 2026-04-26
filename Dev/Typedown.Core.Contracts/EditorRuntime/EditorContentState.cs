using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorContentState
    {
        public EditorWordCount WordCount { get; init; } = new();

        public List<EditorTocItem> Toc { get; init; } = [];

        [JsonPropertyName("cur")]
        public EditorTocItem? Current { get; init; }

        [JsonIgnore]
        public EditorTocItem? Cur => Current;
    }
}
