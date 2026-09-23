using System.Collections.Generic;
using System.Linq;

namespace Typedown.Core.Editor.Legacy
{
    /// <summary>markdown 语义词汇与 Muya 内部词汇（格式名、段落类型名、HTML 标签名）之间的双向翻译。</summary>
    public static class LegacyMuyaVocabulary
    {
        public const string ClearFormatName = "clear";

        public const string PromoteHeadingName = "upgrade heading";

        public const string DemoteHeadingName = "degrade heading";

        private static readonly IReadOnlyDictionary<InlineMark, string> formatNames = new Dictionary<InlineMark, string>
        {
            [InlineMark.Strong] = "strong",
            [InlineMark.Emphasis] = "em",
            [InlineMark.Underline] = "u",
            [InlineMark.Strikethrough] = "del",
            [InlineMark.Highlight] = "mark",
            [InlineMark.InlineCode] = "inline_code",
            [InlineMark.InlineMath] = "inline_math",
            [InlineMark.Link] = "link",
            [InlineMark.Image] = "image",
        };

        private static readonly IReadOnlyDictionary<BlockKind, string> paragraphNames = new Dictionary<BlockKind, string>
        {
            [BlockKind.Paragraph] = "paragraph",
            [BlockKind.Heading1] = "heading 1",
            [BlockKind.Heading2] = "heading 2",
            [BlockKind.Heading3] = "heading 3",
            [BlockKind.Heading4] = "heading 4",
            [BlockKind.Heading5] = "heading 5",
            [BlockKind.Heading6] = "heading 6",
            [BlockKind.CodeBlock] = "pre",
            [BlockKind.MathBlock] = "mathblock",
            [BlockKind.Quote] = "blockquote",
            [BlockKind.OrderedList] = "ol-order",
            [BlockKind.BulletList] = "ul-bullet",
            [BlockKind.TaskList] = "ul-task",
            [BlockKind.Footnote] = "footnote",
            [BlockKind.HorizontalRule] = "hr",
            [BlockKind.FrontMatter] = "front-matter",
            [BlockKind.VegaLiteChart] = "vega-lite",
            [BlockKind.FlowChart] = "flowchart",
            [BlockKind.SequenceDiagram] = "sequence",
            [BlockKind.PlantUmlDiagram] = "plantuml",
            [BlockKind.MermaidDiagram] = "mermaid",
        };

        public static string GetFormatName(InlineMark mark) => formatNames[mark];

        public static bool TryParseFormatName(string? name, out InlineMark mark)
        {
            foreach (var pair in formatNames)
            {
                if (pair.Value == name)
                {
                    mark = pair.Key;
                    return true;
                }
            }

            mark = default;
            return false;
        }

        /// <summary>Muya 只能把块转换成这些种类；<see cref="BlockKind.HtmlBlock"/> 与 <see cref="BlockKind.Table"/> 没有对应的转换名。</summary>
        public static bool TryGetParagraphName(BlockKind kind, out string name) => paragraphNames.TryGetValue(kind, out name!);

        public static bool TryParseParagraphName(string? name, out BlockKind kind)
        {
            foreach (var pair in paragraphNames)
            {
                if (pair.Value == name)
                {
                    kind = pair.Key;
                    return true;
                }
            }

            kind = default;
            return false;
        }

        /// <summary>
        /// 由 Muya 的 selectionFormats（token 类型 + HTML 标签）推出行内标记。
        /// 类型与标签按集合分别判断，保持此前 FormatState 的判定方式。
        /// </summary>
        public static IReadOnlyList<InlineMark> ToInlineMarks(IEnumerable<(string? Type, string? Tag)> formats)
        {
            var list = formats.ToList();
            var types = list.Select(x => x.Type).ToHashSet();
            var tags = list.Select(x => x.Tag).ToHashSet();
            var marks = new List<InlineMark>();
            if (types.Contains("strong")) marks.Add(InlineMark.Strong);
            if (types.Contains("em")) marks.Add(InlineMark.Emphasis);
            if (types.Contains("html_tag") && tags.Contains("u")) marks.Add(InlineMark.Underline);
            if (types.Contains("del")) marks.Add(InlineMark.Strikethrough);
            if (types.Contains("html_tag") && tags.Contains("mark")) marks.Add(InlineMark.Highlight);
            if (types.Contains("inline_code")) marks.Add(InlineMark.InlineCode);
            if (types.Contains("inline_math")) marks.Add(InlineMark.InlineMath);
            if (types.Contains("link")) marks.Add(InlineMark.Link);
            if (types.Contains("image") || tags.Contains("img")) marks.Add(InlineMark.Image);
            return marks;
        }

        /// <summary>
        /// 由页面 menuState（以 HTML 标签名与 Muya functionType 为键的 affiliation 加若干开关）推出块上下文。
        /// </summary>
        public static BlockContext ToBlockContext(
            IReadOnlyCollection<string> affiliation,
            bool isTaskList,
            bool isTable,
            bool isFootnote,
            bool isCodeFences,
            bool isCodeContent,
            bool isMultiline,
            bool isDisabled)
        {
            var kinds = new List<BlockKind>();
            bool Has(string key) => affiliation.Contains(key);

            if (Has("p")) kinds.Add(BlockKind.Paragraph);
            if (Has("h1")) kinds.Add(BlockKind.Heading1);
            if (Has("h2")) kinds.Add(BlockKind.Heading2);
            if (Has("h3")) kinds.Add(BlockKind.Heading3);
            if (Has("h4")) kinds.Add(BlockKind.Heading4);
            if (Has("h5")) kinds.Add(BlockKind.Heading5);
            if (Has("h6")) kinds.Add(BlockKind.Heading6);
            if (isCodeFences && affiliation.Any(x => x.Contains("code"))) kinds.Add(BlockKind.CodeBlock);
            if (Has("multiplemath")) kinds.Add(BlockKind.MathBlock);
            if (Has("html")) kinds.Add(BlockKind.HtmlBlock);
            if (Has("blockquote")) kinds.Add(BlockKind.Quote);
            if (Has("ol")) kinds.Add(BlockKind.OrderedList);
            if (Has("ul")) kinds.Add(isTaskList ? BlockKind.TaskList : BlockKind.BulletList);
            if (isTable) kinds.Add(BlockKind.Table);
            if (isFootnote) kinds.Add(BlockKind.Footnote);
            if (Has("hr")) kinds.Add(BlockKind.HorizontalRule);
            if (Has("frontmatter")) kinds.Add(BlockKind.FrontMatter);

            return new BlockContext(kinds, isMultiline, isCodeFences, isCodeContent, isDisabled);
        }

        public static string GetCopyTypeName(CopyFormat format) => format switch
        {
            CopyFormat.PlainText => "copyAsPlainText",
            CopyFormat.Markdown => "copyAsMarkdown",
            CopyFormat.Html => "copyAsHtml",
            _ => "normal",
        };

        public static string GetPasteTypeName(PasteFormat format) => format == PasteFormat.PlainText ? "pasteAsPlainText" : "normal";

        public static (string Action, string Location, string Target) GetTableEdit(TableEdit edit) => edit switch
        {
            TableEdit.InsertRowAbove => ("insert", "previous", "row"),
            TableEdit.InsertRowBelow => ("insert", "next", "row"),
            TableEdit.RemoveRow => ("remove", "current", "row"),
            TableEdit.InsertColumnLeft => ("insert", "left", "column"),
            TableEdit.InsertColumnRight => ("insert", "right", "column"),
            _ => ("remove", "current", "column"),
        };

        public static string GetImageToolbarActionName(ImageToolbarAction action) => action switch
        {
            ImageToolbarAction.Edit => "edit",
            ImageToolbarAction.Inline => "inline",
            ImageToolbarAction.AlignLeft => "left",
            ImageToolbarAction.AlignCenter => "center",
            ImageToolbarAction.AlignRight => "right",
            _ => "delete",
        };

        /// <summary>在图片原有的 style 上替换 zoom 声明。</summary>
        public static string ComposeZoomStyle(string? currentStyle, int percent)
        {
            var declarations = (currentStyle ?? string.Empty)
                .Split(';')
                .Where(x => !x.StartsWith("zoom:") && !string.IsNullOrWhiteSpace(x))
                .ToList();
            declarations.Add($"zoom:{percent}%");
            return $"{string.Join(';', declarations)};";
        }
    }
}
