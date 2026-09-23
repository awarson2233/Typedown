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
}
