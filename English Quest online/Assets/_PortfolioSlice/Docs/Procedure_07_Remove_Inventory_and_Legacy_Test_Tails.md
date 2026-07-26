# Procedure 07 - Remove Inventory and Legacy Test Tails

## Goal

Remove the last live references to systems that do not belong in the portfolio slice:

- inventory and action bar save domains
- inventory-specific migration wiring
- legacy SpeakAloud / SecretCode test assets
- old sample data that referenced removed mini-game scripts
- dead menu-path constants for removed mini-games

## What Was Removed

### Save system inventory layer

- `InventorySaveData`
- `ActionBarSaveData`
- `InventorySaveDataV1ToV2Migrator`
- inventory and action-bar methods from `SaveManager`
- inventory registration from `SaveSystemBootstrapper`
- inventory and action-bar entries from the save inspector registry

### Legacy test and sample content

- `Scripts/Tests/Editor/SpeakGame`
- `Data/dailyQuestExampleTest` was removed from the live slice

These folders were still referencing removed systems such as `SpeakAloud`, `SecretCodeUI`, and `SecretCodeQuestStep`, so they were moved out of the live slice instead of being patched one-by-one.

### Menu-path cleanup

`ScriptableObjectMenuPaths` was trimmed so it only keeps menu entries that still map to live content in the portfolio slice.

## Current State

The live slice no longer contains working references to:

- inventory
- action bar / SoftKitty UI storage
- SpeakAloud
- SecretCode
- drawing mini-game support
- sentence completion support

This leaves the slice focused on the final portfolio target without stale dependencies in the runtime scripts or the editor tests.

## Notes

- I verified the live `Scripts` and `Data` trees again after the cleanup.
- The remaining mentions of old systems are now only cosmetic comments, not active references.

## Next Step

Run one last project-wide search for any other stale references outside `Scripts` and `Data`, then decide whether the last few comments should stay as historical notes or be cleaned too.
