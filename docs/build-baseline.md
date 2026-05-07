# Typedown build baseline

This document records the current governance baseline for the WinUI3 architecture worktree.

## Current Architecture Baseline

- `Dev\Typedown.Core\Typedown.Core.csproj` targets `net10.0` and has no project references.
- `Dev\Typedown.Presentation\Typedown.Presentation.csproj` targets `net10.0` and references only `Typedown.Core`.
- `Dev\Typedown.WinUI\Typedown.WinUI.csproj` targets `net10.0-windows10.0.26100.0`, references Core and Presentation, owns WinUI3 XAML/WebView2/platform adapters, and copies editor static files from `..\Typedown\Resources\Statics`.
- `Dev\Typedown\Typedown.csproj` is the legacy compatibility app. It targets `net10.0-windows10.0.26100.0` and references Core, Presentation, and the legacy XAML host.

## Owner Governance Baseline

- Editor bridge protocol owner: `docs\editor-bridge-protocol.md` owns the JSON wire shape; WinUI owns its shell-local `EditorHostContracts.cs` DTO/controller surface. Presentation can own command semantics, but it must not own WebView2 transport details.
- Editor static bundle owner: `Dev\Typedown.Editor` owns editor source and generated bundle content. `Dev\Typedown\Resources\Statics` remains the shared staging path consumed by WinUI as temporary migration debt until WinUI has its own bundle staging path.
- WinUI resource owner: `Dev\Typedown.WinUI\Resources\Strings` is the deliberate owner of `.resw` text resources. `Dev\Typedown` links those WinUI-owned strings as temporary migration debt while the legacy shell remains buildable.
- Packaged/MSIX support level: the WinUI project, manifest, launch-settings, install-script shape, and packaging output hook support the packaged path. Certificate material and full packaged validation remain machine-local/manual execution concerns.

## Validation Command

Run the architecture baseline with:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

This is the minimum check for docs/test boundary governance. Feature work that touches WinUI runtime behavior should also run the relevant WinUI build.

## WinUI Runtime Paths

Daily development path:

- `Typedown.WinUI`
- `Debug_Local|x64`
- Unpackaged
- MSIX signing disabled
- Windows App SDK deployment-manager auto initialization disabled

Packaged validation path:

- `Typedown.WinUI`
- `Debug|x64`
- MSIX package launch profile
- Package signing enabled
- Manifest publisher set to the Typedown WinUI development identity

The packaged path is supported by project, manifest, launch-settings, and install-script shape. The development certificate `.cer` and `.pfx` files are not currently checked in, so certificate material is machine-local/manual unless repository assets are restored.

## Boundary Risks

- Core and Presentation must stay free of WinUI, XAML, WebView2, package, and legacy host references.
- WinUI must stay independent of the legacy XAML host.
- The legacy app may continue to depend on the old host until the WinUI3 cutover is complete.
- ARM64 and packaged validation are later execution gates; Phase A only keeps the current guardrails accurate.

## Current Baseline Meaning

Passing architecture tests means the repository graph and documentation match the current architecture truth. It does not prove full runtime parity, ARM64 support, packaged deployment success, or certificate availability on a given machine.
