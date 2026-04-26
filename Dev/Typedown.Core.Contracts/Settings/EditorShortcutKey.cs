using System.Text.Json.Serialization;

namespace Typedown.Core.Contracts.Settings;

public sealed record EditorShortcutKey(
    [property: JsonPropertyName("modifiers")] EditorShortcutModifierFlags Modifiers,
    [property: JsonPropertyName("virtualKeyCode")] int VirtualKeyCode);
