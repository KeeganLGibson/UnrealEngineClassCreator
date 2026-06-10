---
title: Getting Started
---

# Getting Started

## Installation

Download the latest installer from the [Releases](../../releases) page and run it. The installer is per-user by default — no UAC prompt required.

**Requirements:** Windows 10/11 (64-bit). The installer includes the .NET 10 runtime; no separate download needed.

---

## First Launch

When you open the app you will see the message **"Add a project to get started."** The tool is project-centric — you point it at a `.uproject` file and it automatically locates the associated Unreal Engine installation.

![First launch state](images/first-launch.png)

---

## Adding a Project

Click **Add Project** and select your `.uproject` file. The tool resolves the engine for that project using the following lookup chain:

1. **Co-located source build** — if an `Engine/` folder exists next to your project directory (the UGS layout), that engine is used directly
2. **Registry (HKLM)** — launcher binary installs keyed by version string
3. **Registry (HKCU)** — source builds registered by GUID
4. **LauncherInstalled.dat** — `C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat` as a final fallback

If the engine is found, the tool immediately begins scanning headers in the background.

You can add multiple projects. Use the project dropdown at the top of the window to switch between them. To remove a project you no longer need, select it in the dropdown and click **− Remove**. This removes it from the list and clears its saved output path and last-selected class; it does not delete any files on disk.

---

## Header Scanning

On first load the tool scans:

- `{EnginePath}/Engine/Source/` — all engine source headers
- `{EnginePath}/Engine/Plugins/` — engine plugin headers
- `{ProjectDir}/Source/` — your project's source headers
- `{ProjectDir}/Plugins/` — your project's plugin headers

A progress counter is shown in the status bar while scanning. On a typical launcher-installed engine this takes a few seconds.

![Scan progress in the status bar](images/scan-progress.png)

### Caching

Scanned engine classes are cached to `%LOCALAPPDATA%\UEClassCreator\cache\`.

| Engine type | Cache invalidation |
|---|---|
| Launcher install | Automatic — invalidated when `Engine\Build\Build.version` changes |
| Source build (UGS) | **Manual only** — UGS syncs bump `Build.version` on every pull even when no headers changed, so the cache is never auto-invalidated. Click **Rescan** after pulling significant engine changes. |

Project classes are always rescanned at startup (fast, small set).

---

## Building from Source

```
git clone <repo-url>
cd UnrealEngineClassCreator
dotnet build
dotnet run --project UEClassCreator
```

To produce a self-contained release build:

```powershell
.\Installer\Publish-Release.ps1 -Bump patch
```

This bumps the version, publishes a win-x64 self-contained binary, and compiles the Inno Setup installer to `Installer/Output/`.
