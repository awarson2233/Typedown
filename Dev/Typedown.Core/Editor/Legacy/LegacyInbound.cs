using System.Collections.Generic;
using System.Text.Json;
using Typedown.Core.Models;

namespace Typedown.Core.Editor.Legacy
{
    /// <summary>页面发来的一条原始报文（invoke / message / diffmsg 三种信封共用）。</summary>
    public sealed record LegacyEnvelope(string Type, string Id, string Name, JsonElement Args, bool Diff, int Start, int End);

    /// <summary>
    /// 旧协议消息解码后的结果。能直接对应类型化事件的包成 <see cref="LegacyTypedEvent"/>；
    /// 其余几种还牵涉适配器自身的状态（正文镜像、撤销历史、查找选区、图片样式），由适配器先处理再转成事件。
    /// </summary>
    public abstract record LegacyInbound
    {
        private protected LegacyInbound()
        {
        }
    }

    public sealed record LegacyTypedEvent(EditorEvent Event) : LegacyInbound;

    /// <summary>MarkdownChange：正文变了。</summary>
    public sealed record LegacyTextChanged(string Text) : LegacyInbound;

    /// <summary>FileLoaded：页面装载完正文后的回声。</summary>
    public sealed record LegacyFileLoaded(string Text) : LegacyInbound;

    /// <summary>CursorChange：只喂给撤销历史。</summary>
    public sealed record LegacyCursorChanged(CursorState Cursor) : LegacyInbound;

    /// <summary>StateChange：字数与目录；无论载荷是否完整都标志着一轮回灌结束。</summary>
    public sealed record LegacyStateChanged(IReadOnlyList<EditorEvent> Events) : LegacyInbound;

    /// <summary>
    /// SelectionChange / CodeMirrorSelectionChange。<paramref name="PageSelection"/> 是页面自己的选区对象，
    /// 发起查找时要原样带回去。
    /// </summary>
    public sealed record LegacySelectionChanged(SelectionChanged Event, JsonElement PageSelection, bool SourceMode) : LegacyInbound;

    /// <summary>OpenImageToolbar：<paramref name="Style"/> 是图片当前的 style 属性，缩放时要在它上面改。</summary>
    public sealed record LegacyImageToolbarOpened(ImageToolbarRequested Event, string? Style) : LegacyInbound;
}
