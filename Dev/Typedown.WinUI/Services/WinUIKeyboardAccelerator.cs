using System.Reactive.Disposables;
using System.Reactive.Linq;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIKeyboardAccelerator : IKeyboardAccelerator
    {
        public IObservable<KeyEventArgs> GetObservable()
        {
            return Observable.Empty<KeyEventArgs>();
        }

        public string GetShortcutKeyText(ShortcutKey key)
        {
            return key.GetShortcutKeyText();
        }

        public string GetVirtualKeyNameText(KeyboardKey key)
        {
            return key.GetVirtualKeyNameText();
        }

        public IDisposable Register(ShortcutKey key, EventHandler<KeyEventArgs> handler)
        {
            return Disposable.Empty;
        }

        public IDisposable RegisterGlobal(EventHandler<KeyEventArgs> handler)
        {
            return Disposable.Empty;
        }
    }
}
