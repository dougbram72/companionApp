# ESP8266 Macropad

This repo now uses `arduino-cli` so you can build and flash the firmware from PowerShell without opening the Arduino IDE.

## One-time setup

Install the CLI if needed:

```powershell
scoop install arduino-cli
```

Install the ESP8266 board package:

```powershell
.\scripts\install-toolchain.ps1
```

## Build

```powershell
.\scripts\compile.ps1
```

## Upload

If only one USB serial board is connected, the script auto-detects the port:

```powershell
.\scripts\upload.ps1
```

If you want to choose the port explicitly:

```powershell
.\scripts\upload.ps1 -Port COM4
```

## Board target

The scripts default to:

```text
esp8266:esp8266:nodemcuv2
```

That is the usual `NodeMCU 1.0 (ESP-12E Module)` target.

## Pinout

Current wiring:

```text
R1 -> D5
R2 -> D0
C1 -> D1
C2 -> D2
C3 -> D7
C4 -> D6
Enc A -> D3
Enc B -> D4
Enc SW -> RX
Enc COM -> GND
```

## Test The Keypad

After uploading, open the serial monitor:

```powershell
.\scripts\monitor.ps1
```

Or choose the port explicitly:

```powershell
.\scripts\monitor.ps1 -Port COM4
```

The firmware prints events at `115200` baud:

```text
KEY_1_DOWN / KEY_1_UP ... KEY_8_DOWN / KEY_8_UP
ENC_CW / ENC_CCW
ENC_BTN_DOWN / ENC_BTN_UP
```

That lets you verify each key, the encoder rotation, and the encoder push button.

## Notes

- The current machine sees a USB serial device on `COM4`.
- If upload fails because the board is not entering bootloader mode automatically, hold `FLASH`, tap `RST`, then rerun the upload command.

## Windows Companion App

The repo now includes a Windows companion app built with `C#`, `.NET`, and `Avalonia`.

Project layout:

```text
src/KeypadCompanion
tests/KeypadCompanion.Tests
```

Run the app:

```powershell
dotnet run --project .\src\KeypadCompanion\KeypadCompanion.csproj
```

Publish a Windows executable:

```powershell
.\scripts\publish-companion.ps1
```

Default publish output:

```text
artifacts/KeypadCompanion/win-x64/KeypadCompanion.exe
```

Optional examples:

```powershell
.\scripts\publish-companion.ps1 -Runtime win-arm64
.\scripts\publish-companion.ps1 -FrameworkDependent
```

Run the tests:

```powershell
dotnet test .\tests\KeypadCompanion.Tests\KeypadCompanion.Tests.csproj
```

Current companion app features:

- Auto-connect to the last successful serial port
- Live keypad and encoder visualization
- Recent event log
- Saved action editor plus right-click assignment on keys and encoder controls
- Default encoder bindings for volume up, volume down, and mute
- JSON config stored under `%LOCALAPPDATA%\KeypadCompanion`

Current assignment flow:

1. Create or edit an action on the left side of the window.
2. Save the action into the saved-actions list.
3. Right-click a key or encoder control on the right side of the window.
4. Choose the trigger, then choose the saved action to assign.
