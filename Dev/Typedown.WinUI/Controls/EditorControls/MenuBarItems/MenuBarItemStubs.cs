using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Typedown.Presentation.ViewModels;

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

    protected static void SetCommand(ToggleMenuFlyoutItem item, ICommand? command, object? parameter = null)
    {
        item.Command = command;
        if (parameter is not null)
        {
            item.CommandParameter = parameter;
        }

        item.IsEnabled = command is not null;
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

        SetCommand(NewFileItem, files?.NewFileCommand);
        SetCommand(NewWindowItem, null);
        SetCommand(OpenFileItem, files?.OpenFileCommand);
        SetCommand(OpenFolderItem, files?.OpenFolderCommand);
        SetCommand(ClearRecentFilesItem, files?.ClearHistoryCommand);
        SetCommand(SaveItem, files?.SaveCommand);
        SetCommand(SaveAsItem, files?.SaveAsCommand);
        SetCommand(PrintItem, files?.PrintCommand);
        SetCommand(CloseItem, files?.ExitCommand);

        if (FindMenuItem("Import") is { } importItem)
        {
            SetCommand(importItem, files?.ImportCommand);
        }

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
        NoRecentFilesItem.IsEnabled = false;
    }

    private void OnExportSubMenuLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        NoExportConfigItem.IsEnabled = false;
        DisableUnsupportedActions();
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
        DisableMenuItem("PDF");
        DisableMenuItem("HTML");
        DisableMenuItem("TEXT");
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
        if (ViewModel?.SettingsViewModel is { } settings)
        {
            settings.SidePaneOpen = SidePaneItem.IsChecked;
        }
    }

    private void OnSourceCodeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel?.SettingsViewModel is { } settings)
        {
            settings.SourceCode = SourceCodeModeItem.IsChecked;
        }
    }

    private void OnFocusModeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel?.SettingsViewModel is { } settings)
        {
            settings.FocusMode = FocusModeItem.IsChecked;
        }
    }

    private void OnTypewriterModeClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel?.SettingsViewModel is { } settings)
        {
            settings.Typewriter = TypewriterModeItem.IsChecked;
        }
    }

    private void OnStatusBarClick(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel?.SettingsViewModel is { } settings)
        {
            settings.StatusBarOpen = StatusBarItem.IsChecked;
        }
    }
}
