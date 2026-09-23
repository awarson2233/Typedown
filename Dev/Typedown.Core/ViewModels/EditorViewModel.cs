using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Typedown.Core.Editor;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Models.RuntimeModels;
using Typedown.Core.Services;
using Typedown.Core.Utilities;

namespace Typedown.Core.ViewModels
{
    public sealed partial class EditorViewModel : INotifyPropertyChanged, IEditorHostCallbacks, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public AppViewModel AppViewModel => ServiceProvider.GetRequiredService<AppViewModel>();
        public FileViewModel FileViewModel => ServiceProvider.GetRequiredService<FileViewModel>();
        public FloatViewModel FloatViewModel => ServiceProvider.GetRequiredService<FloatViewModel>();
        public FormatViewModel FormatViewModel => ServiceProvider.GetRequiredService<FormatViewModel>();
        public SettingsViewModel Settings => ServiceProvider.GetRequiredService<SettingsViewModel>();

        /// <summary>与编辑引擎对话的唯一通道。</summary>
        public IEditorSession Session { get; }

        public ContentState ContentState { get; set; } = new();
        public ParagraphState ParagraphState { get; set; } = new();
        public TocTreeItem Toc { get; } = new();

        /// <summary>富文本模式下当前选中的图片；源码模式保留最后一次富文本选区的结果。</summary>
        public ImageInfo? SelectedImage { get; private set; }

        /// <summary>当前正文，即会话的正文镜像。</summary>
        public string Markdown => Session.Document.Text;

        public bool Selected { get; set; }
        public string SelectionText { get; set; } = string.Empty;
        public bool TextSelected { get; set; }
        public bool CanUndo { get; private set; }
        public bool CanRedo { get; private set; }
        public bool Saved { get; set; } = true;
        public bool AutoSavedSucc { get; set; } = true;
        public bool DisplaySaved { get; set; } = true;
        public ulong FileHash { get; set; }
        public ulong CurrentHash { get; set; }
        public string? SearchValue { get; set; }
        public bool FirstStart { get; set; } = true;
        public bool FileLoaded { get; set; }

        public Command<Unit> UndoCommand { get; } = new();
        public Command<Unit> RedoCommand { get; } = new();
        public Command<string> CutCommand { get; } = new();
        public Command<string> PasteCommand { get; } = new();
        public Command<string> CopyCommand { get; } = new();
        public Command<Unit> DeleteSelectionCommand { get; } = new();
        public Command<Unit> SelectAllCommand { get; } = new();
        public Command<string> FindCommand { get; } = new();

        public IClipboard Clipboard => ServiceProvider.GetRequiredService<IClipboard>();
        public IDialogService DialogService => ServiceProvider.GetRequiredService<IDialogService>();
        public AutoBackup AutoBackup => ServiceProvider.GetRequiredService<AutoBackup>();

        private readonly CompositeDisposable disposables = new();
        private readonly SerialDisposable tocSelectionDisposables = new();
        private bool disposed;

        /// <summary>最近一次富文本选区是否选中了文字；格式变化时据此重算 <see cref="Selected"/>。</summary>
        private bool richTextSelected;

        private PendingImportGate? pendingImportGate;

        public PendingImportGate PendingImportGate => pendingImportGate ??= new();

        public EditorViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            Session = ServiceProvider.GetRequiredService<IEditorSession>();
            disposables.Add(tocSelectionDisposables);
            UndoCommand.SetCanExecuteFunc.OnNext(_ => CanUndo);
            RedoCommand.SetCanExecuteFunc.OnNext(_ => CanRedo);
            disposables.Add(Session.Events.Subscribe(OnEditorEvent));
            Settings.PropertyChanged += HandleSettingsPropertyChanged;
            disposables.Add(Disposable.Create(() => Settings.PropertyChanged -= HandleSettingsPropertyChanged));
            disposables.Add(Settings.WhenPropertyChanged(nameof(Settings.AutoSave)).Subscribe(_ => Settings_AutoSaveChanged(Settings.AutoSave)));
            disposables.Add(this.WhenPropertyChanged(nameof(SearchValue)).Subscribe(_ => SearchValueChanged()));
            disposables.Add(this.WhenPropertyChanged(nameof(Saved)).Subscribe(_ => SavedOrAutoSavedSuccChanged()));
            disposables.Add(this.WhenPropertyChanged(nameof(AutoSavedSucc)).Subscribe(_ => SavedOrAutoSavedSuccChanged()));
            disposables.Add(UndoCommand.OnExecute.Subscribe(_ => Undo()));
            disposables.Add(RedoCommand.OnExecute.Subscribe(_ => Redo()));
            disposables.Add(FindCommand.OnExecute.Subscribe(x => Find(x)));
            disposables.Add(PasteCommand.OnExecute.Subscribe(x => Paste(x)));
            disposables.Add(CutCommand.OnExecute.Subscribe(x => Cut(x)));
            disposables.Add(CopyCommand.OnExecute.Subscribe(x => Copy(x)));
            disposables.Add(DeleteSelectionCommand.OnExecute.Subscribe(_ => DeleteSelection()));
            disposables.Add(SelectAllCommand.OnExecute.Subscribe(_ => SelectAll()));
        }

        // ── 引擎事件 ─────────────────────────────────────────────────────

        private void OnEditorEvent(EditorEvent editorEvent)
        {
            switch (editorEvent)
            {
                case DocumentChanged changed:
                    OnMarkdownChange(changed.Text);
                    break;
                case DocumentLoaded loaded:
                    OnDocumentLoaded(loaded.Text);
                    break;
                case HistoryChanged history:
                    CanUndo = history.CanUndo;
                    CanRedo = history.CanRedo;
                    UndoCommand.RaiseCanExecuteChanged();
                    RedoCommand.RaiseCanExecuteChanged();
                    break;
                case SelectionChanged selection:
                    ApplySelection(selection);
                    break;
                case OutlineChanged outline:
                    ApplyOutline(outline);
                    break;
                case StatsChanged stats:
                    ContentState = new ContentState
                    {
                        WordCount = new WordCount { Word = stats.Words, Character = stats.Characters },
                        Toc = ContentState.Toc,
                        Cur = ContentState.Cur,
                    };
                    break;
                case LinkOpenRequested link:
                    OpenUri(link.Uri);
                    break;
            }
        }

        private void ApplySelection(SelectionChanged selection)
        {
            SelectionText = selection.Text;
            if (selection.Rich is { } rich)
            {
                ParagraphState = new ParagraphState(rich.Block);
                SelectedImage = rich.SelectedImage;
                richTextSelected = selection.HasText;
                UpdateMuyaSelected();
            }
            else
            {
                TextSelected = Selected = selection.HasText;
            }
        }

        /// <summary>富文本模式下的选中态：选中文字，或者选中了一张图。</summary>
        public void UpdateMuyaSelected()
        {
            TextSelected = richTextSelected;
            Selected = TextSelected || FormatViewModel.FormatState.Image;
        }

        public async void OnMarkdownChange(string markdown)
        {
            OnPropertyChanged(nameof(Markdown));
            CurrentHash = Common.SimpleHash(markdown);
            // 文件刚装载时前端还在归一化正文，稍等一拍再判定保存状态，避免闪一下"未保存"。
            if (!FileLoaded) await Task.Delay(100);
            Saved = FileHash == CurrentHash;
        }

        /// <summary>
        /// 引擎完成装载后的回声。此时的正文已被编辑器归一化过，
        /// 以它为准重算基线哈希、重置撤销历史，保存状态才不会一开门就是脏的。
        /// </summary>
        private void OnDocumentLoaded(string text)
        {
            if (FileLoaded) return;
            FileLoaded = true;
            FileHash = Common.SimpleHash(text);
            Session.Post(new ClearUndoHistory());
            OnMarkdownChange(text);
            if (FloatViewModel.FindReplaceDialogOpen > 0) OnSearch();
        }

        private void ApplyOutline(OutlineChanged outline)
        {
            var toc = outline.Items
                .Select(x => new TocItem { Slug = x.Id.Value, Lvl = x.Level, Content = x.Text })
                .ToList();
            var current = outline.Current is { } cur
                ? new TocItem { Slug = cur.Id.Value, Lvl = cur.Level, Content = cur.Text }
                : null;
            ContentState = new ContentState { WordCount = ContentState.WordCount, Toc = toc, Cur = current };

            var tocSelectionHandlers = new CompositeDisposable();
            toc.ForEach(x =>
            {
                x.IsSelected = x.Slug == current?.Slug;
                EventHandler<bool> handler = (_, b) =>
                {
                    if (b) JumpBySlug(x.Slug);
                };
                x.SelectedChanged += handler;
                tocSelectionHandlers.Add(Disposable.Create(() => x.SelectedChanged -= handler));
            });
            tocSelectionDisposables.Disposable = tocSelectionHandlers;
            Toc.UpdateChildren(toc);
        }

        private bool OpenUri(string? uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return false;
            }

            try
            {
                if (!UriHelper.IsWebUrl(uri) && UriHelper.TryGetLocalPath(uri, out var localPath))
                {
                    var currentFolder = Path.GetDirectoryName(FileViewModel.FilePath ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(currentFolder))
                    {
                        var fullPath = Path.GetFullPath(Path.Combine(currentFolder, localPath));
                        if (File.Exists(fullPath))
                        {
                            if (FileTypeHelper.IsMarkdownFile(fullPath))
                            {
                                FileViewModel.NewWindowCommand.Execute(fullPath);
                            }
                            else
                            {
                                Common.OpenUrl(fullPath);
                            }

                            return true;
                        }
                    }
                }

                Common.OpenUrl(uri);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ── 宿主回调（IEditorHostCallbacks）──────────────────────────────

        public string BasePath => FileViewModel.ImageBasePath;

        public async Task<EditorSettings> PrepareStartupAsync(CancellationToken cancellationToken)
        {
            if (FirstStart)
            {
                FirstStart = false;
                await FileViewModel.LoadStartUpMarkdown();
            }

            return CreateEditorSettings();
        }

        public async Task<TableSize?> PickTableSizeAsync(CancellationToken cancellationToken)
        {
            var result = await ServiceProvider.GetRequiredService<ITableDialogService>().OpenResizeTableDialogAsync();
            return result is null ? null : new TableSize(result.Rows, result.Columns);
        }

        public Task WriteClipboardAsync(ClipboardContent content, CancellationToken cancellationToken)
        {
            if (content.Html is not null)
            {
                Clipboard.SetText(content.Html, TextDataFormat.Html);
            }

            if (content.PlainText is not null)
            {
                Clipboard.SetText(content.PlainText, TextDataFormat.UnicodeText);
            }

            return Task.CompletedTask;
        }

        // ── 设置 → 引擎 ──────────────────────────────────────────────────

        /// <summary>全部编辑器设置，引擎启动时整体下发。</summary>
        public EditorSettings CreateEditorSettings() => new()
        {
            SourceCode = Settings.SourceCode,
            Typewriter = Settings.Typewriter,
            FocusMode = Settings.FocusMode,
            SearchIsCaseSensitive = Settings.SearchIsCaseSensitive,
            SearchIsRegexp = Settings.SearchIsRegexp,
            SearchIsWholeWord = Settings.SearchIsWholeWord,
            FontSize = Settings.FontSize,
            LineHeight = Settings.LineHeight,
            AutoPairBracket = Settings.AutoPairBracket,
            AutoPairQuote = Settings.AutoPairQuote,
            TrimUnnecessaryCodeBlockEmptyLines = Settings.TrimUnnecessaryCodeBlockEmptyLines,
            PreferLooseListItem = Settings.PreferLooseListItem,
            AutoPairMarkdownSyntax = Settings.AutoPairMarkdownSyntax,
            EditorAreaWidth = Settings.EditorAreaWidth,
            TabSize = Settings.TabSize,
            SpellcheckEnabled = Settings.SpellcheckEnabled,
        };

        /// <summary>单个设置项变化对应的编辑器设置增量；与编辑器无关的设置返回 <c>null</c>。</summary>
        public EditorSettings? CreateEditorSettingsChange(string? propertyName) => propertyName switch
        {
            nameof(SettingsViewModel.SourceCode) => new() { SourceCode = Settings.SourceCode },
            nameof(SettingsViewModel.Typewriter) => new() { Typewriter = Settings.Typewriter },
            nameof(SettingsViewModel.FocusMode) => new() { FocusMode = Settings.FocusMode },
            nameof(SettingsViewModel.SearchIsCaseSensitive) => new() { SearchIsCaseSensitive = Settings.SearchIsCaseSensitive },
            nameof(SettingsViewModel.SearchIsRegexp) => new() { SearchIsRegexp = Settings.SearchIsRegexp },
            nameof(SettingsViewModel.SearchIsWholeWord) => new() { SearchIsWholeWord = Settings.SearchIsWholeWord },
            nameof(SettingsViewModel.FontSize) => new() { FontSize = Settings.FontSize },
            nameof(SettingsViewModel.LineHeight) => new() { LineHeight = Settings.LineHeight },
            nameof(SettingsViewModel.AutoPairBracket) => new() { AutoPairBracket = Settings.AutoPairBracket },
            nameof(SettingsViewModel.AutoPairQuote) => new() { AutoPairQuote = Settings.AutoPairQuote },
            nameof(SettingsViewModel.TrimUnnecessaryCodeBlockEmptyLines) => new() { TrimUnnecessaryCodeBlockEmptyLines = Settings.TrimUnnecessaryCodeBlockEmptyLines },
            nameof(SettingsViewModel.PreferLooseListItem) => new() { PreferLooseListItem = Settings.PreferLooseListItem },
            nameof(SettingsViewModel.AutoPairMarkdownSyntax) => new() { AutoPairMarkdownSyntax = Settings.AutoPairMarkdownSyntax },
            nameof(SettingsViewModel.EditorAreaWidth) => new() { EditorAreaWidth = Settings.EditorAreaWidth },
            nameof(SettingsViewModel.TabSize) => new() { TabSize = Settings.TabSize },
            nameof(SettingsViewModel.SpellcheckEnabled) => new() { SpellcheckEnabled = Settings.SpellcheckEnabled },
            _ => null,
        };

        private void HandleSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!disposed && CreateEditorSettingsChange(e.PropertyName) is { } change)
            {
                Session.Post(new ApplySettings(change));
            }
        }

        // ── 命令 ─────────────────────────────────────────────────────────

        /// <summary>装载一篇文档：正文镜像与撤销历史随之重置，引擎在就绪后收到它。</summary>
        public void LoadDocument(string text, string basePath)
        {
            Session.Post(new LoadDocument(text, basePath));
            OnPropertyChanged(nameof(Markdown));
        }

        public Task<PendingImportGate.PersistenceLease?> AcquirePersistenceLeaseAsync() =>
            PendingImportGate.AcquireAsync(TimeSpan.FromSeconds(3));

        public void OnSearch()
        {
            if (string.IsNullOrEmpty(SearchValue))
                SearchValue = null;
            Session.Post(new Search(SearchValue, CurrentSearchOptions()));
        }

        public SearchOptions CurrentSearchOptions() =>
            new(Settings.SearchIsCaseSensitive, Settings.SearchIsWholeWord, Settings.SearchIsRegexp);

        public void Undo() => Session.Post(new Undo());

        public void Redo() => Session.Post(new Redo());

        public void Cut(string type) => Session.Post(new Cut());

        public async void Paste(string type)
        {
            var format = type == "pasteAsPlainText" ? PasteFormat.PlainText : PasteFormat.Rich;
            try
            {
                if (Clipboard.ContainsText(TextDataFormat.UnicodeText) || Clipboard.ContainsText(TextDataFormat.Html))
                {
                    var text = await Clipboard.GetTextAsync(TextDataFormat.UnicodeText);
                    var html = await Clipboard.GetTextAsync(TextDataFormat.Html);
                    if (Common.MatchHtmlImg(html) is HtmlImgTag img)
                    {
                        if (UriHelper.IsWebUrl(img.Src))
                        {
                            img.Src = await ServiceProvider.GetRequiredService<ImageAction>().DoWebFileAction(img.Src);
                        }
                        else if (UriHelper.TryGetLocalPath(img.Src, out _))
                        {
                            img.Src = await ServiceProvider.GetRequiredService<ImageAction>().DoLocalFileAction(img.Src);
                            if (string.IsNullOrWhiteSpace(img.Src))
                                return;
                            img.Src = img.Src.Replace('\\', '/');
                        }
                        Session.Post(new InsertImage(img.Src, img.Alt, img.Title));
                        return;
                    }
                    Session.Post(new Paste(format, text, html));
                }
                else if (await Clipboard.GetFileDropListAsync() is StringCollection files && files.Count == 1)
                {
                    var file = files[0];
                    if (!string.IsNullOrEmpty(file) && FileTypeHelper.IsImageFile(file))
                    {
                        Session.Post(new InsertImage(file, Path.GetFileNameWithoutExtension(file)));
                    }
                }
                else if (await Clipboard.GetImageAsync() is IClipboardImage image)
                {
                    var src = await ServiceProvider.GetRequiredService<ImageAction>().DoClipboardAction(image);
                    if (string.IsNullOrWhiteSpace(src))
                        return;
                    src = src.Replace('\\', '/');
                    Session.Post(new InsertImage(src));
                }
            }
            catch (Exception ex)
            {
                await DialogService.ShowAsync(new DialogRequest
                {
                    Title = Locale.GetString("Error"),
                    Content = ex.Message,
                    CloseButtonText = Locale.GetDialogString("Ok"),
                    DefaultButton = DialogDefaultButton.Close,
                });
            }
        }

        public void Copy(string type)
        {
            var format = type switch
            {
                "copyAsPlainText" => CopyFormat.PlainText,
                "copyAsMarkdown" => CopyFormat.Markdown,
                "copyAsHtml" => CopyFormat.Html,
                _ => CopyFormat.Rich,
            };
            Session.Post(new Copy(format));
        }

        public void DeleteSelection() => Session.Post(new DeleteSelection());

        public void SelectAll() => Session.Post(new SelectAll());

        public void Find(string action)
        {
            if (FloatViewModel.FindReplaceDialogOpen == 0)
            {
                FloatViewModel.FindReplaceDialogOpen = FloatViewModel.FindReplaceDialogState.Search;
                OnSearch();
            }
            else
            {
                // 页面把 "next" 以外的动作一律当作上一个。
                Session.Post(new FindMatch(action == "next" ? SearchDirection.Next : SearchDirection.Previous));
            }
        }

        public void SearchValueChanged()
        {
            if (FloatViewModel.FindReplaceDialogOpen > 0)
                OnSearch();
        }

        public void SavedOrAutoSavedSuccChanged()
        {
            try
            {
                var fileViewModel = ServiceProvider.GetRequiredService<FileViewModel>();
                DisplaySaved = Saved || (Settings.AutoSave && fileViewModel.FilePath != null && AutoSavedSucc);
                if (Saved && fileViewModel.FilePath is not null)
                    AutoBackup.DeleteBackup(fileViewModel.FilePath);
            }
            catch
            {
                // Ignore
            }
        }

        public void Settings_AutoSaveChanged(bool autoSave)
        {
            DisplaySaved = Saved || autoSave;
        }

        public void JumpBySlug(string slug) => Session.Post(new RevealHeading(new HeadingId(slug)));

        public void Dispose()
        {
            disposed = true;
            disposables.Dispose();
        }
    }
}
