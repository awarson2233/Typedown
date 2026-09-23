using System.Collections.Generic;
using System.Linq;
using Typedown.Core.Editor;

namespace Typedown.Core.Models
{
    /// <summary>选区覆盖的行内标记，供格式菜单勾选。</summary>
    public class FormatState
    {
        public FormatState(IEnumerable<InlineMark>? marks = null)
        {
            if (marks == null)
                return;
            var set = marks.ToHashSet();
            Bold = set.Contains(InlineMark.Strong);
            Italic = set.Contains(InlineMark.Emphasis);
            Underline = set.Contains(InlineMark.Underline);
            InlineCode = set.Contains(InlineMark.InlineCode);
            InlineMath = set.Contains(InlineMark.InlineMath);
            Highlight = set.Contains(InlineMark.Highlight);
            Strikethrough = set.Contains(InlineMark.Strikethrough);
            Hyperlink = set.Contains(InlineMark.Link);
            Image = set.Contains(InlineMark.Image);
        }

        public bool Bold { get; }

        public bool Italic { get; }

        public bool Underline { get; }

        public bool Strikethrough { get; }

        public bool Highlight { get; }

        public bool InlineCode { get; }

        public bool InlineMath { get; }

        public bool Hyperlink { get; }

        public bool Image { get; }
    }
}
