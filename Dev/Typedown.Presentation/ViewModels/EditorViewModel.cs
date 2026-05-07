using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Typedown.Core.Models;
using Typedown.Core.Models.RuntimeModels;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Services;
using Typedown.Presentation.Utilities;

namespace Typedown.Presentation.ViewModels
{
    public sealed partial class EditorViewModel : INotifyPropertyChanged, IDisposable
    {
        public IServiceProvider ServiceProvider { get; }

        public AppViewModel AppViewModel => ServiceProvider.GetRequiredService<AppViewModel>();
        public FileViewModel FileViewModel => ServiceProvider.GetRequiredService<FileViewModel>();
        public FloatViewModel FloatViewModel => ServiceProvider.GetRequiredService<FloatViewModel>();
        public FormatViewModel FormatViewModel => ServiceProvider.GetRequiredService<FormatViewModel>();
        public SettingsViewModel Settings => ServiceProvider.GetRequiredService<SettingsViewModel>();
        public EventCenter EventCenter => ServiceProvider.GetRequiredService<EventCenter>();
        public RemoteInvoke RemoteInvoke => ServiceProvider.GetRequiredService<RemoteInvoke>();

        public JToken Selection { get; set; } = new JObject();
        public JToken CodeMirrorSelection { get; set; } = new JObject();
        public ContentState ContentState { get; set; } = new();
        public MenuState MenuState { get; set; } = new();
        public ParagraphState ParagraphState { get; set; } = new(new MenuState());
        public TocTreeItem Toc { get; } = new();
        public ContentHistory History { get; } = new();

        public string Markdown { get; set; } = "";
        public bool Selected { get; set; }
        public string SelectionText { get; set; } = string.Empty;
        public bool TextSelected { get; set; }
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

        public IEditorCommandSink EditorCommandSink => ServiceProvider.GetRequiredService<IEditorCommandSink>();
        public IClipboard Clipboard => ServiceProvider.GetRequiredService<IClipboard>();
        public IDialogService DialogService => ServiceProvider.GetRequiredService<IDialogService>();
        public AutoBackup AutoBackup => ServiceProvider.GetRequiredService<AutoBackup>();

        private readonly CompositeDisposable disposables = new();
        private readonly SerialDisposable tocSelectionDisposables = new();

        private bool contentUpdating = false;

        public EditorViewModel(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            disposables.Add(tocSelectionDisposables);
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("MarkdownChange").Subscribe(x => OnMarkdownChange(x.Args)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("FileLoaded").Subscribe(x => OnFileLoaded(x.Args)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("CursorChange").Subscribe(x => OnCursorChange(x.Args)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("SelectionChange").Subscribe(x => OnSelectionChange(x.Args)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("CodeMirrorSelectionChange").Subscribe(x => OnCodeMirrorSelectionChange(x.Args)));
            disposables.Add(EventCenter.GetObservable<EditorEventArgs>("StateChange").Subscribe(x => OnStateChange(x.Args)));
            disposables.Add(RemoteInvoke.Handle("GetSettings", GetSettings));
            disposables.Add(RemoteInvoke.Handle<JToken>("SetClipboard", OnSetClipboard));
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

        public async Task<object> GetSettings()
        {
            if (FirstStart)
            {
                FirstStart = false;
                await FileViewModel.LoadStartUpMarkdown();
            }
            var settings = new Dictionary<string, object>(Settings.GetEditorSettings(), StringComparer.Ordinal)
            {
                ["markdown"] = Markdown,
                ["basePath"] = FileViewModel.ImageBasePath,
            };
            return settings;
        }

        public void OnSelectionChange(JToken arg)
        {
            Selection = arg["selection"] ?? new JObject();
            MenuState = arg["menuState"]?.ToObject<MenuState>() ?? new MenuState();
            SelectionText = arg["selectionText"]?.ToString() ?? string.Empty;
            ParagraphState = new ParagraphState(MenuState);
            UpdateMuyaSelected();
        }

        public void OnCodeMirrorSelectionChange(JToken arg)
        {
            contentUpdating = false;
            CodeMirrorSelection = arg["cursor"] ?? new JObject();
            var anchor = CodeMirrorSelection["anchor"];
            var head = CodeMirrorSelection["head"];
            if (anchor is null || head is null)
            {
                TextSelected = Selected = false;
                SelectionText = string.Empty;
                return;
            }

            var anchorLine = anchor["line"]?.ToObject<int>() ?? 0;
            var headLine = head["line"]?.ToObject<int>() ?? 0;
            var anchorCh = anchor["ch"]?.ToObject<int>() ?? 0;
            var headCh = head["ch"]?.ToObject<int>() ?? 0;
            TextSelected = Selected = anchorLine != headLine || anchorCh != headCh;
            SelectionText = arg["selectionText"]?.ToString() ?? string.Empty;
        }

        public void UpdateMuyaSelected()
        {
            var formatViewModel = ServiceProvider.GetRequiredService<FormatViewModel>();
            TextSelected = Selection["start"]?["offset"]?.ToString() != Selection["end"]?["offset"]?.ToString();
            Selected = TextSelected || formatViewModel.FormatState.Image;
        }

        public async void OnMarkdownChange(string markdown)
        {
            Markdown = markdown;
            if (!contentUpdating) History.ContentChange(Markdown);
            CurrentHash = Common.SimpleHash(Markdown);
            if (!FileLoaded) await Task.Delay(100);
            Saved = FileHash == CurrentHash;
        }

        public void OnFileLoaded(JToken arg)
        {
            if (!FileLoaded)
            {
                FileLoaded = true;
                var newText = arg["text"]?.ToString() ?? string.Empty;
                FileHash = Common.SimpleHash(newText);
                History.InitHistory(newText);
                OnMarkdownChange(newText);
                if (FloatViewModel.FindReplaceDialogOpen > 0)
                    OnSearch();
            }
        }

        public void OnMarkdownChange(JToken arg)
        {
            OnMarkdownChange(arg["text"]?.ToString() ?? string.Empty);
        }

        public void OnCursorChange(JToken arg)
        {
            if (arg["cursor"]?.ToObject<CursorState>() is CursorState cursor)
                History.CursorChange(cursor);
        }

        public void OnStateChange(JToken arg)
        {
            contentUpdating = false;
            var contentState = arg["state"]?.ToObject<ContentState>();
            if (contentState is null)
                return;

            ContentState = contentState;
            if (ContentState.Cur != null)
            {
                var tocSelectionHandlers = new CompositeDisposable();
                ContentState.Toc.ForEach(x =>
                {
                    x.IsSelected = x.Slug == ContentState.Cur.Slug;
                    EventHandler<bool> handler = (_, b) => { if (b) JumpBySlug(x.Slug); };
                    x.SelectedChanged += handler;
                    tocSelectionHandlers.Add(Disposable.Create(() => x.SelectedChanged -= handler));
                });
                tocSelectionDisposables.Disposable = tocSelectionHandlers;
            }
            else
            {
                tocSelectionDisposables.Disposable = Disposable.Empty;
            }
            Toc.UpdateChildren(ContentState.Toc);
        }

        public void OnSearch()
        {
            if (string.IsNullOrEmpty(SearchValue))
                SearchValue = null;
            EditorCommandSink?.Send("Search", new
            {
                value = SearchValue,
                opt = new
                {
                    searchIsCaseSensitive = Settings.SearchIsCaseSensitive,
                    searchIsWholeWord = Settings.SearchIsWholeWord,
                    searchIsRegexp = Settings.SearchIsRegexp,
                    selection = Settings.SourceCode ? CodeMirrorSelection : Selection
                }
            });
        }

        public void Undo()
        {
            var state = History.Undo();
            if (state == null)
            {
                return;
            }
            OnMarkdownChange(state.Text ?? string.Empty);
            contentUpdating = true;
            EditorCommandSink?.Send("SetMarkdown", new
            {
                text = state.Text,
                cursor = state.Cursor,
                basePath = FileViewModel.ImageBasePath
            });
        }

        public void Redo()
        {
            var state = History.Redo();
            if (state == null)
            {
                return;
            }
            OnMarkdownChange(state.Text ?? string.Empty);
            contentUpdating = true;
            EditorCommandSink?.Send("SetMarkdown", new
            {
                text = state.Text,
                cursor = state.Cursor
            });
        }

        public void Cut(string type)
        {
            EditorCommandSink?.Send("Cut", new { type });
        }

        public async void Paste(string type)
        {
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
                        EditorCommandSink?.Send("InsertImage", img);
                        return;
                    }
                    EditorCommandSink?.Send("Paste", new { type, text, html });
                }
                else if (await Clipboard.GetFileDropListAsync() is StringCollection files && files.Count == 1)
                {
                    var file = files[0];
                    if (!string.IsNullOrEmpty(file) && FileTypeHelper.IsImageFile(file))
                    {
                        EditorCommandSink?.Send("InsertImage", new HtmlImgTag(src: file, alt: Path.GetFileNameWithoutExtension(file)));
                    }
                }
                else if (await Clipboard.GetImageAsync() is IClipboardImage image)
                {

                    var src = await ServiceProvider.GetRequiredService<ImageAction>().DoClipboardAction(image);
                    if (string.IsNullOrWhiteSpace(src))
                        return;
                    src = src.Replace('\\', '/');
                    EditorCommandSink?.Send("InsertImage", new HtmlImgTag(src));

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
            EditorCommandSink?.Send("Copy", new { type });
        }

        public void OnSetClipboard(JToken arg)
        {
            var type = arg["type"]?.ToString();
            var data = arg["data"]?.ToString() ?? string.Empty;
            if (type == "text/plain")
            {
                Clipboard.SetText(data, TextDataFormat.UnicodeText);
            }
            else if (type == "text/html")
            {
                Clipboard.SetText(data, TextDataFormat.Html);
            }
        }

        public void DeleteSelection()
        {
            EditorCommandSink?.Send("DeleteSelection", null);
        }

        public void SelectAll()
        {
            EditorCommandSink?.Send("SelectAll", null);
        }

        public void Find(string action)
        {
            var appViewModel = ServiceProvider.GetRequiredService<AppViewModel>();
            if (appViewModel.FloatViewModel.FindReplaceDialogOpen == 0)
            {
                appViewModel.FloatViewModel.FindReplaceDialogOpen = FloatViewModel.FindReplaceDialogState.Search;
                appViewModel.EditorViewModel.OnSearch();
            }
            else
            {
                EditorCommandSink?.Send("Find", new { action });
            }
        }

        public void SearchValueChanged()
        {
            var floatViewModel = ServiceProvider.GetRequiredService<FloatViewModel>();
            if (floatViewModel.FindReplaceDialogOpen > 0)
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

        public void JumpBySlug(string slug)
        {
            EditorCommandSink?.Send("ScrollTo", new { slug });
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
