# Follow Opened File Folder Startup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `FolderStartupAction.FollowOpenedFileFolder` so a new app instance launched with a markdown file loads that file and points the side pane at the file's containing folder instead of restoring the previous folder context.

**Architecture:** Keep the new startup policy inside `Dev/Typedown.Presentation/ViewModels/FileViewModel.cs`, which already owns startup file loading and folder restoration. Expose the policy through the existing enum-backed settings surface, add WinUI-owned resource text for the new enum member, and lock the behavior with architecture tests that cover enum shape, resource shape, startup flow, and settings-page visibility.

**Tech Stack:** C#/.NET, WinUI 3, MSTest architecture tests, `.resw` resources

---

## File Map

- Modify: `Dev/Typedown.Core/Enums/StartupAction.cs`
  Responsibility: add the new `FolderStartupAction` member without changing the existing enumerable surface.
- Modify: `Dev/Typedown.Presentation/ViewModels/FileViewModel.cs`
  Responsibility: remember whether startup opened a command-line file successfully, then prefer that file's folder during folder-startup resolution.
- Inspect first, modify only if drift is found: `Dev/Typedown.WinUI/Pages/SettingPages/GeneralPage.xaml`
  Responsibility: keep using enum-backed `ComboBox` items and keep the custom-folder picker visible only for `OpenFolder`.
- Inspect first, modify only if drift is found: `Dev/Typedown.WinUI/Pages/SettingPages/GeneralPage.xaml.cs`
  Responsibility: keep `IsStartupOpenFolderItemLoad` scoped to `OpenFolder` only.
- Modify: `Dev/Typedown.WinUI/Resources/Strings/en/SettingsResources.resw`
  Responsibility: add the English label for the new enum member.
- Modify: `Dev/Typedown.WinUI/Resources/Strings/zh-Hans/SettingsResources.resw`
  Responsibility: add the simplified-Chinese label `跟随文件所在文件夹`.
- Modify: `Dev/Typedown.WinUI/Resources/Strings/zh-Hant/SettingsResources.resw`
  Responsibility: add the traditional-Chinese key so resource shape stays aligned with English and Simplified Chinese.
- Modify: `Tests/Typedown.ArchitectureTests/Phase13SettingsContractTests.cs`
  Responsibility: lock the enum contract so the new member and ordinal cannot drift.
- Modify: `Tests/Typedown.ArchitectureTests/Phase13LegacyTextResourceTests.cs`
  Responsibility: lock the new resource key and updated settings-resource key count.
- Modify: `Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs`
  Responsibility: lock the startup-flow code shape and verify the general settings page still hides the custom-folder picker unless the action is `OpenFolder`.

### Task 1: Add The Startup Contract And Resource Text

**Files:**
- Modify: `Tests/Typedown.ArchitectureTests/Phase13SettingsContractTests.cs`
- Modify: `Tests/Typedown.ArchitectureTests/Phase13LegacyTextResourceTests.cs`
- Modify: `Dev/Typedown.Core/Enums/StartupAction.cs`
- Modify: `Dev/Typedown.WinUI/Resources/Strings/en/SettingsResources.resw`
- Modify: `Dev/Typedown.WinUI/Resources/Strings/zh-Hans/SettingsResources.resw`
- Modify: `Dev/Typedown.WinUI/Resources/Strings/zh-Hant/SettingsResources.resw`

- [ ] **Step 1: Write the failing contract and resource tests**

```csharp
// Tests/Typedown.ArchitectureTests/Phase13SettingsContractTests.cs
AssertEnumMembers<FolderStartupAction>(
    ("None", 0),
    ("OpenLast", 1),
    ("OpenFolder", 2),
    ("FollowOpenedFileFolder", 3));
```

```csharp
// Tests/Typedown.ArchitectureTests/Phase13LegacyTextResourceTests.cs
[TestMethod]
public void TextResources_ReadsFollowOpenedFileFolderStartupText()
{
    var english = TestTextResourceReader.GetString(
        "en",
        TextResourceGroup.SettingsResources,
        "General.StartupAction.FolderStartupAction.FollowOpenedFileFolder");
    var simplifiedChinese = TestTextResourceReader.GetString(
        "zh-Hans",
        TextResourceGroup.SettingsResources,
        "General.StartupAction.FolderStartupAction.FollowOpenedFileFolder");

    Assert.AreEqual("Follow opened file folder", english);
    Assert.AreEqual("跟随文件所在文件夹", simplifiedChinese);
}

private static int GetExpectedKeyCount(TextResourceGroup group)
{
    return group switch
    {
        TextResourceGroup.CommonResources => 204,
        TextResourceGroup.DialogResources => 33,
        TextResourceGroup.Resources => 12,
        TextResourceGroup.SettingsResources => 142,
        _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
    };
}
```

- [ ] **Step 2: Run the targeted tests and verify they fail for the missing enum/resource**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug --filter "CoreEnums_RemainPureCoreTypes|TextResources_ReadsFollowOpenedFileFolderStartupText|TextResources_AllSupportedCulturesAndGroupsHaveExpectedKeyCountsAndExcludeTemplateKeys" -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

Expected:

```text
FAIL CoreEnums_RemainPureCoreTypes
FAIL TextResources_ReadsFollowOpenedFileFolderStartupText
FAIL TextResources_AllSupportedCulturesAndGroupsHaveExpectedKeyCountsAndExcludeTemplateKeys
```

- [ ] **Step 3: Add the enum member and resource entries with the final shipped names**

```csharp
// Dev/Typedown.Core/Enums/StartupAction.cs
public enum FolderStartupAction
{
    [Locale("NoAction")]
    None,

    [Locale("General.StartupAction.FolderStartupAction.OpenLast")]
    OpenLast,

    [Locale("General.StartupAction.FolderStartupAction.OpenFolder")]
    OpenFolder,

    [Locale("General.StartupAction.FolderStartupAction.FollowOpenedFileFolder")]
    FollowOpenedFileFolder
}
```

```xml
<!-- Dev/Typedown.WinUI/Resources/Strings/en/SettingsResources.resw -->
<data name="General.StartupAction.FolderStartupAction.FollowOpenedFileFolder" xml:space="preserve">
  <value>Follow opened file folder</value>
  <comment>General/Follow the folder that contains the startup file</comment>
</data>
```

```xml
<!-- Dev/Typedown.WinUI/Resources/Strings/zh-Hans/SettingsResources.resw -->
<data name="General.StartupAction.FolderStartupAction.FollowOpenedFileFolder" xml:space="preserve">
  <value>跟随文件所在文件夹</value>
  <comment>通用/跟随启动文件所在文件夹</comment>
</data>
```

```xml
<!-- Dev/Typedown.WinUI/Resources/Strings/zh-Hant/SettingsResources.resw -->
<data name="General.StartupAction.FolderStartupAction.FollowOpenedFileFolder" xml:space="preserve">
  <value>跟隨檔案所在資料夾</value>
  <comment>通用/跟隨啟動檔案所在資料夾</comment>
</data>
```

- [ ] **Step 4: Re-run the targeted tests and verify the contract passes**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug --filter "CoreEnums_RemainPureCoreTypes|TextResources_ReadsFollowOpenedFileFolderStartupText|TextResources_AllSupportedCulturesAndGroupsHaveExpectedKeyCountsAndExcludeTemplateKeys" -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

Expected:

```text
Passed! 3 tests passed.
```

- [ ] **Step 5: Commit the contract and resource slice**

```bash
git add Dev/Typedown.Core/Enums/StartupAction.cs Dev/Typedown.WinUI/Resources/Strings/en/SettingsResources.resw Dev/Typedown.WinUI/Resources/Strings/zh-Hans/SettingsResources.resw Dev/Typedown.WinUI/Resources/Strings/zh-Hant/SettingsResources.resw Tests/Typedown.ArchitectureTests/Phase13SettingsContractTests.cs Tests/Typedown.ArchitectureTests/Phase13LegacyTextResourceTests.cs
git commit -m "feat: add follow opened file folder startup option"
```

### Task 2: Implement Startup Folder Resolution In FileViewModel

**Files:**
- Modify: `Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs`
- Modify: `Dev/Typedown.Presentation/ViewModels/FileViewModel.cs`

- [ ] **Step 1: Write the failing startup-flow architecture test**

```csharp
// Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs
[TestMethod]
public void WinUIStartup_FollowOpenedFileFolderPrefersStartupFileDirectoryAndFallsBackToOpenLast()
{
    var fileViewModel = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "ViewModels", "FileViewModel.cs"));

    AssertContains(fileViewModel, "private string startupOpenedFilePath = null;");
    AssertContains(fileViewModel, "startupOpenedFilePath = null;");
    AssertContains(fileViewModel, "startupOpenedFilePath = FilePath;");
    AssertContains(fileViewModel, "case FolderStartupAction.FollowOpenedFileFolder:");
    AssertContains(fileViewModel, "Path.GetDirectoryName(startupOpenedFilePath)");
    AssertContains(fileViewModel, "return await ResolveOpenLastFolderAsync();");
}
```

- [ ] **Step 2: Run the focused startup-flow test and verify it fails**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug --filter "WinUIStartup_FollowOpenedFileFolderPrefersStartupFileDirectoryAndFallsBackToOpenLast" -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

Expected:

```text
FAIL WinUIStartup_FollowOpenedFileFolderPrefersStartupFileDirectoryAndFallsBackToOpenLast
```

- [ ] **Step 3: Add the minimal startup-state tracking and folder-resolution helpers**

```csharp
// Dev/Typedown.Presentation/ViewModels/FileViewModel.cs
private string startupOpenedFilePath = null;

public async Task LoadStartUpMarkdown()
{
    startupOpenedFilePath = null;

    var path = CommandLine.GetOpenFilePath(AppViewModel.CommandLineArgs);
    if (!string.IsNullOrEmpty(path))
    {
        if (await LoadFile(path, true, false))
        {
            startupOpenedFilePath = FilePath;
        }
        else
        {
            await NewFileFun(false);
        }

        return;
    }

    switch (SettingsViewModel.FileStartupAction)
    {
        case FileStartupAction.OpenLast:
            var lastFile = SettingsViewModel.LastFilePath;
            if (!string.IsNullOrWhiteSpace(lastFile) && !TryGetOpenedWindow(lastFile, out _) && File.Exists(lastFile))
            {
                await LoadFile(lastFile, true);
            }
            else
            {
                await NewFileFun(false);
                _ = LoadLastFileFromHistoryAfterInitialRenderAsync();
            }
            break;
        default:
            await NewFileFun(false);
            break;
    }
}

private async Task<string> ResolveOpenLastFolderAsync()
{
    var lastFolder = SettingsViewModel.LastFolderPath;
    if (string.IsNullOrWhiteSpace(lastFolder) || !Directory.Exists(lastFolder))
    {
        await AccessHistory.EnsureInitialized();
        lastFolder = AccessHistory.FolderRecentlyOpened.FirstOrDefault();
    }

    return !string.IsNullOrWhiteSpace(lastFolder) && Directory.Exists(lastFolder) ? lastFolder : null;
}

private async Task<string> ResolveStartupFolderAsync()
{
    switch (SettingsViewModel.FolderStartupAction)
    {
        case FolderStartupAction.FollowOpenedFileFolder:
            var startupFolder = string.IsNullOrWhiteSpace(startupOpenedFilePath) ? null : Path.GetDirectoryName(startupOpenedFilePath);
            if (!string.IsNullOrWhiteSpace(startupFolder) && Directory.Exists(startupFolder))
                return startupFolder;
            return await ResolveOpenLastFolderAsync();
        case FolderStartupAction.OpenLast:
            return await ResolveOpenLastFolderAsync();
        case FolderStartupAction.OpenFolder:
            return Directory.Exists(SettingsViewModel.StartupOpenFolder) ? SettingsViewModel.StartupOpenFolder : null;
        default:
            return null;
    }
}

private async void OnStartup()
{
    try
    {
        await WaitForInitialEditorFileLoadedAsync();
        if (!string.IsNullOrEmpty(WorkFolder))
            return;

        var folderToLoad = await ResolveStartupFolderAsync();
        if (!string.IsNullOrWhiteSpace(folderToLoad))
            await LoadFolder(folderToLoad);
    }
    catch
    {
        // Ignore
    }
}
```

- [ ] **Step 4: Re-run the focused startup-flow test and verify it passes**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug --filter "WinUIStartup_FollowOpenedFileFolderPrefersStartupFileDirectoryAndFallsBackToOpenLast" -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

Expected:

```text
Passed! 1 test passed.
```

- [ ] **Step 5: Commit the startup-flow slice**

```bash
git add Dev/Typedown.Presentation/ViewModels/FileViewModel.cs Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs
git commit -m "feat: follow startup file folder on launch"
```

### Task 3: Lock The Settings Page Contract And Run Full Verification

**Files:**
- Inspect first: `Dev/Typedown.WinUI/Pages/SettingPages/GeneralPage.xaml`
- Inspect first: `Dev/Typedown.WinUI/Pages/SettingPages/GeneralPage.xaml.cs`
- Modify: `Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs`

- [ ] **Step 1: Write the failing settings-page contract test**

```csharp
// Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs
[TestMethod]
public void GeneralSettingsPage_UsesEnumBackedFolderStartupOptionsAndOnlyShowsCustomFolderPickerForOpenFolder()
{
    var generalPage = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "SettingPages", "GeneralPage.xaml"));
    var generalPageCodeBehind = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "SettingPages", "GeneralPage.xaml.cs"));

    AssertContains(generalPage, "ItemsSource=\"{x:Bind enums:Enumerable.FolderStartupActions}\"");
    AssertContains(generalPage, "Visibility=\"{x:Bind local:GeneralPage.IsStartupOpenFolderItemLoad(Settings.FolderStartupAction), Mode=OneWay}\"");
    AssertContains(generalPageCodeBehind, "return action == FolderStartupAction.OpenFolder ? Visibility.Visible : Visibility.Collapsed;");
    AssertDoesNotContain(generalPage, "StartupOpenFolder, Mode=TwoWay}\" x:Load=");
}
```

- [ ] **Step 2: Run the focused settings-page test and verify it passes before any WinUI edit**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug --filter "GeneralSettingsPage_UsesEnumBackedFolderStartupOptionsAndOnlyShowsCustomFolderPickerForOpenFolder" -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

Expected:

```text
Passed! 1 test passed.
```

- [ ] **Step 3: Leave the dirty WinUI page files untouched unless the inspection finds drift**

```text
No XAML or code-behind change is expected here because the page already binds
its ComboBox to enums:Enumerable.FolderStartupActions and already gates the
custom-folder picker on FolderStartupAction.OpenFolder only.
```

- [ ] **Step 4: Run the full verification bundle**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug_Local -p:Platform=x64 -p:UseSharedCompilation=false /nodeReuse:false /v:minimal
git diff --check
```

Expected:

```text
Passed! All architecture tests passed.
Build succeeded.
git diff --check returns no output.
```

- [ ] **Step 5: Commit the verification/test lock slice**

```bash
git add Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs
git commit -m "test: lock startup folder settings contract"
```

## Self-Review

- Spec coverage: the new enum and localized label are handled in Task 1, startup-time file-folder resolution is handled in Task 2, and the WinUI settings-page contract plus full verification are handled in Task 3.
- Placeholder scan: no `TODO` or unspecified "handle edge cases" language remains; every task names exact files, code snippets, and commands.
- Type consistency: the plan uses one stable member name, `FollowOpenedFileFolder`, and one startup-state field, `startupOpenedFilePath`, across tests and implementation.

## Notes For Execution

- `Dev/Typedown.WinUI/Pages/SettingPages/GeneralPage.xaml` is already modified in the working tree. Do not edit it unless the inspection in Task 3 proves the current binding no longer matches the contract.
- `Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs` is also already dirty. Read the current file carefully and append the new test methods without discarding existing uncommitted edits.
- The fallback behavior for `FollowOpenedFileFolder` is deliberately `OpenLast` folder resolution when no valid startup file folder is available. This preserves a usable side pane on ordinary launches while keeping the new policy scoped to command-line file startup.

Plan complete and saved to `docs/superpowers/plans/2026-05-07-follow-opened-file-folder-startup-plan.md`. Two execution options:

**1. Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

**2. Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**
