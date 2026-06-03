# KeypadCompanion

A Windows desktop companion app for the ESP8266 macropad. Built with C#, .NET 10, and Avalonia.

## Requirements

- Windows 10 or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (to build or run from source)

## Run from source

```powershell
dotnet run --project .\src\KeypadCompanion\KeypadCompanion.csproj
```

## Publish a self-contained executable

```powershell
.\scripts\publish-companion.ps1
```

Output: `artifacts/KeypadCompanion/win-x64/KeypadCompanion.exe`

Additional options:

```powershell
# ARM64 build
.\scripts\publish-companion.ps1 -Runtime win-arm64

# Framework-dependent (smaller, requires .NET 10 runtime on target machine)
.\scripts\publish-companion.ps1 -FrameworkDependent
```

## Run tests

```powershell
dotnet test .\tests\KeypadCompanion.Tests\KeypadCompanion.Tests.csproj
```

## Connection

The app connects to the macropad over USB serial or TCP (Wi-Fi).

- **Serial**: auto-connects to the last successful COM port on startup. Use the connection bar to pick a different port.
- **Wi-Fi**: configure the device IP via the Wi-Fi settings dialog.

## Inputs and triggers

The macropad exposes 11 inputs:

| Input | ID |
|---|---|
| Keys 1–8 | `Key1` – `Key8` |
| Encoder button | `EncoderButton` |
| Encoder clockwise | `EncoderClockwise` |
| Encoder counter-clockwise | `EncoderCounterClockwise` |

Each key supports three trigger types; the encoder supports two:

| Trigger | Applies to |
|---|---|
| Press | Keys, encoder button |
| Long press | Keys, encoder button |
| Double click | Keys, encoder button |
| Rotate CW | Encoder |
| Rotate CCW | Encoder |

## Action types

| Action | What it does |
|---|---|
| Hotkey | Sends a key chord (e.g. `Ctrl+C`) |
| Type text | Types a text snippet via the clipboard |
| Launch app | Launches an executable with optional arguments |
| Open URL | Opens a URL in the default browser |
| Shell command | Runs a PowerShell or other shell command |
| Volume delta | Raises or lowers system volume by a percentage |
| Toggle mute | Mutes or unmutes system audio |

## Assigning actions

1. Create or edit an action in the action list on the left.
2. Save the action to the preset list.
3. Right-click a key or encoder control on the visualizer.
4. Choose the trigger type, then choose the saved action to assign.

Default encoder bindings (applied automatically on first run):

- Rotate CW → Volume +2%
- Rotate CCW → Volume −2%
- Button press → Toggle mute

## Configuration

Settings are stored as JSON under `%LOCALAPPDATA%\KeypadCompanion`. The app saves the last connected port, gesture timings (long-press and double-click windows), window preferences, Wi-Fi settings, and all action presets and bindings.

## Project layout

```
src/
  KeypadCompanion/        # App source
    Domain/               # Core types (actions, bindings, config)
    Services/             # Serial/TCP device, macro executor, config store
    ViewModels/           # MVVM view models
    Views/                # Avalonia XAML views
tests/
  KeypadCompanion.Tests/  # xUnit tests
scripts/
  publish-companion.ps1   # Publish script
```

## Tech stack

| | |
|---|---|
| UI framework | [Avalonia](https://avaloniaui.net/) 12 |
| MVVM | CommunityToolkit.Mvvm |
| Audio | NAudio |
| Target framework | .NET 10 |
