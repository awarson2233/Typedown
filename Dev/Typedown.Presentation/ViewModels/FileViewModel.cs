using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Timers;
using System.Threading.Tasks;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Utilities;

namespace Typedown.Presentation.ViewModels
{
    public sealed partial class FileViewModel : INotifyPropertyChanged, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public AppViewModel AppViewModel => ServiceProvider.GetRequiredService<AppViewModel>();

        public SettingsViewModel SettingsViewModel => ServiceProvider.GetRequiredService<SettingsViewModel>();

        public EditorViewModel EditorViewModel => ServiceProvider.GetRequiredService<EditorViewModel>();

        public EventCenter EventCenter => ServiceProvider.GetRequiredService<EventCenter>();

        public RemoteInvoke RemoteInvoke => ServiceProvider.GetRequiredService<RemoteInvoke>();

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

        public AutoBackup AutoBackup => ServiceProvider.GetRequiredService<AutoBackup>();

        public IEditorCommandSink EditorCommandSink => ServiceProvider.GetRequiredService<IEditorCommandSink>();

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
            disposables.Add(RemoteInvoke.Handle<JToken, bool>("ExportCallback", ExportCallback));
            disposables.Add(RemoteInvoke.Handle<JToken, bool>("PrintHTML", PrintHTML));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("Save").Subscribe(_ => SaveCommand.Execute(Unit.Default)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("SaveAs").Subscribe(_ => SaveAsCommand.Execute(Unit.Default)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("Close").Subscribe(_ => ExitCommand.Execute(Unit.Default)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("FileLoaded").Take(1).Subscribe(_ => MarkInitialEditorFileLoaded()));
            saveFileTimer.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds;
            saveFileTimer.Elapsed += SaveFileTimerTick;
            disposables.Add(Disposable.Create(() => saveFileTimer.Elapsed -= SaveFileTimerTick));
            saveFileTimer.Start();
            _ = UiDispatcher.RunIdleAsync(() => OnStartup());
        }

        private async void SaveFileTimerTick(object? sender, ElapsedEventArgs e)
        {
            if (disposables.IsDisposed)
            {
                return;
            }
            if (SettingsViewModel.AutoSave)
            {
                EditorViewModel.AutoSavedSucc = await AutoSaveFile();
                if (!EditorViewModel.AutoSavedSucc)
                    await AutoBackupFile();
            }
            else
            {
                await AutoBackupFile();
            }
        }

        public async Task<bool> AutoSaveFile()
        {
            try
            {
                if (SettingsViewModel.AutoSave && EditorViewModel.FileLoaded && (EditorViewModel.FileHash != EditorViewModel.CurrentHash) && FilePath != null)
                    return await Save(false);
                return FilePath != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> AutoBackupFile()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
                return true;

            if (EditorViewModel.FileHash != EditorViewModel.CurrentHash && !string.IsNullOrWhiteSpace(EditorViewModel.Markdown))
                return await AutoBackup.Backup(FilePath, EditorViewModel.Markdown);

            AutoBackup.DeleteBackup(FilePath);
            return true;
        }

        private async Task NewFileFun(bool postMessage = true)
        {
            if (!await AskToSave()) return;
            FilePath = null;
            EditorViewModel.FileHash = Common.SimpleHash(Common.DefaultMarkdwn);
            EditorViewModel.Markdown = Common.DefaultMarkdwn;
            EditorViewModel.CurrentHash = EditorViewModel.FileHash;
            EditorViewModel.Saved = true;
            EditorViewModel.AutoSavedSucc = true;
            EditorViewModel.FileLoaded = false;
            EditorViewModel.History.InitHistory(Common.DefaultMarkdwn);
            if (postMessage)
            {
                EditorCommandSink?.Send("LoadFile", new { text = EditorViewModel.Markdown, basePath = ImageBasePath });
            }
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

        private async Task<bool> LoadFile(string path, bool skipSavedCheck = false, bool postMessage = true)
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
                EditorViewModel.FileHash = Common.SimpleHash(text);
                FilePath = path;
                SettingsViewModel.LastFilePath = path;
                var loadedPath = path;
                _ = RunAfterInitialEditorFileLoadedAsync(() => AccessHistory.RecordFileHistory(loadedPath));
                var backup = await CheckBackup(path, EditorViewModel.FileHash);
                if (backup == null)
                {
                    EditorViewModel.Markdown = text;
                    EditorViewModel.CurrentHash = EditorViewModel.FileHash;
                    EditorViewModel.Saved = true;
                    EditorViewModel.FileLoaded = false;
                }
                else
                {
                    EditorViewModel.Markdown = backup;
                    EditorViewModel.CurrentHash = Common.SimpleHash(backup);
                    EditorViewModel.Saved = false;
                    EditorViewModel.FileLoaded = true;
                }
                EditorViewModel.AutoSavedSucc = true;
                EditorViewModel.History.InitHistory(EditorViewModel.Markdown);
                if (postMessage)
                {
                    EditorCommandSink?.Send("LoadFile", new { text = EditorViewModel.Markdown, filePath = FilePath, basePath = ImageBasePath });
                }
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
                await File.WriteAllTextAsync(path, text);
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

        private async Task<bool> Save(bool alert = true)
        {
            if (FilePath == null)
            {
                var result = await SaveAs();
                return result != null;
            }
            else
            {
                var result = await WriteAllText(FilePath, EditorViewModel.Markdown, alert);
                if (result)
                {
                    EditorViewModel.FileHash = EditorViewModel.CurrentHash;
                    EditorViewModel.Saved = true;
                    AutoBackup.DeleteBackup(FilePath);
                    var savedPath = FilePath;
                    _ = RunAfterInitialEditorFileLoadedAsync(() => AccessHistory.RecordFileHistory(savedPath));
                }
                return result;
            }
        }

        private async Task<string?> SaveAs()
        {
            if (saveAsOpened)
            {
                return null;
            }

            saveAsOpened = true;
            try
            {
                var filePath = await FilePickerService.PickSaveFileAsync(new SaveFileRequest
                {
                    FileTypeChoices =
                    {
                        new SaveFileTypeChoice("Markdown Files", FileTypeHelper.Markdown)
                    },
                    SuggestedFileName = FileName ?? "untitled"
                });
                if (filePath != null)
                {
                    var result = await WriteAllText(filePath, EditorViewModel.Markdown);
                    if (result)
                    {
                        if (FilePath is not null)
                            AutoBackup.DeleteBackup(FilePath);
                        FilePath = filePath;
                        SettingsViewModel.LastFilePath = filePath;
                        EditorViewModel.FileHash = EditorViewModel.CurrentHash;
                        EditorViewModel.Saved = true;
                        var savedPath = filePath;
                        _ = RunAfterInitialEditorFileLoadedAsync(() => AccessHistory.RecordFileHistory(savedPath));
                        return filePath;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                await ShowDialog(
                    Locale.GetString("Error"),
                    ex.Message,
                    Locale.GetString("Ok"));
                return null;
            }
            finally
            {
                saveAsOpened = false;
            }
        }

        private async Task<bool> PrintHTML(JToken args)
        {
            try
            {
                var html = RequireString(args, "html");

                var fileExport = ServiceProvider.GetRequiredService<IFileExport>();
                await fileExport.Print(Path.GetDirectoryName(FilePath ?? string.Empty) ?? string.Empty, html, FileName);
                return true;
            }
            catch (Exception ex)
            {
                await ShowDialog(
                    Locale.GetString("Error"),
                    ex.Message,
                    Locale.GetString("Ok"));
                return false;
            }
        }

        private async Task<bool> ExportCallback(JToken args)
        {
            try
            {
                var html = RequireString(args, "html");
                var context = args["context"] ?? throw new InvalidOperationException("Editor export callback payload is missing context.");
                var filePath = RequireString(context, "filePath");
                var configId = RequireValue<int>(context, "configId");

                var config = await ServiceProvider.GetRequiredService<IFileExport>().GetExportConfig(configId);
                await config.LoadExportConfig().Export(ServiceProvider, html, filePath);
                if (SettingsViewModel.OpenFolderAfterExport)
                    Common.OpenFileLocation(filePath);
                return true;
            }
            catch (Exception ex)
            {
                await ShowDialog(
                    Locale.GetString("Error"),
                    ex.Message,
                    Locale.GetString("Ok"));
                return false;
            }
        }

        private bool askToSaveOpened;
        private bool saveAsOpened;

        public async Task<bool> AskToSave()
        {
            if (EditorViewModel.Saved || (SettingsViewModel.AutoSave && await AutoSaveFile()))
            {
                return true;
            }
            if (askToSaveOpened)
            {
                return false;
            }
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
                switch (result)
                {
                    case DialogButton.Primary:
                        var saveResult = await Save();
                        return saveResult;
                    case DialogButton.Secondary:
                        if (FilePath is not null)
                            AutoBackup.DeleteBackup(FilePath);
                        return true;
                    case DialogButton.None:
                        return false;
                }
                return false;
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
                if (await LoadFile(path, true, false))
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
                    await NewFileFun(false);
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
                            await NewFileFun(false);
                            _ = LoadLastFileFromHistoryAfterInitialRenderAsync();
                        }
                        break;
                    default:
                        await NewFileFun(false);
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
            string? basePath = null;
            if (config.Type == ExportType.PDF || config.Type == ExportType.Image)
                basePath = ImageBasePath;
            EditorCommandSink?.Send("Export", new
            {
                type = "export",
                title = Path.GetFileNameWithoutExtension(filePath),
                context = new { configId = config.Id, filePath },
                basePath,
                options = config.LoadExportConfig()
            });
        }

        private void Print()
        {
            EditorCommandSink?.Send("Export", new
            {
                type = "print",
                basePath = ImageBasePath,
                title = FileName ?? "untitled"
            });
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
                    EditorCommandSink?.Send("ImportFile", new { type = Path.GetExtension(filePath).Substring(1), text });
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

        private static string RequireString(JToken token, string propertyName)
        {
            var valueToken = token[propertyName];
            return valueToken is JValue { Type: JTokenType.String, Value: string value } && !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new InvalidOperationException($"Editor callback payload is missing valid string '{propertyName}'.");
        }

        private static T RequireValue<T>(JToken token, string propertyName)
        {
            var valueToken = token[propertyName];
            return valueToken is not null && valueToken.Type != JTokenType.Null
                ? valueToken.ToObject<T>()!
                : throw new InvalidOperationException($"Editor callback payload is missing '{propertyName}'.");
        }
    }
}
