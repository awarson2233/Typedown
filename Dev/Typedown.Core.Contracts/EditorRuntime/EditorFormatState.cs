using System;
using System.Collections.Generic;
using System.Linq;

namespace Typedown.Core.Contracts.EditorRuntime
{
    public sealed record EditorSelectionFormat(string Type, string Tag);

    public sealed record EditorFormatState
    {
        public bool Bold { get; init; }

        public bool Italic { get; init; }

        public bool Underline { get; init; }

        public bool Strikethrough { get; init; }

        public bool Highlight { get; init; }

        public bool InlineCode { get; init; }

        public bool InlineMath { get; init; }

        public bool Hyperlink { get; init; }

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
