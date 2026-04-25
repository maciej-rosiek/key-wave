# Lenovo Ripple

Reactive keyboard lighting for Windows. When you press a key, the matching zone on your keyboard flashes a highlight color and fades back to a base color — like ripples following your typing. Built for the Lenovo LOQ 15IRX10 (24-zone HID LampArray), but works with any keyboard Windows recognizes under **Settings → Personalization → Dynamic Lighting**.

![icon](Assets/AppIcon.png)

## What it does

- Detects your LampArray keyboard automatically.
- Sets a base color across all zones.
- On each keypress, flashes that key's zone and fades back over ~200 ms.
- Multiple themes (Cyan on Navy, Amber, Lime, Red Alert, Magenta, Ice Blue, …).
- Lives in the system tray; close-to-tray; optional **Start with Windows**.
- Includes an on-screen 24-zone simulator so you can develop and tinker without a physical LampArray attached.

## Running

### Quick start (foreground only)

1. Grab the `LenovoRipple.exe` from [Releases](../../releases) (or `dotnet build && dotnet run`).
2. Launch it. The window shows the simulator grid and the discovered devices.
3. Right-click the tray icon to pick a theme, toggle autostart, or exit.

In this mode, the keyboard only reacts while the Lenovo Ripple window is in foreground. That's fine for trying it out — but if you want the lights to react while you're playing a game, see below.

### Background mode (works while a game is foreground)

The Windows LampArray API is gated: an unpackaged exe can only drive the keyboard when *its own* window is in front. To control lighting from the background you need to install the app as a packaged Windows app (MSIX) with the `com.microsoft.windows.lighting` AppExtension declared in its manifest.

#### One-liner install (easiest)

In an elevated-or-normal PowerShell:

```powershell
irm https://raw.githubusercontent.com/maciej-rosiek/lenovo-ripple/main/package/install.ps1 | iex
```

This downloads the source to `%LOCALAPPDATA%\LenovoRipple\source`, publishes a Release build, and registers the package. Requires the .NET 10 SDK.

#### Or run the scripts manually

The repo ships two in `package/`:

- `dev-register.ps1` — publishes the app and registers the manifest in place. Fast for development; no signing needed.
- `build-msix.ps1` — produces a real signed `.msix` you can install on another machine. Uses the Windows SDK (MakeAppx, SignTool) and a self-signed cert.

#### After registering or installing

1. Open **Settings → Personalization → Dynamic Lighting**.
2. Under **Controlled by**, pick **Lenovo Ripple** (this dropdown only shows apps that declared the lighting AppExtension).
3. Launch the app from the Start menu. Lights now react regardless of which window is focused.

To uninstall:

```powershell
Get-AppxPackage LenovoRipple | Remove-AppxPackage
```

## Themes

The tray menu has a **Theme** submenu listing the presets. Selecting one re-applies the base color and uses the new flash color on subsequent keypresses. You can add or tweak presets in [`ColorTheme.cs`](ColorTheme.cs).

## Building from source

Requires the .NET 10 SDK. From the repo root:

```bash
dotnet build
dotnet run
```

The simulator panel in the window mirrors what would be sent to a real LampArray, so you can iterate on themes and timings even without the hardware attached.

To regenerate the icon at all the sizes the manifest expects:

```powershell
powershell -ExecutionPolicy Bypass -File package\generate-icons.ps1
```

## Project layout

| Path | What's there |
| --- | --- |
| `App.xaml(.cs)`, `MainWindow.xaml(.cs)` | WPF shell, tray icon, theme picker. |
| `LampArrayController.cs` | Device discovery + flash/fade with per-zone fade cancellation. |
| `ILampSurface.cs`, `RealLampSurface.cs` | Abstraction over real and simulated keyboards. |
| `SimulatorPanel.xaml(.cs)`, `KeyZoneMap.cs` | The on-screen 24-zone simulator. |
| `GlobalKeyboardHook.cs` | Win32 `WH_KEYBOARD_LL` hook so keypresses are seen even when the app isn't focused. |
| `AutoStart.cs` | HKCU\…\Run toggle (writes `"<exe>" --hidden`). |
| `package/` | MSIX manifest, asset PNGs, dev-register and build scripts. |

## Notes

- The simulator's key→zone map is a sensible 24-zone fallback. On real hardware, `LampArray.GetIndicesForKey()` from Windows is authoritative — the simulator is only there for development.
- `LampArray.SetColor()` silently no-ops in unpackaged background processes. That's a Windows policy, not a bug in this app — it's exactly why MSIX packaging is the path forward for true ambient mode.
