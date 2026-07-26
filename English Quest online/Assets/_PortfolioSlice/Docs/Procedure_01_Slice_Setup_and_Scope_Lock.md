# Procedure 01: Slice Setup and Scope Lock

## What is already in place

- A new Unity project exists at `C:\Portfolio Projects\English-Quest-Online\English Quest online`.
- Inside `Assets`, there is now a dedicated container for the portfolio slice: `Assets/_PortfolioSlice`.
- The slice container is split into separate buckets for `Art`, `Audio`, `Data`, `Prefabs`, `Scenes`, `Scripts`, `ScriptableObjects`, `Tests`, and `Docs`.

## Why this structure exists

The goal is to build a small, self-contained portfolio project that demonstrates the real systems from the original game without dragging the full world or production content into the new checkout.

## Planned scope for the slice

Keep:
- Service Locator architecture.
- Photon Fusion networking layer.
- Quest system.
- Dialogue system.
- Save system.
- Teacher Adventure systems.
- Two mini-games:
  - missing letters / line drawing.
  - word or letter placement.

Do not bring over:
- the original world environment.
- human character models.
- animal models from the source project.
- unnecessary third-party content that only serves the old visual identity.

## Implementation rule for this copy

The new project should feel like a separate game, not a copy of the original one. The technical systems are the value we want to preserve; the art direction can be rebuilt later with a different look.

## Next step

Inventory the source project dependencies, then copy only the required code, settings, and shared data into the slice container.
