# Procedure 09 - Remove Lip Sync, Behavior, and Package Deadweight

## Goal

Clean the last package-related errors by removing the optional systems that still depended on packages we do not want in the portfolio slice:

- `uLipSync`
- `Unity.Behavior`
- `Whisper`
- `Recorder`
- `Collab Proxy`
- `Multiplayer Center`
- `Multiplayer PlayMode`
- `UI Extensions`

## What Was Removed

### Runtime / editor scripts

- `NpcDialoguePresenter`
- `LipSyncConnector`
- `LipSyncProfileSet`
- `CinematicDialogueManager` lip-sync presenter wiring
- `NetworkedBehaviorRunner`
- `NetworkedBehaviorGraphAgent`
- `PatrolPointsSanitizer`
- `AnimalDayCycleBehaviorAction`
- `LocalAnimalPickupHandler`
- `AnimalPickupHandler`

### Data folders

- `Data/LipSync`
- `Data/Behaviour`

### Package manifest cleanup

Removed the following entries from `Packages/manifest.json`:

- `com.hecomi.ulipsync`
- `com.unity.behavior`
- `com.unity.collab-proxy`
- `com.unity.multiplayer.center`
- `com.unity.multiplayer.playmode`
- `com.unity.recorder`
- `com.unity.uiextensions`
- `com.whisper.unity`

The stale `Packages/packages-lock.json` file was also removed so Unity can regenerate a clean lock file from the trimmed manifest.

## Current Result

The live slice now keeps:

- Service Locator
- Photon Fusion
- dialogue core without lip-sync presentation
- quests, save, and the selected mini-games

The only remaining `Behavior` mention in live code is a comment inside `AnimalNpcBehaviorConfigSO`, which does not affect compilation.

## Notes

- I did not run the Unity editor in this pass.
- If Unity still reports package resolution issues after reopening the project, the next step is to inspect the editor log for the exact package that the registry rejects, but the most obvious optional packages are already gone.
