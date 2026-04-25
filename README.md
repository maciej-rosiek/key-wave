# KeyWave

Reactive keyboard lighting for Windows. When you press a key, the matching zone on your keyboard flashes a highlight color and fades back to a base color — like ripples following your typing. Works with any keyboard Windows recognizes under **Settings → Personalization → Dynamic Lighting** (HID LampArray devices).

![icon](Assets/AppIcon.png)

## What it does

- Detects your LampArray keyboard automatically.
- Sets a base color across all zones.
- Reacts to each keypress with one of two effects:
  - **Flash & Fade** — flash just the pressed key's zone, fade back over ~200 ms.
  - **Ripple** — propagate a wave outward through neighboring zones with tunable distance, width, speed, and optional wall-bounce.
- Multiple color themes (Cyan on Navy, Amber, Lime, Red Alert, Magenta, Ice Blue, Rainbow, …).
- Lives in the system tray; close-to-tray; optional **Start with Windows**.
- Includes an on-screen 24-zone simulator so you can develop and tinker without a physical LampArray attached.

## Running

### Quick start (foreground only)

1. Grab the `KeyWave.exe` from [Releases](../../releases) (or `dotnet build && dotnet run`).
2. Launch it. The window shows the simulator grid and the discovered devices.
3. Right-click the tray icon to pick a theme, toggle autostart, or exit.

In this mode, the keyboard only reacts while the KeyWave window is in foreground. That's fine for trying it out — but if you want the lights to react while you're playing a game, see below.

### Background mode (works while a game is foreground)

The Windows LampArray API is gated: an unpackaged exe can only drive the keyboard when *its own* window is in front. To control lighting from the background you need to install the app as a packaged Windows app (MSIX) with the `com.microsoft.windows.lighting` AppExtension declared in its manifest.

#### One-liner install (recommended)

In PowerShell:

```powershell
irm https://raw.githubusercontent.com/maciej-rosiek/key-wave/main/package/install.ps1 | iex
```

The script self-elevates to Admin, downloads the latest signed `.msix` + cert from [Releases](../../releases), trusts the cert, and installs the package. Requires the .NET 10 Desktop Runtime ([download](https://dotnet.microsoft.com/download/dotnet/10.0)).

#### After install

1. Open **Settings → Personalization → Dynamic Lighting**.
2. Under **Controlled by**, pick **KeyWave** (this dropdown only shows apps that declared the lighting AppExtension).
3. Launch the app from the Start menu. Lights now react regardless of which window is focused.

To uninstall:

```powershell
Get-AppxPackage KeyWave | Remove-AppxPackage
```

#### Manual install paths

If you prefer not to use the one-liner, the repo's `package/` folder has:

- `dev-register.ps1` — publishes from source and registers the manifest in place. **Requires Developer Mode = On** (Settings → Privacy & security → For developers). No signing needed; useful for iterating on the code.
- `build-msix.ps1` — produces the signed `.msix` and cert that the one-liner downloads. Auto-fetches `Microsoft.Windows.SDK.BuildTools` NuGet for `MakeAppx.exe` / `SignTool.exe` if the Windows SDK isn't installed.

## Themes & Effects

The tray menu has separate **Theme** and **Effect** submenus.

- **Themes** are palettes (base + flash colors). Add or tweak in [`ColorTheme.cs`](ColorTheme.cs).
- **Effects** are behaviors (Flash & Fade vs Ripple). The main window has sliders for the Ripple parameters: Distance (how far the wave travels), Width (thickness of the ring), Speed (ms per step), plus a Wall-bounce checkbox so the wave returns inward.

Theme and Effect are independent — pick a Rainbow theme with the Ripple effect and you get a colored wave centered on whichever key you hit.

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
| `App.xaml(.cs)`, `MainWindow.xaml(.cs)` | WPF shell, tray icon, theme + effect pickers, ripple-parameter sliders. |
| `LampArrayController.cs` | Device discovery + per-zone fade cancellation; delegates keypress handling to the active `Effect`. |
| `Effect.cs`, `FlashAndFadeEffect.cs`, `RippleEffect.cs` | Effect strategy + the two implementations. |
| `ColorTheme.cs` | Solid + Rainbow theme implementations. |
| `ILampSurface.cs`, `RealLampSurface.cs` | Abstraction over real and simulated keyboards. |
| `SimulatorPanel.xaml(.cs)`, `KeyZoneMap.cs` | The on-screen 24-zone simulator. |
| `GlobalKeyboardHook.cs` | Win32 `WH_KEYBOARD_LL` hook so keypresses are seen even when the app isn't focused. |
| `AutoStart.cs` | HKCU\…\Run toggle (writes `"<exe>" --hidden`). |
| `package/` | MSIX manifest, asset PNGs, dev-register and build scripts. |

## Notes

- The simulator's key→zone map is a sensible 24-zone fallback. On real hardware, `LampArray.GetIndicesForKey()` from Windows is authoritative — the simulator is only there for development.
- `LampArray.SetColor()` silently no-ops in unpackaged background processes. That's a Windows policy, not a bug in this app — it's exactly why MSIX packaging is the path forward for true ambient mode.
