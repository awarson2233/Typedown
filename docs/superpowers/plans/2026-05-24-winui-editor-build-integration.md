# WinUI Editor Build Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make every `Typedown.WinUI` main app build refresh and consume the latest `Typedown.Editor` static bundle.

**Architecture:** Keep `Typedown.WinUI` as the owner of packaged static resources and let `Typedown.Editor` remain the producer of the React bundle. Add a WinUI MSBuild target that runs the existing editor production build before WinUI build/package content is collected, so `Resources\Statics` is current without turning the JavaScript project into a C# assembly reference.

**Tech Stack:** WinUI 3, MSBuild, CRA/react-app-rewired, npm, existing `Resources\Statics` packaging target.

---

### Task 1: Add WinUI build dependency on Editor bundle

**Files:**
- Modify: `Dev/Typedown.WinUI/Typedown.WinUI.csproj`

- [ ] **Step 1: Add editor build properties**

Add these properties to the existing `Typedown Owner Governance` property group:

```xml
<TypedownEditorProjectDir>..\Typedown.Editor\</TypedownEditorProjectDir>
<TypedownEditorBuildCommand>npm run build</TypedownEditorBuildCommand>
<TypedownSkipEditorBuild>false</TypedownSkipEditorBuild>
```

- [ ] **Step 2: Add pre-build target**

Add this target before `AddEditorStaticBundleToPackagingOutputs`:

```xml
<Target Name="BuildTypedownEditorStaticBundle" BeforeTargets="PrepareForBuild" Condition="'$(TypedownSkipEditorBuild)' != 'true'">
    <Message Importance="high" Text="Building Typedown.Editor static bundle before Typedown.WinUI build." />
    <Exec WorkingDirectory="$(MSBuildProjectDirectory)\$(TypedownEditorProjectDir)" Command="$(TypedownEditorBuildCommand)" />
</Target>
```

- [ ] **Step 3: Verify the XML is valid**

Run:

```bash
dotnet msbuild "D:/source/repos/Typedown/Dev/Typedown.WinUI/Typedown.WinUI.csproj" -t:BuildTypedownEditorStaticBundle -p:Platform=x64 -p:Configuration=Debug_Local -v:minimal
```

Expected: the log prints `Building Typedown.Editor static bundle before Typedown.WinUI build.` and runs `npm run build` in `Dev/Typedown.Editor`.

---

### Task 2: Make editor production build usable from WinUI builds

**Files:**
- Modify if needed: `Dev/Typedown.Editor/package.json`

- [ ] **Step 1: Run the editor build through the new MSBuild target**

Run the command from Task 1 Step 3.

Expected current risk: CRA may fail under inherited `CI=true` because existing repository warnings are treated as errors.

- [ ] **Step 2: If CRA fails only because warnings are treated as errors, make the build script non-CI**

Change `Dev/Typedown.Editor/package.json` build script from:

```json
"build": "set GENERATE_SOURCEMAP=false&&react-app-rewired build"
```

to:

```json
"build": "set CI=false&&set GENERATE_SOURCEMAP=false&&react-app-rewired build"
```

- [ ] **Step 3: Re-run the editor target**

Run:

```bash
dotnet msbuild "D:/source/repos/Typedown/Dev/Typedown.WinUI/Typedown.WinUI.csproj" -t:BuildTypedownEditorStaticBundle -p:Platform=x64 -p:Configuration=Debug_Local -v:minimal
```

Expected: editor bundle generation succeeds and updates `Dev/Typedown.WinUI/Resources/Statics`.

---

### Task 3: Verify full WinUI build consumes latest editor output

**Files:**
- No planned source edits.

- [ ] **Step 1: Run targeted editor regression test**

Run:

```bash
CI=true npm --prefix "D:/source/repos/Typedown/Dev/Typedown.Editor" test -- --runInBand --watch=false pairingCtrl.test.js
```

Expected: `PASS src/components/Muya/lib/contentState/pairingCtrl.test.js`.

- [ ] **Step 2: Build the WinUI app**

Run:

```bash
dotnet build "D:/source/repos/Typedown/Dev/Typedown.WinUI/Typedown.WinUI.csproj" -c Debug_Local -p:Platform=x64 -p:UseSharedCompilation=false /nodeReuse:false /v:minimal
```

Expected: the log first runs the editor build target, then completes the WinUI build.

- [ ] **Step 3: Confirm generated static files are tracked intentionally**

Run:

```bash
git -C "D:/source/repos/Typedown" status --short
```

Expected: `Dev/Typedown.WinUI/Typedown.WinUI.csproj`, any package script change, editor source/test files from the previous fix, and updated `Dev/Typedown.WinUI/Resources/Statics` files may appear. Do not commit generated statics unless the user asks.

---

### Self-review

- Spec coverage: the plan wires editor build into WinUI build and verifies WinUI consumes `Resources\Statics`.
- Placeholder scan: no TBD/TODO placeholders remain.
- Type consistency: property and target names are consistent across tasks.
