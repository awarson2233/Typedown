# Build matrix and platform risk

This document records current build entry points and platform risks for the active WinUI3 architecture.

## Current Project Matrix

| Project | Target framework | Current role | Project references |
| --- | --- | --- | --- |
| `Dev\Typedown.Core` | `net10.0` | Platform-neutral core | none |
| `Dev\Typedown.Presentation` | `net10.0` | Shell-agnostic MVVM and platform ports | `Typedown.Core` |
| `Dev\Typedown.WinUI` | `net10.0-windows10.0.26100.0` | WinUI3 shell, XAML, WebView2 host, platform adapters, package assets | `Typedown.Core`, `Typedown.Presentation` |
| `Dev\Typedown` | `net10.0-windows10.0.26100.0` | Legacy compatibility app | `Typedown.Core`, `Typedown.Presentation`, legacy XAML host |

## Stable Verification Entry

Architecture governance should be validated with:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

For WinUI development, the daily runtime path is `Debug_Local|x64` on `Typedown.WinUI`. This path is unpackaged and disables MSIX signing.

## WinUI Package Path

`Debug|x64` on `Typedown.WinUI` remains the packaged/MSIX verification path:

- `WindowsPackageType` is `MSIX`.
- Package signing is enabled.
- The project stores the expected certificate thumbprint.
- `Package.appxmanifest` and `launchSettings.json` define the package identity and package launch profile.
- `scripts\install-winui-dev-certificate.ps1` still installs a local development certificate when the local `.cer` and `.pfx` files exist.

The development certificate files are not currently in the repository. This means the package path is supported by project/script shape, but certificate material is machine-local/manual unless repository assets are restored.

## ARM64 Risk

ARM64 remains a later validation target, not a Phase A fix:

- Core and Presentation expose `x64;ARM64` platforms and are platform-neutral by design.
- WinUI exposes `x64;ARM64` and `win-x64;win-arm64`, but full runtime and MSIX validation still need separate execution.
- The legacy app still carries old host and native dependency risks.
- Do not infer true ARM64 support from solution configuration names alone.

## Current No-Go Items

- Do not add WinUI/XAML/WebView2 references to Core or Presentation.
- Do not add a legacy XAML host reference to WinUI.
- Do not treat missing development certificate files as a reason to weaken unrelated MSIX project, manifest, launch-profile, or signing guardrails.
- Do not start ARM64 remediation inside Phase A governance cleanup.
