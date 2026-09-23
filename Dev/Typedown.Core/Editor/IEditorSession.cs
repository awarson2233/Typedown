using System;
using System.Threading;
using System.Threading.Tasks;

namespace Typedown.Core.Editor
{
    /// <summary>
    /// 宿主与编辑引擎之间唯一的对话面。ViewModel 只通过它发命令、发请求、收事件，
    /// 不感知引擎是 WebView 里的 Muya 还是将来的新引擎，也不感知线上格式。
    /// </summary>
    public interface IEditorSession
    {
        /// <summary>引擎当前能否接收命令。</summary>
        EditorSessionState State { get; }

        /// <summary><see cref="State"/> 的变化流，只在值变化时推送。</summary>
        IObservable<EditorSessionState> StateChanged { get; }

        /// <summary>
        /// 宿主侧的正文镜像：保存、备份、脏标记都读它，引擎卡死或崩溃时它仍保有最后一次同步的正文。
        /// </summary>
        EditorDocument Document { get; }

        /// <summary>引擎发出的类型化事件，在 UI 线程上推送。</summary>
        IObservable<EditorEvent> Events { get; }

        /// <summary>
        /// 即发即走的命令。引擎未就绪时进就绪门排队，主题、快捷键表、设置这类状态只保留最新值，
        /// 引擎就绪或宿主重新挂载后整体重放。
        /// </summary>
        void Post(EditorCommand command);

        /// <summary>需要引擎产出结果的请求；引擎重载时挂起的请求以取消结束。</summary>
        Task<TResult> RequestAsync<TResult>(EditorRequest<TResult> request, CancellationToken cancellationToken = default);
    }

    public enum EditorSessionState
    {
        /// <summary>没有挂载任何引擎宿主。</summary>
        Detached,

        /// <summary>宿主已挂载，引擎正在加载，命令进就绪门。</summary>
        Loading,

        /// <summary>引擎可以接收命令。</summary>
        Ready,

        /// <summary>引擎报告了致命错误或进程崩溃，宿主正在恢复。</summary>
        Faulted,
    }

    /// <summary>
    /// 正文镜像在某一时刻的快照。<paramref name="Version"/> 单调递增，每次装载与每批改动都会变化；
    /// 保存点按它记录，两份快照版本相同即正文相同。
    /// </summary>
    public sealed record EditorDocument(string Text, long Version)
    {
        public static EditorDocument Empty { get; } = new(string.Empty, 0);
    }
}
