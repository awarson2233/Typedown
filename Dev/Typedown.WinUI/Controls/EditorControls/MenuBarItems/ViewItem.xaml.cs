using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Typedown.Presentation.ViewModels;

namespace Typedown.WinUI.Controls;

public sealed partial class ViewItem : MenuBarItemBase
{
    public ViewItem() : base("View")
    {
        InitializeComponent();
        AttachViewHandlers();
    }

    protected override void ConfigureCommands(AppViewModel? viewModel)
    {
        var settings = viewModel?.SettingsViewModel;
        var hasSettings = settings is not null;

        SidePaneItem.IsEnabled = hasSettings;
        SourceCodeModeItem.IsEnabled = hasSettings;
        FocusModeItem.IsEnabled = hasSettings;
        TypewriterModeItem.IsEnabled = hasSettings;
        StatusBarItem.IsEnabled = hasSettings;

        if (settings is null)
        {
            return;
        }

        SidePaneItem.IsChecked = settings.SidePaneOpen;
        SourceCodeModeItem.IsChecked = settings.SourceCode;
        FocusModeItem.IsChecked = settings.FocusMode;
        TypewriterModeItem.IsChecked = settings.Typewriter;
        StatusBarItem.IsChecked = settings.StatusBarOpen;

        SetShortcut(SidePaneItem, settings.ShortcutSidePane, () => ToggleItem(SidePaneItem, ToggleSidePane));
        SetShortcut(SourceCodeModeItem, settings.ShortcutSourceCodeMode, () => ToggleItem(SourceCodeModeItem, ToggleSourceCode));
        SetShortcut(FocusModeItem, settings.ShortcutFocusMode, () => ToggleItem(FocusModeItem, ToggleFocusMode));
        SetShortcut(TypewriterModeItem, settings.ShortcutTypewriterMode, () => ToggleItem(TypewriterModeItem, ToggleTypewriterMode));
        SetShortcut(StatusBarItem, settings.ShortcutStatusBar, () => ToggleItem(StatusBarItem, ToggleStatusBar));
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitializeMenu();
    }

    private void AttachViewHandlers()
    {
        SidePaneItem.Click -= OnSidePaneClick;
        SourceCodeModeItem.Click -= OnSourceCodeClick;
        FocusModeItem.Click -= OnFocusModeClick;
        TypewriterModeItem.Click -= OnTypewriterModeClick;
        StatusBarItem.Click -= OnStatusBarClick;
        SidePaneItem.Click += OnSidePaneClick;
        SourceCodeModeItem.Click += OnSourceCodeClick;
        FocusModeItem.Click += OnFocusModeClick;
        TypewriterModeItem.Click += OnTypewriterModeClick;
        StatusBarItem.Click += OnStatusBarClick;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ReleaseMenu();
    }

    private void OnSidePaneClick(object sender, RoutedEventArgs e)
    {
        ToggleSidePane();
    }

    private static void ToggleItem(ToggleMenuFlyoutItem item, Action update)
    {
        item.IsChecked = !item.IsChecked;
        update();
    }

    private void ToggleSidePane()
    {
        if (ViewModel?.SettingsViewModel is { } settings)
        {
            settings.SidePaneOpen = SidePaneItem.IsChecked;
        }
    }

    private void OnSourceCodeClick(object sender, RoutedEventArgs e)
    {
        ToggleSourceCode();
    }

    private void ToggleSourceCode()
    {
        if (ViewModel?.SettingsViewModel is { } settings)
        {
            settings.SourceCode = SourceCodeModeItem.IsChecked;
        }
    }

    private void OnFocusModeClick(object sender, RoutedEventArgs e)
    {
        ToggleFocusMode();
    }

    private void ToggleFocusMode()
    {
        if (ViewModel?.SettingsViewModel is { } settings)
        {
            settings.FocusMode = FocusModeItem.IsChecked;
        }
    }

    private void OnTypewriterModeClick(object sender, RoutedEventArgs e)
    {
        ToggleTypewriterMode();
    }

    private void ToggleTypewriterMode()
    {
        if (ViewModel?.SettingsViewModel is { } settings)
        {
            settings.Typewriter = TypewriterModeItem.IsChecked;
        }
    }

    private void OnStatusBarClick(object sender, RoutedEventArgs e)
    {
        ToggleStatusBar();
    }

    private void ToggleStatusBar()
    {
        if (ViewModel?.SettingsViewModel is { } settings)
        {
            settings.StatusBarOpen = StatusBarItem.IsChecked;
        }
    }
}
