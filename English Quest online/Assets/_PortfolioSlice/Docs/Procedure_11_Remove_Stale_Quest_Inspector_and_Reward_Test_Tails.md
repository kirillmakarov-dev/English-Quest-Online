# Procedure 11 - Remove Stale Quest Inspector and Reward Test Tails

## Goal

Remove the last live editor/test scripts that still referenced deleted quest reward, level-up, and letter database systems.

The console errors were coming from these missing symbols:

- `RewardGrantResult`
- `EnglishKingdom.Editor.Tools.Quests`
- `EnglishKingdom.UI.LevelUp`
- `LetterDatabase`

## What Was Removed

### Quest reward tests

- `QuestManagerRewardFallbackTests`
- `QuestManagerStepRewardTests`

These tests were holding on to the removed reward-grant result type, so they no longer belonged in the trimmed slice.

### Editor inspectors and wiring helpers

- `QuestInfoInspector`
- `LetterDatabaseInspector`
- `LevelUpCelebrationFeelPrefabWiring`

These were support tools for systems that are not part of the final portfolio demo scope.

## Current Result

The live `Scripts` tree no longer has compile-affecting references to the removed quest reward, level-up, or letter database namespaces.

What remains in the project is the cleaner portfolio surface:

- Service Locator
- Photon Fusion
- quests and dialogue
- save flow
- selected mini-games

## Notes

- I did not remove any of the main quest runtime systems in this pass.
- I did not run the Unity editor itself here; this is based on source-level cleanup and symbol checks.
