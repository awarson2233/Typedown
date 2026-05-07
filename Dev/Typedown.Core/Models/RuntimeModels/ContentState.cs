using System.Collections.Generic;

namespace Typedown.Core.Models
{
    public class ContentState
    {
        public WordCount WordCount { get; set; } = new();

        public List<TocItem> Toc { get; set; } = new();

        public TocItem? Cur { get; set; }
    }
}
