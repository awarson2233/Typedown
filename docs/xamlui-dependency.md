# Typedown.XamlUI legacy dependency boundary

This document records the current repository truth for the legacy XAML host. It is not a migration plan and should not be read as permission to add new dependencies on the legacy host.

## Current Fact

- `Dev\Typedown.XamlUI` remains in the repository as the legacy XAML host for the old `Dev\Typedown` application path.
- `Dev\Typedown\Typedown.csproj` references `$(TypedownXamlUIProject)`, imports the host `buildTransitive` props/targets, and copies the legacy `Microsoft.UI.Xaml` runtime assets from `$(TypedownXamlUIProjectDir)`.
- `Dev\Typedown.Core\Typedown.Core.csproj` targets `net10.0` and has no project references.
- `Dev\Typedown.Presentation\Typedown.Presentation.csproj` targets `net10.0` and references only `Typedown.Core`.
- `Dev\Typedown.WinUI\Typedown.WinUI.csproj` targets `net10.0-windows10.0.26100.0`, references `Typedown.Core` and `Typedown.Presentation`, and does not reference the legacy XAML host.

## Boundary Rules

- `Typedown.Core` must stay platform-neutral: no WinUI, XAML, WebView2, WinRT UI, or legacy host references.
- `Typedown.Presentation` owns shell-agnostic MVVM, application orchestration, localization abstraction, and platform ports. It must not reference WinUI or the legacy XAML host.
- `Typedown.WinUI` owns the WinUI3 shell, XAML pages/controls, WebView2 host, platform service implementations, package assets, and activation path.
- The legacy `Dev\Typedown` app may continue to reference Core, Presentation, and the legacy XAML host until the WinUI3 cutover is complete.

## Repository Checks

The old app validates the host path through `$(TypedownXamlUIProject)` before build. If the legacy host is absent, the build fails with an explicit error that points back to this document.

The WinUI3 path should be validated by checking project references rather than by checking file presence alone:

```powershell
dotnet test .\Tests\Typedown.ArchitectureTests\Typedown.ArchitectureTests.csproj -c Debug -p:UseSharedCompilation=false /nodeReuse:false /v:minimal --no-restore
```

## Retirement Direction

The direction is current Core -> Presentation -> WinUI. The legacy app remains a compatibility path and flows to Core, Presentation, and the legacy XAML host. New WinUI3 work should move toward Presentation ports and WinUI adapters, not toward additional legacy host coupling.
