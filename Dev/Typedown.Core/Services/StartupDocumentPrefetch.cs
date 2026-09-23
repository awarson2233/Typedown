using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Typedown.Core.Enums;
using Typedown.Core.Utilities;

namespace Typedown.Core.Services
{
    /// <summary>启动文档从哪里来。</summary>
    public enum StartupDocumentOrigin
    {
        /// <summary>命令行（双击 .md、「用 Typedown 打开」、新窗口带的文件）。</summary>
        CommandLine,

        /// <summary>设置了「启动时打开上次的文件」，且记着上次的文件路径。</summary>
        LastFile,
    }

    /// <summary>启动时要打开的文件；没有启动文件（新建空文档）时整个值为 null。</summary>
    public sealed record StartupDocumentTarget(StartupDocumentOrigin Origin, string Path)
    {
        public static StartupDocumentTarget? Resolve(string[] commandLineArgs, FileStartupAction fileStartupAction, string? lastFilePath)
        {
            var commandLinePath = CommandLine.GetOpenFilePath(commandLineArgs);
            if (!string.IsNullOrEmpty(commandLinePath))
                return new(StartupDocumentOrigin.CommandLine, commandLinePath);

            if (fileStartupAction == FileStartupAction.OpenLast && !string.IsNullOrWhiteSpace(lastFilePath))
                return new(StartupDocumentOrigin.LastFile, lastFilePath);

            return null;
        }
    }

    /// <summary>
    /// 启动文档在磁盘上的快照：文件是否存在、正文、备份。只含文件 IO 的结果，不改任何 ViewModel 状态、不弹对话框；
    /// 读正文失败时保存原异常，由装载方在 UI 线程上按原来的路径重新抛出。
    /// </summary>
    public sealed class StartupFileSnapshot
    {
        private readonly string? text;
        private readonly ExceptionDispatchInfo? readError;

        private StartupFileSnapshot(string path, bool exists, string? text, ExceptionDispatchInfo? readError, string? backup)
        {
            Path = path;
            Exists = exists;
            this.text = text;
            this.readError = readError;
            Backup = backup;
        }

        public string Path { get; }

        public bool Exists { get; }

        /// <summary>备份正文；没有备份或读备份失败时为 null（与 <see cref="AutoBackup.GetBackup"/> 一致）。</summary>
        public string? Backup { get; }

        /// <summary>正文；读取失败时重新抛出当时的异常。</summary>
        public string GetText()
        {
            readError?.Throw();
            return text ?? throw new FileNotFoundException("File does not exist.", Path);
        }

        public static async Task<StartupFileSnapshot> ReadAsync(string path, AutoBackup autoBackup)
        {
            if (!File.Exists(path))
                return new(path, false, null, null, null);

            string text;
            try
            {
                text = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new(path, true, null, ExceptionDispatchInfo.Capture(ex), null);
            }

            var backup = await autoBackup.GetBackup(path).ConfigureAwait(false);
            return new(path, true, text, null, backup);
        }
    }

    /// <summary>
    /// 启动文档预读：宿主在 <c>OnLaunched</c> 解析出启动文件后立即在线程池上读正文与备份，
    /// 与 WebView2 环境预热、XAML 构建并行；页面握手要初始状态时，<see cref="TakeAsync"/> 直接交出已读好的快照。
    /// 解析结果、对话框（读错误、恢复备份）与状态变更仍由 <see cref="ViewModels.FileViewModel"/> 在 UI 线程上完成。
    /// </summary>
    public sealed class StartupDocumentPrefetch
    {
        private readonly AutoBackup autoBackup;
        private Task<StartupFileSnapshot>? pending;

        public StartupDocumentPrefetch(AutoBackup autoBackup)
        {
            this.autoBackup = autoBackup;
        }

        /// <summary>开始预读；<paramref name="target"/> 为 null（没有启动文件）时什么都不做。</summary>
        public void Start(StartupDocumentTarget? target)
        {
            if (target is null)
                return;

            var path = target.Path;
            Interlocked.Exchange(ref pending, Task.Run(() => StartupFileSnapshot.ReadAsync(path, autoBackup)));
        }

        /// <summary>
        /// 取 <paramref name="target"/> 的快照：预读的正是这个文件就等它完成（多半早已完成），否则当场读。
        /// 预读结果只用一次，之后的调用都会重新读盘。
        /// </summary>
        public async Task<StartupFileSnapshot> TakeAsync(StartupDocumentTarget target)
        {
            var prefetched = Interlocked.Exchange(ref pending, null);
            if (prefetched is not null)
            {
                var snapshot = await prefetched;
                if (string.Equals(snapshot.Path, target.Path, StringComparison.Ordinal))
                    return snapshot;
            }

            return await StartupFileSnapshot.ReadAsync(target.Path, autoBackup);
        }
    }
}
