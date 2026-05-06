# Typedown.WinUI Settings Unwired Audit

## Summary

This audit checked the current `Dev/Typedown.WinUI` settings shell, settings pages, setting-item controls, and related navigation/service entry points for UI that is visible but not actually connected to behavior.

The current route table is no longer the main settings gap: `Route.GetSettingsPageType` includes `General`, `View`, `Editor`, `Image`, `ImageUpload`, `Export`, `ExportConfig`, `Shortcut`, `UploadConfig`, and `About`. The confirmed gaps are mostly at the behavior layer:

- `ViewPage` is visible in the settings navigation, but its controls are static Toolkit markup and do not bind to `SettingsViewModel`.
- Several in-settings drill-in cards and config-list cards execute `AppViewModel.NavigateCommand`, but WinUI `SettingsPage` no longer subscribes to that command.
- `AboutPage` shows clickable/action UI for credits, Store, and feedback, but the page code-behind does not launch links or open the feedback dialog.

## Resolution Notes

2026-05-05 implementation pass:

- `ViewPage.xaml` now binds its visible Toolkit controls to `SettingsViewModel` for app theme, Mica, editor Mica, animation, topmost, compact mode, status bar, and side pane settings.
- `SettingsPage.xaml.cs` now subscribes to `AppViewModel.NavigateCommand.OnExecute`, routes settings drill-in commands through the existing `Navigate` helper, and disposes the subscription on unload.
- `AboutPage.xaml(.cs)` now wires the credits and Store cards to `Launcher.LaunchUriAsync(...)`, and wires the feedback button to `FeedbackDialog.OpenFeedbackDialog(XamlRoot)`.

The original audit details below are kept as the evidence trail for what was found before the repair.

## Confirmed Unwired Or Partially Wired Items

### View settings page controls are static UI

- Paths:
  - `Dev/Typedown.WinUI/Pages/SettingPages/ViewPage.xaml`
  - `Dev/Typedown.WinUI/Pages/SettingPages/ViewPage.xaml.cs`
  - `Dev/Typedown.WinUI/Controls/SettingControls/SettingItems/ViewSetting.xaml`
- What the UI suggests should happen:
  - The `View` settings page should edit appearance/window settings such as app theme, Mica, editor Mica, animation, topmost, compact mode, status bar visibility, and side pane visibility.
- What is actually missing:
  - `ViewPage.xaml` contains direct `CommunityToolkit.WinUI.Controls` cards with fixed state such as `SelectedIndex="0"`, `ToggleSwitch IsOn="True"`, and unbound `ToggleSwitch` elements.
  - `ViewPage.xaml.cs` stores `ViewModel` and `SettingsViewModel` on navigation, but the XAML does not bind to them.
  - A wired legacy-style WinUI control exists at `Controls/SettingControls/SettingItems/ViewSetting.xaml`, with bindings like `SettingsViewModel.AppTheme`, `Settings.UseMicaEffect`, `Settings.AnimationEnable`, `Settings.Topmost`, `Settings.AppCompactMode`, `Settings.StatusBarOpen`, and `Settings.SidePaneOpen`, but current search finds no reference to `ViewSetting` from `ViewPage` or elsewhere.
- Suggested next repair:
  - Replace the static contents of `ViewPage.xaml` with the already wired `ViewSetting` control, or port those bindings into the Toolkit `SettingsCard`/`SettingsExpander` markup one setting at a time.
  - After repair, verify each setting changes the backing `SettingsViewModel` value and, for live UI settings, still propagates to the app shell/editor.

### Internal settings drill-in navigation uses an unsubscribed command

- Paths:
  - `Dev/Typedown.WinUI/Pages/SettingsPage.xaml.cs`
  - `Dev/Typedown.WinUI/Controls/SettingControls/SettingItems/GeneralSetting.xaml`
  - `Dev/Typedown.WinUI/Controls/SettingControls/SettingItems/ImageSetting.xaml`
  - `Dev/Typedown.WinUI/Controls/SettingControls/SettingItems/ExportSetting.xaml.cs`
  - `Dev/Typedown.WinUI/Controls/SettingControls/SettingItems/ImageUploadSetting.xaml.cs`
  - Legacy comparison: `Dev/Typedown/Pages/SettingsPage.xaml.cs`
- What the UI suggests should happen:
  - `General > Shortcut keys` should open `ShortcutPage`.
  - `Image > Upload configs` should open `ImageUploadPage`.
  - Clicking an export config should open `ExportConfigPage?id`.
  - Clicking an image upload config should open `UploadConfigPage?id`.
- What is actually missing:
  - The visible cards/list items execute `ViewModel.NavigateCommand` with route strings such as `Settings/Shortcut`, `Settings/ImageUpload`, `Settings/ExportConfig?{id}`, and `Settings/UploadConfig?{id}`.
  - WinUI `SettingsPage.xaml.cs` has a private `Navigate(string args)` helper that can resolve these routes, but `OnLoaded` is empty and there is no `NavigateCommand.OnExecute.Subscribe(...)` hookup.
  - The legacy `Dev/Typedown/Pages/SettingsPage.xaml.cs` subscribed to `ViewModel.NavigateCommand.OnExecute` in `OnLoaded`, which is the missing bridge in the WinUI page.
- Suggested next repair:
  - Add a WinUI settings-page subscription from `ViewModel.NavigateCommand.OnExecute` to the existing private `Navigate` helper, dispose it on unload, and preserve current `SettingsNavigationParameter` propagation.
  - Verify the four drill-in routes above, including query parsing for config IDs and breadcrumb/back behavior.

### Image upload settings entry from the image context menu executes the same dead navigation command

- Paths:
  - `Dev/Typedown.WinUI/Controls/EditorControls/ContextMenuItems/ImageItem.Actions.cs`
  - `Dev/Typedown.WinUI/Pages/SettingsPage.xaml.cs`
- What the UI suggests should happen:
  - The image context menu item for image upload settings should open the relevant settings page.
- What is actually missing:
  - `OnImageUploadSettingsClick` executes `viewModel?.NavigateCommand.Execute("Settings/Image")`.
  - As above, WinUI has no active subscription for `AppViewModel.NavigateCommand`, so this path has no settings navigation target unless some caller handles the command externally; no such handler was found in current `Dev/Typedown.WinUI` or `Dev/Typedown.Presentation` searches.
- Suggested next repair:
  - Fix the central `NavigateCommand` subscription in `SettingsPage` if this context-menu entry is only expected to work while settings is already open.
  - If the intent is to open settings from the editor context menu, route it through an app/root navigation service or the existing `MenuBar.NavigateRequested` pattern instead of relying on a settings-page-local subscriber.

### About page action cards and feedback button are visible but inert

- Paths:
  - `Dev/Typedown.WinUI/Pages/SettingPages/AboutPage.xaml`
  - `Dev/Typedown.WinUI/Pages/SettingPages/AboutPage.xaml.cs`
  - Legacy/alternate WinUI reference: `Dev/Typedown.WinUI/Controls/SettingControls/AboutApp.xaml(.cs)`
  - Current untracked dialog reference: `Dev/Typedown.WinUI/Controls/DialogControls/FeedbackDialog.xaml(.cs)`
- What the UI suggests should happen:
  - The open-source software card should open the credits page.
  - The Microsoft Store card should open the Store listing.
  - The feedback button should open a feedback dialog.
- What is actually missing:
  - `AboutPage.xaml` marks the credits and Store cards as `IsClickEnabled="True"`, but no click handler, command, or navigation URI is attached.
  - The feedback `Button` has only content text; it has no `Click`, `Command`, or flyout/dialog binding.
  - `AboutPage.xaml.cs` only exposes `VersionText` and initializes the page.
  - A separate `AboutApp` control still shows the older working pattern: `HyperlinkButton NavigateUri` for links and `FeedBackButton_Click` calling `FeedbackDialog.OpenFeedbackDialog(XamlRoot)`. The current `AboutPage` does not use that control or reproduce its handlers.
- Suggested next repair:
  - Add explicit card click handlers or commands to launch `https://typedown.ownbox.cn/credits` and the Store URL.
  - Wire the feedback button to `FeedbackDialog.OpenFeedbackDialog(XamlRoot)` or replace the page content with a wired equivalent of `AboutApp`.
  - Ensure the currently untracked `Controls/DialogControls` folder is intentionally tracked if `FeedbackDialog` is the intended implementation.

## Confirmed Wired Items Checked

- Settings shell root navigation:
  - `SettingsPage.xaml` exposes root `NavigationViewItem` entries for `General`, `View`, `Editor`, `Image`, `Export`, and `About`.
  - `SettingsPage.xaml.cs` handles `NavigationView.SelectionChanged`, resolves root page types through `Route.GetSettingsPageType`, navigates `ContentFrame`, updates breadcrumb items, and maps subpages back to their root selection for `ShortcutPage`, `ImageUploadPage`, and `UploadConfigPage`.
- Route inventory:
  - `Route.cs` currently includes the settings root pages and the drill-in pages `ExportConfig`, `ImageUpload`, `Shortcut`, and `UploadConfig`.
- General settings:
  - Startup actions, startup folder picker, autosave, keep-run, language, and reset settings are bound to `SettingsViewModel` or `Settings.ResetSettingsCommand`.
- Editor settings:
  - Font size, line height, editor width, tab size, auto-pair quote/bracket, and Markdown auto-pair syntax bind to `SettingsViewModel` properties.
- Image settings:
  - Insert-action comboboxes, path pickers, relative path toggles, and upload-config selection are bound to settings and the `ImageUpload.ImageUploadConfigs` collection.
  - The upload-config picker list is partially wired; the confirmed break is the drill-in navigation card, not the combo-box binding itself.
- Export settings:
  - Add/delete export config operations call `IFileExport.AddExportConfig` and `IFileExport.RemoveExportConfig`.
  - Export config edit page loads by ID, saves on unload, and resolves detail controls for PDF/HTML/Image config types.
- Image upload config settings:
  - Add/delete image upload config operations call `ImageUpload.AddImageUploadConfig` and `ImageUpload.RemoveImageUploadConfig`.
  - Upload config edit page loads by ID, saves on unload, renders FTP/Git/OSS/SCP/PowerShell detail controls, and includes a wired test-upload button.
- Shortcut page:
  - `ShortcutSetting` reflects over `SettingsViewModel` `ShortcutKey` properties, filters/searches them, and uses `ShortcutPickerButton` to edit the selected shortcut value.
  - This page is wired once reachable; the confirmed gap is the navigation command that should open it from `GeneralSetting`.
- Main menu route into settings:
  - `MenuBar` / `FileItem` uses `NavigateRequested`, and `MainPage.OnMenuBarNavigateRequested` navigates the root frame to `SettingsPage` with a `SettingsNavigationParameter`. This path does not depend on `AppViewModel.NavigateCommand`.
- Export/print backing service:
  - `WinUIFileExport.Print` currently calls `IFileConverter.HtmlToPdf`; it is not the old documented `NotSupportedException` gap in the current tree.

## Verification Notes

- This was a static source audit only. I did not launch the WinUI app or click through the UI.
- I used `docs/winui3-feature-gap-audit.md` only as an inventory starting point. Several items in that older doc are stale against current code, especially route registration and `WinUIFileExport.Print`.
- The working tree already contains unrelated modifications and an untracked `Dev/Typedown.WinUI/Controls/DialogControls/` directory. This audit treats current file contents as ground truth but does not assume whether those changes are finalized.
- `SettingsPage.xaml.cs` currently contains a private `Navigate` helper that appears intended for the missing `NavigateCommand` subscription. I am not claiming the helper itself is broken, only that no current WinUI subscription invokes it.
- `ViewSetting.xaml` appears wired, but it is not referenced by current `ViewPage.xaml`. If another runtime XAML substitution mechanism exists outside the searched files, re-check this item dynamically.
