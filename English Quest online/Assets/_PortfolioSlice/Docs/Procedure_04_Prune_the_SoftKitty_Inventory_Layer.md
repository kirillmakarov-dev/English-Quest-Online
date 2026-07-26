# Procedure 04: Prune the SoftKitty Inventory Layer

## What changed

The inventory-dependent runtime layer has been removed from the Unity `Assets` tree and parked in a root-level archive folder instead of being compiled with the slice.

Archived outside Unity asset compilation:
- `Core/Inventory`
- `Core/Skills`
- `Core/Abilities`
- `Core/LevelSystem`
- `Core/RewardSystem`
- `Systems/DropSystem`
- `UI/LevelUp`

Archive location:
- `C:\Portfolio Projects\English-Quest-Online\Archive\SoftKittyPruned`

## Why it was removed

The new portfolio project is supposed to show the core architecture and the educational gameplay slice without depending on the old SoftKitty inventory stack.

That keeps the scope aligned with the current goal:

- Service Locator
- Photon Fusion
- Quests
- Dialogue
- Save system
- Teacher Adventure
- Two mini-games

## What remains in Assets

The live project still contains some text mentions of SoftKitty in comments and tooltips, but those are not runtime dependencies and do not affect compilation.

The remaining live quest and save code now behaves as if inventory is a future feature, which is exactly the intended state for this slice.

## Next step

Reopen or refresh the Unity project, then inspect the console again. Any remaining errors should now be unrelated to SoftKitty inventory and much easier to isolate.
