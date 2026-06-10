---
title: Templates
render_with_liquid: false
---

# Templates

Files are generated using [Mustache](https://mustache.github.io/) templates via [Stubble](https://github.com/StubbleOrg/Stubble). See the [Mustache manual](https://mustache.github.io/mustache.5.html) for the full syntax reference — sections (`{{#var}}`…`{{/var}}`), inverted sections (`{{^var}}`), and variable substitution (`{{var}}`) are the main constructs used here.

---

## Default Templates

Three templates ship with the tool. They live in the `Templates/` folder of the installation and can be overridden per-project (see [Per-Project Overrides](#per-project-overrides) below).

### Header.mustache

Used for the `.h` file of every class.

```handlebars
{{#CustomCopyright}}//	{{CustomCopyright}}
{{/CustomCopyright}}//  {{ProjectName}}
{{^CustomCopyright}}//
//  {{Year}}(c) - Copyright {{ProjectCompany}}{{/CustomCopyright}}
//
//  File Name   :   {{FileName}}.h
//  Description :   {{Description}}
//

#pragma once

// Library Includes{{^bIsGameModule}}{{#bHasParent}}
#include "{{ParentClassSource}}"{{/bHasParent}}{{/bIsGameModule}}

// Local Includes{{#bIsGameModule}}{{#bHasParent}}
#include "{{ParentClassSource}}"{{/bHasParent}}{{/bIsGameModule}}

// This Includes{{#bIsUClass}}
#include "{{FileName}}.generated.h"{{/bIsUClass}}

// Types

// Constants

// Prototypes

{{#bIsUClass}}
// {{Description}}
UCLASS(){{/bIsUClass}}
class {{Class}}{{#ParentClass}} : public {{ParentClass}}{{/ParentClass}}
{
{{#bIsUClass}}	GENERATED_BODY()
{{/bIsUClass}}	// Member Functions
public:

protected:

private:

	// Member Variables
public:

protected:

private:

};
```

### Cpp.mustache

Used for the `.cpp` file of every class.

```handlebars
{{#CustomCopyright}}//	{{CustomCopyright}}
{{/CustomCopyright}}//  {{ProjectName}}
{{^CustomCopyright}}//
//  {{Year}}(c) - Copyright {{ProjectCompany}}{{/CustomCopyright}}
//
//  File Name   :   {{FileName}}.cpp
//  Description :   {{Description}}
//

// This Includes
#include "{{FileName}}.h"

// Library Includes

// Local Includes
{{#bIsUClass}}

// Generated CPP
#include UE_INLINE_GENERATED_CPP_BY_NAME({{FileName}})
{{/bIsUClass}}

// Static Variables

// Static function prototypes

// Implementation
```

### Struct.mustache

Used for the `.h` file of structs. No `.cpp` is generated.

```handlebars
{{#CustomCopyright}}//	{{CustomCopyright}}
{{/CustomCopyright}}//  {{ProjectName}}
{{^CustomCopyright}}//
//  {{Year}}(c) - Copyright {{ProjectCompany}}{{/CustomCopyright}}
//
//  File Name   :   {{FileName}}.h
//  Description :   {{Description}}
//

#pragma once

// Library Includes{{^bIsGameModule}}{{#bHasParent}}
#include "{{ParentClassSource}}"{{/bHasParent}}{{/bIsGameModule}}

// Local Includes{{#bIsGameModule}}{{#bHasParent}}
#include "{{ParentClassSource}}"{{/bHasParent}}{{/bIsGameModule}}

// This Includes{{#bIsUStruct}}
#include "{{FileName}}.generated.h"{{/bIsUStruct}}

// Types

// Constants

// Prototypes
{{#bIsUStruct}}
// {{Description}}
USTRUCT()
{{/bIsUStruct}}struct {{Class}}{{#ParentClass}} : public {{ParentClass}}{{/ParentClass}}
{
{{#bIsUStruct}}	GENERATED_BODY()
{{/bIsUStruct}}
};
```

---

## Per-Project Overrides

Drop custom versions of any template into:

```
{ProjectDir}/build/ClassCreator/Header.mustache
{ProjectDir}/build/ClassCreator/Cpp.mustache
{ProjectDir}/build/ClassCreator/Struct.mustache
```

The tool checks this location first and falls back to the bundled templates if no override is found. Only the templates you place there are overridden — you do not need to provide all three.

---

## Template Variables

| Variable | Type | Description |
|---|---|---|
| `Class` | string | Full class name, e.g. `AMyActor` |
| `FileName` | string | Class name with leading `U`, `A`, or `F` stripped, e.g. `MyActor` |
| `ParentClass` | string | Parent class name, e.g. `AActor`. Empty for standalone classes. |
| `ParentClassSource` | string | Relative `#include` path to the parent header |
| `ModuleName` | string | Parent class module name, e.g. `Engine` |
| `ProjectName` | string | Unreal project name |
| `ProjectCompany` | string | Company/author from the Company Name setting |
| `Year` | string | Current four-digit year |
| `Description` | string | User-provided description, or `TODO:` if blank |
| `bIsUClass` | bool | `true` if the parent is a `U`- or `A`-prefixed class — emits `UCLASS()` and `GENERATED_BODY()` |
| `bIsUStruct` | bool | `true` for structs that need `USTRUCT()` and `GENERATED_BODY()` |
| `bHasParent` | bool | `true` when a parent class was selected |
| `bIsGameModule` | bool | `true` if the parent class is from the game project — affects include section ordering |
| `CustomCopyright` | string | Optional copyright line override; replaces the default project/company header block when present |

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

// Library Includes

// Local Includes

// Generated CPP
#include UE_INLINE_GENERATED_CPP_BY_NAME(MyActor)

// Static Variables

// Static function prototypes

// Implementation
```
