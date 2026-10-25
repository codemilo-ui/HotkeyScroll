# HotkeyScroll Release Guide

This document outlines the workflow for developing changes and publishing official releases of **HotkeyScroll** via GitHub Actions.

---

## 1. Official Repository & Branding

- **Repository**: [https://github.com/codemilo-ui/HotkeyScroll](https://github.com/codemilo-ui/HotkeyScroll)
- **Developer**: CodeMilo
- **Official Product**: HotkeyScroll
- **Target Platform**: Windows x64 (WPF / .NET 8)

---

## 2. Standard Development Workflow

For normal bug fixes, performance improvements, or UI refinements:

```bash
# 1. Check current status
git status

# 2. Stage changes
git add .

# 3. Commit with a clear, descriptive message
git commit -m "Describe change"

# 4. Push to the main branch
git push origin main
```

### What Happens on Push to `main`:
- GitHub Actions triggers the **CI** workflow (`.github/workflows/ci.yml`).
- It restores dependencies, builds the solution, and runs all unit tests.
- **No release is published** on regular commits to `main`.
- If a build or test fails, GitHub reports the failure immediately.

---

## 3. Publishing an Official Release

HotkeyScroll uses **Semantic Versioning** (`MAJOR.MINOR.PATCH`):
- `1.0.0`
- `1.0.1`
- `1.1.0`
- `2.0.0`

When you are ready to publish a new public release:

### Step 1: Update the Version in Project Configuration
Open `RobloxHotbarScroll/RobloxHotbarScroll.csproj` and update `<Version>`:

```xml
<PropertyGroup>
  ...
  <!-- Versioning & Metadata -->
  <Version>1.2.1</Version>
  <AssemblyVersion>$(Version)</AssemblyVersion>
  <FileVersion>$(Version)</FileVersion>
  ...
</PropertyGroup>
```

> **Note**: Application version, assembly version, file version, and the internal update checker dynamically resolve this version from the compiled assembly. You do not need to edit version strings across multiple files.

### Step 2: Commit the Version Bump
```bash
git add RobloxHotbarScroll/RobloxHotbarScroll.csproj
git commit -m "Bump version to v1.2.1"
git push origin main
```

### Step 3: Create and Push a Version Tag
Create a Git tag corresponding to the version (with a leading `v`):

```bash
# Create local tag
git tag v1.2.1

# Push tag to GitHub
git push origin v1.2.1
```

---

## 4. Automated Release Pipeline

Once the tag `v1.2.1` is pushed:

1. **Trigger**: GitHub Actions triggers the **Release** workflow (`.github/workflows/release.yml`).
2. **Checkout & Test**: Checks out the tagged commit and runs the full test suite. If any test fails, the process stops immediately and no broken release is published.
3. **Build & Publish**: Builds the self-contained, single-file Windows x64 executable (`HotkeyScroll.exe`) with native libraries included and debug symbols omitted.
4. **Package**: Compresses the standalone executable into `HotkeyScroll-v1.2.1-win-x64.zip`.
5. **Create GitHub Release**: Creates the official GitHub Release associated with tag `v1.2.1`, attaches `HotkeyScroll-v1.2.1-win-x64.zip`, and generates release notes automatically.
6. **Zero Manual Uploads**: You never need to build or manually upload ZIP files to GitHub.

---

## 5. In-App Update Detection

1. HotkeyScroll runs a lightweight background check against `https://api.github.com/repos/codemilo-ui/HotkeyScroll/releases/latest`.
2. When users run HotkeyScroll, it detects release `v1.2.1` via semantic comparison (`1.2.1 > 1.2.0`).
3. The app displays:
   ```text
   ✦ Update Available
   HotkeyScroll v1.2.1 is available.
   [ View Release ]
   ```
4. Clicking **[ View Release ]** opens the official GitHub Release page in the user's default browser where they can download the clean ZIP.
5. HotkeyScroll never runs background updater processes, installers, or self-replacement.

---

## 6. Local Build Verification Commands

To verify builds locally at any time:

### Debug Build
```bash
dotnet build -c Debug
```

### Release Build
```bash
dotnet build -c Release
```

### Run All Unit Tests
```bash
dotnet test -c Release
```

### Publish Standalone Windows x64 Single-File Executable
```bash
dotnet publish RobloxHotbarScroll/RobloxHotbarScroll.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false -o ./publish/x64
```
