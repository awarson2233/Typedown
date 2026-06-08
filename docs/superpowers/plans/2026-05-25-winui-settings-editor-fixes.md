# WinUI Settings and Editor Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix three WinUI migration regressions: language selection not showing the current value, KeepRun being a no-op, and stale WebView horizontal scrollbar state after side-pane width changes.

**Architecture:** Keep fixes local to existing boundaries. Presentation owns persisted settings and stable option collections; WinUI owns window lifecycle and XAML binding contracts; the editor web bundle owns scroll state calculation and exposes a host-triggered refresh command through the existing transport channel.

**Tech Stack:** WinUI 3 / Windows App SDK 2.0, C# MSTest architecture tests, React/TypeScript editor bundle, existing editor transport bridge.

---

## File Structure

- Modify `Dev/Typedown.Presentation/Utilities/Locale.cs`: expose a stable language option dictionary and stable key list.
- Modify `Dev/Typedown.WinUI/Pages/SettingPages/GeneralPage.xaml`: bind the language ComboBox to the stable key list with consistent `x:Bind` selection.
- Modify `Dev/Typedown.WinUI/App.xaml.cs`: subscribe to `AppWindow.Closing` and hide the window when `KeepRun` is enabled.
- Modify `Dev/Typedown.WinUI/Services/WinUIWindowContext.cs`: make activation show a hidden `AppWindow` before activating it.
- Modify `Dev/Typedown.WinUI/Controls/EditorControls/EditorContainer.xaml.cs`: request a web scroll-state refresh whenever the XAML host size changes.
- Modify `Dev/Typedown.Editor/src/services/scrollbar.ts`: add a `RefreshScrollState` host command that calls the existing scroll-state reporter.
- Modify `Tests/Typedown.ArchitectureTests/Phase13SettingsContractTests.cs`: add language option stability/binding contract tests.
- Modify `Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs`: add WebView scroll refresh contract tests.
- Modify `Tests/Typedown.ArchitectureTests/Phase14StartupBehaviorTests.cs`: add KeepRun window lifecycle contract tests.

---

### Task 1: Add failing contract tests

**Files:**
- Modify: `Tests/Typedown.ArchitectureTests/Phase13SettingsContractTests.cs`
- Modify: `Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs`
- Modify: `Tests/Typedown.ArchitectureTests/Phase14StartupBehaviorTests.cs`

- [ ] **Step 1: Add language option stability test**

Add this test to `Phase13SettingsContractTests.cs`:

```csharp
[TestMethod]
public void LanguageSettings_UseStableOptionsAndConsistentXBindSelection()
{
    var presentationLocale = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "Utilities", "Locale.cs"));
    var generalPage = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "SettingPages", "GeneralPage.xaml"));

    AssertHasTypeReference(presentationLocale, "private static readonly Lazy<IReadOnlyDictionary<string, string>> langsOptions");
    AssertHasTypeReference(presentationLocale, "private static readonly Lazy<IReadOnlyList<string>> langOptionKeys");
    AssertHasTypeReference(presentationLocale, "public static IReadOnlyDictionary<string, string> LangsOptions => langsOptions.Value;");
    AssertHasTypeReference(presentationLocale, "public static IReadOnlyList<string> LangOptionKeys => langOptionKeys.Value;");
    AssertNoTypeReference(presentationLocale, "public static IReadOnlyDictionary<string, string> LangsOptions =>\r\n            new Dictionary<string, string>");
    AssertHasTypeReference(generalPage, "ItemsSource=\"{x:Bind utils:Locale.LangOptionKeys}\"");
    AssertHasTypeReference(generalPage, "SelectedItem=\"{x:Bind Settings.Language, Mode=TwoWay}\"");
    AssertNoTypeReference(generalPage, "SelectedItem=\"{Binding SettingsViewModel.Language");
}
```

- [ ] **Step 2: Add WebView refresh test**

Add this test to `Phase14WinUILayoutTests.cs`:

```csharp
[TestMethod]
public void EditorHostSizeChanges_RequestWebScrollStateRefresh()
{
    var winuiRoot = Path.Combine(RepoRoot, "Dev", "Typedown.WinUI");
    var editorContainer = File.ReadAllText(Path.Combine(winuiRoot, "Controls", "EditorControls", "EditorContainer.xaml.cs"));
    var scrollbarService = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.Editor", "src", "services", "scrollbar.ts"));

    AssertContains(editorContainer, "RequestScrollStateRefresh();");
    AssertContains(editorContainer, "private void RequestScrollStateRefresh()");
    AssertContains(editorContainer, "IEditorCommandSink") ;
    AssertContains(editorContainer, "Send(\"RefreshScrollState\", null)");
    AssertContains(scrollbarService, "transport.addListener('RefreshScrollState', postScrollState)");
}
```

- [ ] **Step 3: Add KeepRun lifecycle test**

Add this test to `Phase14StartupBehaviorTests.cs`:

```csharp
[TestMethod]
public void KeepRun_CancelsSystemCloseAndHidesWindowWithoutDestroyingIt()
{
    var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));
    var windowContextSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services", "WinUIWindowContext.cs"));

    AssertContains(appSource, "appWindow.Closing -= OnAppWindowClosing;");
    AssertContains(appSource, "appWindow.Closing += OnAppWindowClosing;");
    AssertContains(appSource, "private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)");
    AssertContains(appSource, "if (appViewModel?.SettingsViewModel.KeepRun != true)");
    AssertContains(appSource, "args.Cancel = true;");
    AssertContains(appSource, "sender.Hide();");
    AssertContains(windowContextSource, "window.AppWindow.Show();");
    AssertContains(windowContextSource, "window.Activate();");
}
```

- [ ] **Step 4: Run focused tests and verify RED**

Run:

```bash
dotnet test Tests/Typedown.ArchitectureTests/Typedown.ArchitectureTests.csproj --filter "LanguageSettings_UseStableOptionsAndConsistentXBindSelection|EditorHostSizeChanges_RequestWebScrollStateRefresh|KeepRun_CancelsSystemCloseAndHidesWindowWithoutDestroyingIt"
```

Expected: FAIL because the new contract snippets do not exist yet.

---

### Task 2: Implement stable language binding

**Files:**
- Modify: `Dev/Typedown.Presentation/Utilities/Locale.cs`
- Modify: `Dev/Typedown.WinUI/Controls/SettingControls/Locale.cs`
- Modify: `Dev/Typedown.WinUI/Pages/SettingPages/GeneralPage.xaml`

- [ ] **Step 1: Add stable language option members**

In `Locale.cs`, replace the computed `LangsOptions` property with cached values:

```csharp
private static readonly Lazy<IReadOnlyDictionary<string, string>> langsOptions = new(() =>
    new Dictionary<string, string>(SupportedLangs.Append(new("default", GetString("UseSystemSetting"))));

private static readonly Lazy<IReadOnlyList<string>> langOptionKeys = new(() => LangsOptions.Keys.ToArray());

public static IReadOnlyDictionary<string, string> LangsOptions => langsOptions.Value;

public static IReadOnlyList<string> LangOptionKeys => langOptionKeys.Value;
```

Keep:

```csharp
public static string GetLangOptionDisplayName(string key) => LangsOptions[key];
```

- [ ] **Step 2: Forward the stable key list in the WinUI setting-control facade**

In `Dev/Typedown.WinUI/Controls/SettingControls/Locale.cs`, add:

```csharp
public static IReadOnlyList<string> LangOptionKeys => PresentationLocale.LangOptionKeys;
```

- [ ] **Step 3: Make the ComboBox use stable `x:Bind` for source and selected item**

In `GeneralPage.xaml`, change the language ComboBox to:

```xml
<ComboBox Grid.Column="1" VerticalAlignment="Center" ItemsSource="{x:Bind utils:Locale.LangOptionKeys}" Width="200" SelectedItem="{x:Bind Settings.Language, Mode=TwoWay}">
```

- [ ] **Step 4: Run the language contract test**

Run:

```bash
dotnet test Tests/Typedown.ArchitectureTests/Typedown.ArchitectureTests.csproj --filter LanguageSettings_UseStableOptionsAndConsistentXBindSelection
```

Expected: PASS.

---

### Task 3: Implement WebView scroll-state refresh

**Files:**
- Modify: `Dev/Typedown.WinUI/Controls/EditorControls/EditorContainer.xaml.cs`
- Modify: `Dev/Typedown.Editor/src/services/scrollbar.ts`

- [ ] **Step 1: Request refresh from host size changes**

In `EditorContainer.xaml.cs`, call the refresh helper at the end of `OnSizeChanged`:

```csharp
RequestScrollStateRefresh();
```

Add the helper near `OnScroll`:

```csharp
private void RequestScrollStateRefresh()
{
    viewModel?.ServiceProvider.GetService<IEditorCommandSink>()?.Send("RefreshScrollState", null);
}
```

- [ ] **Step 2: Handle refresh command in web scroll service**

In `scrollbar.ts`, add:

```typescript
transport.addListener('RefreshScrollState', postScrollState)
```

near the existing resize/scroll listeners.

- [ ] **Step 3: Run the WebView contract test**

Run:

```bash
dotnet test Tests/Typedown.ArchitectureTests/Typedown.ArchitectureTests.csproj --filter EditorHostSizeChanges_RequestWebScrollStateRefresh
```

Expected: PASS.

---

### Task 4: Implement KeepRun window lifecycle

**Files:**
- Modify: `Dev/Typedown.WinUI/App.xaml.cs`
- Modify: `Dev/Typedown.WinUI/Services/WinUIWindowContext.cs`

- [ ] **Step 1: Wire AppWindow closing once per created window**

In `App.xaml.cs`, after obtaining `appWindow` in `OnLaunched`, add:

```csharp
appWindow.Closing -= OnAppWindowClosing;
appWindow.Closing += OnAppWindowClosing;
```

- [ ] **Step 2: Add close interception**

Add this method in `App.xaml.cs` near `OnWindowClosed`:

```csharp
private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
{
    if (appViewModel?.SettingsViewModel.KeepRun != true)
    {
        return;
    }

    args.Cancel = true;
    sender.Hide();
}
```

- [ ] **Step 3: Detach closing handler during final close cleanup**

In `OnWindowClosed`, before clearing fields, add:

```csharp
if (sender is Window closedWindow)
{
    closedWindow.AppWindow.Closing -= OnAppWindowClosing;
}
```

- [ ] **Step 4: Show hidden window on activation**

In `WinUIWindowContext.Activate`, change the active branch to:

```csharp
if (!isClosed)
{
    window.AppWindow.Show();
    window.Activate();
}
```

- [ ] **Step 5: Run the KeepRun contract test**

Run:

```bash
dotnet test Tests/Typedown.ArchitectureTests/Typedown.ArchitectureTests.csproj --filter KeepRun_CancelsSystemCloseAndHidesWindowWithoutDestroyingIt
```

Expected: PASS.

---

### Task 5: Run full focused verification

- [ ] **Step 1: Run all affected architecture tests**

Run:

```bash
dotnet test Tests/Typedown.ArchitectureTests/Typedown.ArchitectureTests.csproj --filter "Phase13SettingsContractTests|Phase14WinUILayoutTests|Phase14StartupBehaviorTests"
```

Expected: PASS.

- [ ] **Step 2: Build WinUI Debug_Local x64**

Run:

```bash
dotnet build Dev/Typedown.WinUI/Typedown.WinUI.csproj -c Debug_Local -p:Platform=x64
```

Expected: exit code 0.

- [ ] **Step 3: Run editor tests for touched JS/TS area if the test runner is available**

Run from `Dev/Typedown.Editor`:

```bash
npm test -- --watchAll=false
```

Expected: exit code 0, or report any pre-existing test-runner/environment blocker with output.
