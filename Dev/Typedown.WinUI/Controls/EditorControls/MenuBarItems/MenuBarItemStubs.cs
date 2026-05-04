using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Typedown.Presentation.ViewModels;
using Windows.System;

namespace Typedown.WinUI.Controls;

public abstract partial class MenuBarItemBase : Microsoft.UI.Xaml.Controls.MenuBarItem
{
    protected MenuBarItemBase(string title)
    {
        Title = title;
        DataContextChanged += OnDataContextChanged;
    }

    protected void AddPlaceholder(string text)
    {
        Items.Add(new MenuFlyoutItem { Text = text });
    }

    protected AppViewModel? ViewModel => DataContext as AppViewModel;

    protected abstract void ConfigureCommands(AppViewModel? viewModel);

    protected void InitializeMenu()
    {
        ConfigureCommands(ViewModel);
    }

    protected void ReleaseMenu()
    {
        ConfigureCommands(null);
    }

    protected static void SetCommand(MenuFlyoutItem item, ICommand? command, object? parameter = null)
    {
        item.Command = command;
        if (parameter is not null)
        {
            item.CommandParameter = parameter;
        }

        item.IsEnabled = command is not null;
    }

    protected static void SetShortcut(MenuFlyoutItem item, ShortcutKey? shortcut)
    {
        item.KeyboardAccelerators.Clear();
        item.KeyboardAcceleratorTextOverride = string.Empty;

        if (!HasShortcutKey(shortcut))
        {
            return;
        }

        var activeShortcut = shortcut!;
        item.KeyboardAcceleratorTextOverride = activeShortcut.GetShortcutKeyText();
        item.KeyboardAccelerators.Add(CreateKeyboardAccelerator(activeShortcut, () =>
        {
            if (item.Command?.CanExecute(item.CommandParameter) == true)
            {
                item.Command.Execute(item.CommandParameter);
            }
        }));
    }

    protected static void SetCommand(ToggleMenuFlyoutItem item, ICommand? command, object? parameter = null)
    {
        item.Command = command;
        if (parameter is not null)
        {
            item.CommandParameter = parameter;
        }

        item.IsEnabled = command is not null;
    }

    protected static void SetShortcut(ToggleMenuFlyoutItem item, ShortcutKey? shortcut, Action? invoke = null)
    {
        item.KeyboardAccelerators.Clear();
        item.KeyboardAcceleratorTextOverride = string.Empty;

        if (!HasShortcutKey(shortcut))
        {
            return;
        }

        var activeShortcut = shortcut!;
        item.KeyboardAcceleratorTextOverride = activeShortcut.GetShortcutKeyText();
        item.KeyboardAccelerators.Add(CreateKeyboardAccelerator(activeShortcut, () =>
        {
            if (invoke is not null)
            {
                invoke();
            }
            else if (item.Command?.CanExecute(item.CommandParameter) == true)
            {
                item.Command.Execute(item.CommandParameter);
            }
        }));
    }

    private static bool HasShortcutKey(ShortcutKey? shortcut)
    {
        return shortcut is not null && shortcut.Key != KeyboardKey.None;
    }

    private static KeyboardAccelerator CreateKeyboardAccelerator(ShortcutKey shortcut, Action invoke)
    {
        var accelerator = new KeyboardAccelerator
        {
            Key = (VirtualKey)(int)shortcut.Key,
            Modifiers = ToVirtualKeyModifiers(shortcut.Modifiers)
        };

        accelerator.Invoked += (_, args) =>
        {
            invoke();
            args.Handled = true;
        };

        return accelerator;
    }

    private static VirtualKeyModifiers ToVirtualKeyModifiers(KeyboardModifiers modifiers)
    {
        var result = VirtualKeyModifiers.None;

        if (modifiers.HasFlag(KeyboardModifiers.Control))
        {
            result |= VirtualKeyModifiers.Control;
        }

        if (modifiers.HasFlag(KeyboardModifiers.Menu))
        {
            result |= VirtualKeyModifiers.Menu;
        }

        if (modifiers.HasFlag(KeyboardModifiers.Shift))
        {
            result |= VirtualKeyModifiers.Shift;
        }

        if (modifiers.HasFlag(KeyboardModifiers.Windows))
        {
            result |= VirtualKeyModifiers.Windows;
        }

        return result;
    }

    protected MenuFlyoutItem? FindMenuItem(string text)
    {
        return Items
            .SelectMany(Flatten)
            .OfType<MenuFlyoutItem>()
            .FirstOrDefault(item => string.Equals(item.Text, text, StringComparison.Ordinal));
    }

    protected void DisableMenuItem(string text)
    {
        if (FindMenuItem(text) is { } item)
        {
            item.Command = null;
            item.IsEnabled = false;
        }
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        ConfigureCommands(args.NewValue as AppViewModel);
    }

    private static System.Collections.Generic.IEnumerable<object> Flatten(object item)
    {
        yield return item;

        if (item is MenuFlyoutSubItem subItem)
        {
            foreach (var child in subItem.Items.SelectMany(Flatten))
            {
                yield return child;
            }
        }
    }
}

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
        SetCommand(NewWindowItem, null);
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

        if (FindMenuItem("Import") is { } importItem)
        {
            SetCommand(importItem, files?.ImportCommand);
        }

        UpdateOpenRecentItem();
        UpdateExportItem();
        DisableUnsupportedActions();
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        InitializeMenu();
        SettingItem.Click -= OnNavigateItemClick;
        SettingItem.Click += OnNavigateItemClick;
        ExportSettingsItem.Click -= OnNavigateItemClick;
        ExportSettingsItem.Click += OnNavigateItemClick;
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SettingItem.Click -= OnNavigateItemClick;
        ExportSettingsItem.Click -= OnNavigateItemClick;
        ReleaseMenu();
    }

    private void OnOpenRecentSubMenuLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        UpdateOpenRecentItem();
    }

    private void OnExportSubMenuLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        UpdateExportItem();
    }

    private void OnNavigateItemClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.CommandParameter is string route)
        {
            NavigateRequested?.Invoke(this, route);
        }
    }

    private void DisableUnsupportedActions()
    {
        SetCommand(NewWindowItem, null);
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

public sealed partial class EditItem : MenuBarItemBase
{
    public EditItem() : base("Edit")
    {
        InitializeComponent();
    }

    protected override void ConfigureCommands(AppViewModel? viewModel)
    {
        var editor = viewModel?.EditorViewModel;
        var floatView = viewModel?.FloatViewModel;
        var settings = viewModel?.SettingsViewModel;

        SetCommand(UndoItem, editor?.UndoCommand);
        SetCommand(RedoItem, editor?.RedoCommand);
        SetCommand(CutItem, editor?.CutCommand);
        SetCommand(CopyItem, editor?.CopyCommand);
        SetCommand(PasteItem, editor?.PasteCommand);
        SetCommand(CopyAsPlainTextItem, editor?.CopyCommand);
        SetCommand(CopyAsMarkdownItem, editor?.CopyCommand);
        SetCommand(CopyAsHTMLCodeItem, editor?.CopyCommand);
        SetCommand(PasteAsPlainTextItem, editor?.PasteCommand);
        SetCommand(DeleteItem, editor?.DeleteSelectionCommand);
        SetCommand(SelectAllItem, editor?.SelectAllCommand);
        SetCommand(FindItem, editor?.FindCommand);
        SetCommand(FindNextItem, editor?.FindCommand);
        SetCommand(FindPreviousItem, editor?.FindCommand);
        SetCommand(ReplaceItem, floatView?.SearchCommand);

        SetShortcut(UndoItem, settings?.ShortcutUndo);
        SetShortcut(RedoItem, settings?.ShortcutRedo);
        SetShortcut(CutItem, settings?.ShortcutCut);
        SetShortcut(CopyItem, settings?.ShortcutCopy);
        SetShortcut(PasteItem, settings?.ShortcutPaste);
        SetShortcut(CopyAsPlainTextItem, settings?.ShortcutCopyAsPlainText);
        SetShortcut(CopyAsMarkdownItem, settings?.ShortcutCopyAsMarkdown);
        SetShortcut(CopyAsHTMLCodeItem, settings?.ShortcutCopyAsHTMLCode);
        SetShortcut(PasteAsPlainTextItem, settings?.ShortcutPasteAsPlainText);
        SetShortcut(DeleteItem, settings?.ShortcutDelete);
        SetShortcut(SelectAllItem, settings?.ShortcutSelectAll);
        SetShortcut(FindItem, settings?.ShortcutFind);
        SetShortcut(FindNextItem, settings?.ShortcutFindNext);
        SetShortcut(FindPreviousItem, settings?.ShortcutFindPrevious);
        SetShortcut(ReplaceItem, settings?.ShortcutReplace);
    }

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        InitializeMenu();
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ReleaseMenu();
    }
}

public sealed partial class ParagraphItem : MenuBarItemBase
{
    public ParagraphItem() : base("Paragraph")
    {
        InitializeComponent();
    }

    protected override void ConfigureCommands(AppViewModel? viewModel)
    {
        var paragraph = viewModel?.ParagraphViewModel;
        var settings = viewModel?.SettingsViewModel;

        SetCommand(Heading1Item, paragraph?.UpdateParagraphCommand);
        SetCommand(Heading2Item, paragraph?.UpdateParagraphCommand);
        SetCommand(Heading3Item, paragraph?.UpdateParagraphCommand);
        SetCommand(Heading4Item, paragraph?.UpdateParagraphCommand);
        SetCommand(Heading5Item, paragraph?.UpdateParagraphCommand);
        SetCommand(Heading6Item, paragraph?.UpdateParagraphCommand);
        SetCommand(ItemParagraphItem, paragraph?.UpdateParagraphCommand);
        SetCommand(IncreaseHeadingLevelItem, paragraph?.UpdateParagraphCommand);
        SetCommand(DecreaseHeadingLevelItem, paragraph?.UpdateParagraphCommand);
        SetCommand(TableItem, paragraph?.InsertTableCommand);
        SetCommand(CodeFencesItem, paragraph?.UpdateParagraphCommand);
        SetCommand(MathBlockItem, paragraph?.UpdateParagraphCommand);
        SetCommand(QuoteItem, paragraph?.UpdateParagraphCommand);
        SetCommand(OrderedListItem, paragraph?.UpdateParagraphCommand);
        SetCommand(UnorderedListItem, paragraph?.UpdateParagraphCommand);
        SetCommand(TaskListItem, paragraph?.UpdateParagraphCommand);
        SetCommand(InsertParagraphBeforeItem, paragraph?.InsertParagraphCommand);
        SetCommand(InsertParagraphAfterItem, paragraph?.InsertParagraphCommand);
        SetCommand(VegaChartItem, paragraph?.UpdateParagraphCommand);
        SetCommand(FlowChartItem, paragraph?.UpdateParagraphCommand);
        SetCommand(SequenceDiagramItem, paragraph?.UpdateParagraphCommand);
        SetCommand(PlantUMLDiagramItem, paragraph?.UpdateParagraphCommand);
        SetCommand(MermaidItem, paragraph?.UpdateParagraphCommand);
        SetCommand(FootNoteItem, paragraph?.UpdateParagraphCommand);
        SetCommand(HorizontalLineItem, paragraph?.UpdateParagraphCommand);
        SetCommand(YAMLFrontMatterItem, paragraph?.UpdateParagraphCommand);

        SetShortcut(Heading1Item, settings?.ShortcutHeading1);
        SetShortcut(Heading2Item, settings?.ShortcutHeading2);
        SetShortcut(Heading3Item, settings?.ShortcutHeading3);
        SetShortcut(Heading4Item, settings?.ShortcutHeading4);
        SetShortcut(Heading5Item, settings?.ShortcutHeading5);
        SetShortcut(Heading6Item, settings?.ShortcutHeading6);
        SetShortcut(ItemParagraphItem, settings?.ShortcutParagraph);
        SetShortcut(IncreaseHeadingLevelItem, settings?.ShortcutIncreaseHeadingLevel);
        SetShortcut(DecreaseHeadingLevelItem, settings?.ShortcutDecreaseHeadingLevel);
        SetShortcut(TableItem, settings?.ShortcutTable);
        SetShortcut(CodeFencesItem, settings?.ShortcutCodeFences);
        SetShortcut(MathBlockItem, settings?.ShortcutMathBlock);
        SetShortcut(QuoteItem, settings?.ShortcutQuote);
        SetShortcut(OrderedListItem, settings?.ShortcutOrderedList);
        SetShortcut(UnorderedListItem, settings?.ShortcutUnorderedList);
        SetShortcut(TaskListItem, settings?.ShortcutTaskList);
        SetShortcut(InsertParagraphBeforeItem, settings?.ShortcutInsertParagraphBefore);
        SetShortcut(InsertParagraphAfterItem, settings?.ShortcutInsertParagraphAfter);
        SetShortcut(VegaChartItem, settings?.ShortcutVegaChart);
        SetShortcut(FlowChartItem, settings?.ShortcutFlowChart);
        SetShortcut(SequenceDiagramItem, settings?.ShortcutSequenceDiagram);
        SetShortcut(PlantUMLDiagramItem, settings?.ShortcutPlantUMLDiagram);
        SetShortcut(MermaidItem, settings?.ShortcutMermaid);
        SetShortcut(FootNoteItem, settings?.ShortcutFootNote);
        SetShortcut(HorizontalLineItem, settings?.ShortcutHorizontalLine);
        SetShortcut(YAMLFrontMatterItem, settings?.ShortcutYAMLFrontMatter);
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ReleaseMenu();
    }
}

public sealed partial class FormatItem : MenuBarItemBase
{
    public FormatItem() : base("Format")
    {
        InitializeComponent();
    }

    protected override void ConfigureCommands(AppViewModel? viewModel)
    {
        var format = viewModel?.FormatViewModel;
        var settings = viewModel?.SettingsViewModel;

        SetCommand(StrongItem, format?.SetFormatCommand);
        SetCommand(EmphasisItem, format?.SetFormatCommand);
        SetCommand(UnderlineItem, format?.SetFormatCommand);
        SetCommand(InlineCodeItem, format?.SetFormatCommand);
        SetCommand(InlineMathItem, format?.SetFormatCommand);
        SetCommand(StrikethroughItem, format?.SetFormatCommand);
        SetCommand(HighlightItem, format?.SetFormatCommand);
        SetCommand(HyperlinkItem, format?.SetFormatCommand);
        SetCommand(ImageItem, format?.SetFormatCommand);
        SetCommand(ClearFormatItem, format?.SetFormatCommand);

        SetShortcut(StrongItem, settings?.ShortcutStrong);
        SetShortcut(EmphasisItem, settings?.ShortcutEmphasis);
        SetShortcut(UnderlineItem, settings?.ShortcutUnderline);
        SetShortcut(InlineCodeItem, settings?.ShortcutInlineCode);
        SetShortcut(InlineMathItem, settings?.ShortcutInlineMath);
        SetShortcut(StrikethroughItem, settings?.ShortcutStrikethrough);
        SetShortcut(HighlightItem, settings?.ShortcutHighlight);
        SetShortcut(HyperlinkItem, settings?.ShortcutHyperlink);
        SetShortcut(ImageItem, settings?.ShortcutImage);
        SetShortcut(ClearFormatItem, settings?.ShortcutClearFormat);
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ReleaseMenu();
    }
}

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

    private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ReleaseMenu();
    }

    private void OnSidePaneClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

    private void OnSourceCodeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

    private void OnFocusModeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

    private void OnTypewriterModeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

    private void OnStatusBarClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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
