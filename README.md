# HotkeyScroll

A lightweight Windows utility that remaps mouse wheel scrolling directly to the keyboard number row (`1`–`0`).

In Roblox and similar games, the inventory hotbar is controlled with the number row rather than the mouse wheel, which zooms the camera instead. HotkeyScroll intercepts wheel scroll events, tracks your active slot, and simulates number row presses so you can cycle items without triggering camera zoom.

Developed by **CodeMilo**.

---

## Features

- **Mouse Wheel to Hotbar Keys**: Maps scroll up and down directly to number keys `1` through `0`.
- **Camera Zoom Interception**: Prevents accidental camera zooming while cycling slots.
- **Physical Key Sync**: Pressing physical number keys `1`–`0` on your keyboard immediately synchronizes the internal slot tracker.
- **Roblox-Only Mode**: Automatically activates only when Roblox is focused, leaving regular scrolling intact in other applications.
- **Configurable Toggle Hotkey**: Default `F8` to pause or resume remapping instantly.
- **Slot Wrap-Around**: Seamlessly wrap between slot 10 and slot 1, or stop at edges.
- **Reverse Scroll**: Invert scroll direction to match your preference.
- **System Tray & Startup**: Minimizes to the tray and can launch with Windows.
- **Lightweight Update Checker**: Checks GitHub Releases in the background and notifies you with a direct link when a new release is available.

---

## Requirements

- **OS**: Windows 10 or Windows 11 (64-bit)
- **Runtime**: Self-contained releases include all dependencies (no .NET installation needed).
  - If building from source, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) is required.

---

## Installation

1. Go to the [Releases](https://github.com/codemilo-ui/HotkeyScroll/releases) page.
2. Download `HotkeyScroll-vX.X.X-win-x64.zip`.
3. Extract the ZIP and run `HotkeyScroll.exe`.

---

## Configuration

Settings can be adjusted directly from the interface:

- **Toggle Hotkey**: Global shortcut to pause/resume remapping (default: `F8`).
- **Roblox Only**: Restrict remapping to the active Roblox window.
- **Wrap Around**: Choose whether scrolling past slot 10 loops back to 1.
- **Reverse Scroll**: Invert scroll wheel direction.
- **Scroll Sensitivity**: Adjust how many scroll notches advance a slot.
- **Run on Startup**: Launch minimized to tray on Windows login.

---

## Building from Source

```bash
# Restore dependencies for win-x64
dotnet restore RobloxHotbarScroll.sln -r win-x64

# Build Release
dotnet build -c Release

# Run tests
dotnet test RobloxHotbarScroll.sln -c Release --no-restore
```

### Standalone Production Publish

To create the self-contained, single-file Windows x64 executable:

```bash
dotnet publish RobloxHotbarScroll/RobloxHotbarScroll.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/x64
```

The output executable will be placed in `publish/x64/HotkeyScroll.exe`.

---

## Releases & Updates

Releases are published manually by CodeMilo on GitHub Releases.

HotkeyScroll includes a lightweight version checker that queries the GitHub Releases API. When a newer release is published, it alerts you and provides a link to view and download it directly from GitHub. The app does not download files in the background, run updater services, or replace its own files.

---

## License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
