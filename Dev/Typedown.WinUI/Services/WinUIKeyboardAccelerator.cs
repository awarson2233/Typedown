using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Interfaces;
using Windows.System;
using CoreVirtualKeyStates = Windows.UI.Core.CoreVirtualKeyStates;
using KeyEventArgs = Typedown.Core.Models.KeyEventArgs;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIKeyboardAccelerator : IKeyboardAccelerator
    {
        private readonly Subject<KeyEventArgs> keyEvents = new();
        private UIElement? attachedRoot;

        public void Attach(UIElement root)
        {
            if (ReferenceEquals(attachedRoot, root))
            {
                return;
            }

            if (attachedRoot is not null)
            {
                attachedRoot.KeyDown -= OnRootKeyDown;
            }

            attachedRoot = root;
            attachedRoot.KeyDown += OnRootKeyDown;
        }

        public IObservable<KeyEventArgs> GetObservable()
        {
            return keyEvents.AsObservable();
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
            if (key is null || key.Key == KeyboardKey.None)
            {
                return Disposable.Empty;
            }

            return keyEvents
                .Where(e => e.Key == key.Key && e.Modifiers == key.Modifiers)
                .Subscribe(e => handler(this, e));
        }

        public IDisposable RegisterGlobal(EventHandler<KeyEventArgs> handler)
        {
            return keyEvents.Subscribe(e => handler(this, e));
        }

        private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var args = Emit(ToKeyboardKey(e.Key), GetCurrentModifiers());
            e.Handled = args.Handled;
        }

        internal KeyEventArgs Emit(KeyboardKey key, KeyboardModifiers modifiers)
        {
            var args = new KeyEventArgs(key, modifiers);
            keyEvents.OnNext(args);
            return args;
        }

        private static KeyboardKey ToKeyboardKey(VirtualKey key)
        {
            return (KeyboardKey)(int)key;
        }

        internal static KeyboardModifiers GetCurrentModifiers()
        {
            var modifiers = KeyboardModifiers.None;

            if (IsKeyDown(VirtualKey.Control))
            {
                modifiers |= KeyboardModifiers.Control;
            }

            if (IsKeyDown(VirtualKey.Menu))
            {
                modifiers |= KeyboardModifiers.Menu;
            }

            if (IsKeyDown(VirtualKey.Shift))
            {
                modifiers |= KeyboardModifiers.Shift;
            }

            if (IsKeyDown(VirtualKey.LeftWindows) || IsKeyDown(VirtualKey.RightWindows))
            {
                modifiers |= KeyboardModifiers.Windows;
            }

            return modifiers;
        }

        private static bool IsKeyDown(VirtualKey key)
        {
            return (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }
    }
}
