namespace Typedown.UI.ViewModels;

public sealed record ShellCommandGroupViewModel(
    string Key,
    string Label,
    IReadOnlyList<ShellCommandItemViewModel> Items);
