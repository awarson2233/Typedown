# Presentation Capability Boundary Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完善 `Typedown.Presentation` 的承接能力，使 WinUI3 和旧 App 都通过同一套 shell-agnostic MVVM / port contract 接入应用逻辑。

**Architecture:** `Typedown.Presentation` 是应用编排层，拥有 ViewModel、命令、资源抽象和 UI/platform 端口接口；`Typedown.WinUI` 和旧 `Typedown` app head 只实现这些端口并完成 DI composition root。`Typedown.Core` 保持纯逻辑、模型、持久化和平台无关服务，不继续承载 UI shell port。

**Tech Stack:** .NET 9 class libraries, MSTest architecture tests, Microsoft.Extensions.DependencyInjection, WinUI3 app head, legacy XAML app head, WebView2 editor bridge.

---

## Current State

当前分支已经把主 MVVM 文件放在 `Dev\Typedown.Presentation`：

- `ViewModels\AppViewModel.cs`
- `ViewModels\EditorViewModel.cs`
- `ViewModels\FileViewModel.cs`
- `ViewModels\FloatViewModel.cs`
- `ViewModels\FormatViewModel.cs`
- `ViewModels\ParagraphViewModel.cs`
- `ViewModels\SettingsViewModel.cs`
- `ViewModels\UIViewModel.cs`

Presentation 已有部分端口：

- `IAppActivationService`
- `IDialogService`
- `IEditorCommandSink`
- `IEditorSettingsNotifier`
- `IFilePickerService`
- `IUiDispatcher`
- `IWindowContext`

但 Presentation ViewModel 仍直接使用多项位于 `Typedown.Core.Interfaces` 的 UI/platform port：

- `IClipboard`
- `IFileExport`
- `IFileOperation`
- `IFloatViewService`
- `IKeyboardAccelerator`
- `ITableDialogService`
- `IWindowService`

这些接口的实现仍由 app head 提供；问题是接口归属还在 Core，使 Core 边界不够纯，也让 WinUI 接入时不容易看清 Presentation 需要哪些 adapter。

`IFileConverter` 与 `IPowerShellService` 暂时保留在 Core：`PDFConfigModel` 与 `PowerShellModel` 仍直接从 Core 模型中解析这两个服务。若现在移动它们，会迫使 Core 反向依赖 Presentation。后续应先把导出/上传执行编排从 Core 模型中拆到 Presentation，再移动这两个端口。

## Target Boundary

### Typedown.Presentation owns

- ViewModel and command orchestration.
- UI/platform port interfaces needed by ViewModels.
- Resource key and localization resolver abstraction.
- Editor bridge orchestration against `RemoteInvoke` / `EventCenter`.
- Service registration extension for Presentation-owned ViewModels and pure services.

### Typedown.Core owns

- Persistent models, runtime models, enums, utility algorithms.
- Database and non-UI services.
- Editor protocol primitives that are UI-neutral.
- `IAppDataPathProvider`, because Core persistence needs it before Presentation exists.

### Typedown.WinUI owns

- WinUI3 XAML, controls and pages.
- WebView2 host implementation.
- Window/App lifecycle, Package/MSIX, launch settings.
- Implementations of Presentation ports.
- DI composition root that wires WinUI implementations to Presentation ports.

### Legacy Typedown app head owns

- Legacy XAML host wiring.
- Legacy implementations of Presentation ports.
- Temporary compatibility until WinUI3 reaches functional parity.

## Task 1: Add architecture tests for Presentation port ownership

**Files:**

- Modify: `Tests/Typedown.ArchitectureTests/Phase10CoreContractsBoundaryTests.cs`
- Modify or create: `Tests/Typedown.ArchitectureTests/Phase15PresentationBoundaryTests.cs`

- [ ] **Step 1: Write failing tests for moved port ownership**

Add a test that scans `Dev\Typedown.Presentation\Interfaces` and asserts the required port interfaces exist there:

```csharp
private static readonly string[] RequiredPresentationPorts =
[
    "IClipboard.cs",
    "IFileExport.cs",
    "IFileOperation.cs",
    "IFloatViewService.cs",
    "IKeyboardAccelerator.cs",
    "ITableDialogService.cs",
    "IWindowService.cs"
];

[TestMethod]
public void Presentation_OwnsViewModelPortInterfaces()
{
    var interfacesRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Presentation", "Interfaces");

    foreach (var fileName in RequiredPresentationPorts)
    {
        Assert.IsTrue(File.Exists(Path.Combine(interfacesRoot, fileName)), $"{fileName} must be owned by Typedown.Presentation.");
    }
}
```

Add a Core boundary test that rejects these interface declarations in `Dev\Typedown.Core\Interfaces` after migration:

```csharp
[TestMethod]
public void Core_DoesNotOwnPresentationViewModelPorts()
{
    var coreInterfacesRoot = Path.Combine(RepoRoot, "Dev", "Typedown.Core", "Interfaces");

    foreach (var fileName in RequiredPresentationPorts)
    {
        Assert.IsFalse(File.Exists(Path.Combine(coreInterfacesRoot, fileName)), $"{fileName} belongs in Typedown.Presentation.");
    }
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore --filter Presentation
```

Expected: fails because the listed interfaces still live in `Dev\Typedown.Core\Interfaces`.

- [ ] **Step 3: Keep tests only, commit not yet**

Do not move files until Task 2. The failing tests define the target boundary.

## Task 2: Move UI/platform ports from Core to Presentation

**Files:**

- Move from: `Dev\Typedown.Core\Interfaces\IClipboard.cs`
- Move from: `Dev\Typedown.Core\Interfaces\IFileExport.cs`
- Move from: `Dev\Typedown.Core\Interfaces\IFileOperation.cs`
- Move from: `Dev\Typedown.Core\Interfaces\IFloatViewService.cs`
- Move from: `Dev\Typedown.Core\Interfaces\IKeyboardAccelerator.cs`
- Move from: `Dev\Typedown.Core\Interfaces\ITableDialogService.cs`
- Move from: `Dev\Typedown.Core\Interfaces\IWindowService.cs`
- To: `Dev\Typedown.Presentation\Interfaces\`
- Modify: all compile errors caused by namespace changes in `Dev\Typedown.Presentation`, `Dev\Typedown`, `Dev\Typedown.WinUI`, and tests.

- [ ] **Step 1: Move files with git mv**

Run:

```powershell
git mv .\Dev\Typedown.Core\Interfaces\IClipboard.cs .\Dev\Typedown.Presentation\Interfaces\IClipboard.cs
git mv .\Dev\Typedown.Core\Interfaces\IFileExport.cs .\Dev\Typedown.Presentation\Interfaces\IFileExport.cs
git mv .\Dev\Typedown.Core\Interfaces\IFileOperation.cs .\Dev\Typedown.Presentation\Interfaces\IFileOperation.cs
git mv .\Dev\Typedown.Core\Interfaces\IFloatViewService.cs .\Dev\Typedown.Presentation\Interfaces\IFloatViewService.cs
git mv .\Dev\Typedown.Core\Interfaces\IKeyboardAccelerator.cs .\Dev\Typedown.Presentation\Interfaces\IKeyboardAccelerator.cs
git mv .\Dev\Typedown.Core\Interfaces\ITableDialogService.cs .\Dev\Typedown.Presentation\Interfaces\ITableDialogService.cs
git mv .\Dev\Typedown.Core\Interfaces\IWindowService.cs .\Dev\Typedown.Presentation\Interfaces\IWindowService.cs
```

- [ ] **Step 2: Change namespaces only**

In each moved file, change:

```csharp
namespace Typedown.Core.Interfaces
```

to:

```csharp
namespace Typedown.Presentation.Interfaces
```

Do not rewrite method signatures in this task unless a signature references a UI framework type and blocks `Typedown.Presentation` from building.

- [ ] **Step 3: Update using directives**

Replace references in active projects:

```powershell
rg -n "Typedown.Core.Interfaces" Dev\Typedown.Presentation Dev\Typedown Dev\Typedown.WinUI Tests -g "*.cs"
```

For files that use only moved interfaces, change to:

```csharp
using Typedown.Presentation.Interfaces;
```

For files that use both Core-owned and Presentation-owned interfaces, keep both usings:

```csharp
using Typedown.Core.Interfaces;
using Typedown.Presentation.Interfaces;
```

- [ ] **Step 4: Build Presentation**

Run:

```powershell
dotnet build .\Dev\Typedown.Presentation\Typedown.Presentation.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

Expected: build passes. If it fails because a moved interface references a type still in Core, keep the Core model reference; do not move models in this task.

- [ ] **Step 5: Build Core**

Run:

```powershell
dotnet build .\Dev\Typedown.Core\Typedown.Core.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

Expected: build passes and no Core source declares the moved interfaces.

- [ ] **Step 6: Run architecture tests**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

Expected: all architecture tests pass.

- [ ] **Step 7: Commit**

```powershell
git add -A
git commit -m "refactor: move presentation ports out of core"
```

## Task 3: Register complete WinUI platform service stubs against Presentation ports

**Files:**

- Modify: `Dev\Typedown.WinUI\App.xaml.cs`
- Modify: `Dev\Typedown.WinUI\Services\WinUIPlatformServices.cs`
- Create or modify service files under: `Dev\Typedown.WinUI\Services\`
- Test: `Tests\Typedown.ArchitectureTests\Phase15PresentationBoundaryTests.cs`

- [ ] **Step 1: Write failing test for WinUI service registration surface**

Add a source-level architecture test that asserts `App.xaml.cs` or a WinUI service registration extension mentions every Presentation port required by ViewModels:

```csharp
[TestMethod]
public void WinUI_CompositionRootRegistersPresentationPorts()
{
    var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));
    var servicesSource = string.Join(
        Environment.NewLine,
        Directory.GetFiles(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Services"), "*.cs")
            .Select(File.ReadAllText));

    var source = appSource + Environment.NewLine + servicesSource;
    foreach (var interfaceName in new[]
    {
        "IClipboard",
        "IFileExport",
        "IFileOperation",
        "IFloatViewService",
        "IKeyboardAccelerator",
        "IEditorCommandSink",
        "IEditorSettingsNotifier",
        "ITableDialogService",
        "IWindowService"
    })
    {
        StringAssert.Contains(source, interfaceName);
    }
}
```

- [ ] **Step 2: Run test and verify it fails**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore --filter WinUI_CompositionRootRegistersPresentationPorts
```

Expected: fails because WinUI only registers the minimal platform services.

- [ ] **Step 3: Add minimal service classes**

Create WinUI implementations that are honest about unsupported behavior. Prefer forwarding-capable implementations for simple services and explicit `NotSupportedException` only for high-risk services that need a later dedicated migration.

Minimum target classes:

```text
Dev\Typedown.WinUI\Services\WinUIClipboard.cs
Dev\Typedown.WinUI\Services\WinUIEditorCommandSink.cs
Dev\Typedown.WinUI\Services\WinUIEditorSettingsNotifier.cs
Dev\Typedown.WinUI\Services\WinUIFileOperation.cs
Dev\Typedown.WinUI\Services\WinUIFloatViewService.cs
Dev\Typedown.WinUI\Services\WinUIKeyboardAccelerator.cs
Dev\Typedown.WinUI\Services\WinUITableDialogService.cs
Dev\Typedown.WinUI\Services\WinUIWindowService.cs
Dev\Typedown.WinUI\Services\WinUIFileExport.cs
```

Implementation rule:

- `WinUIClipboard` should use `Windows.ApplicationModel.DataTransfer.Clipboard`.
- `WinUIEditorCommandSink` should target the active `WinUIEditorHost`; if no host is active, return `false`.
- `WinUIEditorSettingsNotifier` should send `SettingsChanged` through `IEditorCommandSink`.
- `WinUIWindowService` should expose `WindowHandle` and WinUI-safe coordinate helpers.
- `WinUIFloatViewService`, `WinUITableDialogService`, `WinUIFileExport`, `WinUIFileOperation`, `WinUIKeyboardAccelerator` may start as minimal implementations only if they compile and fail explicitly on unsupported operations.

- [ ] **Step 4: Register services in the WinUI composition root**

In `App.xaml.cs`, after existing platform service registrations, register all Presentation ports:

```csharp
.AddSingleton<IClipboard, WinUIClipboard>()
.AddSingleton<IEditorCommandSink, WinUIEditorCommandSink>()
.AddSingleton<IEditorSettingsNotifier, WinUIEditorSettingsNotifier>()
.AddSingleton<IFileOperation, WinUIFileOperation>()
.AddSingleton<IFloatViewService, WinUIFloatViewService>()
.AddSingleton<IKeyboardAccelerator, WinUIKeyboardAccelerator>()
.AddSingleton<ITableDialogService, WinUITableDialogService>()
.AddSingleton<IWindowService, WinUIWindowService>()
.AddSingleton<IFileExport, WinUIFileExport>()
```

`IFileConverter` and `IPowerShellService` are still Core-owned service contracts in this phase. WinUI may need implementations for functional parity, but that should be tracked as Core execution-model cleanup rather than Presentation port migration.

If a service needs scoped lifetime because it depends on scoped ViewModels, register it as scoped and document the reason in code with a single short comment.

- [ ] **Step 5: Build WinUI**

Run with ARM64 MSBuild targeting x64:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe' .\Dev\Typedown.WinUI\Typedown.WinUI.csproj /t:Build /p:Configuration=Debug_Local /p:Platform=x64 /p:UseSharedCompilation=false /v:minimal /m:1 /nodeReuse:false
```

Expected: build passes. Existing nullable or XAML warnings may remain, but no errors.

- [ ] **Step 6: Run architecture tests**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

Expected: all tests pass.

- [ ] **Step 7: Commit**

```powershell
git add Dev\Typedown.WinUI Tests\Typedown.ArchitectureTests
git commit -m "feat: register winui presentation port adapters"
```

## Task 4: Make WinUI editor bridge call Presentation instead of local stubs

**Files:**

- Modify: `Dev\Typedown.WinUI\Controls\WinUIEditorBridgeAdapter.cs`
- Modify: `Dev\Typedown.WinUI\Controls\WinUIEditorDocumentSession.cs`
- Modify: `Dev\Typedown.WinUI\Controls\WinUIEditorHost.cs`
- Modify: `Dev\Typedown.WinUI\Controls\WinUIEditorHostController.cs`
- Modify: `Dev\Typedown.WinUI\Controls\EditorControls\EditorContainer.xaml.cs`
- Test: architecture tests under `Tests\Typedown.ArchitectureTests`

- [ ] **Step 1: Add failing architecture test rejecting editor invoke stubs**

Add:

```csharp
[TestMethod]
public void WinUI_EditorBridgeDoesNotUseSmokeInvokeStubs()
{
    var sessionSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "WinUIEditorDocumentSession.cs"));
    Assert.IsFalse(sessionSource.Contains("HandleStubInvoke", StringComparison.Ordinal));
    Assert.IsFalse(sessionSource.Contains("\"ExportCallback\" => HandleStubInvoke", StringComparison.Ordinal));
    Assert.IsFalse(sessionSource.Contains("\"PrintHTML\" => HandleStubInvoke", StringComparison.Ordinal));
    Assert.IsFalse(sessionSource.Contains("\"SetClipboard\" => HandleStubInvoke", StringComparison.Ordinal));
    Assert.IsFalse(sessionSource.Contains("\"OpenNewWindow\" => HandleStubInvoke", StringComparison.Ordinal));
}
```

- [ ] **Step 2: Run test and verify it fails**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore --filter WinUI_EditorBridgeDoesNotUseSmokeInvokeStubs
```

Expected: fails because `HandleStubInvoke` exists.

- [ ] **Step 3: Pass IServiceProvider/AppViewModel into WinUIEditorHost**

Expose a property on `WinUIEditorHost`:

```csharp
public IServiceProvider? Services { get; set; }
```

When `EditorContainer` creates the host, pass the inherited DataContext service provider:

```csharp
if (DataContext is AppViewModel appViewModel)
{
    editorHost.Services = appViewModel.ServiceProvider;
}
```

- [ ] **Step 4: Route invoke messages to Presentation RemoteInvoke**

Change bridge handling so invoke names first call `RemoteInvoke` from the `AppViewModel.ServiceProvider`. Preserve only host-private commands such as `ContentLoaded` and `GetCurrentTheme` in WinUI.

Target behavior:

```text
ContentLoaded     -> WinUI host readiness handling
GetCurrentTheme   -> WinUI theme payload
GetSettings       -> Presentation RemoteInvoke
SetClipboard      -> Presentation RemoteInvoke
ExportCallback    -> Presentation RemoteInvoke
PrintHTML         -> Presentation RemoteInvoke
GetStringResources-> Presentation RemoteInvoke
OpenNewWindow     -> Presentation RemoteInvoke or FileViewModel.NewWindowCommand
UnhandledException-> Presentation/logging handler
```

- [ ] **Step 5: Route editor events to Presentation EventCenter**

For `message` and `diffmsg`, forward at least these events to `EventCenter`:

```text
MarkdownChange
FileLoaded
CursorChange
SelectionChange
CodeMirrorSelectionChange
StateChange
```

Keep session state updates only as a local mirror, not the source of truth.

- [ ] **Step 6: Build and run tests**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe' .\Dev\Typedown.WinUI\Typedown.WinUI.csproj /t:Build /p:Configuration=Debug_Local /p:Platform=x64 /p:UseSharedCompilation=false /v:minimal /m:1 /nodeReuse:false
```

Expected: tests and WinUI build pass.

- [ ] **Step 7: Manual smoke**

Run WinUI Debug_Local and verify:

```text
editor loads
typing updates saved marker/state
GetSettings returns actual settings values
theme change still updates editor colors
```

- [ ] **Step 8: Commit**

```powershell
git add Dev\Typedown.WinUI Tests\Typedown.ArchitectureTests
git commit -m "feat: route winui editor bridge through presentation"
```

## Task 5: Initialize localization through Presentation in WinUI

**Files:**

- Create: `Dev\Typedown.WinUI\Services\WinUILocaleService.cs`
- Create or modify: `Dev\Typedown.WinUI\Controls\LocaleString.cs`
- Modify: `Dev\Typedown.WinUI\App.xaml.cs`
- Test: `Tests\Typedown.ArchitectureTests\Phase13LegacyTextResourceTests.cs`

- [ ] **Step 1: Add failing test for WinUI localization initialization**

Add assertions:

```csharp
[TestMethod]
public void WinUI_InitializesPresentationLocalization()
{
    var appSource = File.ReadAllText(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "App.xaml.cs"));
    var winUISources = string.Join(
        Environment.NewLine,
        Directory.GetFiles(Path.Combine(RepoRoot, "Dev", "Typedown.WinUI"), "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText));

    StringAssert.Contains(winUISources, "PresentationLocale.StringResolver");
    StringAssert.Contains(appSource + winUISources, "Initialize");
    StringAssert.Contains(winUISources, "ResourceManager.Current.MainResourceMap");
}
```

- [ ] **Step 2: Run test and verify it fails**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore --filter WinUI_InitializesPresentationLocalization
```

Expected: fails because WinUI has no formal localization initializer.

- [ ] **Step 3: Add WinUILocaleService**

Implement a WinUI service that assigns:

```csharp
PresentationLocale.StringResolver = GetString;
```

Lookup order:

```text
Typedown.WinUI/{source}
Typedown/{source}
file fallback: AppContext.BaseDirectory\Resources\Strings\<culture>\<source>.resw
```

The `.resw` fallback must reuse the same key normalization rule as legacy App:

```csharp
key.Replace('.', '/')
```

- [ ] **Step 4: Add WinUI LocaleString markup extension**

Create `Typedown.WinUI.Controls.LocaleString` for WinUI XAML pages:

```csharp
public sealed class LocaleString : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public PresentationLocale.ResourceSource Source { get; set; }

    protected override object ProvideValue()
    {
        return PresentationLocale.GetString(Key, Source);
    }
}
```

Use the WinUI `Microsoft.UI.Xaml.Markup.MarkupExtension` base type.

- [ ] **Step 5: Initialize before first page navigation**

Call locale initialization in `App.OnLaunched` before creating/navigating `RootControl`.

- [ ] **Step 6: Validate**

Run:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore --filter Phase13LegacyTextResourceTests
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe' .\Dev\Typedown.WinUI\Typedown.WinUI.csproj /t:Build /p:Configuration=Debug_Local /p:Platform=x64 /p:UseSharedCompilation=false /v:minimal /m:1 /nodeReuse:false
```

- [ ] **Step 7: Commit**

```powershell
git add Dev\Typedown.WinUI Tests\Typedown.ArchitectureTests
git commit -m "feat: initialize winui presentation localization"
```

## Task 6: Reconnect settings pages after ports and localization are stable

**Files:**

- Modify: `Dev\Typedown.WinUI\Typedown.WinUI.csproj`
- Modify: `Dev\Typedown.WinUI\Pages\SettingPages\*.xaml`
- Modify: `Dev\Typedown.WinUI\Pages\SettingPages\*.xaml.cs`
- Modify: `Dev\Typedown.WinUI\Controls\SettingControls\SettingItems\**`
- Test: architecture tests for active setting pages.

- [ ] **Step 1: Add tests forbidding stale namespace references**

Add source scan for active WinUI setting files:

```csharp
[TestMethod]
public void WinUI_ActiveSettingsPagesDoNotReferenceRemovedUiOrLegacyNamespaces()
{
    var roots = new[]
    {
        Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Pages", "SettingPages"),
        Path.Combine(RepoRoot, "Dev", "Typedown.WinUI", "Controls", "SettingControls")
    };

    foreach (var file in roots.SelectMany(root => Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
        .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)))
    {
        var source = File.ReadAllText(file);
        Assert.IsFalse(source.Contains("Typedown.Core.ViewModels", StringComparison.Ordinal), file);
        Assert.IsFalse(source.Contains("Typedown.Core.Legacy", StringComparison.Ordinal), file);
        Assert.IsFalse(source.Contains("Typedown.UI", StringComparison.Ordinal), file);
    }
}
```

- [ ] **Step 2: Restore one page at a time**

Restore in this order:

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

For each page:

1. Remove the specific `Page Remove` / `Compile Remove` entry from `Typedown.WinUI.csproj`.
2. Fix namespace references from old `Typedown.Core.ViewModels` to `Typedown.Presentation.ViewModels`.
3. Bind to `AppViewModel.SettingsViewModel`.
4. Build WinUI.
5. Commit the page.

- [ ] **Step 3: Validate each page**

Run after each page:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe' .\Dev\Typedown.WinUI\Typedown.WinUI.csproj /t:Build /p:Configuration=Debug_Local /p:Platform=x64 /p:UseSharedCompilation=false /v:minimal /m:1 /nodeReuse:false
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

Expected: WinUI build and architecture tests pass.

## Verification Bundle

Run this bundle after each committed task:

```powershell
dotnet build .\Dev\Typedown.Core\Typedown.Core.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
dotnet build .\Dev\Typedown.Presentation\Typedown.Presentation.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\arm64\MSBuild.exe' .\Dev\Typedown.WinUI\Typedown.WinUI.csproj /t:Build /p:Configuration=Debug_Local /p:Platform=x64 /p:UseSharedCompilation=false /v:minimal /m:1 /nodeReuse:false
```

Expected:

- Core build passes.
- Presentation build passes.
- Architecture tests pass.
- WinUI x64 Debug_Local build passes.

## Execution Order

1. Task 1 and Task 2 are mandatory first. They make the boundary visible and move port ownership.
2. Task 3 comes next. It prevents null/missing services when WinUI starts binding real Presentation ViewModels.
3. Task 4 should follow before menu and file workflows. It makes editor events and invokes use Presentation.
4. Task 5 can run after Task 3, but before restoring many XAML pages.
5. Task 6 is last and should be split page by page.

## Stop Conditions

Stop and re-plan if any of these happen:

- Moving a port forces `Typedown.Presentation` to reference `Microsoft.UI.Xaml`, `Windows.UI.Xaml`, or WebView2.
- A WinUI service implementation requires changing Core model semantics.
- A settings page restoration pulls `Typedown.UI`, `Typedown.Core.Legacy`, or legacy XamlUI back into active WinUI code.
- Editor bridge changes require modifying the React editor protocol.

In those cases, keep the failing test and write a smaller adapter or DTO boundary before continuing.
