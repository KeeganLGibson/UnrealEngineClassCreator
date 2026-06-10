---
title: UE Class Creator
---

# UE Class Creator

A fast, offline Windows desktop tool for generating Unreal Engine C++ class boilerplate.

[Download latest release](https://github.com/KeeganLGibson/UnrealEngineClassCreator/releases/latest)

---

## Why I Built This

Unreal Engine's built-in class wizard means booting the editor just to create a couple of files — on a large project that can mean waiting several minutes before you've written a single line of code.

Beyond the wait, I wanted the generated files to actually reflect how my team writes code: a consistent include ordering, a clean file header, and a structure we'd agreed on — not the engine's generic default. And I wanted that style to be configurable per project rather than hardcoded, so different projects or teams could have their own conventions without maintaining a fork.

The result is a standalone tool that stays open alongside the editor, lets you search the full class hierarchy instantly, and writes files exactly the way your project expects them.

---

![Main window showing search panel and class detail](images/main-window.png)

---

## Features

- **Fast as-you-type search** — filters the full engine and project class index instantly, matching on both class name and parent class name
- **Inheritance context** — selecting a class shows its full ancestry chain and direct subclasses; every entry is clickable to navigate the hierarchy
- **Common classes pinned** — when search is empty, the classes you reach for most (`AActor`, `ACharacter`, `UActorComponent`, `UObject`, etc.) appear at the top
- **Mustache templates** — generates `.h` + `.cpp` pairs with correct `#include` paths, `UCLASS()`, `GENERATED_BODY()`, and module-aware include ordering; drop custom templates into `{ProjectDir}/build/ClassCreator/` to override defaults per project
- **Smart Public/Private routing** — automatically places `.h` in `Public` and `.cpp` in `Private` based on whichever branch you pick as the output path
- **Automatic engine and project discovery** — resolves installed and source-built engines from the registry and `LauncherInstalled.dat`; scans your project's `Source/` and `Plugins/` directories alongside the engine
- **Standalone class/struct creation** — create a plain C++ class or struct with no parent when you need one
- **Fully offline** — no network dependency beyond the optional update check

---

## Example Output

Creating `AMyActor` with parent `AActor` produces:

**`Public/MyActor.h`**
```cpp
//  MyProject
//
//  2026(c) - Copyright MyCompany
//
//  File Name   :   MyActor.h
//  Description :   TODO:
//

#pragma once

// Library Includes
#include "GameFramework/Actor.h"

// Local Includes

// This Includes
#include "MyActor.generated.h"

// Types

// Constants

// Prototypes

// TODO:
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

**`Private/MyActor.cpp`**
```cpp
//  MyProject
//
//  2026(c) - Copyright MyCompany
//
//  File Name   :   MyActor.cpp
//  Description :   TODO:
//

// This Includes
#include "MyActor.h"

// Library Includes

// Local Includes

// Generated CPP
#include UE_INLINE_GENERATED_CPP_BY_NAME(MyActor)

// Static Variables

// Static function prototypes

// Implementation
```

---

## Pages

- [Getting Started](getting-started) — installation, adding your first project, engine discovery
- [Usage Guide](usage) — search, class selection, generating files, settings
- [Output Path Routing](output-routing) — how Public/Private path splitting works
- [Templates](templates) — custom templates and template variable reference
