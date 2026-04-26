namespace Typedown.UI.ViewModels;

public sealed record ShellStatusItemViewModel(
    string Key,
    string Label,
    string Value,
    bool IsVisible = true);
