# Core Decoupling and Slimming Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn `Dev/Typedown.Core` into the durable pure logic and MVVM contract layer for WinUI3, while shrinking `Dev/Typedown.Core.Legacy` to reference-only UWP compatibility code.

**Architecture:** Keep `Typedown.Core` free of XAML, WinRT UI types, file pickers, WebView2, and shell services. Move reusable state, settings, editor commands, persistence DTOs, and platform-neutral services into Core; keep WinUI3 adapters in `Typedown.WinUI` and legacy UWP implementation in `Typedown.Core.Legacy`.

**Tech Stack:** .NET 9 class libraries, MSTest architecture tests, WinUI3 / Windows App SDK shell, legacy WinUI2/UWP reference project.

---

## Current Baseline

The main branch is now at `bb7d09a`. `Dev/Typedown.Core` already contains the former contracts surface: `Editor`, `EditorRuntime`, `Settings`, `Shell`, and platform interfaces. `Dev/Typedown.UI` contains shell-agnostic MVVM and resource helpers. `Dev/Typedown.WinUI` compiles against pure Core and UI; old copied XAML/code-behind lives under `LegacyCopied` and is excluded from build. `Dev/Typedown.Core.Legacy` is the renamed old UWP core.

The biggest remaining coupling is in copied but currently excluded WinUI setting pages and setting controls. They still reference old namespaces such as `Typedown.Core.Models`, `Typedown.Core.Services`, and `Typedown.Core.ViewModels`. Treat those files as migration references until their models and ViewModels have been moved into pure Core/UI.

## File Ownership Map

- `Dev/Typedown.Core/Settings`: pure settings records, default values, shortcut definitions, validation helpers.
- `Dev/Typedown.Core/EditorRuntime`: editor state snapshots such as paragraph, menu, format, toc, word count, content history DTOs.
- `Dev/Typedown.Core/Shell`: shell chrome and document UI state.
- `Dev/Typedown.Core/Interfaces`: platform-neutral service contracts only.
- `Dev/Typedown.UI/ViewModels`: MVVM classes that compose Core state and contracts, with no XAML references.
- `Dev/Typedown.UI/Resources`: resource loading/catalog helpers; may read legacy resources while migration is incomplete.
- `Dev/Typedown.WinUI/Services`: WinUI3 adapters for dialogs, activation, dispatcher, file picker, window context.
- `Dev/Typedown.WinUI/Controls` and `Dev/Typedown.WinUI/Pages`: XAML and view-only code-behind, no business logic.
- `Dev/Typedown.Core.Legacy`: read-only migration source unless a legacy UWP build break must be fixed.
- `Tests/Typedown.ArchitectureTests`: boundary tests for every migration slice.

---

### Task 1: Lock the Pure Core Boundary

**Files:**
- Modify: `Tests/Typedown.ArchitectureTests/Phase10CoreContractsBoundaryTests.cs`
- Modify: `Tests/Typedown.ArchitectureTests/Phase13SettingsContractTests.cs`
- Modify: `Tests/Typedown.ArchitectureTests/Phase13RuntimeStateContractTests.cs`

- [ ] **Step 1: Add failing boundary tests for Core dependencies**

Add assertions that every `.cs` file under `Dev/Typedown.Core` rejects these references:

```csharp
AssertNoTypeReference(source, "Microsoft.UI.Xaml");
AssertNoTypeReference(source, "Windows.UI.Xaml");
AssertNoTypeReference(source, "Microsoft.Web.WebView2");
AssertNoTypeReference(source, "Windows.Storage.Pickers");
AssertNoTypeReference(source, "Typedown.Core.Legacy");
```

- [ ] **Step 2: Run the boundary tests**

Run:

```powershell
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
```

Expected before implementation: any accidental Core UI dependency fails with an explicit source file path.

- [ ] **Step 3: Fix only the reported Core dependency**

If a failure points to a UI type, move the type usage into `Dev/Typedown.WinUI/Services` and expose only a Core interface in `Dev/Typedown.Core/Interfaces`.

- [ ] **Step 4: Verify and commit**

Run the same test command. Expected: `80+` tests pass, `0` fail.

Commit:

```powershell
git add Tests\Typedown.ArchitectureTests Dev\Typedown.Core Dev\Typedown.WinUI
git commit -m "test: lock pure core dependency boundary"
```

### Task 2: Move Settings Models Out of Legacy Core

**Files:**
- Create/modify: `Dev/Typedown.Core/Settings/*.cs`
- Modify: `Dev/Typedown.UI/ViewModels/*Settings*.cs`
- Modify: `Dev/Typedown.WinUI/Controls/SettingControls/**`
- Test: `Tests/Typedown.ArchitectureTests/Phase13SettingsContractTests.cs`

- [ ] **Step 1: List legacy setting model dependencies**

Run:

```powershell
rg -n "Typedown.Core.Models|Typedown.Core.ViewModels|Typedown.Core.Services" Dev\Typedown.WinUI\Controls\SettingControls Dev\Typedown.WinUI\Pages\SettingPages -g "*.cs" -g "*.xaml"
```

Record each model type and owner file in the task notes before editing.

- [ ] **Step 2: Add tests for pure setting snapshots**

In `Phase13SettingsContractTests.cs`, add tests that verify default values and serialization-friendly shape for migrated settings, for example:

```csharp
AssertHasTypeReference(source, "public sealed record EditorSettingsSnapshot");
AssertNoTypeReference(source, "DependencyObject");
AssertNoTypeReference(source, "Windows.UI.Xaml");
AssertNoTypeReference(source, "Microsoft.UI.Xaml");
```

- [ ] **Step 3: Move one settings model family at a time**

Move only one family per commit, starting with low-risk enum/DTO groups:

```text
ImageUploadMethod / InsertImageAction
ExportType / PrintOrientation
StartupAction / AppTheme
ShortcutKey equivalents already represented by EditorShortcutKey
```

Keep model names stable when possible. If names conflict with current Core contract names, add a small adapter in `Dev/Typedown.UI` rather than changing XAML-facing names and behavior in the same commit.

- [ ] **Step 4: Update WinUI setting references**

For each migrated type, change WinUI setting files from old namespaces to either `Typedown.Core.Settings` or `Typedown.UI.ViewModels`. Do not bind XAML directly to `Typedown.Core.Legacy`.

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet build Dev\Typedown.Core\Typedown.Core.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.UI\Typedown.UI.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 -p:UseSharedCompilation=false
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
```

Commit:

```powershell
git add Dev\Typedown.Core Dev\Typedown.UI Dev\Typedown.WinUI Tests\Typedown.ArchitectureTests
git commit -m "refactor: move settings models to pure core"
```

### Task 3: Move Runtime Editor State Out of Legacy Core

**Files:**
- Modify: `Dev/Typedown.Core/EditorRuntime/*.cs`
- Modify: `Dev/Typedown.UI/ViewModels/*Editor*.cs`
- Modify: `Dev/Typedown.WinUI/Controls/EditorControls/**`
- Test: `Tests/Typedown.ArchitectureTests/Phase13RuntimeStateContractTests.cs`

- [ ] **Step 1: Add tests for runtime state shape**

For each runtime state type used by editor menus, assert that the Core type exists and avoids UI dependencies:

```csharp
AssertHasTypeReference(source, "public sealed record EditorFormatState");
AssertHasTypeReference(source, "public sealed record EditorParagraphState");
AssertNoTypeReference(source, "MenuFlyoutItem");
AssertNoTypeReference(source, "DependencyProperty");
```

- [ ] **Step 2: Map legacy runtime state to Core records**

Compare these legacy files against current Core equivalents:

```powershell
rg -n "class .*State|record .*State" Dev\Typedown.Core.Legacy\Models\RuntimeModels Dev\Typedown.Core\EditorRuntime
```

Only add missing data fields that are required by WinUI3 menu state or editor bridge payloads.

- [ ] **Step 3: Keep command execution outside Core**

Represent menu state and command requests in Core. Keep actual command execution in `Dev/Typedown.UI/ViewModels` and `Dev/Typedown.WinUI/Controls/WinUIEditorHostController.cs`.

- [ ] **Step 4: Verify and commit**

Run Core, UI, WinUI build and architecture tests. Commit:

```powershell
git add Dev\Typedown.Core Dev\Typedown.UI Dev\Typedown.WinUI Tests\Typedown.ArchitectureTests
git commit -m "refactor: move editor runtime state to pure core"
```

### Task 4: Rebuild Settings MVVM on Pure Core

**Files:**
- Modify/create: `Dev/Typedown.UI/ViewModels/Settings*.cs`
- Modify: `Dev/Typedown.WinUI/Pages/SettingPages/*.xaml.cs`
- Modify: `Dev/Typedown.WinUI/Controls/SettingControls/**/*.xaml`
- Modify: `Dev/Typedown.WinUI/Controls/SettingControls/**/*.xaml.cs`
- Test: `Tests/Typedown.ArchitectureTests/Phase10CoreContractsBoundaryTests.cs`

- [ ] **Step 1: Add tests that WinUI settings do not reference old Core namespaces**

Assert active WinUI settings files no longer contain:

```csharp
AssertNoTypeReference(source, "Typedown.Core.Models");
AssertNoTypeReference(source, "Typedown.Core.Services");
AssertNoTypeReference(source, "Typedown.Core.ViewModels");
AssertNoTypeReference(source, "Typedown.Core.Legacy");
```

Skip `Dev/Typedown.WinUI/LegacyCopied`.

- [ ] **Step 2: Introduce setting page ViewModels in UI**

Create or extend `Typedown.UI.ViewModels` classes so XAML binds to UI ViewModels, not legacy model/services. The ViewModel may depend on Core records and Core service interfaces.

- [ ] **Step 3: Reactivate one setting page per commit**

Recommended order:

```text
GeneralPage
ViewPage
EditorPage
ShortcutPage
ImagePage
ImageUploadPage
ExportPage
ExportConfigPage
UploadConfigPage
AboutPage
```

For each page, remove its `Page Remove` or `Compile Remove` exclusion only after the page compiles and has no legacy namespace references.

- [ ] **Step 4: Verify and commit each page**

Run:

```powershell
dotnet build Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 -p:UseSharedCompilation=false
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
```

Commit one page family at a time:

```powershell
git commit -m "feat: reconnect WinUI general settings page"
```

### Task 5: Replace Legacy Resource Access

**Files:**
- Modify/create: `Dev/Typedown.Core/Resources` or `Dev/Typedown.UI/Resources`
- Modify: `Dev/Typedown.UI/Resources/LegacyTextResourceReader.cs`
- Test: `Tests/Typedown.ArchitectureTests/Phase13LegacyTextResourceTests.cs`

- [ ] **Step 1: Decide resource ownership**

If resources are UI text only, keep reader/catalog in `Typedown.UI`. If settings defaults need localized labels, keep only keys in Core and resolve text in UI/WinUI.

- [ ] **Step 2: Remove direct dependency on legacy resource folders**

Create a `Typedown.UI.Resources.TextResourceCatalog` that loads embedded or copied resources from a stable UI location. Do not read from `Typedown.Core.Legacy` after this task completes.

- [ ] **Step 3: Update tests**

Change Phase13 resource tests from `LegacyTextResources_*` to neutral naming such as `TextResources_*`, while preserving key-count assertions for `en`, `zh-Hans`, and `zh-Hant`.

- [ ] **Step 4: Verify and commit**

Run architecture tests and WinUI build. Commit:

```powershell
git add Dev\Typedown.UI Tests\Typedown.ArchitectureTests
git commit -m "refactor: move text resources out of legacy core"
```

### Task 6: Shrink Legacy Surface and Remove Dead References

**Files:**
- Modify: `Typedown.sln`
- Modify: `Dev/Typedown.Core.Legacy/Typedown.Core.Legacy.csproj`
- Modify: `Dev/Typedown.WinUI/Typedown.WinUI.csproj`
- Modify: `Tests/Typedown.ArchitectureTests/Phase10CoreContractsBoundaryTests.cs`

- [ ] **Step 1: Count remaining active references to legacy namespaces**

Run:

```powershell
rg -n "Typedown.Core.Legacy|Typedown.Core.Models|Typedown.Core.ViewModels|Typedown.Core.Services" Dev Tests -g "*.cs" -g "*.xaml" -g "*.csproj"
```

Expected before this task: references remain only in `Dev/Typedown.Core.Legacy`, `Dev/Typedown.WinUI/LegacyCopied`, and tests that explicitly mention legacy.

- [ ] **Step 2: Add a legacy allowlist test**

Assert that active WinUI and UI projects have no old namespace references except under `LegacyCopied`.

- [ ] **Step 3: Reduce legacy project role**

Do not delete `Typedown.Core.Legacy` yet. First mark it as reference-only in docs and ensure WinUI3 solution configs do not build/deploy legacy projects for `Debug_Local|x64`.

- [ ] **Step 4: Verify and commit**

Run:

```powershell
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 -p:UseSharedCompilation=false
```

Commit:

```powershell
git add Typedown.sln Dev\Typedown.Core.Legacy Dev\Typedown.WinUI Tests\Typedown.ArchitectureTests
git commit -m "chore: shrink legacy core integration surface"
```

---

## Execution Notes

Use a fresh worktree for each large task:

```powershell
git worktree add .worktrees/core-settings-slice -b codex/core-settings-slice
```

Keep each task as a separate commit. Do not edit files under `Dev/Typedown.WinUI/LegacyCopied` except to refresh reference copies with explicit approval. Do not reintroduce `Typedown.Core.Legacy` references into `Dev/Typedown.WinUI/Typedown.WinUI.csproj`.

## Final Verification Gate

Before declaring any phase complete, run:

```powershell
dotnet build Dev\Typedown.Core\Typedown.Core.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.UI\Typedown.UI.csproj -c Debug -p:UseSharedCompilation=false
dotnet build Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 -p:UseSharedCompilation=false
dotnet test Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false
```

Expected result: all builds succeed, WinUI build has `0` warnings and `0` errors, architecture tests have `0` failures.
