using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Typedown.Core.Contracts.Settings;

public static class TypedownDefaultShortcuts
{
    public static IReadOnlyDictionary<string, EditorShortcutKey> All { get; } =
        new ReadOnlyDictionary<string, EditorShortcutKey>(new Dictionary<string, EditorShortcutKey>
        {
            ["NewFile"] = Shortcut(EditorShortcutModifierFlags.Control, 78),
            ["NewWindow"] = Shortcut(EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 78),
            ["OpenFile"] = Shortcut(EditorShortcutModifierFlags.Control, 79),
            ["Save"] = Shortcut(EditorShortcutModifierFlags.Control, 83),
            ["SaveAs"] = Shortcut(EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 83),
            ["Print"] = Shortcut(EditorShortcutModifierFlags.Alt | EditorShortcutModifierFlags.Shift, 80),
            ["Close"] = Shortcut(EditorShortcutModifierFlags.Control, 87),
            ["Undo"] = Shortcut(EditorShortcutModifierFlags.Control, 90),
            ["Redo"] = Shortcut(EditorShortcutModifierFlags.Control, 89),
            ["Cut"] = Shortcut(EditorShortcutModifierFlags.Control, 88),
            ["Copy"] = Shortcut(EditorShortcutModifierFlags.Control, 67),
            ["Paste"] = Shortcut(EditorShortcutModifierFlags.Control, 86),
            ["SelectAll"] = Shortcut(EditorShortcutModifierFlags.Control, 65),
            ["Find"] = Shortcut(EditorShortcutModifierFlags.Control, 70),
            ["Replace"] = Shortcut(EditorShortcutModifierFlags.Control, 72),
            ["Heading1"] = Shortcut(EditorShortcutModifierFlags.Control, 49),
            ["Paragraph"] = Shortcut(EditorShortcutModifierFlags.Control, 48),
            ["Table"] = Shortcut(EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 84),
            ["Strong"] = Shortcut(EditorShortcutModifierFlags.Control, 66),
            ["Emphasis"] = Shortcut(EditorShortcutModifierFlags.Control, 73),
            ["Underline"] = Shortcut(EditorShortcutModifierFlags.Control, 85),
            ["Hyperlink"] = Shortcut(EditorShortcutModifierFlags.Control, 75),
            ["SidePane"] = Shortcut(EditorShortcutModifierFlags.Control | EditorShortcutModifierFlags.Shift, 76),
            ["FocusMode"] = Shortcut(EditorShortcutModifierFlags.None, 119),
            ["TypewriterMode"] = Shortcut(EditorShortcutModifierFlags.None, 120),
        });

    private static EditorShortcutKey Shortcut(EditorShortcutModifierFlags modifiers, int virtualKeyCode) =>
        new(modifiers, virtualKeyCode);
}
