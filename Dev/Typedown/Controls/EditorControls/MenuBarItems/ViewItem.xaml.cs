namespace Typedown.Controls.EditorControls.MenuBarItems
{
    public sealed partial class ViewItem : MenuBarItemBase
    {
        public ViewItem()
        {
            InitializeComponent();
        }

        protected override void OnRegisterShortcut()
        {
            RegisterWindowShortcut(Settings.ShortcutSidePane, SidePaneItem);
            RegisterWindowShortcut(Settings.ShortcutSourceCodeMode, SourceCodeModeItem);
            RegisterWindowShortcut(Settings.ShortcutFocusMode, FocusModeItem);
            RegisterWindowShortcut(Settings.ShortcutTypewriterMode, TypewriterModeItem);
            RegisterWindowShortcut(Settings.ShortcutStatusBar, StatusBarItem);
        }

        private void OnUnloaded(object sender, global::Windows.UI.Xaml.RoutedEventArgs e)
        {
            Bindings?.StopTracking();
        }
    }
}
