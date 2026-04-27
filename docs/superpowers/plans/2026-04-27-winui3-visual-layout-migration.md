# WinUI3 Visual Layout Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the WinUI3 `MainPage` from the Phase 14 dashboard-style migration shell to a legacy Typedown visual layout skeleton.

**Architecture:** Keep `Typedown.UI` platform-neutral and keep `Typedown.WinUI` as the WinUI3 XAML owner. This phase migrates visual layout only: caption, menu strip, editor/side-pane split, editor host slot, find/search placeholder, and status bar. It does not wire real commands, does not migrate WebView2 internals, does not move legacy `Typedown.Core` XAML controls, and does not switch the default startup path.

**Tech Stack:** .NET 9, WinUI 3, Windows App SDK, MSTest architecture tests, XAML `x:Bind`.

---

## File Map

- `Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs`: Locks the WinUI3 visual layout to legacy-shaped regions instead of the previous Phase 14 dashboard regions.
- `Dev/Typedown.WinUI/Views/MainPage.xaml`: Implements the first visual-only legacy shell layout.
- `docs/phase14-ui-visual-migration-plan.md`: Records that Phase 14 has moved from migration-dashboard skeleton to first legacy visual shell skeleton.

## Task 1: Lock Legacy Visual Region Names

**Files:**
- Modify: `Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs`

- [ ] **Step 1: Write failing architecture assertions**

Replace the old dashboard-region assertions with checks for `LegacyCaptionSlot`, `LegacyMenuBar`, `LegacyMainContent`, `LegacySidePane`, `LegacyEditorContainer`, `LegacyFindReplaceHost`, and `LegacyBottomStatusBar`.

- [ ] **Step 2: Run the failing test**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug --filter Phase14WinUILayoutTests /nologo /v:minimal
```

Expected: fail because the current WinUI3 page still contains `AppDocumentChrome`, `PrimaryToolbar`, `MigrationStatusPanel`, and `BottomStatusBar`.

## Task 2: Replace Dashboard Shell With Legacy-Shaped Visual Shell

**Files:**
- Modify: `Dev/Typedown.WinUI/Views/MainPage.xaml`

- [ ] **Step 1: Implement minimal visual layout**

Create a root with four rows matching legacy `RootControl -> MainPage`: caption, menu strip, main content, status bar. In main content, create left side pane, splitter line, editor container, retained `WinUIEditorHost`, search/find placeholder, scrollbars, and editor load failure placeholder.

- [ ] **Step 2: Run architecture test**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug --filter Phase14WinUILayoutTests /nologo /v:minimal
```

Expected: pass.

## Task 3: Build WinUI3 Project

**Files:**
- Verify: `Dev/Typedown.WinUI/Typedown.WinUI.csproj`

- [ ] **Step 1: Build WinUI3 x64**

Run:

```powershell
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
```

Expected: 0 errors.

## Task 4: Document Phase 14 Visual Status

**Files:**
- Modify: `docs/phase14-ui-visual-migration-plan.md`

- [ ] **Step 1: Update implementation status**

Record that the first visual migration pass now mirrors the legacy root structure rather than the migration-status dashboard.

- [ ] **Step 2: Run boundary tests**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug /nologo /v:minimal
```

Expected: all architecture tests pass.

## Scope Guard

- Do not move `Dev/Typedown.Core` XAML controls into `Typedown.WinUI`.
- Do not reference `Typedown.Core` UI controls from `Typedown.WinUI`.
- Do not delete `Dev/Typedown.XamlUI`.
- Do not change `Typedown.sln` startup/default project behavior.
- Do not implement real file/editor/settings commands in this pass.
