using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using Typedown.Core.Editor;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Models.ExportConfigModels;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Core.Interfaces;

namespace Typedown.Core.ViewModels
{
    public sealed partial class FileViewModel : INotifyPropertyChanged, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public AppViewModel AppViewModel => ServiceProvider.GetRequiredService<AppViewModel>();

        public SettingsViewModel SettingsViewModel => ServiceProvider.GetRequiredService<SettingsViewModel>();

        public EditorViewModel EditorViewModel => ServiceProvider.GetRequiredService<EditorViewModel>();

        public IEditorSession EditorSession => ServiceProvider.GetRequiredService<IEditorSession>();

        public AccessHistory AccessHistory => ServiceProvider.GetRequiredService<AccessHistory>();

        public string? WorkFolder { get; private set; }

        public string? FilePath { get; private set; }

        private string? startupOpenedFilePath;

        public string ImageBasePath => string.IsNullOrEmpty(FilePath) ? SettingsViewModel.DefaultImageBasePath : Path.GetDirectoryName(FilePath) ?? SettingsViewModel.DefaultImageBasePath;

        public string? FileName => string.IsNullOrEmpty(FilePath) ? null : Path.GetFileName(FilePath);

        public Command<Unit> NewFileCommand { get; } = new();
        public Command<string> NewWindowCommand { get; } = new();
        public Command<string> OpenFileCommand { get; } = new();
        public Command<string> OpenFolderCommand { get; } = new();
        public Command<Unit> NewFolderCommand { get; } = new();
        public Command<Unit> ClearHistoryCommand { get; } = new();
        public Command<Unit> SaveCommand { get; } = new();
        public Command<Unit> SaveAsCommand { get; } = new();
        public Command<Unit> ImportCommand { get; } = new();
        public Command<ExportConfig> ExportCommand { get; } = new();
        public Command<Unit> PrintCommand { get; } = new();
        public Command<Unit> ExitCommand { get; } = new();

        private readonly System.Timers.Timer saveFileTimer = new();
        private readonly AsyncSingleOperation autoPersistenceOperation = new();

        public AutoBackup AutoBackup => ServiceProvider.GetRequiredService<AutoBackup>();

        public IAtomicFileWriter FileWriter => ServiceProvider.GetRequiredService<IAtomicFileWriter>();

        public IDialogService DialogService => ServiceProvider.GetRequiredService<IDialogService>();

        public IFilePickerService FilePickerService => ServiceProvider.GetRequiredService<IFilePickerService>();

        public IUiDispatcher UiDispatcher => ServiceProvider.GetRequiredService<IUiDispatcher>();

        public IWindowContext WindowContext => ServiceProvider.GetRequiredService<IWindowContext>();

        private readonly CompositeDisposable disposables = new();
        private readonly TaskCompletionSource<bool> initialEditorFileLoadedTask = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public FileViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            disposables.Add(NewFileCommand.OnExecute.Subscribe(async _ => await NewFileFun()));
            disposables.Add(OpenFileCommand.OnExecute.Subscribe(async x => await OpenFile(x)));
            disposables.Add(OpenFolderCommand.OnExecute.Subscribe(async x => await OpenFolder(x)));
            disposables.Add(SaveAsCommand.OnExecute.Subscribe(async _ => await SaveAs()));
            disposables.Add(SaveCommand.OnExecute.Subscribe(async _ => await Save()));
            disposables.Add(ExitCommand.OnExecute.Subscribe(_ => Exit()));
            disposables.Add(ClearHistoryCommand.OnExecute.Subscribe(x => { _ = AccessHistory.ClearHistory(); }));
            disposables.Add(ExportCommand.OnExecute.Subscribe(Export));
            disposables.Add(PrintCommand.OnExecute.Subscribe(_ => Print()));
            disposables.Add(ImportCommand.OnExecute.Subscribe(_ => Import()));
            disposables.Add(EditorSession.Events.OfType<DocumentLoaded>().Take(1).Subscribe(_ => MarkInitialEditorFileLoaded()));
            saveFileTimer.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds;
            saveFileTimer.Elapsed += SaveFileTimerTick;
            disposables.Add(Disposable.Create(() => saveFileTimer.Elapsed -= SaveFileTimerTick));
            saveFileTimer.Start();
            _ = UiDispatcher.RunIdleAsync(() => OnStartup());
        }

        /// <summary>
        /// 计时器在线程池上触发；自动保存会改 <see cref="EditorViewModel.AutoSavedSucc"/> 与保存状态，
        /// 进而改窗口标题，所以整轮保存都切回 UI 线程执行。
        /// </summary>
        private void SaveFileTimerTick(object? sender, ElapsedEventArgs e)
        {
            if (disposables.IsDisposed) return;
            try
            {
                _ = UiDispatcher.RunAsync(() =>
                {
                    if (!disposables.IsDisposed)
                        _ = autoPersistenceOperation.TryRunAsync(RunAutoPersistenceTickAsync);
                });
            }
            catch (UiDispatcherUnavailableException)
            {
                // 窗口正在关闭，UI 线程已不可用，这一轮直接跳过。
            }
        }

        private async Task RunAutoPersistenceTickAsync()
        {
            using var lease = await AcquirePersistenceLease(false);
            if (lease is null)
            {
                EditorViewModel.AutoSavedSucc = false;
                return;
            }
            if (SettingsViewModel.AutoSave)
            {
                EditorViewModel.AutoSavedSucc = await AutoSaveFile(lease);
                if (!EditorViewModel.AutoSavedSucc)
                    await AutoBackupFile(lease);
            }
            else
            {
                await AutoBackupFile(lease);
            }
        }

        private async Task<PendingImportGate.PersistenceLease?> AcquirePersistenceLease(bool showError)
        {
            var lease = await EditorViewModel.AcquirePersistenceLeaseAsync();
            if (lease is null && showError)
                await ShowDialog(
                    Locale.GetString("Error"),
                    "The imported content is still being finalized. Please try again.",
                    Locale.GetString("Ok"));
            return lease;
        }

        public async Task<bool> AutoSaveFile()
        {
            using var lease = await AcquirePersistenceLease(false);
            return lease is not null && await AutoSaveFile(lease);
        }

        private async Task<bool> AutoSaveFile(PendingImportGate.PersistenceLease lease)
        {
            try
            {
                if (!lease.IsValid) return false;
                if (SettingsViewModel.AutoSave && EditorViewModel.FileLoaded && (EditorViewModel.FileHash != EditorViewModel.CurrentHash) && FilePath != null)
                    return await Save(false, lease);
                return FilePath != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> AutoBackupFile(PendingImportGate.PersistenceLease lease)
        {
            if (!lease.IsValid) return false;
            var path = FilePath;
            var markdown = EditorViewModel.Markdown;
            var changed = EditorViewModel.FileHash != EditorViewModel.CurrentHash;
            if (string.IsNullOrWhiteSpace(path)) return true;
            if (changed && !string.IsNullOrWhiteSpace(markdown))
            {
                var temporaryPath = await AutoBackup.PrepareBackup(path, markdown);
                if (temporaryPath is null) return false;
                var committed = false;
                var valid = lease.TryCommit(
                    () => FilePath == path && EditorViewModel.Markdown == markdown && EditorViewModel.FileHash != EditorViewModel.CurrentHash,
                    () => committed = AutoBackup.CommitBackup(path, temporaryPath));
                if (!valid) FileWriter.Discard(temporaryPath);
                return valid && committed;
            }

            return lease.TryCommit(
                () => FilePath == path && EditorViewModel.Markdown == markdown && EditorViewModel.FileHash == EditorViewModel.CurrentHash,
                () => AutoBackup.DeleteBackup(path));
        }

        /// <summary>
        /// 装载一篇文档：先把状态落到 ViewModel，再把正文交给编辑会话。
        /// 编辑引擎正在启动握手时，会话把正文放进握手应答，不再另发装载命令。
        /// 只有"已保存"的内容才把 FileLoaded 置 false——此时基线哈希要等引擎归一化后的
        /// 装载回声重新计算；从备份恢复的内容本来就是脏的，不需要重新定基线。
        /// </summary>
        private void ApplyDocument(string text, ulong fileHash, string? filePath, bool saved)
        {
            FilePath = filePath;
            EditorViewModel.FileHash = fileHash;
            EditorViewModel.CurrentHash = Common.SimpleHash(text);
            EditorViewModel.Saved = saved;
            EditorViewModel.AutoSavedSucc = true;
            EditorViewModel.FileLoaded = !saved;
            EditorViewModel.LoadDocument(text, ImageBasePath);
        }

        private async Task NewFileFun()
        {
            if (!await AskToSave()) return;
            var text = Common.DefaultMarkdwn;
            ApplyDocument(text, Common.SimpleHash(text), null, true);
        }

        public async Task<bool> OpenFile(string? filePath = null)
        {
            if (!await AskToSave())
                return false;
            filePath ??= await FilePickerService.PickOpenFileAsync(new OpenFileRequest
            {
                FileTypeFilter = FileTypeHelper.Markdown.ToList()
            });
            if (filePath == null)
                return false;
            return await LoadFile(filePath, true);
        }

        public async Task<bool> OpenFolder(string? folderPath = null)
        {
            folderPath ??= await FilePickerService.PickFolderAsync(new PickFolderRequest());
            if (folderPath == null)
                return false;
            if (!await LoadFolder(folderPath))
                return false;
            SettingsViewModel.SidePaneOpen = true;
            SettingsViewModel.SidePaneIndex = 0;
            return true;
        }

        private async Task<bool> LoadFile(string path, bool skipSavedCheck = false)
        {
            try
            {
                if (TryGetOpenedWindow(path, out var window) && window != WindowContext.WindowHandle)
                {
                    _ = UiDispatcher.RunIdleAsync(() => PInvoke.SetForegroundWindow(window));
                    return false;
                }
                if (!File.Exists(path))
                {
                    _ = RunAfterInitialEditorFileLoadedAsync(() => AccessHistory.RemoveFileHistory(path));
                    throw new FileNotFoundException("File does not exist.");
                }
                else if (!skipSavedCheck && !await AskToSave())
                {
                    return false;
                }
                var text = await File.ReadAllTextAsync(path);
                EditorViewModel.FirstStart = false;
                var fileHash = Common.SimpleHash(text);
                SettingsViewModel.LastFilePath = path;
                var loadedPath = path;
                _ = RunAfterInitialEditorFileLoadedAsync(() => AccessHistory.RecordFileHistory(loadedPath));
                var backup = await CheckBackup(path, fileHash);
                var markdown = backup ?? text;
                var saved = backup is null;
                ApplyDocument(markdown, fileHash, path, saved);
                return true;
            }
            catch (Exception ex)
            {
                await ShowDialog(
                    Locale.GetDialogString("ReadErrorTitle"),
                    ex.Message,
                    Locale.GetDialogString("Ok"));
                return false;
            }
        }

        private async Task<bool> LoadFolder(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    _ = RunAfterInitialEditorFileLoadedAsync(() => AccessHistory.RemoveFolderHistory(folderPath));
                    throw new FileNotFoundException("Folder does not exist.");
                }
                WorkFolder = folderPath;
                SettingsViewModel.LastFolderPath = folderPath;
                _ = RunAfterInitialEditorFileLoadedAsync(() => AccessHistory.RecordFolderHistory(folderPath));
                return true;
            }
            catch (Exception ex)
            {
                await ShowDialog(
                    Locale.GetString("Error"),
                    ex.Message,
                    Locale.GetDialogString("Ok"));
                return false;
            }
        }

        private async Task<string?> CheckBackup(string path, ulong fileHash)
        {
            string? text = await AutoBackup.GetBackup(path);
            if (text == null || Common.SimpleHash(text) == fileHash) return null;
            var result = await DialogService.ShowAsync(new DialogRequest
            {
                Title = Locale.GetDialogString("RecoverTitle"),
                Content = Locale.GetDialogString("RecoverContent"),
                PrimaryButtonText = Locale.GetDialogString("Recover"),
                SecondaryButtonText = Locale.GetDialogString("Delete"),
                DefaultButton = DialogDefaultButton.Primary
            });
            if (result == DialogButton.Primary)
            {
                return text;
            }
            else
            {
                AutoBackup.DeleteBackup(path);
                return null;
            }
        }

        private async Task<bool> WriteAllText(string path, string text, bool alert = true)
        {
            try
            {
                await FileWriter.WriteAllTextAsync(path, text);
                return true;
            }
            catch (Exception ex)
            {
                if (alert)
                {
                    await ShowDialog(
                        Locale.GetDialogString("SaveErrorTitle"),
                        ex.Message,
                        Locale.GetDialogString("Ok"));
                }
                return false;
            }
        }

        private async Task<bool> Save(bool alert = true, PendingImportGate.PersistenceLease? existingLease = null)
        {
            var lease = existingLease ?? await AcquirePersistenceLease(true);
            if (lease is null) return false;
            try
            {
                if (!lease.IsValid) return false;
                if (FilePath == null)
                {
                    var savedPath = await SaveAs(lease);
                    return savedPath != null;
                }
                var path = FilePath;
                var markdown = EditorViewModel.Markdown;
                var currentHash = EditorViewModel.CurrentHash;
                if (!await WriteAllText(path, markdown, alert)) return false;
                return lease.TryCommit(
                    () => FilePath == path && EditorViewModel.Markdown == markdown && EditorViewModel.CurrentHash == currentHash,
                    () =>
                    {
                        EditorViewModel.FileHash = currentHash;
                        EditorViewModel.Saved = true;
                        AutoBackup.DeleteBackup(path);
                        _ = RunAfterInitialEditorFileLoadedAsync(() => AccessHistory.RecordFileHistory(path));
                    });
            }
            finally
            {
                if (existingLease is null) lease.Dispose();
            }
        }

        private async Task<string?> SaveAs(PendingImportGate.PersistenceLease? existingLease = null)
        {
            existingLease?.Dispose();
            if (saveAsOpened) return null;
            saveAsOpened = true;
            try
            {
                var filePath = await FilePickerService.PickSaveFileAsync(new SaveFileRequest
                {
                    FileTypeChoices = { new SaveFileTypeChoice("Markdown Files", FileTypeHelper.Markdown) },
                    SuggestedFileName = FileName ?? "untitled"
                });
                if (filePath is null) return null;

                using var lease = await AcquirePersistenceLease(true);
                if (lease is null || !lease.IsValid) return null;
                var oldPath = FilePath;
                var markdown = EditorViewModel.Markdown;
                var currentHash = EditorViewModel.CurrentHash;
                if (!await WriteAllText(filePath, markdown)) return null;

                var committed = lease.TryCommit(
                    () => FilePath == oldPath && EditorViewModel.Markdown == markdown && EditorViewModel.CurrentHash == currentHash,
                    () =>
                    {
                        if (oldPath is not null) AutoBackup.DeleteBackup(oldPath);
                        FilePath = filePath;
                        SettingsViewModel.LastFilePath = filePath;
                        EditorViewModel.FileHash = currentHash;
                        EditorViewModel.Saved = true;
                        _ = RunAfterInitialEditorFileLoadedAsync(() => AccessHistory.RecordFileHistory(filePath));
                    });
                return committed ? filePath : null;
            }
            catch (Exception ex)
            {
                await ShowDialog(Locale.GetString("Error"), ex.Message, Locale.GetString("Ok"));
                return null;
            }
            finally
            {
                saveAsOpened = false;
            }
        }

        private bool askToSaveOpened;
        private bool saveAsOpened;

        public async Task<bool> AskToSave()
        {
            var initialLease = await AcquirePersistenceLease(true);
            if (initialLease is null || !initialLease.IsValid) return false;
            var generation = initialLease.Generation;
            var saved = EditorViewModel.Saved;
            initialLease.Dispose();

            if (saved)
            {
                using var validationLease = await AcquirePersistenceLease(true);
                return validationLease is not null && validationLease.Generation == generation && validationLease.IsValid;
            }
            if (SettingsViewModel.AutoSave && await AutoSaveFile()) return true;
            if (askToSaveOpened) return false;

            askToSaveOpened = true;
            try
            {
                var result = await DialogService.ShowAsync(new DialogRequest
                {
                    Title = Locale.GetDialogString("AsKToSaveTitle"),
                    Content = Locale.GetDialogString("AsKToSaveContent"),
                    CloseButtonText = Locale.GetDialogString("Cancel"),
                    PrimaryButtonText = Locale.GetDialogString("Save"),
                    SecondaryButtonText = Locale.GetDialogString("Don'tSave"),
                    DefaultButton = DialogDefaultButton.Primary
                });
                using var validationLease = await AcquirePersistenceLease(true);
                if (validationLease is null || validationLease.Generation != generation || !validationLease.IsValid) return false;
                switch (result)
                {
                    case DialogButton.Primary:
                        return await Save(existingLease: validationLease);
                    case DialogButton.Secondary:
                        if (FilePath is not null) AutoBackup.DeleteBackup(FilePath);
                        return true;
                    default:
                        return false;
                }
            }
            finally
            {
                askToSaveOpened = false;
            }
        }

        public async Task LoadStartUpMarkdown()
        {
            startupOpenedFilePath = null;

            var path = CommandLine.GetOpenFilePath(AppViewModel.CommandLineArgs);
            if (!string.IsNullOrEmpty(path))
            {
                if (await LoadFile(path, true))
                {
                    startupOpenedFilePath = FilePath;
                    var openedFileFolder = Path.GetDirectoryName(FilePath);
                    if (!string.IsNullOrWhiteSpace(openedFileFolder))
                    {
                        await LoadFolder(openedFileFolder);
                    }
                }
                else
                {
                    await NewFileFun();
                }
            }
            else
            {
                switch (SettingsViewModel.FileStartupAction)
                {
                    case FileStartupAction.OpenLast:
                        var lastFile = SettingsViewModel.LastFilePath;
                        if (!string.IsNullOrWhiteSpace(lastFile) && !TryGetOpenedWindow(lastFile, out _) && File.Exists(lastFile))
                        {
                            await LoadFile(lastFile, true);
                        }
                        else
                        {
                            await NewFileFun();
                            _ = LoadLastFileFromHistoryAfterInitialRenderAsync();
                        }
                        break;
                    default:
                        await NewFileFun();
                        break;
                }
            }
        }

        private async Task<string?> ResolveOpenLastFolderAsync()
        {
            var lastFolder = SettingsViewModel.LastFolderPath;
            if (string.IsNullOrWhiteSpace(lastFolder) || !Directory.Exists(lastFolder))
            {
                await AccessHistory.EnsureInitialized();
                lastFolder = AccessHistory.FolderRecentlyOpened.FirstOrDefault();
            }

            return !string.IsNullOrWhiteSpace(lastFolder) && Directory.Exists(lastFolder) ? lastFolder : null;
        }

        private async Task<string?> ResolveStartupFolderAsync()
        {
            switch (SettingsViewModel.FolderStartupAction)
            {
                case FolderStartupAction.FollowOpenedFileFolder:
                    var startupFolder = string.IsNullOrWhiteSpace(startupOpenedFilePath) ? null : Path.GetDirectoryName(startupOpenedFilePath);
                    if (!string.IsNullOrWhiteSpace(startupFolder) && Directory.Exists(startupFolder))
                        return startupFolder;
                    return await ResolveOpenLastFolderAsync();
                case FolderStartupAction.OpenLast:
                    return await ResolveOpenLastFolderAsync();
                case FolderStartupAction.OpenFolder:
                    return Directory.Exists(SettingsViewModel.StartupOpenFolder) ? SettingsViewModel.StartupOpenFolder : null;
                default:
                    return null;
            }
        }

        private async void Export(ExportConfig config)
        {
            var filePath = await FilePickerService.PickSaveFileAsync(new SaveFileRequest
            {
                FileTypeChoices = config.FileExtensions
                    .Select(x => new SaveFileTypeChoice(x.name, new List<string> { x.extension }))
                    .ToList()
            });
            if (filePath == null) return;
            try
            {
                string? basePath = null;
                if (config.Type == ExportType.PDF || config.Type == ExportType.Image)
                    basePath = ImageBasePath;
                var html = await EditorSession.RequestAsync(new RenderExportHtml(
                    ExportPurpose.Export,
                    Path.GetFileNameWithoutExtension(filePath),
                    basePath,
                    CreateExportHtmlOptions(config.LoadExportConfig())));

                var exportConfig = await ServiceProvider.GetRequiredService<IFileExport>().GetExportConfig(config.Id);
                await exportConfig.LoadExportConfig().Export(ServiceProvider, html, filePath);
                if (SettingsViewModel.OpenFolderAfterExport)
                    Common.OpenFileLocation(filePath);
            }
            catch (OperationCanceledException)
            {
                // 编辑引擎在生成期间重载，请求随之作废。
            }
            catch (Exception ex)
            {
                await ShowDialog(Locale.GetString("Error"), ex.Message, Locale.GetString("Ok"));
            }
        }

        /// <summary>导出配置里页面生成 HTML 时要用到的部分。</summary>
        private static ExportHtmlOptions? CreateExportHtmlOptions(ConfigModel model) => model switch
        {
            HTMLConfigModel html => new ExportHtmlOptions(html.ExtraHead, html.ExtraBody, null, null),
            PDFConfigModel pdf => new ExportHtmlOptions(pdf.ExtraHead, null, pdf.Header, pdf.Footer),
            _ => null,
        };

        private async void Print()
        {
            try
            {
                var html = await EditorSession.RequestAsync(new RenderExportHtml(
                    ExportPurpose.Print,
                    FileName ?? "untitled",
                    ImageBasePath,
                    null));
                var fileExport = ServiceProvider.GetRequiredService<IFileExport>();
                await fileExport.Print(Path.GetDirectoryName(FilePath ?? string.Empty) ?? string.Empty, html, FileName);
            }
            catch (OperationCanceledException)
            {
                // 编辑引擎在生成期间重载，请求随之作废。
            }
            catch (Exception ex)
            {
                await ShowDialog(Locale.GetString("Error"), ex.Message, Locale.GetString("Ok"));
            }
        }

        private async void Import()
        {
            try
            {
                var filePath = await FilePickerService.PickOpenFileAsync(new OpenFileRequest
                {
                    FileTypeFilter = { ".html" }
                });
                if (filePath != null)
                {
                    var text = await File.ReadAllTextAsync(filePath);
                    EditorSession.Post(new ImportHtml(text));
                }
            }
            catch (Exception ex)
            {
                await ShowDialog(
                    Locale.GetDialogString("ImportErrorTitle"),
                    ex.Message,
                    Locale.GetDialogString("Ok"));
            }
        }

        private async void OnStartup()
        {
            try
            {
                await WaitForInitialEditorFileLoadedAsync();
                if (string.IsNullOrEmpty(WorkFolder))
                {
                    var folderToLoad = await ResolveStartupFolderAsync();
                    if (!string.IsNullOrWhiteSpace(folderToLoad))
                        await LoadFolder(folderToLoad);
                }
            }
            catch
            {
                // Ignore
            }
        }

        private async Task LoadLastFileFromHistoryAfterInitialRenderAsync()
        {
            try
            {
                await WaitForInitialEditorFileLoadedAsync();
                if (FilePath != null || SettingsViewModel.FileStartupAction != FileStartupAction.OpenLast)
                    return;

                await AccessHistory.EnsureInitialized();
                if (AccessHistory.FileRecentlyOpened.FirstOrDefault() is string lastFile && !TryGetOpenedWindow(lastFile, out _) && File.Exists(lastFile))
                    await LoadFile(lastFile, true);
            }
            catch
            {
                // Ignore startup history failures; the editor has already rendered a usable document.
            }
        }

        private async Task RunAfterInitialEditorFileLoadedAsync(Func<Task> action)
        {
            try
            {
                await WaitForInitialEditorFileLoadedAsync();
                await action();
            }
            catch
            {
                // History writes must not block or destabilize the first editor render.
            }
        }

        private async Task WaitForInitialEditorFileLoadedAsync()
        {
            if (EditorViewModel.FileLoaded)
                MarkInitialEditorFileLoaded();

            var completed = await Task.WhenAny(initialEditorFileLoadedTask.Task, Task.Delay(TimeSpan.FromSeconds(8)));
            if (completed != initialEditorFileLoadedTask.Task)
                MarkInitialEditorFileLoaded();
        }

        private void MarkInitialEditorFileLoaded()
        {
            initialEditorFileLoadedTask.TrySetResult(true);
        }

        public static bool TryGetOpenedWindow(string? filePath, out IntPtr window)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                window = default;
                return false;
            }
            window = AppViewModel.GetInstances()
                .Where(x => string.Equals(x.FileViewModel.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.WindowContext?.WindowHandle ?? default)
                .FirstOrDefault();
            return window != default;
        }

        public bool RenameFile(string to)
        {
            if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath) || string.IsNullOrWhiteSpace(to))
                return false;

            var from = FilePath;
            var fileOperation = ServiceProvider.GetRequiredService<IFileOperation>();
            if (fileOperation.Rename(from, to))
            {
                MoveBackup(from, to);
                FilePath = to;
                SettingsViewModel.LastFilePath = to;
                _ = RunAfterInitialEditorFileLoadedAsync(async () =>
                {
                    await AccessHistory.RemoveFileHistory(from);
                    await AccessHistory.RecordFileHistory(to);
                });
                return true;
            }
            return false;
        }

        private void MoveBackup(string from, string to)
        {
            try
            {
                var fromBackup = AutoBackup.GetBackupFilePath(from);
                if (fromBackup is null || !File.Exists(fromBackup))
                    return;

                var toBackup = AutoBackup.GetBackupFilePath(to);
                if (toBackup is null)
                    return;

                File.Move(fromBackup, toBackup, true);
            }
            catch
            {
                // Backup migration must not fail the completed file rename.
            }
        }

        public void Dispose()
        {
            saveFileTimer.Stop();
            disposables.Dispose();
            saveFileTimer.Dispose();
        }

        private void Exit()
        {
            WindowContext.RequestClose();
        }

        private Task<DialogButton> ShowDialog(string title, object content, string closeButtonText)
        {
            return DialogService.ShowAsync(new DialogRequest
            {
                Title = title,
                Content = content,
                CloseButtonText = closeButtonText,
                DefaultButton = DialogDefaultButton.Close
            });
        }
    }
}
