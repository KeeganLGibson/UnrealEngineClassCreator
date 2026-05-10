# Unreal Engine Class Creator

A fast, offline Windows desktop tool for generating Unreal Engine C++ class boilerplate from templates.

---

## Features

- **Fast as-you-type search** across all engine and project classes — no slow tree population
- **Inheritance context** — selected class shows its full ancestry chain and direct subclasses
- **Common classes pinned** for quick access: `AActor`, `APawn`, `ACharacter`, `AGameModeBase`, `APlayerController`, `UActorComponent`, `USceneComponent`, `UObject`, `UUserWidget`, `UGameInstance`, `UGameInstanceSubsystem`, `UWorldSubsystem`
- **Automatic engine discovery** — finds both source builds and Launcher-installed engines
- **Game project scanning** — indexes your own project's classes alongside engine classes
- **Mustache templates** — generates `.h` + `.cpp` pairs with correct `#include` paths, `UCLASS()`, `GENERATED_BODY()`, and module-aware include ordering
- **Fully offline** — no network dependency

---

## How It Works

At startup the app scans Unreal Engine header files in the background and builds a flat in-memory index of every class and struct it finds. The index includes the class name, parent class, module, and header path. Search filters this index instantly as you type.

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

- Windows 10/11
- .NET 10 runtime (or use the self-contained build)
- Unreal Engine installed via Epic Games Launcher or built from source

---

## Building

```
git clone https://github.com/your-username/UnrealEngineClassCreator
cd UnrealEngineClassCreator
dotnet build
dotnet run --project UEClassCreator
```

---

## Templates

Generated files follow this structure:

**Header (`ClassName.h`)**
```cpp
// MyProject
// 2025(c) - Copyright MyCompany
//
// File Name   :   MyActor.h
// Description :   ...

#pragma once

#include "GameFramework/Actor.h"
#include "MyActor.generated.h"

UCLASS()
class AMyActor : public AActor
{
    GENERATED_BODY()
public:
protected:
private:
};
```

Custom templates can be placed alongside the executable to override the defaults.

---

## License

MIT
