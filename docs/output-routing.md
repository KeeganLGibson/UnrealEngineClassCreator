---
title: Output Path Routing
---

# Output Path Routing

When you choose an output path that contains a `Public` or `Private` segment anywhere **after** a `Source` segment, the tool automatically splits the output:

| File | Destination |
|---|---|
| `.h` | `…/Source/…/Public/…` |
| `.cpp` | `…/Source/…/Private/…` |

This applies whether you type the `Public` path, the `Private` path, or any subdirectory within either branch — the tool rewrites to the correct branch for each file.

When no `Public`/`Private` structure is detected, both files are written to the same directory.

---

## Examples

```
Input path                                   → .h destination              .cpp destination
─────────────────────────────────────────────────────────────────────────────────────────────
.../Source/MyModule/Private/Systems/Design   → MyModule/Public/Systems/Design   MyModule/Private/Systems/Design
.../Source/MyModule/Public/Systems/Design    → MyModule/Public/Systems/Design   MyModule/Private/Systems/Design
.../Source/MyModule/Private                  → MyModule/Public                  MyModule/Private
.../Plugins/Combat/Source/Combat/Private/AI  → Combat/Public/AI                 Combat/Private/AI
.../Source/MyModule/Shared                   → MyModule/Shared                  MyModule/Shared   (no split)
```

---

## Scope of the Check

Only the portion of the path **after** the first `Source` segment is examined. A project or plugin folder named `Public` or `Private` above the `Source` directory has no effect.

---

## Struct Files

Structs only generate a `.h` file — the routing logic still applies for placing it in the correct `Public` branch, but no `.cpp` is created.
