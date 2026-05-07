namespace Typedown.Core.Models
{
    public record ShortcutKey(KeyboardModifiers Modifiers, KeyboardKey Key)
    {
        public override string ToString()
        {
            return $"{Modifiers}, {Key}";
        }
    }
}
