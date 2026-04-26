using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorSelectionFormat(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("tag")] string Tag);

    public sealed record EditorFormatState
    {
        [JsonPropertyName("bold")]
        public bool Bold { get; init; }

        [JsonPropertyName("italic")]
        public bool Italic { get; init; }

        [JsonPropertyName("underline")]
        public bool Underline { get; init; }

        [JsonPropertyName("strikethrough")]
        public bool Strikethrough { get; init; }

        [JsonPropertyName("highlight")]
        public bool Highlight { get; init; }

        [JsonPropertyName("inlineCode")]
        public bool InlineCode { get; init; }

        [JsonPropertyName("inlineMath")]
        public bool InlineMath { get; init; }

        [JsonPropertyName("hyperlink")]
        public bool Hyperlink { get; init; }

        [JsonPropertyName("image")]
        public bool Image { get; init; }

        public static EditorFormatState FromSelectionFormats(IEnumerable<EditorSelectionFormat>? selectionFormats)
        {
            if (selectionFormats is null)
            {
                return new EditorFormatState();
            }

            var formats = selectionFormats.ToArray();
            var types = formats.Select(format => format.Type).ToHashSet(StringComparer.Ordinal);
            var tags = formats.Select(format => format.Tag).ToHashSet(StringComparer.Ordinal);

            return new EditorFormatState
            {
                Bold = types.Contains("strong"),
                Italic = types.Contains("em"),
                Underline = types.Contains("html_tag") && tags.Contains("u"),
                InlineCode = types.Contains("inline_code"),
                InlineMath = types.Contains("inline_math"),
                Highlight = types.Contains("html_tag") && tags.Contains("mark"),
                Strikethrough = types.Contains("del"),
                Hyperlink = types.Contains("link"),
                Image = types.Contains("image") || tags.Contains("img"),
            };
        }
    }
}
