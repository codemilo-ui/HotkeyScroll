# HotkeyScroll Release Guide

This document outlines the workflow for developing changes and publishing official releases of **HotkeyScroll** via GitHub Actions.

---

## 1. Official Repository & Branding

- **Repository**: [https://github.com/codemilo-ui/HotkeyScroll](https://github.com/codemilo-ui/HotkeyScroll)
- **Developer**: CodeMilo
- **Official Product**: HotkeyScroll
- **Target Platform**: Windows x64 (WPF / .NET 8)

---

## 2. Standard Development Workflow (Pushing Source Code)

Normal development pushes **ONLY** push source code and run verification tests.
They **NEVER** create a release, tag, or upload binaries.

```bash
# 1. Check working tree status
git status

# 2. Stage changes
git add .

# 3. Commit with a clear, descriptive message
git commit -m "Describe changes"

# 4. Push source code to main branch
git push origin main
```

### What Happens on Push to `main`:
- GitHub Actions triggers the **CI** workflow (`.github/workflows/ci.yml`).
- It restores dependencies, builds the solution, and runs all unit tests.
- **No release is published** on regular commits to `main`.
- If a build or test fails, GitHub reports the failure immediately.

---

## 3. Manual Release Publishing Workflow

Releases are published **only when you manually trigger them** in GitHub Actions.
Nothing is automatically released without your explicit command.

### Step 1: Update the Version in Project Configuration
Open `RobloxHotbarScroll/RobloxHotbarScroll.csproj` and set the desired version:

```xml
<PropertyGroup>
  ...
  <!-- Versioning & Metadata -->
  <Version>1.2.0</Version>
  <AssemblyVersion>$(Version)</AssemblyVersion>
  <FileVersion>$(Version)</FileVersion>
  ...
</PropertyGroup>
```

> **Note**: Application version, assembly version, file version, and the internal update checker dynamically resolve this version from the compiled assembly.

### Step 2: Commit and Push the Version Update
```bash
git add RobloxHotbarScroll/RobloxHotbarScroll.csproj
git commit -m "Prepare release v1.2.0"
git push origin main
```

### Step 3: Trigger the Manual Release in GitHub Actions
1. Navigate to your repository: [https://github.com/codemilo-ui/HotkeyScroll](https://github.com/codemilo-ui/HotkeyScroll)
2. Click on the **Actions** tab.
3. In the left sidebar, select the **Release** workflow.
4. Click the **Run workflow** dropdown on the right.
5. Enter the desired version (for example: `1.2.0`).
6. Click the green **Run workflow** button.

---

## 4. What the Release Workflow Does Automatically

When you trigger "Run workflow":

1. **Verify**: Checks out the code and executes the full unit test suite in Release configuration.
2. **Build**: Builds and publishes the standalone, single-file Windows x64 executable (`HotkeyScroll.exe`) with native libraries included and debug symbols omitted.
3. **Package**: Compresses the standalone binary into `HotkeyScroll-v1.2.0-win-x64.zip`.
4. **Publish**: Creates the official GitHub Release associated with tag `v1.2.0`, attaches `HotkeyScroll-v1.2.0-win-x64.zip`, and automatically generates release notes.
5. **Zero Manual Uploads**: You never need to build or manually upload ZIP files to GitHub.

---

## 5. In-App Update Detection

1. HotkeyScroll runs a lightweight background check against `https://api.github.com/repos/codemilo-ui/HotkeyScroll/releases/latest`.
2. When users run HotkeyScroll, it detects new releases via semantic version comparison (`1.2.0 > 1.1.0`).
3. The app displays:
   ```text
   ✦ Update Available
   HotkeyScroll v1.2.0 is available.
   [ View Release ]
   ```
4. Clicking **[ View Release ]** opens the official GitHub Release page in the user's default browser where they can download the clean ZIP.
5. HotkeyScroll never runs background updater processes, installers, or self-replacement.

---

## 6. Local Build & Run Commands

You can build and run the application locally at any time. Local binaries are kept in your local directory and excluded from git commits via `.gitignore`:

### Debug Build
```bash
dotnet build -c Debug
```

### Release Build
```bash
dotnet build -c Release
```

### Restore Dependencies (win-x64)
```bash
dotnet restore RobloxHotbarScroll.sln -r win-x64
```

### Run All Unit Tests
```bash
dotnet test RobloxHotbarScroll.sln -c Release --no-restore --verbosity normal
```

### Publish Standalone Windows x64 Single-File Executable
```bash
dotnet publish RobloxHotbarScroll/RobloxHotbarScroll.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false -o ./publish/x64
```

### Run Executable Locally
```powershell
Start-Process "./publish/x64/HotkeyScroll.exe"
```
