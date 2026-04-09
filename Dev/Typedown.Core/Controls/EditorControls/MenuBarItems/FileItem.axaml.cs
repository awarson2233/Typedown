using PropertyChanged;
﻿using System;
using System.Collections.ObjectModel;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.Data.Converters;
using System.Linq;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Typedown.Core.Controls.EditorControls.MenuBarItems
{
[DoNotNotify]
        public sealed partial class FileItem : MenuBarItemBase
    {
        public FileViewModel File => ViewModel?.FileViewModel;

        public AccessHistory FileHistory => this.GetService<AccessHistory>();

        public IFileExport FileExport => this.GetService<IFileExport>();

        public FileItem()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateOpenRecentItem();
            UpdateExportItem();
        }

        protected override void OnRegisterShortcut()
        {
            RegisterWindowShortcut(Settings.ShortcutNewFile, this.FindControl<MenuItem>("NewFileItem"));
            RegisterWindowShortcut(Settings.ShortcutNewWindow, this.FindControl<MenuItem>("NewWindowItem"));
            RegisterWindowShortcut(Settings.ShortcutOpenFile, this.FindControl<MenuItem>("OpenFileItem"));
            RegisterWindowShortcut(Settings.ShortcutOpenFolder, this.FindControl<MenuItem>("OpenFolderItem"));
            RegisterWindowShortcut(Settings.ShortcutClearRecentFiles, this.FindControl<MenuItem>("ClearRecentFilesItem"));
            RegisterWindowShortcut(Settings.ShortcutSave, this.FindControl<MenuItem>("SaveItem"));
            RegisterWindowShortcut(Settings.ShortcutSaveAs, this.FindControl<MenuItem>("SaveAsItem"));
            RegisterWindowShortcut(Settings.ShortcutExportSettings, this.FindControl<MenuItem>("ExportSettingsItem"));
            RegisterWindowShortcut(Settings.ShortcutPrint, this.FindControl<MenuItem>("PrintItem"));
            RegisterWindowShortcut(Settings.ShortcutSettings, this.FindControl<MenuItem>("SettingItem"));
            RegisterWindowShortcut(Settings.ShortcutClose, this.FindControl<MenuItem>("CloseItem"));
        }

        private void UpdateOpenRecentItem()
        {
            var openRecentSubMenu = this.FindControl<MenuItem>("OpenRecentSubMenu");
            var noRecentFilesItem = this.FindControl<MenuItem>("NoRecentFilesItem");
            var clearRecentFilesItem = this.FindControl<MenuItem>("ClearRecentFilesItem");
            if (openRecentSubMenu == null) return;
            var files = FileHistory.FileRecentlyOpened.ToList();
            while (openRecentSubMenu.Items.Count > 2 && openRecentSubMenu.Items[1] is not Separator)
                ((ItemsControl)openRecentSubMenu).Items.Remove(openRecentSubMenu.Items[1]);
            foreach (var file in files.Reverse<string>())
                ((ItemsControl)openRecentSubMenu).Items.Insert(1, new MenuItem() { Header = file, Command = File.OpenFileCommand, CommandParameter = file });
            if (noRecentFilesItem != null) noRecentFilesItem.IsVisible = !files.Any();
            if (clearRecentFilesItem != null) clearRecentFilesItem.IsEnabled = files.Any();
        }

        private void OnOpenRecentSubMenuLoaded(object sender, RoutedEventArgs e)
        {
            UpdateOpenRecentItem();
        }

        private void UpdateExportItem()
        {
            var exportSubMenu = this.FindControl<MenuItem>("ExportSubMenu");
            var noExportConfigItem = this.FindControl<MenuItem>("NoExportConfigItem");
            if (exportSubMenu == null) return;
            var configs = FileExport.ExportConfigs.ToList();
            while (exportSubMenu.Items.Count > 2 && exportSubMenu.Items[1] is not Separator)
                ((ItemsControl)exportSubMenu).Items.Remove(exportSubMenu.Items[1]);
            foreach (var config in configs.Reverse<ExportConfig>())
                ((ItemsControl)exportSubMenu).Items.Insert(1, new MenuItem() { Header = config.Name, Command = File.ExportCommand, CommandParameter = config });
            if (noExportConfigItem != null) noExportConfigItem.IsVisible = !configs.Any();
        }

        private void OnExportSubMenuLoaded(object sender, RoutedEventArgs e)
        {
            UpdateExportItem();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
