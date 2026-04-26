using Typedown.Core.Contracts.Settings;

namespace Typedown.UI.ViewModels;

public sealed record ShellCommandItemViewModel(
    string Key,
    string Label,
    string CommandName,
    EditorShortcutKey? Shortcut,
    string ShortcutDisplayText,
    bool IsEnabled = true,
    bool IsChecked = false);
