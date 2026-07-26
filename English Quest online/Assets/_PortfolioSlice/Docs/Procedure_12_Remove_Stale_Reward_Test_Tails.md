# Procedure 12 - Remove Stale Reward Test Tails

## Goal

Clear the last reward-related test leftovers that still expected an older `RewardBundle` shape.

The compile noise came from obsolete test assertions, not from the active quest runtime:

- `RewardBundle.xpRewards`
- `RewardBundle.IsEmpty`

## What Was Removed

### Reward tests

- `QuestRewardDataTests`

This test file belonged to the previous reward implementation and no longer matched the slimmed compatibility layer.

## What Stayed

The live compatibility layer still exposes the simple reward API used by the active quest flow:

- `RewardBundle.AddXp`
- `RewardBundle.AddLegacyCoins`
- `QuestRewardData.ToRewardBundle`
- `QuestManager` reward granting

That keeps the runtime quest reward path working without reintroducing the old editor/test surface.

## Current Result

The portfolio slice is now cleaner around quest authoring and reward handling, while still keeping the actual quest runtime intact.

## Notes

- I did not widen the compatibility layer back out to the old reward model.
- I did not run the Unity editor compile here; this is based on source cleanup and symbol checks.
