using Typedown.Core.Contracts.Settings;
using Typedown.Core.Contracts.Shell;
using Typedown.UI.Resources;

namespace Typedown.UI.ViewModels;

public sealed class ShellViewModel
{
    public ShellViewModel()
    {
        File = FileUiState.FromValues(workFolder: null, filePath: null, defaultImageBasePath: null);
        Chrome = ShellChromeState.FromValues(
            title: ShellTextResources.AppTitle,
            isSaved: true,
            displaySaved: true,
            isTopmost: false,
            captionHeight: 0,
            compactMode: false,
            currentPageName: "Main");

        AppTitle = ShellTextResources.AppTitle;
        DocumentTitle = File.FileName ?? ShellTextResources.UntitledDocumentTitle;
        EditorPanelTitle = ShellTextResources.EditorPanelTitle;
        EditorPanelDescription = ShellTextResources.EditorPanelDescription;
        CommandGroups = CreateCommandGroups();
        SidePaneSections = CreateSidePaneSections();
        StatusItems = CreateStatusItems();
    }

    public FileUiState File { get; }

    public ShellChromeState Chrome { get; }

    public string AppTitle { get; }

    public string DocumentTitle { get; }

    public string EditorPanelTitle { get; }

    public string EditorPanelDescription { get; }

    public IReadOnlyList<ShellCommandGroupViewModel> CommandGroups { get; }

    public IReadOnlyList<ShellSidePaneSectionViewModel> SidePaneSections { get; }

    public IReadOnlyList<ShellStatusItemViewModel> StatusItems { get; }

    private static IReadOnlyList<ShellCommandGroupViewModel> CreateCommandGroups()
    {
        return
        [
            new ShellCommandGroupViewModel(
                "File",
                ShellTextResources.FileGroupLabel,
                [
                    CreateCommand("NewFile", "New", "NewFile"),
                    CreateCommand("OpenFile", "Open...", "OpenFile"),
                    CreateCommand("Save", "Save", "Save"),
                ]),
            new ShellCommandGroupViewModel(
                "Edit",
                ShellTextResources.EditGroupLabel,
                [
                    CreateCommand("Undo", "Undo", "Undo"),
                    CreateCommand("Redo", "Redo", "Redo"),
                ]),
            new ShellCommandGroupViewModel(
                "View",
                ShellTextResources.ViewGroupLabel,
                [
                    CreateCommand("SidePane", "Side Pane", "SidePane", isChecked: true),
                    CreateCommand("StatusBar", "Status Bar", "StatusBar", isChecked: true),
                ]),
        ];
    }

    private static IReadOnlyList<ShellSidePaneSectionViewModel> CreateSidePaneSections()
    {
        return
        [
            new ShellSidePaneSectionViewModel("Document", ShellTextResources.DocumentSectionTitle, "Document metadata and quick context."),
            new ShellSidePaneSectionViewModel("Outline", ShellTextResources.OutlineSectionTitle, "A platform-neutral placeholder for heading structure."),
            new ShellSidePaneSectionViewModel("Search", ShellTextResources.SearchSectionTitle, "A platform-neutral placeholder for find and replace state."),
        ];
    }

    private static IReadOnlyList<ShellStatusItemViewModel> CreateStatusItems()
    {
        return
        [
            new ShellStatusItemViewModel("FileName", "File", ShellTextResources.UntitledDocumentTitle),
            new ShellStatusItemViewModel("SavedState", "Status", ShellTextResources.SavedStatusReady),
            new ShellStatusItemViewModel("EditorMode", "Mode", ShellTextResources.EditorModeMarkdown),
            new ShellStatusItemViewModel("Encoding", "Encoding", ShellTextResources.EncodingUtf8),
        ];
    }

    private static ShellCommandItemViewModel CreateCommand(string key, string label, string commandName, bool isChecked = false)
    {
        TypedownDefaultShortcuts.All.TryGetValue(commandName, out var shortcut);
        return new ShellCommandItemViewModel(
            key,
            label,
            commandName,
            shortcut,
            ShellShortcutFormatter.Format(shortcut),
            IsEnabled: true,
            IsChecked: isChecked);
    }
}
