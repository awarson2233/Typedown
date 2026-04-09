using PropertyChanged;
﻿using System.Collections.Generic;
using System.Reactive.Disposables;
using Typedown.Core.Interfaces;
using Typedown.Core.Models;
using Typedown.Core.Services;
using Typedown.Core.Utilities;
using Typedown.Core.ViewModels;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Controls;
using Typedown.Core.Enums;
using Key = Typedown.Core.Enums.VirtualKey;
using Mod = Typedown.Core.Enums.VirtualKeyModifiers;

namespace Typedown.Core.Controls.EditorControls.MenuBarItems
{
[DoNotNotify]
        public sealed partial class EditItem : MenuBarItemBase
    {
        public EditorViewModel Editor => ViewModel?.EditorViewModel;

        public FloatViewModel Float => ViewModel?.FloatViewModel;

        public FloatViewModel.FindReplaceDialogState OpenSearch => FloatViewModel.FindReplaceDialogState.Search;
        public FloatViewModel.FindReplaceDialogState OpenReplace => FloatViewModel.FindReplaceDialogState.Replace;

        private readonly CompositeDisposable disposables = new();

        public AccessHistory FileHistory => this.GetService<AccessHistory>();

        public HashSet<ShortcutKey> handledKey;

        public EditItem()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var acc = this.GetService<IKeyboardAccelerator>();
            disposables.Add(acc.RegisterGlobal((s, e) =>
            {
                handledKey ??= new()
                {
                    new(Mod.Control, Key.Z),
                    new(Mod.Control, Key.Y),
                    new(Mod.Control, Key.X),
                    new(Mod.Control, Key.C),
                    new(Mod.Control, Key.V),
                    new(Mod.Control, Key.A),
                    // new(Mod.None, Key.Delete),
                };
                if (handledKey.Contains(new(e.Modifiers, e.Key)))
                {
                    var editor = this.GetService<IMarkdownEditor>();
                    // TODO: Avalonia focus check - FocusManager API is different
                    // var focused = FocusManager.GetFocusedElement(XamlRoot);
                    // if (focused == editor)
                    //     e.Handled = true;
                }
            }));
        }

        protected override void OnRegisterShortcut()
        {
            RegisterEditorShortcut(Settings.ShortcutUndo, this.FindControl<MenuItem>("UndoItem"));
            RegisterEditorShortcut(Settings.ShortcutRedo, this.FindControl<MenuItem>("RedoItem"));
            RegisterEditorShortcut(Settings.ShortcutCut, this.FindControl<MenuItem>("CutItem"));
            RegisterEditorShortcut(Settings.ShortcutCopy, this.FindControl<MenuItem>("CopyItem"));
            RegisterEditorShortcut(Settings.ShortcutPaste, this.FindControl<MenuItem>("PasteItem"));
            RegisterEditorShortcut(Settings.ShortcutCopyAsPlainText, this.FindControl<MenuItem>("CopyAsPlainTextItem"));
            RegisterEditorShortcut(Settings.ShortcutCopyAsMarkdown, this.FindControl<MenuItem>("CopyAsMarkdownItem"));
            RegisterEditorShortcut(Settings.ShortcutCopyAsHTMLCode, this.FindControl<MenuItem>("CopyAsHTMLCodeItem"));
            RegisterEditorShortcut(Settings.ShortcutPasteAsPlainText, this.FindControl<MenuItem>("PasteAsPlainTextItem"));
            // RegisterEditorShortcut(Settings.ShortcutDelete, this.FindControl<MenuItem>("DeleteItem"));
            RegisterEditorShortcut(Settings.ShortcutSelectAll, this.FindControl<MenuItem>("SelectAllItem"));
            RegisterWindowShortcut(Settings.ShortcutFind, this.FindControl<MenuItem>("FindItem"));
            RegisterWindowShortcut(Settings.ShortcutFindNext, this.FindControl<MenuItem>("FindNextItem"));
            RegisterWindowShortcut(Settings.ShortcutFindPrevious, this.FindControl<MenuItem>("FindPreviousItem"));
            RegisterWindowShortcut(Settings.ShortcutReplace, this.FindControl<MenuItem>("ReplaceItem"));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            disposables.Clear();
        }
    }
}
