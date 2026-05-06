# Typedown.WinUI main editor unwired audit

This audit only covers the current main editor surface: `MainContent`, `MenuBar`, `EditorContainer`, `WinUIEditorHost`, and the editor context menus / accelerator plumbing they expose.

## Current read

The main editor shell is mostly wired. `MainContent` hosts `MenuBar` plus `EditorContainer`; `EditorContainer` already handles drag/drop, scroll syncing, and the `FindReplace` popup; the top menu items are command-bound through `MenuBarItemBase`; and `WinUIKeyboardAccelerator` is implemented and attached from `RootControl`, so I did not find a remaining Ctrl+F stub to call out.

## Resolved in current working tree

### Editor right-click flyout actions

Surface: `Dev\Typedown.WinUI\Controls\EditorControls\EditorContainer.xaml:81-108`

Status: wired in the current working tree.

What is wired today: `EditorContainer.xaml.cs` now configures the flyout commands when the control is created, when `DataContext` changes, and when the flyout opens. The actions reuse `EditorViewModel` commands for Undo, Cut, Copy, Paste, Copy As Plain Text/Markdown/HTML, Paste As Plain Text, Delete, and Select All. Items are disabled when the editor is not available, and selection/history-sensitive actions are disabled when they cannot run.

Evidence: `Dev\Typedown.WinUI\Controls\EditorControls\EditorContainer.xaml` names `UndoItem`; `Dev\Typedown.WinUI\Controls\EditorControls\EditorContainer.xaml.cs` uses `ConfigureContextMenuCommands` and `SetCommand(...)` for all visible edit actions.

## Still unwired or only partially wired

### 1. File menu "New Window" is explicitly disabled

Surface: `Dev\Typedown.WinUI\Controls\EditorControls\MenuBarItems\FileItem.xaml:13-31`

What is wired today: `FileItem.ConfigureCommands` binds New/Open/Save/Import/Print/Close and keeps the recent/export submenus live.

What is missing: `SetCommand(NewWindowItem, null)` disables the item, so there is no current path to open a second editor window from the main menu.

Suggested seam: if this should work, route it through the app/window creation flow or an `IWindowService`-style host abstraction; otherwise hide the item.

Priority/risk: medium. It may be intentional, but it is visibly present in the editor shell and not functional.

## Wired enough to avoid rework

- The top Edit menu is connected: `Undo/Redo/Cut/Copy/Paste/Delete/SelectAll/Find/Replace` are command-bound and have shortcuts in `MenuBarItemStubs.cs:360-396`.
- `FindReplace` is already a real UI path, not a stub: `EditorContainer` observes `FloatViewModel.FindReplaceDialogOpen` and creates the popup, while `FindReplace.xaml.cs` drives search/replace and calls back into the editor sink.
- Drag/drop on the editor surface is wired: Markdown opens via `OpenFileCommand`, and images route to `IEditorCommandSink.Send("InsertImage", ...)`.
- The context submenus for format/image editing are wired: `ContextFormatItem` uses `FormatViewModel.SetFormatCommand`, and `ImageItem.Actions.cs` sends `ReplaceImage` and related actions through the editor sink.
- `WinUIKeyboardAccelerator` is implemented and attached from `RootControl`, so shortcut plumbing exists even though this audit found no Ctrl+F stub.

## Recommended next slices

1. Decide whether `NewWindow` should be supported or removed/hidden.
2. Re-run a quick main-editor pass after that wiring to confirm the visible menu surface and shortcut surface agree.
