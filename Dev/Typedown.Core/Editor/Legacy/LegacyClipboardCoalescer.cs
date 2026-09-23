using System.Collections.Generic;

namespace Typedown.Core.Editor.Legacy
{
    /// <summary>
    /// 页面复制时对同一份内容连发两次 SetClipboard：先 text/html，再 text/plain。
    /// 逐条写剪贴板会让后一次覆盖前一次，富文本就丢了；这里把相邻的一对合并成一次多格式写入。
    /// 空的 HTML（「复制为纯文本 / Markdown / HTML 代码」时页面发的占位）不写。
    /// </summary>
    public sealed class LegacyClipboardCoalescer
    {
        public const string HtmlType = "text/html";

        public const string PlainTextType = "text/plain";

        private string? pendingHtml;

        public bool HasPending => pendingHtml is not null;

        /// <summary>收下一条 SetClipboard，返回此刻应当写入剪贴板的内容（可能为空）。</summary>
        public IReadOnlyList<ClipboardContent> Accept(string? type, string data)
        {
            switch (type)
            {
                case HtmlType:
                    var flushed = Flush();
                    pendingHtml = data;
                    return flushed;
                case PlainTextType:
                    var html = pendingHtml;
                    pendingHtml = null;
                    return [new ClipboardContent(data, string.IsNullOrEmpty(html) ? null : html)];
                default:
                    return [];
            }
        }

        /// <summary>没有等到配对的纯文本：单独写出积压的 HTML。</summary>
        public IReadOnlyList<ClipboardContent> Flush()
        {
            var html = pendingHtml;
            pendingHtml = null;
            return string.IsNullOrEmpty(html) ? [] : [new ClipboardContent(null, html)];
        }
    }
}
