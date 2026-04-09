using PropertyChanged;
﻿using Avalonia.Interactivity;
using Avalonia.Controls;

namespace Typedown.Core.Controls.EditorControls.MenuBarItems
{
[DoNotNotify]
        public sealed partial class ViewItem : MenuBarItemBase
    {
        public ViewItem()
        {
            InitializeComponent();
        }

        protected override void OnRegisterShortcut()
        {
            RegisterWindowShortcut(Settings.ShortcutSidePane, this.FindControl<MenuItem>("SidePaneItem"));
            RegisterWindowShortcut(Settings.ShortcutSourceCodeMode, this.FindControl<MenuItem>("SourceCodeModeItem"));
            RegisterWindowShortcut(Settings.ShortcutFocusMode, this.FindControl<MenuItem>("FocusModeItem"));
            RegisterWindowShortcut(Settings.ShortcutTypewriterMode, this.FindControl<MenuItem>("TypewriterModeItem"));
            RegisterWindowShortcut(Settings.ShortcutStatusBar, this.FindControl<MenuItem>("StatusBarItem"));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
