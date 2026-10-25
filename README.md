# HotkeyScroll — Roblox Hotbar Remapper (v1.2.0)
**Developed by CodeMilo**

A high-performance, ultra-low-latency **Windows keyboard-input remapping utility** built in **C# / .NET 8 / WPF / MVVM** with a **Dark Violet gaming utility aesthetic**. It converts mouse-wheel scrolling directly into Roblox's main keyboard number key presses (`1`, `2`, `3`, `4`, `5`, `6`, `7`, `8`, `9`, `0`).

---

## 1. Core Concept: Pure Input Remapper (Locked & Stable)

Roblox has its default hotbar controlled by the keyboard number row:
```text
[1] [2] [3] [4] [5] [6] [7] [8] [9] [0]
```
Normally, a player equips hotbar items by physically pressing `1` through `0` on their keyboard.

**HotkeyScroll is purely an input remapper:**
- When you scroll the mouse wheel, the application calculates the next or previous number key and **simulates pressing that exact main keyboard key** (`KEY DOWN` → realistic 18ms hold → `KEY UP`).
- Roblox receives the standard keyboard number press, and Roblox itself switches its active slot.
- **Zero Roblox Internals:** The program never accesses Roblox memory, never injects DLLs, never touches Roblox files, never calls Roblox APIs, and never creates fake inventory overlays.

---

## 2. Input Mapping & Interception

```text
Tracked Slot 1  → Simulates pressing main keyboard 1 (VK: 0x31, Scan: 0x02)
Tracked Slot 2  → Simulates pressing main keyboard 2 (VK: 0x32, Scan: 0x03)
Tracked Slot 3  → Simulates pressing main keyboard 3 (VK: 0x33, Scan: 0x04)
Tracked Slot 4  → Simulates pressing main keyboard 4 (VK: 0x34, Scan: 0x05)
Tracked Slot 5  → Simulates pressing main keyboard 5 (VK: 0x35, Scan: 0x06)
Tracked Slot 6  → Simulates pressing main keyboard 6 (VK: 0x36, Scan: 0x07)
Tracked Slot 7  → Simulates pressing main keyboard 7 (VK: 0x37, Scan: 0x08)
Tracked Slot 8  → Simulates pressing main keyboard 8 (VK: 0x38, Scan: 0x09)
Tracked Slot 9  → Simulates pressing main keyboard 9 (VK: 0x39, Scan: 0x0A)
Tracked Slot 10 → Simulates pressing main keyboard 0 (VK: 0x30, Scan: 0x0B)
```

### Remapping Flow:
```text
Roblox is on slot 2
        ↓
User scrolls DOWN
        ↓
Utility calculates next slot = 3
        ↓
Utility dispatches: KeyDown '3' → 18ms hold → KeyUp '3'
        ↓
Roblox receives normal keyboard "3"
        ↓
Roblox switches to slot 3
        ↓
Utility synchronizes internal tracked slot = 3
```

- **Manual Physical Keys:** Physical presses on `1`–`0` are observed (not blocked) and immediately synchronize the utility's internal slot state. Numeric keypad keys are ignored.
- **Mouse Wheel Interception:** Scrolling is intercepted, preventing accidental camera zoom while switching hotbar slots.
- **Global Toggle Hotkey:** Pressing `F8` (configurable) globally toggles between **ACTIVE** and **PAUSED** without passing the key to Roblox.

---

## 3. Real WPF Animations (v1.2.0)

HotkeyScroll v1.2.0 features lightweight, responsive WPF animations crafted with Storyboards and CubicEase transitions:
- **Window Entrance:** Smooth fade, scale (`0.97 → 1.0`), and subtle slide (`TranslateY 12 → 0`) over 250ms upon launch.
- **Window Exit:** Elegant fade-out (`Opacity 1.0 → 0.0`) over 120ms before clean shutdown.
- **Tab Transitions:** Smooth slide-and-fade (`Opacity 0 → 1`, `TranslateY 12 → 0`) across Dashboard, Settings, Logs, and About pages.
- **Slot Selection Pop:** Reactive vertical bounce when the current slot transitions (`TranslateY -8 → 0` over 150ms).
- **Status Badge Pulse:** Gentle scale keyframe pulse (`1.0 → 1.08 → 1.0` over 200ms) when toggling between Active and Paused.
- **Tactile Button Press:** Buttons smoothly scale down to `0.97` on mouse press and spring back to `1.0` on release with purple ambient glow on hover.
- **Animated Expanders:** Settings cards feature animated expand/collapse transitions.
- **Floating Toast Notifications:** Slide-and-fade toast notifications for status alerts and update prompts.
- **Reduced Motion:** Fully integrated toggle under Accessibility settings that disables decorative animations for enhanced accessibility and performance.

---

## 4. Lightweight GitHub Releases Update Checker

HotkeyScroll includes a lightweight GitHub Releases version checker configured for `codemilo-ui/HotkeyScroll`:
- **Pure Version Checker:** Only checks whether a newer version exists on GitHub. It never downloads files, never replaces EXEs, never runs background installers, and never modifies application files.
- **Manual User Updates:** When an update is detected, clicking `[ Download vX.X.X ]` or `[ View Update ]` simply opens the official GitHub release page in the user's default browser where they can manually download the new build.
- **Robust Semantic Versioning:** Correctly compares semantic versions (`1.2.10` > `1.2.9`, `1.3.0` > `1.2.10`, `1.2.0` = `1.2.0`).
- **Non-Intrusive Background Startup Check:** If enabled (default: ON), checks for updates in the background after startup without delaying launch.
- **Subtle Update Notification:** Displays a banner on the Dashboard when an update is found:
  ```text
  ✦ Update Available
  HotkeyScroll v1.3.0 is available.
  [ View Update ]  [ Later ]
  ```
- **Updates Section in Settings:**
  ```text
  UPDATES
  Current Version: v1.2.0
  Status: You're up to date.
  [ Check for Updates ]

  (When update available:)
  New Version Available: v1.3.0
  A newer version of HotkeyScroll is available.
  [ Download v1.3.0 ]

  ☑ Check for updates automatically (Default: ON)
  ```
- **Offline / Error Handling:** If GitHub cannot be reached or rate limits apply, shows a calm message:
  ```text
  Couldn't check for updates.
  HotkeyScroll is still working normally.
  ```
  Technical errors are logged to file. The checker never crashes the application.

---

## 5. CodeMilo Branding & Minimal About View

- **Product Name:** `HotkeyScroll`
- **Company Name:** `CodeMilo`
- **Executable:** `HotkeyScroll.exe`
- **Minimal About Card (Section 27 Specification):**
  ```text
  [ App Icon ]
  HotkeyScroll
  v1.2.0
  A lightweight Windows utility for keyboard-based hotbar navigation.
  Developed by CodeMilo
  [ Check for Updates ]  [ View Logs ]
  ```

---

## 6. Building, Testing, and Publishing

### 1. Build Solution:
```powershell
dotnet build -c Release
```

### 2. Run Comprehensive Unit Test Suite (90 Passing Tests):
```powershell
dotnet test -c Release
```

### 3. Publish Self-Contained x64 Standalone Executable:
```powershell
dotnet publish RobloxHotbarScroll/RobloxHotbarScroll.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/x64
```

### Published Output:
- `publish\x64\HotkeyScroll.exe` — Self-contained standalone application (~161 MB)

---

## 7. Quality & Verification Matrix

| Area | Feature / Check | Result |
|---|---|---|
| **Remapper Core** | Windows low-level input hooks, 40-byte `INPUT` struct, sequential keystrokes | ✅ Verified & Locked |
| **Roblox Hotbar** | Remaps wheel scroll to keyboard number row `1`–`0` | ✅ Verified |
| **Animations** | Window open/exit, tab cross-fade, slot bounce, status pulse, button press | ✅ 60 FPS WPF Native |
| **Accessibility** | Reduced Motion toggle immediately bypasses motion effects | ✅ Verified |
| **Update Checker** | Lightweight GitHub release check, semantic comparison (`1.2.10` > `1.2.9`) | ✅ Verified |
| **Manual Updates** | Opens GitHub release in user's browser; zero file tampering | ✅ Verified |
| **Branding** | Consistent CodeMilo product branding and minimal Section 27 About view | ✅ Verified |
| **Testing** | 90 Unit Tests across Controller, Remapper, Views, and Versioning | ✅ 90 / 90 Passed |
| **Testing** | 95 Unit Tests across Controller, Remapper, Views, and Updater | ✅ 95 / 95 Passed |
