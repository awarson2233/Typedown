namespace Typedown.UI.ViewModels;

public sealed record ShellSidePaneSectionViewModel(
    string Key,
    string Title,
    string Description,
    bool IsVisible = true);
