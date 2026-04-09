using PropertyChanged;
﻿using Typedown.Core.ViewModels;
using Avalonia.Interactivity;
using Avalonia.Controls;

namespace Typedown.Core.Controls.EditorControls.MenuBarItems
{
[DoNotNotify]
        public sealed partial class FormatItem : MenuBarItemBase
    {
        public FormatViewModel Format => ViewModel?.FormatViewModel;

        public EditorViewModel Editor => ViewModel?.EditorViewModel;

        public FormatItem()
        {
            InitializeComponent();
        }

        protected override void OnRegisterShortcut()
        {
            RegisterEditorShortcut(Settings.ShortcutStrong, this.FindControl<MenuItem>("StrongItem"));
            RegisterEditorShortcut(Settings.ShortcutEmphasis, this.FindControl<MenuItem>("EmphasisItem"));
            RegisterEditorShortcut(Settings.ShortcutUnderline, this.FindControl<MenuItem>("UnderlineItem"));
            RegisterEditorShortcut(Settings.ShortcutInlineCode, this.FindControl<MenuItem>("InlineCodeItem"));
            RegisterEditorShortcut(Settings.ShortcutInlineMath, this.FindControl<MenuItem>("InlineMathItem"));
            RegisterEditorShortcut(Settings.ShortcutStrikethrough, this.FindControl<MenuItem>("StrikethroughItem"));
            RegisterEditorShortcut(Settings.ShortcutHighlight, this.FindControl<MenuItem>("HighlightItem"));
            RegisterEditorShortcut(Settings.ShortcutHyperlink, this.FindControl<MenuItem>("HyperlinkItem"));
            RegisterEditorShortcut(Settings.ShortcutImage, this.FindControl<MenuItem>("ImageItem"));
            RegisterEditorShortcut(Settings.ShortcutClearFormat, this.FindControl<MenuItem>("ClearFormatItem"));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
