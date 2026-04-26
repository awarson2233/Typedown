# Phase 10a Core Contracts

Phase 10a adds a new platform-neutral `Dev\Typedown.Core.Contracts` project so the WinUI3 shell can reference a safe subset of the current shell contracts without importing `Dev\Typedown.Core` or `Dev\Typedown.XamlUI`.

## Scope

- Added `Typedown.Core.Contracts` targeting `net9.0` with nullable enabled and no Windows-specific TFM, `UseUwp`, CsWinRT, or XamlUI dependency.
- Moved these neutral interfaces and DTOs into the contracts project while preserving the `Typedown.Core.Interfaces` namespace:
  - `IAppDataPathProvider`
  - `IDialogService`
  - `IFilePickerService`
  - `IUiDispatcher`
  - `IWindowContext`
  - `IAppActivationService`
- Updated `Typedown.Core` and the legacy `Typedown` app to reference the contracts project.
- Updated `Typedown.WinUI` to reference contracts directly and added a minimal compile-time probe that touches `IAppDataPathProvider` and `IDialogService`.
- Moved the Phase 10 architecture checks into `Tests\Typedown.ArchitectureTests`, a lightweight `net9.0` MSTest project that scans source files directly and does not `ProjectReference` the legacy app or `Typedown.Core`.

## Non-Goals

- No page, control, or resource migration.
- No `Typedown.XamlUI` host changes.
- No `Typedown.WinUI -> Typedown.Core` or `Typedown.WinUI -> Typedown.XamlUI` reference.
- No ARM64 work.
- No React, CRA, or TypeScript upgrade.

## Follow-Up

- Platform service implementations still live in later phases.
- WinUI shell DI, service wiring, and real app activation/file picker/dialog implementations remain future work after the contracts boundary is stable.
- Keep future architecture-only checks in the lightweight test project unless they truly need compiled legacy app artifacts; this avoids reintroducing the legacy XAML build chain into `dotnet test`.
