using System.Collections.Generic;

namespace Typedown.Core.Editor.Wire
{
    // 只存在于线上的载荷与应答：正文同步、生命周期与回问宿主的请求。契约 record 能直接充当载荷的
    // 都不在这里（见 EditorWireTypes）；这里的类型由会话自己消化，不暴露给 ViewModel。

    // ── 宿主 → 页面 ─────────────────────────────────────────────────────

    /// <summary>
    /// <c>doc.load</c>：整篇替换正文并清空撤销历史。<paramref name="Version"/> 由 <see cref="DocumentMirror.Load"/> 分配；
    /// 崩溃恢复时带上崩溃前的选区与滚动位置。
    /// </summary>
    public sealed record DocLoad(long Version, string Text, string BasePath, DocSelection? Selection = null, double? ScrollTop = null);

    /// <summary>正文里的一个选区，UTF-16 码元偏移。</summary>
    public readonly record struct DocSelection(int Anchor, int Head);

    /// <summary><c>doc.getText</c>：取页面当前全文与版本号，应答 <see cref="DocText"/>。</summary>
    public sealed record DocGetText;

    // ── 页面 → 宿主：会话自用的事件 ──────────────────────────────────────

    /// <summary>页面发来、由会话自己处理而不直接外发给 ViewModel 的事件。</summary>
    public abstract record EditorWireSignal
    {
        private protected EditorWireSignal()
        {
        }
    }

    /// <summary><c>lifecycle.ready</c>：页面已挂载完、能接收 <c>doc.load</c>。</summary>
    public sealed record LifecycleReady(int Protocol, string Engine) : EditorWireSignal;

    /// <summary><c>lifecycle.fault</c>：页面未捕获的异常；<paramref name="Fatal"/> 为真时编辑器状态不可信，需要重载。</summary>
    public sealed record LifecycleFault(string Message, string? Stack, bool Fatal) : EditorWireSignal;

    /// <summary>
    /// <c>doc.changed</c>：一帧内的正文改动。<paramref name="Changes"/> 按 <see cref="DocChange.From"/> 升序、互不重叠，
    /// 偏移都相对于 <paramref name="BaseVersion"/> 的正文；<paramref name="Version"/> = <paramref name="BaseVersion"/> + 1。
    /// </summary>
    public sealed record DocChanged(long BaseVersion, long Version, IReadOnlyList<DocChange> Changes) : EditorWireSignal;

    /// <summary>把 [<paramref name="From"/>, <paramref name="To"/>) 替换为 <paramref name="Insert"/>。</summary>
    public readonly record struct DocChange(int From, int To, string Insert);

    /// <summary>
    /// <c>selection.changed</c>：契约 <see cref="SelectionChanged"/> 之外多带正文偏移，会话留存最后一份供崩溃恢复，
    /// 对外只发 <see cref="ToEvent"/>。
    /// </summary>
    public sealed record SelectionReport(bool HasText, string Text, RichSelection? Rich, int Anchor, int Head) : EditorWireSignal
    {
        public SelectionChanged ToEvent() => new(HasText, Text, Rich);

        public DocSelection ToSelection() => new(Anchor, Head);
    }

    // ── 页面 → 宿主：请求 ───────────────────────────────────────────────

    /// <summary>页面回问宿主的请求，由会话调 <see cref="IEditorHostCallbacks"/> 的对应方法应答。</summary>
    public abstract record EditorWireHostRequest
    {
        private protected EditorWireHostRequest()
        {
        }
    }

    /// <summary><c>table.pickSize</c>：弹建表尺寸对话框，应答 <see cref="TableSize"/> 或 <c>null</c>（用户取消）。</summary>
    public sealed record TablePickSize : EditorWireHostRequest;

    /// <summary><c>clipboard.write</c>：把页面生成的复制内容写入剪贴板，应答 <c>{}</c>。</summary>
    public sealed record ClipboardWrite(string? PlainText, string? Html) : EditorWireHostRequest
    {
        public ClipboardContent ToContent() => new(PlainText, Html);
    }

    /// <summary><c>image.resolve</c>：粘贴或拖入的图片由宿主落盘或上传，应答 <see cref="ImageResolved"/> 或 <c>null</c>（放弃插入）。</summary>
    public sealed record ImageResolve(ImageSource Source) : EditorWireHostRequest;

    // ── 应答载荷 ────────────────────────────────────────────────────────

    /// <summary><c>doc.flush</c> 的应答。</summary>
    public sealed record DocVersion(long Version);

    /// <summary><c>doc.getText</c> 的应答。</summary>
    public sealed record DocText(long Version, string Text);

    /// <summary><c>export.renderHtml</c> 的应答。</summary>
    public sealed record ExportHtml(string Html);

    /// <summary><c>image.resolve</c> 的应答。</summary>
    public sealed record ImageResolved(string Src);

    /// <summary>无内容的应答（<c>clipboard.write</c>）。</summary>
    public sealed record WireEmpty;

    /// <summary>失败应答里的 <c>err</c>。</summary>
    public sealed record WireErrorBody(EditorWireError Code, string? Message);
}
