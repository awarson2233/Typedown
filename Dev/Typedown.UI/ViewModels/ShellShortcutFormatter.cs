using Typedown.Core.Contracts.Settings;

namespace Typedown.UI.ViewModels;

internal static class ShellShortcutFormatter
{
    public static string Format(EditorShortcutKey? shortcut)
    {
        if (shortcut is null)
        {
            return string.Empty;
        }

        var segments = new List<string>();
        if (shortcut.Modifiers.HasFlag(EditorShortcutModifierFlags.Control))
        {
            segments.Add("Ctrl");
        }

        if (shortcut.Modifiers.HasFlag(EditorShortcutModifierFlags.Alt))
        {
            segments.Add("Alt");
        }

        if (shortcut.Modifiers.HasFlag(EditorShortcutModifierFlags.Shift))
        {
            segments.Add("Shift");
        }

        if (shortcut.Modifiers.HasFlag(EditorShortcutModifierFlags.Windows))
        {
            segments.Add("Win");
        }

        segments.Add(FormatVirtualKey(shortcut.VirtualKeyCode));
        return string.Join("+", segments);
    }

    private static string FormatVirtualKey(int virtualKeyCode)
    {
        return virtualKeyCode switch
        {
            >= 65 and <= 90 => ((char)virtualKeyCode).ToString(),
            >= 48 and <= 57 => ((char)virtualKeyCode).ToString(),
            114 => "F3",
            119 => "F8",
            120 => "F9",
            13 => "Enter",
            187 => "=",
            189 => "-",
            188 => ",",
            192 => "`",
            219 => "[",
            220 => "\\",
            221 => "]",
            191 => "/",
            _ => $"VK{virtualKeyCode}",
        };
    }
}
