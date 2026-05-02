using System;

namespace Typedown.Core.Models
{
    public class KeyEventArgs : EventArgs
    {
        public bool Handled { get; set; }

        public KeyboardKey Key { get; }

        public KeyboardModifiers Modifiers { get; }

        public KeyEventArgs(KeyboardKey key, KeyboardModifiers modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }
    }
}
