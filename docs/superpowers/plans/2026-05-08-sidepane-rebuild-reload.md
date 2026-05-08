# Side Pane Rebuild And Reload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `FolderPage` and `TocPage` rebuild on every entry, and switch folder refresh to change-detect plus full tree reload.

**Architecture:** Keep `MainPage` and `LeftPane` shell behavior intact, but force the side-pane `Frame` to recreate the selected page whenever the side pane becomes active again. `FolderPage` will stop relying on in-place incremental tree patching and instead rebuild a new `ExplorerItem` root after debounced file-system changes; `TocPage` will rebind from current editor state on each entry.

**Tech Stack:** WinUI 3, x:Bind pages, `FileSystemWatcher`, Rx subscriptions, MSTest architecture tests

---

### Task 1: Lock The Rebuild Contract With Tests

**Files:**
- Modify: `Tests/Typedown.ArchitectureTests/Phase14WinUILayoutTests.cs`

- [ ] **Step 1: Write failing tests**
- [ ] **Step 2: Run the focused architecture test filter and confirm failure**
- [ ] **Step 3: Implement the smallest code changes to satisfy the assertions**
- [ ] **Step 4: Re-run the same focused tests and confirm pass**

### Task 2: Recreate Current Side-Pane Page On Entry

**Files:**
- Modify: `Dev/Typedown.WinUI/Controls/SidePaneControls/LeftPane.xaml.cs`

- [ ] **Step 1: Add an explicit refresh path for the currently selected side-pane page**
- [ ] **Step 2: Trigger that refresh from `OnLoaded` so returning to the shell recreates `FolderPage` / `TocPage`**
- [ ] **Step 3: Keep normal tab-switch navigation behavior unchanged**
- [ ] **Step 4: Re-run focused architecture tests**

### Task 3: Move FolderPage To Full Reload On Change

**Files:**
- Modify: `Dev/Typedown.WinUI/Pages/SidePanePages/FolderPage.xaml.cs`
- Modify: `Dev/Typedown.Presentation/Models/ExplorerItem.cs`

- [ ] **Step 1: Introduce a page-level watcher/debounce path in `FolderPage`**
- [ ] **Step 2: Replace root-tree reuse with explicit `WorkFolderExplorerItem` rebuilds plus `Bindings.Update()`**
- [ ] **Step 3: Disable per-node live incremental watcher patching while preserving expanded-folder enumeration**
- [ ] **Step 4: Re-run focused architecture tests**

### Task 4: Rebuild TocPage On Every Entry

**Files:**
- Modify: `Dev/Typedown.WinUI/Pages/SidePanePages/TocPage.xaml.cs`

- [ ] **Step 1: Ensure `TocPage` refreshes bindings from current editor state on each navigation entry**
- [ ] **Step 2: Avoid keeping stale page-level tracked bindings across unload/load cycles**
- [ ] **Step 3: Re-run focused architecture tests**

### Task 5: Verify WinUI Wiring

**Files:**
- Verify: `Dev/Typedown.WinUI/Typedown.WinUI.csproj`

- [ ] **Step 1: Run the focused architecture test filter**
- [ ] **Step 2: Run `dotnet build .\\Dev\\Typedown.WinUI\\Typedown.WinUI.csproj -c Debug_Local -p:Platform=x64 -p:UseSharedCompilation=false /nodeReuse:false /v:minimal`**
- [ ] **Step 3: Run `git diff --check` on touched files**
