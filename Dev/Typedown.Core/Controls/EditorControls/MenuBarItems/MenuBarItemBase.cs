using System;
using System.Collections.ObjectModel;
using Avalonia.Metadata;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using System.Reactive.Disposables;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Typedown.Core.Controls.EditorControls.MenuBarItems
{
    // In Avalonia there is no MenuBarItem base class from WinUI.
    // Use a regular UserControl that acts as a menu bar item container.
    public abstract class MenuBarItemBase : UserControl
    {
        public AppViewModel ViewModel => DataContext as AppViewModel;

        public SettingsViewModel Settings => ViewModel?.SettingsViewModel;

        private readonly CompositeDisposable disposables = new();

        public MenuBarItemBase()
        {
            Unloaded += OnUnloaded;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // TODO: Implement idle dispatch for Avalonia
            OnRegisterShortcut();
        }

        protected abstract void OnRegisterShortcut();

        protected void RegisterWindowShortcut(ShortcutKey key, MenuItem item)
        {
            RegisterMenuItemShortcut(OnWindowShortcutEvent, key, item);
        }

        protected void RegisterEditorShortcut(ShortcutKey key, MenuItem item)
        {
            RegisterMenuItemShortcut(OnEditorShortcutEvent, key, item);
        }

        private void RegisterMenuItemShortcut(Func<MenuItem, bool> handler, ShortcutKey key, MenuItem item)
        {
            var acc = this.GetService<IKeyboardAccelerator>();
            // TODO: Avalonia MenuItem doesn't have KeyboardAcceleratorTextOverride
            // item.KeyboardAcceleratorTextOverride = acc.GetShortcutKeyText(key);
            disposables.Add(acc.Register(key, (s, e) =>
            {
                if (handler(item))
                    e.Handled = true;
            }));
        }

        private bool OnWindowShortcutEvent(MenuItem item)
        {
            // TODO: Check focused window for Avalonia
            TriggerMenuItem(item);
            return true;
        }

        private bool OnEditorShortcutEvent(MenuItem item)
        {
            // TODO: Check if editor is focused for Avalonia
            var editor = this.GetService<IMarkdownEditor>();
            TriggerMenuItem(item);
            return true;
        }

        private void TriggerMenuItem(MenuItem item)
        {
            item.Command?.Execute(item.CommandParameter);
            // TODO: ToggleMenuItem doesn't exist in Avalonia
            // if (item is ToggleMenuItem toggle)
            //     toggle.IsChecked = !toggle.IsChecked;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            disposables.Clear();
        }
    }
}
