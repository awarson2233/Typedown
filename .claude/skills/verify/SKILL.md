---
name: verify
description: Launch and drive the ARM64 Debug_Local Typedown WinUI app for GUI verification.
---

# Typedown GUI verification

1. Build `Dev/Typedown.Editor` with `yarn --cwd "Dev/Typedown.Editor" build`.
2. Build `Typedown.sln` with ARM64 MSBuild using `Configuration=Debug_Local` and `Platform=ARM64`.
3. Launch `Dev/Typedown.WinUI/bin/ARM64/Debug_Local/net10.0-windows10.0.26100.0/win-arm64/Typedown.WinUI.exe --typedown-new-window` so an existing user window is not reused.
4. Wait for a responsive window titled `Typedown`; capture it with `UIAutomationClient` bounding bounds and `System.Drawing.Graphics.CopyFromScreen`.
5. Click the WebView editor and use Win32 `SendInput` for keyboard chords. `WScript.SendKeys` is not reliable for WebView2 accelerator verification.
6. Open menu items and Settings through UI Automation IDs such as `EditMenuItem`, `UndoItem`, `RedoItem`, `SettingsButton`, and `BackButton`.
7. Close test windows with `CloseMainWindow`; if the save dialog appears, invoke `SecondaryButton` (`不保存`) only for verification content.

Do not stop pre-existing Typedown processes. Do not move or delete `%LOCALAPPDATA%\Typedown\WinUI\settings.json`; the unpackaged app has no isolated app-data override. Do not kill shared WebView2 child processes when another user window is running.
