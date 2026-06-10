---
title: Output Path Routing
---

# Output Path Routing

When you choose an output path that contains a `Public` or `Private` segment anywhere **after** a `Source` segment, the `.h` and `.cpp` are written to separate branches automatically — `.h` goes to `Public`, `.cpp` goes to `Private` — regardless of which branch you actually picked.

When no `Public`/`Private` structure is detected, both files land in the same directory.

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

Only the portion of the path **after** the first `Source` segment is examined, so a project or plugin folder named `Public` or `Private` above `Source` has no effect.

## Struct Files

Structs only generate a `.h` file — the routing logic still applies for placing it in the correct `Public` branch, but no `.cpp` is created.
