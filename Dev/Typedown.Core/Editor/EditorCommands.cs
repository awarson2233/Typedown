using System.Collections.Generic;

namespace Typedown.Core.Editor
{
    /// <summary>
    /// 宿主发给引擎的命令。集合是封闭的：每个派生记录对应一种现有能力，
    /// 没有字符串寻址的万能入口；引擎适配器对每一种命令给出确定的翻译。
    /// </summary>
    public abstract record EditorCommand
    {
        private protected EditorCommand()
        {
        }
    }

    // ── doc ──────────────────────────────────────────────────────────────

    /// <summary>装载一篇文档；<paramref name="BasePath"/> 是相对图片路径的基准目录。</summary>
    public sealed record LoadDocument(string Text, string BasePath) : EditorCommand;

    /// <summary>把一段 HTML 转成 markdown 替换当前正文。</summary>
    public sealed record ImportHtml(string Html) : EditorCommand;

    // ── history ──────────────────────────────────────────────────────────

    public sealed record Undo : EditorCommand;

    public sealed record Redo : EditorCommand;

    /// <summary>清空撤销历史，以当前正文为新的起点。</summary>
    public sealed record ClearUndoHistory : EditorCommand;

    // ── selection / clipboard ────────────────────────────────────────────

    public sealed record SelectAll : EditorCommand;

    public sealed record DeleteSelection : EditorCommand;

    public sealed record Copy(CopyFormat Format) : EditorCommand;

    public sealed record Cut : EditorCommand;

    /// <summary>把宿主读到的剪贴板内容粘贴进正文。</summary>
    public sealed record Paste(PasteFormat Format, string Text, string Html) : EditorCommand;

    // ── format / block ───────────────────────────────────────────────────

    public sealed record ToggleInlineMark(InlineMark Mark) : EditorCommand;

    public sealed record ClearInlineMarks : EditorCommand;

    public sealed record SetBlockKind(BlockKind Kind) : EditorCommand;

    /// <summary>标题升一级（如 H2 → H1）。</summary>
    public sealed record PromoteHeading : EditorCommand;

    /// <summary>标题降一级（如 H1 → H2）。</summary>
    public sealed record DemoteHeading : EditorCommand;

    public sealed record InsertParagraph(ParagraphPosition Position) : EditorCommand;

    public sealed record DeleteParagraph : EditorCommand;

    public sealed record DuplicateParagraph : EditorCommand;

    /// <summary>宿主画的段落菜单已关闭。</summary>
    public sealed record BlockMenuClosed : EditorCommand;

    // ── table ────────────────────────────────────────────────────────────

    public sealed record InsertTable(int Rows, int Columns) : EditorCommand;

    public sealed record EditTable(TableEdit Edit) : EditorCommand;

    // ── image ────────────────────────────────────────────────────────────

    public sealed record InsertImage(string Src, string? Alt = null, string? Title = null) : EditorCommand;

    public sealed record ReplaceImage(ImageTarget Target, string Src, string? Alt, string? Title) : EditorCommand;

    public sealed record ApplyImageToolbarAction(ImageToolbarAction Action) : EditorCommand;

    /// <summary>把最近一次打开图片工具条的那张图缩放到 <paramref name="Percent"/>%。</summary>
    public sealed record SetImageZoom(int Percent) : EditorCommand;

    // ── search ───────────────────────────────────────────────────────────

    /// <summary>以 <paramref name="Query"/> 开始（或刷新）一次查找；<c>null</c> 表示清空查找词。</summary>
    public sealed record Search(string? Query, SearchOptions Options) : EditorCommand;

    public sealed record FindMatch(SearchDirection Direction) : EditorCommand;

    public sealed record Replace(string? Query, string Replacement, bool All, SearchOptions Options) : EditorCommand;

    /// <summary>查找栏已关闭，清除高亮。</summary>
    public sealed record EndSearch : EditorCommand;

    // ── outline ──────────────────────────────────────────────────────────

    public sealed record RevealHeading(string Id) : EditorCommand;

    // ── view ─────────────────────────────────────────────────────────────

    /// <summary>设置变更，只带变化的字段。</summary>
    public sealed record ApplySettings(EditorSettings Changes) : EditorCommand;

    public sealed record ApplyTheme(EditorTheme Theme) : EditorCommand;

    /// <summary>宿主当前认领的全部快捷键和弦；引擎拦下命中的按键并以 <see cref="ShortcutPressed"/> 回报。</summary>
    public sealed record SetKeymap(IReadOnlyList<KeyChord> Chords) : EditorCommand;

    /// <summary>把视口滚动到页面坐标 (<paramref name="X"/>, <paramref name="Y"/>)。</summary>
    public sealed record ScrollTo(double X, double Y) : EditorCommand;

    /// <summary>宿主尺寸或缩放变了，请引擎重报一次 <see cref="ViewportChanged"/>。</summary>
    public sealed record RefreshViewport : EditorCommand;
}
