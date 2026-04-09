using PropertyChanged;
﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace Typedown.Core.Controls.SidePanelControls.Pages
{
[DoNotNotify]
        public sealed partial class FolderPage : UserControl, INotifyPropertyChanged
    {
        public AppViewModel ViewModel => DataContext as AppViewModel;

        public FileViewModel FileViewModel => ViewModel?.FileViewModel;

        public IFileOperation FileOperation => this.GetService<IFileOperation>();

        public IClipboard Clipboard => this.GetService<IClipboard>();

        public ExplorerItem WorkFolderExplorerItem { get; private set; }

        private readonly CompositeDisposable disposables = new();

        public FolderPage()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            WorkFolderExplorerItem = new ExplorerItem(FileViewModel) { IsExpanded = true };
            disposables.Add(FileViewModel.WhenPropertyChanged(nameof(FileViewModel.WorkFolder)).Cast<string>().StartWith(FileViewModel.WorkFolder).Subscribe(UpdateWorkFolder));
            disposables.Add(FileViewModel.WhenPropertyChanged(nameof(FileViewModel.FilePath)).Cast<string>().StartWith(FileViewModel.FilePath).Subscribe(_ => UpdateSelectedItem(WorkFolderExplorerItem)));
        }

        private void UpdateWorkFolder(string workFolder)
        {
            WorkFolderExplorerItem.FullPath = workFolder;
        }

        private void UpdateSelectedItem(ExplorerItem item)
        {
            item.IsSelected = FileViewModel.FilePath == item.FullPath;
            foreach (var next in item.Children)
                UpdateSelectedItem(next);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            WorkFolderExplorerItem?.Dispose();
            WorkFolderExplorerItem = null;
            disposables.Clear();
            var treeView = this.FindControl<TreeView>("TreeView");
            if (treeView != null)
            {
                treeView.DataContext = null;
                treeView.ItemsSource = null;
            }
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
            // TODO: Implement focus border highlight for Avalonia TreeViewItem
        }

        private ExplorerItem GetExplorerItemFromMenuItem(object menuFlyoutItem)
        {
            return (menuFlyoutItem as MenuItem)?.DataContext as ExplorerItem;
        }

        private async void OnNewFileClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuItem(sender);
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
                    if (flag) return;
                }
                await Task.Yield();
                item.IsExpanded = true;
            }
            catch (Exception ex)
            {
                await AppContentDialog.Create(Locale.GetDialogString("CreateDocErrorTitle"), ex.Message, Locale.GetDialogString("Ok")).ShowAsync(this.VisualRoot as Control);
            }
        }

        private async void OnNewFolderClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuItem(sender);
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
                            catch { return; }
                            flag = false;
                            break;
                        }
                    }
                    if (flag) return;
                }
                await Task.Yield();
                item.IsExpanded = true;
            }
            catch (Exception ex)
            {
                await AppContentDialog.Create(Locale.GetDialogString("CreateFolderErrorTitle"), ex.Message, Locale.GetDialogString("Ok")).ShowAsync(this.VisualRoot as Control);
            }
        }

        private void OnOpenFileLocationClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuItem(sender);
            Common.OpenFileLocation(item?.FullPath);
        }

        private void OnCutClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuItem(sender);
            FileOperation.CutToClipboardAsync(new StringCollection() { item?.FullPath });
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuItem(sender);
            FileOperation.CopyToClipboardAsync(new StringCollection() { item?.FullPath });
        }

        private void OnPasteClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuItem(sender);
            FileOperation.PasteFromClipboard(item?.FullPath);
        }

        private void OnCopyAsPathClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuItem(sender);
            Clipboard.SetText(item?.FullPath);
        }

        private void OnRenameClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuItem(sender);
            // TODO: Implement inline rename for Avalonia TreeView
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuItem(sender);
            FileOperation.Delete(new StringCollection() { item?.FullPath });
        }

        private async void OnOpenClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuItem(sender);
            if (item != null)
                await ViewModel.FileViewModel.OpenFile(item.FullPath);
            UpdateSelectedItem(WorkFolderExplorerItem);
        }

        private void OnOpenInNewWindowClick(object sender, RoutedEventArgs e)
        {
            var item = GetExplorerItemFromMenuItem(sender);
            if (item != null)
                ViewModel.FileViewModel.NewWindowCommand.Execute(item.FullPath);
        }

        private void OnTreeViewContextFlyoutOpening(object sender, object e)
        {
            if (!Directory.Exists(FileViewModel.WorkFolder) && sender is MenuFlyout flyout)
                flyout.Hide();
        }
    }

    // Avalonia doesn't have DataTemplateSelector. Use a FuncDataTemplate or IDataTemplate instead.
    // This is kept as a placeholder for XAML-side template selection.
}
