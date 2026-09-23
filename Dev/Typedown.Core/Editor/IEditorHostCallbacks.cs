using System.Threading;
using System.Threading.Tasks;

namespace Typedown.Core.Editor
{
    /// <summary>
    /// 引擎向宿主发起、需要宿主应答的请求，由 Core 实现、编辑会话在收到请求时调用。
    /// 会话在调用时才解析实现，所以不存在「处理器还没注册」的时序约束。
    /// </summary>
    public interface IEditorHostCallbacks
    {
        /// <summary>相对图片路径的基准目录；随打开、另存为、重命名变化，会话整篇回灌正文时读取。</summary>
        string BasePath { get; }

        /// <summary>
        /// 引擎启动或重载时索取初始状态，返回全量编辑器设置；首次调用会装载启动文档。
        /// 正文取 <see cref="IEditorSession.Document"/>，基准目录取 <see cref="BasePath"/>。
        /// </summary>
        Task<EditorSettings> PrepareStartupAsync(CancellationToken cancellationToken);

        /// <summary>让用户选择表格尺寸；取消时返回 <c>null</c>。</summary>
        Task<TableSize?> PickTableSizeAsync(CancellationToken cancellationToken);

        /// <summary>把引擎生成的复制内容写入系统剪贴板。</summary>
        Task WriteClipboardAsync(ClipboardContent content, CancellationToken cancellationToken);

        /// <summary>
        /// 粘贴或拖入的图片按设置复制到本地目录或上传，返回写进正文的地址；返回 <c>null</c> 表示放弃插入。
        /// </summary>
        Task<string?> ResolveImageAsync(ImageSource source, CancellationToken cancellationToken);
    }
}
