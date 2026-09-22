using System.Reactive;
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

        /// <summary>
        /// 已注册的和弦及其引用数。WebView2 会吃掉落在网页里的键盘输入，XAML 收不到 KeyDown，
        /// 所以编辑器宿主要把这张表下发给页面，由页面判断哪些和弦该拦截并回传给 <see cref="Emit"/>。
        /// </summary>
        private readonly Dictionary<(KeyboardKey Key, KeyboardModifiers Modifiers), int> registrations = new();

        private readonly Subject<Unit> registrationsChanged = new();

        internal IObservable<Unit> RegistrationsChanged => registrationsChanged.AsObservable();

        internal IReadOnlyList<(KeyboardKey Key, KeyboardModifiers Modifiers)> RegisteredShortcuts =>
            registrations.Keys.ToList();

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

            var chord = (key.Key, key.Modifiers);
            AddRegistration(chord);

            var subscription = keyEvents
                .Where(e => e.Key == key.Key && e.Modifiers == key.Modifiers)
                .Subscribe(e => handler(this, e));

            return new CompositeDisposable(subscription, Disposable.Create(() => RemoveRegistration(chord)));
        }

        private void AddRegistration((KeyboardKey, KeyboardModifiers) chord)
        {
            registrations[chord] = registrations.TryGetValue(chord, out var count) ? count + 1 : 1;
            if (count == 0)
            {
                registrationsChanged.OnNext(Unit.Default);
            }
        }

        private void RemoveRegistration((KeyboardKey, KeyboardModifiers) chord)
        {
            if (!registrations.TryGetValue(chord, out var count))
            {
                return;
            }

            if (count > 1)
            {
                registrations[chord] = count - 1;
                return;
            }

            registrations.Remove(chord);
            registrationsChanged.OnNext(Unit.Default);
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
