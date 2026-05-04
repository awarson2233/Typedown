using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.Models;
using Typedown.Presentation.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Muxc = Microsoft.UI.Xaml.Controls;
using PresentationLocale = Typedown.Presentation.Utilities.Locale;

namespace Typedown.WinUI.Controls.SidePaneControls.Pages
{
    public sealed partial class FolderPage : Page
    {
        public AppViewModel ViewModel => (DataContext as AppViewModel)!;

        public FileViewModel FileViewModel => ViewModel.FileViewModel;

        public IFileOperation FileOperation => ViewModel.ServiceProvider.GetRequiredService<IFileOperation>();

        public IClipboard Clipboard => ViewModel.ServiceProvider.GetRequiredService<IClipboard>();

        public ExplorerItem? WorkFolderExplorerItem { get; private set; }

        private readonly CompositeDisposable disposables = new();

        public FolderPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is AppViewModel viewModel)
            {
                DataContext = viewModel;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not AppViewModel)
            {
                return;
            }

            WorkFolderExplorerItem = new ExplorerItem(FileViewModel) { IsExpanded = true };
            Bindings.Update();
            disposables.Add(FileViewModel.WhenPropertyChanged(nameof(FileViewModel.WorkFolder)).Cast<string>().StartWith(FileViewModel.WorkFolder).Subscribe(UpdateWorkFolder));
            disposables.Add(FileViewModel.WhenPropertyChanged(nameof(FileViewModel.FilePath)).Cast<string>().StartWith(FileViewModel.FilePath).Subscribe(_ => UpdateSelectedItem(WorkFolderExplorerItem)));
        }

        private void UpdateWorkFolder(string? workFolder)
        {
            if (WorkFolderExplorerItem is not null)
            {
                WorkFolderExplorerItem.FullPath = workFolder;
            }
        }

        private void UpdateSelectedItem(ExplorerItem? item)
        {
            if (item is null)
            {
                return;
            }

            item.IsSelected = FileViewModel.FilePath == item.FullPath;
            foreach (var next in item.Children)
            {
                UpdateSelectedItem(next);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            WorkFolderExplorerItem?.Dispose();
            WorkFolderExplorerItem = null;
            disposables.Clear();
            TreeView.DataContext = null;
            TreeView.ItemTemplateSelector = null;
            TreeView.ItemsSource = null;
            TreeView.ContextFlyout = null;
            Bindings.StopTracking();
        }

        private void OnItemContextFlyoutOpened(object sender, object e)
        {
            SetItemFocusState(sender, true);
        }

        private void OnItemContextFlyoutClosed(object sender, object e)
        {
            SetItemFocusState(sender, false);
        }

        private void SetItemFocusState(object menuFlyout, bool isFocus)
        {
            var container = GetContainerFromMenuFlyout(menuFlyout);
            if (container is not null)
            {
                container.BorderBrush = Resources[isFocus ? "FocusStrokeColorOuterBrush" : "NormalStrokeColorOuterBrush"] as Brush;
            }
        }

        private Muxc.TreeViewItem? GetContainerFromMenuFlyout(object menuFlyout)
        {
            var item = GetExplorerItemFromMenuFlyout(menuFlyout);
            return TreeView.ContainerFromItem(item) as Muxc.TreeViewItem;
        }

        private static ExplorerItem? GetExplorerItemFromMenuFlyout(object menuFlyout)
        {
            var flyout = menuFlyout as MenuFlyout;
            return flyout?.Items[0].DataContext as ExplorerItem;
        }

        private static ExplorerItem? GetExplorerItemFromMenuFlyoutItem(object menuFlyoutItem)
        {
            return (menuFlyoutItem as MenuFlyoutItem)?.DataContext as ExplorerItem;
        }

        private static ExplorerItem? GetExplorerItemFromTreeViewItem(object menuFlyoutItem)
        {
            return (menuFlyoutItem as Muxc.TreeViewItem)?.DataContext as ExplorerItem;
        }

        private async void OnNewFileClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuFlyoutItem(sender);
            if (item is null)
            {
                return;
            }

            try
            {
                var filename = "Untitled";
                var extension = ".md";
                var fullname = filename + extension;
                if (FileOperation.IsFilenameValid(item.FullPath, fullname))
                {
                    File.Create(Path.Combine(item.FullPath, fullname)).Close();
                }
                else
                {
                    var flag = true;
                    for (var i = 2; i < 10000; i++)
                    {
                        fullname = $"{filename} ({i}){extension}";
                        if (FileOperation.IsFilenameValid(item.FullPath, fullname))
                        {
                            File.Create(Path.Combine(item.FullPath, fullname)).Close();
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                    {
                        return;
                    }
                }
                await Task.Yield();
                item.IsExpanded = true;
                await Task.Delay(100);
                RenameFile(item.Children.FirstOrDefault(x => Path.GetFileName(x.FullPath) == fullname));
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(PresentationLocale.GetDialogString("CreateDocErrorTitle"), ex.Message);
            }
        }

        private async void OnNewFolderClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuFlyoutItem(sender);
            if (item is null)
            {
                return;
            }

            try
            {
                var foldername = "New Folder";
                var fullname = foldername;
                if (FileOperation.IsFilenameValid(item.FullPath, fullname))
                {
                    Directory.CreateDirectory(Path.Combine(item.FullPath, fullname));
                }
                else
                {
                    var flag = true;
                    for (var i = 2; i < 10000; i++)
                    {
                        fullname = $"{foldername} ({i})";
                        if (FileOperation.IsFilenameValid(item.FullPath, fullname))
                        {
                            try
                            {
                                Directory.CreateDirectory(Path.Combine(item.FullPath, fullname));
                            }
                            catch
                            {
                                return;
                            }
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                    {
                        return;
                    }
                }
                await Task.Yield();
                item.IsExpanded = true;
                await Task.Delay(100);
                RenameFile(item.Children.FirstOrDefault(x => Path.GetFileName(x.FullPath) == fullname));
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(PresentationLocale.GetDialogString("CreateFolderErrorTitle"), ex.Message);
            }
        }

        private void OnOpenFileLocationClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuFlyoutItem(sender);
            Common.OpenFileLocation(item?.FullPath);
        }

        private void OnCutClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuFlyoutItem(sender);
            _ = FileOperation.CutToClipboardAsync(new StringCollection { item?.FullPath });
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuFlyoutItem(sender);
            _ = FileOperation.CopyToClipboardAsync(new StringCollection { item?.FullPath });
        }

        private void OnPasteClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuFlyoutItem(sender);
            FileOperation.PasteFromClipboard(item?.FullPath);
        }

        private void OnCopyAsPathClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuFlyoutItem(sender);
            Clipboard.SetText(item?.FullPath);
        }

        private void OnRenameClick(object sender, RoutedEventArgs e)
        {
            RenameFile(GetExplorerItemFromMenuFlyoutItem(sender));
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuFlyoutItem(sender);
            FileOperation.Delete(new StringCollection { item?.FullPath });
        }

        private async void RenameFile(ExplorerItem? item)
        {
            if (item == null || TreeView.ContainerFromItem(item) is not Muxc.TreeViewItem treeViewItem)
            {
                return;
            }

            var container = treeViewItem.FindName("TextBoxContainer") as ContentPresenter;
            var textblock = treeViewItem.FindName("NameTextBlock") as TextBlock;
            if (container is null || textblock is null)
            {
                return;
            }

            var textBox = new TextBox
            {
                Style = Resources["RenameTextBoxStyle"] as Style,
                Text = Path.GetFileName(item.FullPath),
            };
            var source = new TaskCompletionSource<string>();
            container.Content = textBox;
            textBox.Loaded += (s, e) =>
            {
                textblock.Visibility = Visibility.Collapsed;
                textBox.Focus(FocusState.Programmatic);
                if (item.Type == ExplorerItem.ExplorerItemType.Folder || !textBox.Text.Contains('.'))
                {
                    textBox.SelectAll();
                }
                else
                {
                    textBox.Select(0, textBox.Text.LastIndexOf('.'));
                }
            };
            textBox.LostFocus += (s, e) =>
            {
                if (!source.Task.IsCompleted)
                {
                    source.SetResult(textBox.Text);
                }
                container.Content = null;
            };
            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == global::Windows.System.VirtualKey.Enter)
                {
                    if (!source.Task.IsCompleted)
                    {
                        source.SetResult(textBox.Text);
                    }
                    container.Content = null;
                }
            };
            await source.Task;
            textblock.Visibility = Visibility.Visible;
            if (source.Task.Result != Path.GetFileName(item.FullPath))
            {
                var parent = Path.GetDirectoryName(item.FullPath);
                if (parent is null)
                {
                    return;
                }

                var newPath = Path.Combine(parent, source.Task.Result);
                if (item.FullPath == ViewModel.FileViewModel.FilePath)
                {
                    ViewModel.FileViewModel.RenameFile(newPath);
                }
                else
                {
                    FileOperation.Rename(item.FullPath, newPath);
                }
                DispatcherQueue.TryEnqueue(() => UpdateSelectedItem(WorkFolderExplorerItem));
            }
        }

        public static void OnTreeViewItemLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Muxc.TreeViewItem item || !item.IsLoaded || VisualTreeHelper.GetChild(item, 0) is not UIElement grid || GetAncestor<FolderPage>(item) is not FolderPage page)
            {
                return;
            }

            var treeViewItemPointerPressedEventHandler = new PointerEventHandler(OnTreeViewItemPointerPressed);
            var treeViewItemPointerReleasedEventHandler = new PointerEventHandler(page.OnTreeViewItemPointerReleased);
            grid.AddHandler(PointerPressedEvent, treeViewItemPointerPressedEventHandler, true);
            item.AddHandler(PointerReleasedEvent, treeViewItemPointerReleasedEventHandler, true);
            if (item.DataContext is ExplorerItem explorerItem)
            {
                page.UpdateSelectedItem(explorerItem);
            }

            item.Unloaded += OnTreeViewItemUnloaded;
            void OnTreeViewItemUnloaded(object unloadSender, RoutedEventArgs unloadArgs)
            {
                item.Unloaded -= OnTreeViewItemUnloaded;
                grid.RemoveHandler(PointerPressedEvent, treeViewItemPointerPressedEventHandler);
                item.RemoveHandler(PointerReleasedEvent, treeViewItemPointerReleasedEventHandler);
            }
        }

        private static void OnTreeViewItemPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.OriginalSource is not FrameworkElement originalSource)
            {
                return;
            }

            if (originalSource.Name != "ExpandCollapseChevron" && GetAncestor<TextBox>(originalSource) is null)
            {
                if ((sender as Grid)?.DataContext is ExplorerItem item && item.Type == ExplorerItem.ExplorerItemType.Folder)
                {
                    e.Handled = true;
                    if (e.GetCurrentPoint(sender as UIElement).Properties.IsLeftButtonPressed)
                    {
                        item.IsExpanded = !item.IsExpanded;
                    }
                }
            }
        }

        private async void OnTreeViewItemPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                var kind = e.GetCurrentPoint(sender as UIElement).Properties.PointerUpdateKind;
                if (kind == Microsoft.UI.Input.PointerUpdateKind.LeftButtonReleased &&
                    (sender as Muxc.TreeViewItem)?.DataContext is ExplorerItem item &&
                    item.Type == ExplorerItem.ExplorerItemType.File)
                {
                    if (item.FullPath != ViewModel.FileViewModel.FilePath)
                    {
                        await ViewModel.FileViewModel.OpenFile(item.FullPath);
                    }
                }
                UpdateSelectedItem(WorkFolderExplorerItem);
            }
            catch
            {
                // Ignore
            }
        }

        private async void OnOpenClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuFlyoutItem(sender);
            if (item is not null)
            {
                await ViewModel.FileViewModel.OpenFile(item.FullPath);
            }
            UpdateSelectedItem(WorkFolderExplorerItem);
        }

        private void OnOpenInNewWindowClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuFlyoutItem(sender);
            if (item is not null)
            {
                ViewModel.FileViewModel.NewWindowCommand.Execute(item.FullPath);
            }
        }

        public static async void OnItemDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            try
            {
                if (sender is not Muxc.TreeViewItem treeViewItem || GetAncestor<FolderPage>(treeViewItem) is not FolderPage page)
                {
                    return;
                }
                var item = GetExplorerItemFromTreeViewItem(sender);
                if (item?.Type == ExplorerItem.ExplorerItemType.File)
                {
                    var file = await StorageFile.GetFileFromPathAsync(item.FullPath);
                    args.Data.SetStorageItems(new List<IStorageItem> { file });
                }
                if (item?.Type == ExplorerItem.ExplorerItemType.Folder)
                {
                    var folder = await StorageFolder.GetFolderFromPathAsync(item.FullPath);
                    args.Data.SetStorageItems(new List<IStorageItem> { folder });
                }
            }
            catch
            {
                // Ignore
            }
        }

        public static void OnItemDragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (sender is not Muxc.TreeViewItem treeViewItem || GetAncestor<FolderPage>(treeViewItem) is not FolderPage page)
                {
                    return;
                }
                var target = GetExplorerItemFromTreeViewItem(sender);
                if (e.DataView.Contains(StandardDataFormats.StorageItems) &&
                    target?.Type == ExplorerItem.ExplorerItemType.Folder)
                {
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
            }
            catch
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        public static async void OnFolderItemDrop(object sender, DragEventArgs e)
        {
            if (sender is not Muxc.TreeViewItem treeViewItem || GetAncestor<FolderPage>(treeViewItem) is not FolderPage page)
            {
                return;
            }
            try
            {
                var target = GetExplorerItemFromTreeViewItem(sender);
                if (e.DataView.Contains(StandardDataFormats.StorageItems) &&
                    target?.Type == ExplorerItem.ExplorerItemType.Folder)
                {
                    var items = await e.DataView.GetStorageItemsAsync();
                    e.AcceptedOperation = DataPackageOperation.Move;
                    var collection = new StringCollection();
                    foreach (var item in items)
                    {
                        collection.Add(item.Path);
                    }
                    page.FileOperation.Move(collection, target.FullPath);
                }
            }
            catch (Exception ex)
            {
                await page.ShowErrorAsync(PresentationLocale.GetString("Error"), ex.Message);
            }
        }

        private void OnTreeViewContextFlyoutOpening(object sender, object e)
        {
            if (!Directory.Exists(FileViewModel.WorkFolder) && sender is MenuFlyout flyout)
            {
                flyout.Hide();
            }
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = title,
                Content = message,
                CloseButtonText = PresentationLocale.GetDialogString("Ok")
            };
            await dialog.ShowAsync();
        }

        private static T? GetAncestor<T>(FrameworkElement element) where T : FrameworkElement
        {
            for (var current = VisualTreeHelper.GetParent(element); current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is T match)
                {
                    return match;
                }
            }

            return null;
        }
    }

    public sealed class ExplorerItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? FolderTemplate { get; set; }
        public DataTemplate? FileTemplate { get; set; }

        protected override DataTemplate? SelectTemplateCore(object item)
        {
            return item is ExplorerItem explorerItem && explorerItem.Type == ExplorerItem.ExplorerItemType.Folder ? FolderTemplate : FileTemplate;
        }
    }
}
