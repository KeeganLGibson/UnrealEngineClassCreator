# Unreal Engine Class Creator

A fast, offline Windows desktop tool for generating Unreal Engine C++ class boilerplate from templates.

**[Full documentation →](docs/index.md)**

---

## Features

- Fast as-you-type search across all engine, plugin, and project classes
- Inheritance context — ancestry chain and direct subclasses, all clickable
- Common classes pinned for quick access when search is empty
- UObject-only filter
- Standalone class and struct creation (no parent required)
- Multiple projects — add, switch between, and remove projects from the list
- Automatic engine discovery — source builds, Launcher installs, UGS co-located layout
- Smart Public/Private output path routing for `.h` and `.cpp`
- Mustache templates with per-project overrides
- Persistent settings per project
- Class name prefix hints (`A`/`U`)
- Update notifications

---

## Requirements

- Windows 10/11 (64-bit)
- .NET 10 runtime (included in the installer build)
- Unreal Engine via Epic Games Launcher or built from source

---

## Installation

Download the latest installer from the [Releases](../../releases) page and run it. Per-user install — no UAC prompt required.

---

## Building from Source

```
git clone <repo-url>
cd UnrealEngineClassCreator
dotnet build
dotnet run --project UEClassCreator
```

```powershell
# Self-contained installer build
.\Installer\Publish-Release.ps1 -Bump patch
```

---

## License

MIT
