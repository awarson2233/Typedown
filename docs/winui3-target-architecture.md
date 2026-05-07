# WinUI3 target architecture

This document records the current target architecture for the WinUI3 migration. Historical phase names are intentionally omitted unless they describe still-active boundaries.

## Current Modules

```text
Typedown.Core
  net10.0 platform-neutral core.
  Owns pure models, persistence, document/editor protocol services,
  non-UI utilities, and core service contracts.
  Current project references: none.

Typedown.Presentation
  net10.0 shell-agnostic presentation layer.
  Owns MVVM, application orchestration, localization abstraction,
  and platform-neutral ports consumed by UI shells.
  Current project references: Typedown.Core only.

Typedown.WinUI
  net10.0-windows10.0.26100.0 WinUI3 shell.
  Owns App/Window startup, XAML pages and controls, WebView2 host,
  platform service adapters, package/manifest assets, and activation wiring.
  Current project references: Typedown.Core and Typedown.Presentation.
```

## Dependency Direction

```text
Typedown.Core -> no project references
Typedown.Presentation -> Typedown.Core
Typedown.WinUI -> Typedown.Presentation + Typedown.Core
```

Forbidden directions:

- Core must not reference Presentation, WinUI, XAML, WinRT UI types, WebView2, or the legacy host.
- Presentation must not reference WinUI, XAML, WebView2, package assets, activation infrastructure, or the legacy host.
- WinUI must not reference the legacy XAML host.
- New runtime behavior should flow through Presentation/Core ports and WinUI adapters, not through legacy host APIs.

## Ownership

Core ownership:

- Persistent models, database context, access history, backup services, transport, remote invoke, editor bridge, and pure utility logic.
- Core contracts that do not expose shell types.

Presentation ownership:

- `AppViewModel`, editor/file/format/paragraph/settings/float view models, localization facade, and shell-agnostic service ports.
- Application command orchestration that can be tested without WinUI.

WinUI ownership:

- XAML pages, controls, styles, converters, image assets, menu/flyout controls, WebView2 host, package manifest, launch settings, window/dialog/file-picker adapters, dispatcher/window context, activation service, and WinUI-specific localization loading.

## Packaging And Certificate Boundary

`Debug_Local` is the daily unpackaged WinUI3 path and disables MSIX signing. The packaged path remains supported by the WinUI project shape, manifest, launch profile, and certificate install script. The development certificate assets are not currently checked in; they are machine-local/manual unless repository certificate files are restored.

## Validation

Architecture governance is enforced by `Tests\Typedown.ArchitectureTests`. The key guardrails are project-reference direction, platform-neutral source checks, WinUI independence from the legacy host, and documentation alignment.
