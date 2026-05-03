using System;
using Typedown.Core.Models;

namespace Typedown.Presentation.Interfaces
{
    public interface IKeyboardAccelerator
    {
        IDisposable Register(ShortcutKey key, EventHandler<KeyEventArgs> handler);

        IDisposable RegisterGlobal(EventHandler<KeyEventArgs> handler);

        IObservable<KeyEventArgs> GetObservable();

        string GetShortcutKeyText(ShortcutKey key);

        string GetVirtualKeyNameText(KeyboardKey key);
    }
}
