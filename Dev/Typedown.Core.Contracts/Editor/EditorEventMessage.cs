using System.Text.Json;

namespace Typedown.Core.Contracts.Editor
{
    public sealed record EditorEventMessage(string Name, JsonElement? Args);
}
