using System.Collections.Generic;
using Typedown.Core.Models;

namespace Typedown.Core.Editor
{
    /// <summary>引擎发给宿主的事件，集合同样是封闭的。</summary>
    public abstract record EditorEvent
    {
        private protected EditorEvent()
        {
        }
    }

    // ── doc ──────────────────────────────────────────────────────────────

    /// <summary>正文变了（用户编辑、撤销、重做）；<see cref="IEditorSession.Document"/> 已同步更新。</summary>
    public sealed record DocumentChanged(string Text) : EditorEvent;

    /// <summary>引擎装载完一篇文档，<paramref name="Text"/> 是它装载后的正文（可能被引擎归一化过）。</summary>
    public sealed record DocumentLoaded(string Text) : EditorEvent;

    // ── history ──────────────────────────────────────────────────────────

    public sealed record HistoryChanged(bool CanUndo, bool CanRedo) : EditorEvent;

    // ── selection ────────────────────────────────────────────────────────

    /// <summary>
    /// 选区变了。<paramref name="Rich"/> 为 <c>null</c> 表示源码模式，没有块与图片上下文。
    /// </summary>
    public sealed record SelectionChanged(bool HasText, string Text, RichSelection? Rich) : EditorEvent;

    /// <summary>选区覆盖的行内标记。</summary>
    public sealed record MarksChanged(IReadOnlyList<InlineMark> Marks) : EditorEvent;

    // ── outline / stats ──────────────────────────────────────────────────

    public sealed record OutlineChanged(IReadOnlyList<OutlineItem> Items, OutlineItem? Current) : EditorEvent;

    public sealed record StatsChanged(int Characters, int Words) : EditorEvent;

    // ── view ─────────────────────────────────────────────────────────────

    /// <summary>视口尺寸、可滚动量与当前位置，单位是页面坐标（CSS px）。</summary>
    public sealed record ViewportChanged(
        double ViewportWidth,
        double ViewportHeight,
        double MaximumX,
        double MaximumY,
        double ScrollX,
        double ScrollY) : EditorEvent;

    /// <summary>引擎拦下了一个 <see cref="SetKeymap"/> 里的和弦。</summary>
    public sealed record ShortcutPressed(KeyboardKey Key, KeyboardModifiers Modifiers) : EditorEvent;

    public sealed record LinkOpenRequested(string Uri) : EditorEvent;

    // ── 宿主浮层 ─────────────────────────────────────────────────────────
    // 锚点缺省时宿主退回到编辑区自身。

    public sealed record BlockMenuRequested(EditorRect? Anchor) : EditorEvent;

    public sealed record FormatPickerRequested(EditorRect? Anchor) : EditorEvent;

    public sealed record ImageEditorRequested(EditorRect? Anchor, ImageInfo Image) : EditorEvent;

    public sealed record ImageToolbarRequested(EditorRect? Anchor) : EditorEvent;

    public sealed record TableToolsRequested(EditorRect? Anchor, TableAxis Axis) : EditorEvent;

    /// <summary><paramref name="ResourceKey"/> 为 <c>null</c> 表示关闭提示。</summary>
    public sealed record TooltipRequested(string? ResourceKey, EditorRect? Anchor) : EditorEvent;
}
