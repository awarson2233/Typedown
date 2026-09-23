using System.Collections.Generic;
using Typedown.Core.Models;

namespace Typedown.Core.Editor
{
    // 命令、事件与请求共用的 markdown 语义词汇。只收录现有协议实际用到的取值，
    // 不认识 HTML 标签名、Muya token 名这类引擎内部词汇——那些翻译留在各引擎的适配器里。

    /// <summary>行内标记。</summary>
    public enum InlineMark
    {
        Strong,
        Emphasis,
        Underline,
        Strikethrough,
        Highlight,
        InlineCode,
        InlineMath,
        Link,
        Image,
    }

    /// <summary>
    /// 块的种类。既用于描述光标所在的块（<see cref="BlockContext"/>），也用于块转换（<see cref="SetBlockKind"/>）；
    /// <see cref="HtmlBlock"/> 与 <see cref="Table"/> 只会出现在前者。
    /// </summary>
    public enum BlockKind
    {
        Paragraph,
        Heading1,
        Heading2,
        Heading3,
        Heading4,
        Heading5,
        Heading6,
        CodeBlock,
        MathBlock,
        HtmlBlock,
        Quote,
        OrderedList,
        BulletList,
        TaskList,
        Table,
        Footnote,
        HorizontalRule,
        FrontMatter,
        VegaLiteChart,
        FlowChart,
        SequenceDiagram,
        PlantUmlDiagram,
        MermaidDiagram,
    }

    public enum ParagraphPosition
    {
        Before,
        After,
    }

    /// <summary>复制或剪切时写入剪贴板的形式。</summary>
    public enum CopyFormat
    {
        /// <summary>富文本与纯文本各一份。</summary>
        Rich,
        PlainText,
        Markdown,
        Html,
    }

    public enum PasteFormat
    {
        Rich,
        PlainText,
    }

    public enum TableAxis
    {
        Row,
        Column,
    }

    public enum TableEdit
    {
        InsertRowAbove,
        InsertRowBelow,
        RemoveRow,
        InsertColumnLeft,
        InsertColumnRight,
        RemoveColumn,
    }

    /// <summary>替换图片时作用于哪一张图。</summary>
    public enum ImageTarget
    {
        /// <summary>最近一次 <see cref="ImageEditorRequested"/> 打开编辑的那张图。</summary>
        Edited,

        /// <summary>当前选中的图。</summary>
        Selected,
    }

    public enum ImageToolbarAction
    {
        Edit,
        Inline,
        AlignLeft,
        AlignCenter,
        AlignRight,
        Delete,
    }

    public enum SearchDirection
    {
        Next,
        Previous,
    }

    public enum ExportPurpose
    {
        Export,
        Print,
    }

    /// <summary>页面坐标系（CSS px）下的矩形，宿主浮层据此摆放锚点。</summary>
    public readonly record struct EditorRect(double X, double Y, double Width, double Height);

    public sealed record ImageInfo(string Src, string Alt, string Title);

    public sealed record SearchOptions(bool CaseSensitive, bool WholeWord, bool Regex);

    public readonly record struct KeyChord(KeyboardKey Key, KeyboardModifiers Modifiers);

    public readonly record struct TableSize(int Rows, int Columns);

    public readonly record struct EditorColor(int R, int G, int B, double A);

    public sealed record EditorTheme(bool IsDark, EditorColor Accent, EditorColor Background);

    /// <summary>一次写入剪贴板的内容；两种格式可以只有其一。</summary>
    public sealed record ClipboardContent(string? PlainText, string? Html);

    /// <summary>引擎为标题分配的标识，宿主只原样带回（跳转到标题），不解析其内容。</summary>
    public readonly record struct HeadingId(string Value);

    public sealed record OutlineItem(HeadingId Id, int Level, string Text);

    /// <summary>引擎请求宿主显示的悬停提示。</summary>
    public enum TooltipKind
    {
        /// <summary>代码块右上角的复制按钮。</summary>
        CopyContent,

        /// <summary>链接：Ctrl + 单击打开。</summary>
        CtrlClickToOpenLink,

        /// <summary>表格工具条：调整表格尺寸。</summary>
        ResizeTable,

        AlignLeft,

        AlignCenter,

        AlignRight,

        DeleteTable,
    }

    /// <summary>光标所在块的语义描述，菜单的勾选与可用状态由它投影。</summary>
    public sealed record BlockContext(
        IReadOnlyList<BlockKind> Kinds,
        bool MultipleBlocks,
        bool CodeLike,
        bool CodeLine,
        bool BlockCommandsDisabled)
    {
        public static BlockContext Empty { get; } = new([], false, false, false, false);
    }

    /// <summary>富文本编辑模式下选区附带的上下文；源码模式没有。</summary>
    public sealed record RichSelection(BlockContext Block, ImageInfo? SelectedImage);

    /// <summary>编辑器相关设置。全部字段可空：整体下发时给全量，变更时只给变化的字段。</summary>
    public sealed record EditorSettings
    {
        public bool? SourceCode { get; init; }
        public bool? Typewriter { get; init; }
        public bool? FocusMode { get; init; }
        public bool? SearchIsCaseSensitive { get; init; }
        public bool? SearchIsRegexp { get; init; }
        public bool? SearchIsWholeWord { get; init; }
        public double? FontSize { get; init; }
        public double? LineHeight { get; init; }
        public bool? AutoPairBracket { get; init; }
        public bool? AutoPairQuote { get; init; }
        public bool? TrimUnnecessaryCodeBlockEmptyLines { get; init; }
        public bool? PreferLooseListItem { get; init; }
        public bool? AutoPairMarkdownSyntax { get; init; }
        public string? EditorAreaWidth { get; init; }
        public int? TabSize { get; init; }
        public bool? SpellcheckEnabled { get; init; }

        public static EditorSettings None { get; } = new();

        /// <summary>以 <paramref name="newer"/> 中非空的字段覆盖本对象，得到合并后的变更。</summary>
        public EditorSettings Merge(EditorSettings newer) => new()
        {
            SourceCode = newer.SourceCode ?? SourceCode,
            Typewriter = newer.Typewriter ?? Typewriter,
            FocusMode = newer.FocusMode ?? FocusMode,
            SearchIsCaseSensitive = newer.SearchIsCaseSensitive ?? SearchIsCaseSensitive,
            SearchIsRegexp = newer.SearchIsRegexp ?? SearchIsRegexp,
            SearchIsWholeWord = newer.SearchIsWholeWord ?? SearchIsWholeWord,
            FontSize = newer.FontSize ?? FontSize,
            LineHeight = newer.LineHeight ?? LineHeight,
            AutoPairBracket = newer.AutoPairBracket ?? AutoPairBracket,
            AutoPairQuote = newer.AutoPairQuote ?? AutoPairQuote,
            TrimUnnecessaryCodeBlockEmptyLines = newer.TrimUnnecessaryCodeBlockEmptyLines ?? TrimUnnecessaryCodeBlockEmptyLines,
            PreferLooseListItem = newer.PreferLooseListItem ?? PreferLooseListItem,
            AutoPairMarkdownSyntax = newer.AutoPairMarkdownSyntax ?? AutoPairMarkdownSyntax,
            EditorAreaWidth = newer.EditorAreaWidth ?? EditorAreaWidth,
            TabSize = newer.TabSize ?? TabSize,
            SpellcheckEnabled = newer.SpellcheckEnabled ?? SpellcheckEnabled,
        };
    }

    /// <summary>导出 HTML 时引擎需要的附加内容。</summary>
    public sealed record ExportHtmlOptions(string? ExtraHead, string? ExtraBody, string? Header, string? Footer);

    /// <summary>待落盘或上传的图片从哪里来。</summary>
    public enum ImageSourceKind
    {
        /// <summary>拖入的本地文件，<see cref="ImageSource.Value"/> 是文件路径。</summary>
        FilePath,

        /// <summary>剪贴板位图，<see cref="ImageSource.Value"/> 是 <c>data:</c> URL。</summary>
        DataUrl,

        /// <summary>粘贴的网络图片，<see cref="ImageSource.Value"/> 是 http(s) 地址。</summary>
        WebUrl,
    }

    public sealed record ImageSource(ImageSourceKind Kind, string Value);

    /// <summary>
    /// 引擎页面启动时同步读取的初始态：全量设置、主题、快捷键表与界面语言代码，
    /// 由会话在导航前注入，页面启动不需要往返。
    /// </summary>
    public sealed record EditorInitState(
        int Protocol,
        EditorSettings Settings,
        EditorTheme Theme,
        IReadOnlyList<KeyChord> Keymap,
        string Locale);
}
