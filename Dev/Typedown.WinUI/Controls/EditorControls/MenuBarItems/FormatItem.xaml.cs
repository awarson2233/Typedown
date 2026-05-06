using Microsoft.UI.Xaml;
using Typedown.Presentation.ViewModels;

namespace Typedown.WinUI.Controls;

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

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ReleaseMenu();
    }
}
