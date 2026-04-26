using System;

namespace Typedown.Core.Contracts.Settings;

[Flags]
public enum EditorShortcutModifierFlags
{
    None = 0,
    Control = 1,
    // Legacy WinRT names this bit Menu; Typedown UI exposes it as Alt.
    Alt = 2,
    Shift = 4,
    Windows = 8,
}
