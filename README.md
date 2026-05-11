# Unreal Engine Class Creator

A fast, offline Windows desktop tool for generating Unreal Engine C++ class boilerplate from templates.

---

## Features

- **Fast as-you-type search** across all engine and project classes — no slow tree population
- **Inheritance context** — selected class shows its full ancestry chain and direct subclasses
- **Common classes pinned** for quick access: `AActor`, `APawn`, `ACharacter`, `AGameModeBase`, `APlayerController`, `UActorComponent`, `USceneComponent`, `UObject`, `UUserWidget`, `UGameInstance`, `UGameInstanceSubsystem`, `UWorldSubsystem`
- **UObject descendants filter** — optionally narrow results to `UObject`-derived classes only
- **Automatic engine discovery** — finds both source builds and Launcher-installed engines
- **Game project scanning** — indexes your own project's classes alongside engine classes; supports multiple projects
- **Mustache templates** — generates `.h` + `.cpp` pairs with correct `#include` paths, `UCLASS()`, `GENERATED_BODY()`, and module-aware include ordering
- **Per-project template overrides** — drop custom `Header.mustache`, `Cpp.mustache`, or `Struct.mustache` into `{ProjectDir}/build/ClassCreator/` to override the defaults for that project
- **Persistent settings** — remembers your last output path and selected class per project
- **Fully offline** — no network dependency

---

## How It Works

At startup the app scans Unreal Engine header files in the background and builds a flat in-memory index of every class and struct it finds. The index stores the class name, parent class, module, and header path. Search filters this index instantly as you type.

When you select a parent class, the tool resolves its full ancestry chain and lists its direct subclasses from the same index — no separate tree structure is maintained.

Class files are generated using [Mustache](https://mustache.github.io/) templates via [Stubble](https://github.com/StubbleOrg/Stubble).

---

## Engine Discovery

The tool finds installed engines from:

- **Launcher installs** — `C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat`
- **Source builds** — Windows registry (`HKCU\Software\Epic Games\Unreal Engine\Builds`)

Scanned engine classes are cached to `%LOCALAPPDATA%\UEClassCreator\cache\` and reused until the engine version changes.

---

## Requirements

- Windows 10/11 (64-bit)
- .NET 10 runtime (or use the self-contained installer build)
- Unreal Engine installed via Epic Games Launcher or built from source

---

## Installation

Download the latest installer from the [Releases](../../releases) page and run it. The installer is per-user by default (no UAC prompt required).

---

## Building from Source

```
git clone <repo-url>
cd UnrealEngineClassCreator
dotnet build
dotnet run --project UEClassCreator
```

To produce a self-contained release build, use the included PowerShell script:

```powershell
.\Installer\Publish-Release.ps1 -Bump patch
```

This bumps the version, publishes a win-x64 self-contained binary, and compiles the Inno Setup installer to `Installer/Output/`.

---

## Generated File Structure

**Header (`MyActor.h`)**
```cpp
//  MyProject
//
//  2026(c) - Copyright MyCompany
//
//  File Name   :   MyActor.h
//  Description :   TODO:

#pragma once

// Library Includes
#include "GameFramework/Actor.h"

// This Includes
#include "MyActor.generated.h"

// UCLASS description here
UCLASS()
class AMyActor : public AActor
{
    GENERATED_BODY()
    // Member Functions
public:

protected:

private:

    // Member Variables
public:

protected:

private:

};
```

**Implementation (`MyActor.cpp`)**
```cpp
//  MyProject
//
//  2026(c) - Copyright MyCompany
//
//  File Name   :   MyActor.cpp
//  Description :   TODO:

// This Includes
#include "MyActor.h"

// Generated CPP
#include UE_INLINE_GENERATED_CPP_BY_NAME(MyActor)

// Implementation
```

---

## Template Variables

| Variable | Description |
|---|---|
| `Class` | Full class name e.g. `AMyActor` |
| `FileName` | Class name with leading U/A/F stripped e.g. `MyActor` |
| `ParentClass` | Parent class name |
| `ParentClassSource` | Relative `#include` path to parent header |
| `ModuleName` | Parent's module name |
| `ProjectName` | UE project name |
| `ProjectCompany` | Company/author from settings |
| `Year` | Current year |
| `Description` | User-provided description (defaults to `TODO:`) |
| `bIsUClass` | `true` if parent is UObject-derived — emits `UCLASS()` / `GENERATED_BODY()` |
| `bIsGameModule` | `true` if parent is from the game project — affects include section ordering |
| `CustomCopyright` | Optional copyright line override (replaces the default header block) |

---

## License

MIT
