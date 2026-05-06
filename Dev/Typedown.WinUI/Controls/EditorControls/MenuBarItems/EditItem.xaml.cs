using Microsoft.UI.Xaml;
using Typedown.Presentation.ViewModels;

namespace Typedown.WinUI.Controls;

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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitializeMenu();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ReleaseMenu();
    }
}
