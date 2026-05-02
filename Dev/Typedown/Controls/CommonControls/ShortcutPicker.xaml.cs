using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using Typedown.Controls.SettingControls.SettingItems;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Presentation.ViewModels;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Typedown.Controls
{
    public sealed partial class ShortcutPicker : UserControl
    {
        public static DependencyProperty ShortcutKeyProperty = DependencyProperty.Register(nameof(ShortcutKey), typeof(ShortcutKey), typeof(ShortcutPicker), new(null));
        public ShortcutKey ShortcutKey { get => (ShortcutKey)GetValue(ShortcutKeyProperty); set => SetValue(ShortcutKeyProperty, value); }

        public static DependencyProperty VerifiedProperty = DependencyProperty.Register(nameof(Verified), typeof(bool), typeof(ShortcutPicker), new(true));
        public bool Verified { get => (bool)GetValue(VerifiedProperty); set => SetValue(VerifiedProperty, value); }

        private readonly CompositeDisposable disposables = new();

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
            var acc = this.GetService<IKeyboardAccelerator>();
            disposables.Add(acc.RegisterGlobal(OnKeyEvent));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            disposables.Clear();
            Bindings?.StopTracking();
        }

        private void OnKeyEvent(object sender, KeyEventArgs args)
        {
            ErrorMsgPanel.Visibility = Visibility.Collapsed;
            var shortcutKey = new ShortcutKey(args.Modifiers, args.Key);
            var displayText = Common.GetShortcutKeyTextList(shortcutKey);
            if (!modifiers.Contains(args.Key) && (args.Modifiers != 0 || (args.Key >= KeyboardKey.F1 && args.Key <= KeyboardKey.F12) || args.Key == KeyboardKey.Delete))
            {
                if (existShortcutKeys.ContainsKey(shortcutKey) && shortcutKey != currentShortcutKey)
                {
                    Verified = false;
                    ErrorMsgPanel.Visibility = Visibility.Visible;
                    ExistOwnerTextBlock.Text = new ShortcutSettingItemModel(settings, existShortcutKeys[shortcutKey]).Description;
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

        public void ResetShortcutKey()
        {
            Verified = true;
            ErrorMsgPanel.Visibility = Visibility.Collapsed;
            ShortcutKey = new(0, 0);
        }
    }
}
