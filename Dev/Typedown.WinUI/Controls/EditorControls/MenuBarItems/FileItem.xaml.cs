using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.ViewModels;
using Typedown.WinUI.Services;

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

    private void OnOpenRecentSubMenuRequested(object sender, RoutedEventArgs e)
    {
        _ = UpdateOpenRecentItemAsync();
    }

    private void OnOpenRecentSubMenuRequested(object sender, PointerRoutedEventArgs e)
    {
        _ = UpdateOpenRecentItemAsync();
    }

    private void OnOpenRecentSubMenuRequested(object sender, TappedRoutedEventArgs e)
    {
        _ = UpdateOpenRecentItemAsync();
    }

    private void OnExportSubMenuRequested(object sender, RoutedEventArgs e)
    {
        _ = UpdateExportItemAsync();
    }

    private void OnExportSubMenuRequested(object sender, PointerRoutedEventArgs e)
    {
        _ = UpdateExportItemAsync();
    }

    private void OnExportSubMenuRequested(object sender, TappedRoutedEventArgs e)
    {
        _ = UpdateExportItemAsync();
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

    private async Task UpdateOpenRecentItemAsync()
    {
        var files = ViewModel?.FileViewModel;

        RemoveDynamicSubMenuItems(OpenRecentSubMenu);
        if (files is null)
        {
            NoRecentFilesItem.Visibility = Visibility.Visible;
            ClearRecentFilesItem.IsEnabled = false;
            return;
        }

        await files.AccessHistory.EnsureInitialized();
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

    private async Task UpdateExportItemAsync()
    {
        var files = ViewModel?.FileViewModel;

        RemoveDynamicSubMenuItems(ExportSubMenu);
        if (files is null)
        {
            NoExportConfigItem.Visibility = Visibility.Visible;
            return;
        }

        var fileExport = files.ServiceProvider.GetService<IFileExport>();
        if (fileExport is WinUIFileExport winUIFileExport)
        {
            await winUIFileExport.EnsureExportConfigsLoaded();
        }
        else if (fileExport is not null)
        {
            await fileExport.UpdateExportConfigs();
        }

        var exportConfigs = fileExport?.ExportConfigs.ToList() ?? [];
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
