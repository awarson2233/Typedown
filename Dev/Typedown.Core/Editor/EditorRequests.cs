using System;

namespace Typedown.Core.Editor
{
    /// <summary>宿主发给引擎、需要结果的请求；结果类型随请求定死。</summary>
    public abstract record EditorRequest
    {
        private protected EditorRequest()
        {
        }
    }

    public abstract record EditorRequest<TResult> : EditorRequest
    {
        private protected EditorRequest()
        {
        }
    }

    /// <summary>
    /// 按当前正文生成一份独立的 HTML（导出或打印用）。
    /// <paramref name="BasePath"/> 非空时相对图片按它解析为绝对地址。
    /// </summary>
    public sealed record RenderExportHtml(
        ExportPurpose Purpose,
        string Title,
        string? BasePath,
        ExportHtmlOptions? Options) : EditorRequest<string>;

    /// <summary>
    /// 让引擎先把挂起的改动同步进 <see cref="IEditorSession.Document"/>，结果是同步后的正文版本。
    /// 手动保存、另存为、导出、关闭窗口前发出，之后读到的正文就是引擎此刻的正文。
    /// </summary>
    public sealed record FlushDocument : EditorRequest<long>;

    /// <summary>
    /// 页面坐标 (<paramref name="X"/>, <paramref name="Y"/>) 处的富文本上下文，右键菜单据此决定菜单项；
    /// 坐标不在正文上或处于源码模式时结果为 <c>null</c>。
    /// </summary>
    public sealed record ContextAt(double X, double Y) : EditorRequest<RichSelection?>;

    /// <summary>引擎对请求的应答是一个错误。<see cref="Code"/> 是线协议的错误码。</summary>
    public sealed class EditorRequestFailedException : Exception
    {
        public EditorRequestFailedException(EditorWireError code, string message)
            : base(message)
        {
            Code = code;
        }

        public EditorWireError Code { get; }
    }

    /// <summary>线协议应答的错误码，封闭集合；两端对应同一张表。</summary>
    public enum EditorWireError
    {
        /// <summary>对方不认识这个类型名。</summary>
        UnknownType,

        /// <summary>载荷缺必填字段或类型不对。</summary>
        InvalidPayload,

        /// <summary>页面在 ready 之前收到请求。</summary>
        NotReady,

        /// <summary>页面重载或宿主卸载，请求作废。</summary>
        Canceled,

        /// <summary>处理过程中抛出异常。</summary>
        Failed,
    }
}
