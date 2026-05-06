using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.ViewModels;

namespace Typedown.WinUI.Controls;

public sealed partial class FileItem : MenuBarItemBase
{
    public event EventHandler<string>? NavigateRequested;

    public FileItem() : base("File")
    {
        InitializeComponent();
    }

    protected override void ConfigureCommands(AppViewModel? viewModel)
    {
        var files = viewModel?.FileViewModel;
        var settings = viewModel?.SettingsViewModel;

        SetCommand(NewFileItem, files?.NewFileCommand);
        SetCommand(NewWindowItem, files?.NewWindowCommand);
        SetCommand(OpenFileItem, files?.OpenFileCommand);
        SetCommand(OpenFolderItem, files?.OpenFolderCommand);
        SetCommand(ClearRecentFilesItem, files?.ClearHistoryCommand);
        SetCommand(SaveItem, files?.SaveCommand);
        SetCommand(SaveAsItem, files?.SaveAsCommand);
        SetCommand(PrintItem, files?.PrintCommand);
        SetCommand(CloseItem, files?.ExitCommand);

        SetShortcut(NewFileItem, settings?.ShortcutNewFile);
        SetShortcut(NewWindowItem, settings?.ShortcutNewWindow);
        SetShortcut(OpenFileItem, settings?.ShortcutOpenFile);
        SetShortcut(OpenFolderItem, settings?.ShortcutOpenFolder);
        SetShortcut(ClearRecentFilesItem, settings?.ShortcutClearRecentFiles);
        SetShortcut(SaveItem, settings?.ShortcutSave);
        SetShortcut(SaveAsItem, settings?.ShortcutSaveAs);
        SetShortcut(PrintItem, settings?.ShortcutPrint);
        SetShortcut(CloseItem, settings?.ShortcutClose);

        SetCommand(ImportItem, files?.ImportCommand);

        UpdateOpenRecentItem();
        UpdateExportItem();
        DisableUnsupportedActions();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitializeMenu();
        SettingItem.Click -= OnNavigateItemClick;
        SettingItem.Click += OnNavigateItemClick;
        ExportSettingsItem.Click -= OnNavigateItemClick;
        ExportSettingsItem.Click += OnNavigateItemClick;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SettingItem.Click -= OnNavigateItemClick;
        ExportSettingsItem.Click -= OnNavigateItemClick;
        ReleaseMenu();
    }

    private void OnOpenRecentSubMenuLoaded(object sender, RoutedEventArgs e)
    {
        UpdateOpenRecentItem();
    }

    private void OnExportSubMenuLoaded(object sender, RoutedEventArgs e)
    {
        UpdateExportItem();
    }

    private void OnNavigateItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.CommandParameter is string route)
        {
            NavigateRequested?.Invoke(this, route);
        }
    }

    private void DisableUnsupportedActions()
    {
    }

    private void UpdateOpenRecentItem()
    {
        var files = ViewModel?.FileViewModel;

        RemoveDynamicSubMenuItems(OpenRecentSubMenu);
        if (files is null)
        {
            NoRecentFilesItem.Visibility = Visibility.Visible;
            ClearRecentFilesItem.IsEnabled = false;
            return;
        }

        var recentFiles = files.AccessHistory.FileRecentlyOpened.ToList();
        foreach (var file in recentFiles.AsEnumerable().Reverse())
        {
            OpenRecentSubMenu.Items.Insert(1, new MenuFlyoutItem
            {
                Text = file,
                Command = files.OpenFileCommand,
                CommandParameter = file
            });
        }

        NoRecentFilesItem.Visibility = recentFiles.Any() ? Visibility.Collapsed : Visibility.Visible;
        ClearRecentFilesItem.IsEnabled = recentFiles.Any() && ClearRecentFilesItem.Command is not null;
    }

    private void UpdateExportItem()
    {
        var files = ViewModel?.FileViewModel;

        RemoveDynamicSubMenuItems(ExportSubMenu);
        if (files is null)
        {
            NoExportConfigItem.Visibility = Visibility.Visible;
            return;
        }

        var exportConfigs = files.ServiceProvider.GetService<IFileExport>()?.ExportConfigs.ToList() ?? [];
        foreach (var config in exportConfigs.AsEnumerable().Reverse())
        {
            ExportSubMenu.Items.Insert(1, new MenuFlyoutItem
            {
                Text = config.Name,
                Command = files.ExportCommand,
                CommandParameter = config
            });
        }

        NoExportConfigItem.Visibility = exportConfigs.Any() ? Visibility.Collapsed : Visibility.Visible;
    }

    private static void RemoveDynamicSubMenuItems(MenuFlyoutSubItem subMenu)
    {
        while (subMenu.Items.Count > 1 && subMenu.Items[1] is not MenuFlyoutSeparator)
        {
            subMenu.Items.RemoveAt(1);
        }
    }
}
