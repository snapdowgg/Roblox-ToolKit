# Crosshair Studio Pro (Roblox Toolkit)

```
  ____                                 _ _    _____ _                 
 / ___|___  _ __ ___  _ __   ___  _ __(_) |_ |___ /(_) ___ _ __  ___  
| |   / _ \| '_ ` _ \| '_ \ / _ \| '__| | __|  |_ \| |/ _ \ '_ \/ __| 
| |__| (_) | | | | | | |_) | (_) | |  | | |_  ___) | |  __/ | | \__ \ 
 \____\___/|_| |_| |_| .__/ \___/|_|  |_|\__||____/|_|\___|_| |_|___/ 
                     |_|                                                

  Roblox crosshair overlay + booster utility (GUI)
```

By: @snapdowgg

Small desktop app to create, preview and apply custom crosshair overlays on Windows while offering simple FPS/network tweak utilities.

---

## Overview

This repository contains `Crosshair Studio Pro` — a WinForms toolkit that helps you design crosshairs, preview them, save/load presets and optionally run simple system optimizations (FPS / network tweaks). It's a GUI application targeting .NET 10.

Use this tool only on systems you own or where you have explicit permission to run diagnostics/tweaks.

## Features

- Create and preview crosshair types (plus, cross, dot, circle, etc.)
- Live overlay that draws a crosshair on-screen
- Save / load presets (JSON `.csp` files) and import/export configurations
- Color palette + custom color picker
- Basic FPS and network helper options (process priority, DNS flush, TCP settings)
- Tray icon with minimize-to-tray support

## Build & Run

- Requirements: .NET 10 SDK
- Open solution in Visual Studio or run from command line:

```sh
dotnet build
dotnet run --project Roblox
```

After build the executable is in `bin/Debug/net10.0-windows` (or `Release`).

## Usage (GUI)

1. Launch the application (`Roblox.exe`).
2. Use the left navigation to switch between `Crosshair`, `Colors`, `Settings`, `Presets`, and utility panels.
3. In `Crosshair`:
   - Enable the overlay and choose a crosshair type and size.
   - Click `Apply Changes` to update the overlay.
4. In `Colors` you can pick a preset color or choose a custom color.
5. Save presets in `Presets` and load them later.

Notes:
- Presets are stored under `%APPDATA%/CrosshairStudio` as `.csp` JSON files.
- Some FPS/network operations require administrator rights (the app will not attempt to elevate automatically).

## Configuration / Files

- Preset format: JSON with fields `Type`, `Size`, `ColorArgb`.
- Exported configurations use the same JSON format.

## Troubleshooting

- If the app won't build, ensure no running instance is locking `Roblox.exe` (close app or stop debugging).
- On multi-monitor setups the overlay uses primary screen center by default.

## Disclaimer

This tool is provided for educational and personal use only. The author is not responsible for misuse, damage, or illegal activity resulting from running this software. Always obtain permission before modifying system settings or testing on computers you do not own.

---

If you want screenshots, badges, or a CONTRIBUTING section added, tell me what to include and I'll update the README.
