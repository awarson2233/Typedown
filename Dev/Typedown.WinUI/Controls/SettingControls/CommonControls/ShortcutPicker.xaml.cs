using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.Utilities;
using Typedown.Presentation.ViewModels;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Typedown.WinUI.Controls
{
    public sealed partial class ShortcutPicker : UserControl
    {
        public static DependencyProperty ShortcutKeyProperty = DependencyProperty.Register(nameof(ShortcutKey), typeof(ShortcutKey), typeof(ShortcutPicker), new(null));
        public ShortcutKey ShortcutKey { get => (ShortcutKey)GetValue(ShortcutKeyProperty); set => SetValue(ShortcutKeyProperty, value); }

        public static DependencyProperty VerifiedProperty = DependencyProperty.Register(nameof(Verified), typeof(bool), typeof(ShortcutPicker), new(true));
        public bool Verified { get => (bool)GetValue(VerifiedProperty); set => SetValue(VerifiedProperty, value); }

        private ShortcutKey currentShortcutKey;

        private Dictionary<ShortcutKey, PropertyInfo> existShortcutKeys;

        private HashSet<KeyboardKey> modifiers;

        private SettingsViewModel settings;

        public ShortcutPicker(ShortcutKey currentShortcutKey)
        {
            this.currentShortcutKey = currentShortcutKey;
            ShortcutKey = currentShortcutKey;
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            settings = this.GetService<SettingsViewModel>();
            existShortcutKeys = new();
            typeof(SettingsViewModel)
                .GetProperties()
                .Where(x => x.PropertyType == typeof(ShortcutKey))
                .Select(x => (PropertyInfo: x, ShortcutKey: x.GetValue(settings) as ShortcutKey))
                .Where(x => x.ShortcutKey != null && x.ShortcutKey != new ShortcutKey(0, 0))
                .ToList()
                .ForEach(x => existShortcutKeys[x.ShortcutKey] = x.PropertyInfo);

            modifiers = new HashSet<KeyboardKey>() {
                (KeyboardKey)162,
                (KeyboardKey)163,
                (KeyboardKey)160,
                (KeyboardKey)161,
                (KeyboardKey)164,
                (KeyboardKey)165,
                KeyboardKey.LeftWindows,
                KeyboardKey.RightWindows };
            Focus(FocusState.Programmatic);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Bindings?.StopTracking();
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs args)
        {
            ErrorMsgPanel.Visibility = Visibility.Collapsed;
            var key = (KeyboardKey)(int)args.Key;
            var shortcutKey = new ShortcutKey(GetModifiers(), key);
            var displayText = Common.GetShortcutKeyTextList(shortcutKey);
            if (!modifiers.Contains(key) && (shortcutKey.Modifiers != 0 || (key >= KeyboardKey.F1 && key <= KeyboardKey.F12) || key == KeyboardKey.Delete))
            {
                if (existShortcutKeys.ContainsKey(shortcutKey) && shortcutKey != currentShortcutKey)
                {
                    Verified = false;
                    ErrorMsgPanel.Visibility = Visibility.Visible;
                    ExistOwnerTextBlock.Text = GetShortcutDescription(existShortcutKeys[shortcutKey]);
                }
                else
                {
                    Verified = true;
                }
                ShortcutKey = shortcutKey;
                args.Handled = true;
            }
            else if (displayText.ToHashSet().Count == displayText.Count)
            {
                Verified = false;
                ShortcutKey = shortcutKey;
            }
        }

        private static KeyboardModifiers GetModifiers()
        {
            var modifiers = KeyboardModifiers.None;
            if (PInvoke.GetIsKeyDown(KeyboardKey.Control))
            {
                modifiers |= KeyboardModifiers.Control;
            }

            if (PInvoke.GetIsKeyDown(KeyboardKey.Menu))
            {
                modifiers |= KeyboardModifiers.Menu;
            }

            if (PInvoke.GetIsKeyDown(KeyboardKey.Shift))
            {
                modifiers |= KeyboardModifiers.Shift;
            }

            if (PInvoke.GetIsKeyDown(KeyboardKey.LeftWindows) || PInvoke.GetIsKeyDown(KeyboardKey.RightWindows))
            {
                modifiers |= KeyboardModifiers.Windows;
            }

            return modifiers;
        }

        public void ResetShortcutKey()
        {
            Verified = true;
            ErrorMsgPanel.Visibility = Visibility.Collapsed;
            ShortcutKey = new(0, 0);
        }

        private static string GetShortcutDescription(PropertyInfo property)
        {
            var texts = property.GetCustomAttribute<LocaleAttribute>()?.Texts.ToList();
            if (texts == null || !texts.Any())
            {
                return "Unknown / " + property.Name;
            }

            var displayName = string.IsNullOrEmpty(texts.Last()) ? property.Name : texts.Last();
            return string.Join(" / ", texts.Take(texts.Count - 1).Append(displayName));
        }
    }
}
