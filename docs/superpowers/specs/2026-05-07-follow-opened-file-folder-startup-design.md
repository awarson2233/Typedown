# Follow Opened File Folder Startup Design

## Goal

Add a new `FolderStartupAction` option so that when a new app instance starts with a target file path, the side pane loads that file's containing folder instead of restoring the previous folder context.

This behavior only applies to new-instance startup with a file path. It must not change:

- the old instance
- current-instance `OpenFile(...)` behavior
- normal startup without a file path

## Current Context

Current startup behavior is split across:

- `Dev/Typedown.Core/Enums/StartupAction.cs`
- `Dev/Typedown.Presentation/ViewModels/SettingsViewModel.cs`
- `Dev/Typedown.Presentation/ViewModels/FileViewModel.cs`
- `Dev/Typedown.WinUI/Pages/SettingPages/GeneralPage.xaml`

`FileViewModel` already:

- reads the startup file path from `CommandLine.GetOpenFilePath(AppViewModel.CommandLineArgs)`
- loads that file first when present
- separately runs folder startup behavior using `FolderStartupAction`

The gap is that `FolderStartupAction.OpenLast` currently restores `LastFolderPath` or recent folder history even when the new instance was explicitly started for a different file.

## User-Facing Behavior

Add a new `FolderStartupAction` enum value:

- `FollowOpenedFileFolder`

User-facing text:

- Chinese meaning: `跟随文件所在文件夹`
- English resource key meaning: follow the folder that contains the startup file

Behavior rules:

1. If the new instance starts with a valid target file path, load that file using the existing file startup path.
2. After file loading succeeds, evaluate `FolderStartupAction`.
3. If `FolderStartupAction == FollowOpenedFileFolder`, load the containing folder of the opened file into the side pane.
4. If the startup file path is missing, invalid, or has no usable containing folder, fall back to the existing folder startup logic.
5. If `FolderStartupAction` is any other value, preserve current behavior.

Non-goals:

- Do not sync folder changes back to the old instance.
- Do not change ordinary in-instance `OpenFile(...)` behavior.
- Do not introduce a new startup-source abstraction just for this feature.

## Recommended Approach

Use a dedicated new enum value and keep the logic in `FileViewModel` startup flow.

Why this is the preferred approach:

- it preserves the meaning of existing `OpenLast` and `OpenFolder`
- it keeps startup policy in the layer that already owns startup file/folder restoration
- it avoids overengineering the activation layer

Rejected alternatives:

- extending `OpenLast` to sometimes mean "follow startup file folder"
  - rejected because it muddies settings semantics
- adding a new activation-source object
  - rejected because current requirements only depend on whether a startup file path exists

## Detailed Design

### 1. Enum and Settings Surface

Update `Dev/Typedown.Core/Enums/StartupAction.cs`:

- add `FolderStartupAction.FollowOpenedFileFolder`
- add the locale attribute for the new value
- keep `Enumerable.FolderStartupActions` unchanged so the UI picks up the new option automatically

No new persisted settings property is required. The existing `SettingsViewModel.FolderStartupAction` remains the source of truth.

### 2. Settings UI

Update `Dev/Typedown.WinUI/Pages/SettingPages/GeneralPage.xaml`:

- allow the `Folder` startup ComboBox to show the new enum value automatically
- keep the `StartupOpenFolder` path card visible only for `FolderStartupAction.OpenFolder`

No extra text box, toggle, or browse control is needed for `FollowOpenedFileFolder`.

### 3. Startup Data Flow

Update `Dev/Typedown.Presentation/ViewModels/FileViewModel.cs` startup logic.

Recommended control flow:

1. Read the startup file path from command-line args.
2. If a valid file path exists, attempt the existing file load flow.
3. Record the successfully opened file path for the current startup pass.
4. In the folder startup section:
   - if `FolderStartupAction == FollowOpenedFileFolder` and startup file load succeeded
   - compute `Path.GetDirectoryName(openedFilePath)`
   - if that directory exists, call the existing folder-loading path
5. Otherwise continue with the current `OpenLast` / `OpenFolder` / `None` branch behavior.

Important constraint:

- "follow file folder" should happen only after the startup file has been successfully resolved and loaded, so the side pane cannot drift to a folder unrelated to the actual opened file.

### 4. Side Pane Behavior

When `FollowOpenedFileFolder` applies, preserve the existing folder-open side effects:

- use the current folder-loading path
- keep side pane activation behavior consistent with current folder open behavior
- do not invent a separate lightweight side pane sync path

This keeps folder rendering, history recording, and shell visibility behavior consistent with the current implementation.

## Error Handling and Fallbacks

Fallback to existing folder startup behavior when any of the following is true:

- the startup file path is absent
- the startup file path does not exist
- file load fails
- `Path.GetDirectoryName(...)` returns null or empty
- the containing directory does not exist

This feature must not introduce a second error surface beyond the existing file startup failure path.

## Testing Strategy

Minimum coverage:

1. New-instance startup with valid file path and `FollowOpenedFileFolder`
   - file is loaded
   - side pane folder resolves to the file's containing folder
2. Startup without file path and `FollowOpenedFileFolder`
   - no special follow-folder behavior runs
   - existing folder startup behavior remains intact
3. Startup with invalid file path and `FollowOpenedFileFolder`
   - no new exception path
   - folder startup falls back to existing behavior
4. Settings UI visibility
   - `StartupOpenFolder` picker remains visible only for `OpenFolder`
   - it does not appear for `FollowOpenedFileFolder`

Recommended verification bundle after implementation:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug_Local -p:Platform=x64 -p:UseSharedCompilation=false /nodeReuse:false /v:minimal
git diff --check
```

## Scope Boundaries

In scope:

- new `FolderStartupAction` enum value
- settings UI exposure for that value
- startup-time folder selection behavior for new instances with a startup file
- focused tests for the new startup policy

Out of scope:

- multi-instance folder-state synchronization
- changing existing current-instance file-open behavior
- redesigning app activation routing
- broader startup-policy refactors unrelated to this feature
