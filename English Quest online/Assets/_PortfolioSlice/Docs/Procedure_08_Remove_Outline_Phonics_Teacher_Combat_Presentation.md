# Procedure 08 - Remove Outline, Phonics, Teacher Menu, and Combat Presentation Tails

## Goal

Eliminate the last live script references behind the current namespace errors:

- `Outline`
- `RMF_RadialMenu`
- `PhonicsData`
- `LocalPhonicsObject`
- `IAttackPresentation`

These belonged to optional visual helpers or legacy gameplay layers that are not part of the final portfolio slice.

## What the Errors Were From

### `Outline`

This was the old outline/highlight visual layer.

It affected:

- `OutlineWidthShaker`
- `PlayerInteractionController`
- `OpenWorldNpcLocationPopulator`

The live interaction and NPC tooling no longer depend on the outline package after this cleanup.

### `RMF_RadialMenu`

This belonged to the teacher radial-menu tool.

It was isolated inside `TeacherRadialMenuController`, which is not needed for the portfolio slice.

### `PhonicsData` / `LocalPhonicsObject`

This was the old phonics object delivery layer.

It affected:

- `LocalPhonicsObject`
- `LocalDropZone`
- `QuestStep_LocalObjectDeliverySet`

That subsystem was moved out because it is not part of the selected portfolio mini-games.

### `IAttackPresentation`

This belonged to the combat presentation layer that played VFX and sound for attack casts.

`AttackPresentationPlayer` was removed because the portfolio slice does not need that visual presentation stack.

## Current Result

The live slice now keeps the core gameplay and save/quest structure, but no longer depends on these missing helper layers.

## Notes

- The remaining `AttackHitScheduler` comment still mentions the removed presentation player, but that is only a comment and does not affect compilation.
- I did not run a Unity editor compile in this pass.

## Next Step

If the editor still reports package issues after a refresh, the next search should focus on package manifest dependencies rather than the gameplay scripts, because the major missing type references have now been removed from the live code.
