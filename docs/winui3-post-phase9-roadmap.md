# WinUI3 roadmap after the editor-host baseline

This roadmap reflects the current repository shape. Older phase documents may mention retired intermediate module names; this document is the current source for the active direction.

## Current Baseline

- `Typedown.Core` targets `net10.0`, has no project references, and must remain platform-neutral.
- `Typedown.Presentation` targets `net10.0`, references only Core, and owns shell-agnostic MVVM plus platform ports.
- `Typedown.WinUI` targets `net10.0-windows10.0.26100.0`, references Core and Presentation, owns WinUI3 XAML/WebView2/platform adapters, and carries the editor static bundle in `Resources\Statics`.

## Roadmap

```text
Phase A  Architecture governance refresh
Phase B  WinUI3 parity completion
Phase C  Debug_Local hardening
Phase D  ARM64 and packaged validation
```

## Phase A: Architecture governance refresh

Goal: keep docs and architecture tests aligned with the current module graph instead of historical phase details.

Tasks:

- Assert Core and Presentation target `net10.0` and stay platform-neutral.
- Assert WinUI references Core and Presentation but not the legacy XAML host.
- Treat WinUI packaged signing as project/script-supported but certificate-asset-local unless repository assets are restored.
- Remove stale assertions that require exact historical XAML snippets when the durable behavior is now code-created or adapter-owned.

## Phase B: WinUI3 parity completion

Goal: close remaining user-path gaps in the WinUI3 shell without changing ownership boundaries.

Focus areas:

- File open/save/export/print behavior through Presentation ports and WinUI adapters.
- Editor bridge command/event parity through the WinUI WebView2 host.
- Settings pages, shortcut handling, context menus, image flows, side pane, and localized command surfaces.
- Startup performance without delaying visible shell regions.

## Phase C: Debug_Local hardening

Goal: keep the WinUI3 unpackaged path as the default daily startup path and remove fallback assumptions about legacy shells.

Acceptance:

- `Debug_Local|x64` starts WinUI3 without MSIX signing or deployment.
- The editor static bundle is available from local output.
- Repository scripts, docs, and tests no longer assume a parallel legacy startup path.

## Phase D: ARM64 and packaged validation

Goal: validate ARM64 and MSIX after WinUI3 cutover, not against the old host architecture.

Acceptance:

- x64 remains the stable baseline.
- ARM64 mappings are explicit and no longer silently fall back to x64.
- Packaged validation has clear certificate prerequisites and payload checks.
