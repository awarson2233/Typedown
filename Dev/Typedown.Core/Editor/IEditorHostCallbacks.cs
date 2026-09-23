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
        /// <summary>引擎启动或重载时索取初始状态；首次调用会装载启动文档。</summary>
        Task<EditorStartup> PrepareStartupAsync(CancellationToken cancellationToken);

        /// <summary>让用户选择表格尺寸；取消时返回 <c>null</c>。</summary>
        Task<TableSize?> PickTableSizeAsync(CancellationToken cancellationToken);

        /// <summary>把引擎生成的复制内容一次性写入系统剪贴板。</summary>
        Task WriteClipboardAsync(ClipboardContent content, CancellationToken cancellationToken);
    }

    /// <summary>引擎启动时需要的宿主状态；正文取 <see cref="IEditorSession.Document"/>。</summary>
    public sealed record EditorStartup(EditorSettings Settings, string BasePath);
}
