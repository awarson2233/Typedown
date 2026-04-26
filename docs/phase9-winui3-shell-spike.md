# Phase 9 WinUI3 Shell Spike

Phase 9 creates a minimal standalone WinUI3 shell under `Dev\Typedown.WinUI`. This is a feasibility spike only. It does not migrate the existing XAML controls, does not create `Typedown.UI`, does not implement the WebView2 editor host, does not connect `Typedown.Core`, and does not start ARM64 adaptation.

## Implemented

- Added `Dev\Typedown.WinUI\Typedown.WinUI.csproj` as an unpackaged x64 WinUI3 app targeting `net9.0-windows10.0.26100.0`.
- Added the project to `Typedown.sln`.
- Added a minimal `App` and `MainPage` that create and activate a WinUI3 window with an explicit Phase 9 spike status page.
- Pinned Windows App SDK and Windows SDK BuildTools package versions for repeatable restore.
- Kept the project independent from `Dev\Typedown.XamlUI` props, targets, PRI, and runtime copy logic.

## Deliberate Limitations

- `Typedown.WinUI` does not reference `Typedown.Core` yet, even though the post-Phase 9 roadmap wants the shell to consume Core contracts.
- The reason is structural: current `Dev\Typedown.Core\Typedown.Core.csproj` still has a `ProjectReference` to `Dev\Typedown.XamlUI`, so directly referencing Core would pull the legacy host build chain into the WinUI3 spike and violate the Phase 9 isolation goal.
- Existing application pages, controls, resources, ViewModels, and editor host remain in their legacy locations.
- `Debug_Local` semantics are not cut over to WinUI3 in this phase.
- WebView2/editor host parity is not attempted in this phase.
- ARM64 is not attempted in this phase; the spike project is intentionally x64-only.
- Publish profiles are not part of Phase 9; the WinUI3 spike should build directly without committing `.pubxml` files.
- `Typedown.sln` only validates the WinUI3 spike project for Debug, Debug_Local, and Release x64 configurations.

## Phase 10 Gap List

- Split Core contracts/service registration so `Typedown.WinUI` can consume platform-neutral contracts without importing legacy XamlUI.
- Add WinUI3 implementations for app data paths, dialog, file picker, dispatcher, window context, and app activation.
- Decide whether to introduce a small `Typedown.Core.Contracts` project or first remove the remaining legacy host reference from `Typedown.Core`.
- Add smoke checks for picker/dialog/window activation once the service boundary is connected.
- Re-introduce WebView2 only in Phase 11 when validating editor host parity against the frozen existing React bundle.

## Verification

Use x64 Debug for this spike:

```powershell
dotnet build .\Dev\Typedown.WinUI\Typedown.WinUI.csproj -c Debug -p:Platform=x64 /nologo /v:minimal /m:1 /nodeReuse:false
```
