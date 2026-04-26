using System;

namespace Typedown.Core.Contracts.Settings;

[Flags]
public enum EditorShortcutModifierFlags
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8,
}
