# Procedure 14 - Create the Portfolio Integration Test Scene

## Goal

Build one small playable scene that proves the transferred systems can work together before the final portfolio environment is created.

The scene is intentionally made from primitives. Its purpose is to validate architecture and gameplay flow, not final art.

## Scene

The test scene is located at:

- `Assets/_PortfolioSlice/Demo/Scenes/PortfolioDemo.unity`

It is also placed first in Unity Build Settings.

The scene contains:

- a capsule player with local movement and proximity interaction
- an NPC named `Teacher Ada`
- a dialogue canvas and dialogue manager
- a Line Match station
- a Letter Ordering station
- a Word Ordering station
- the real UI prefabs for all three selected mini-games
- a quest objective event bus
- an explicit global Service Locator
- a small HUD that reports interaction prompts and completed systems

## Runtime Integration

### Player

`PortfolioDemoPlayerController` provides only the lightweight presentation code needed by this scene:

- reads movement from the Unity Input System
- moves a `CharacterController`
- searches nearby colliders for the existing `IInteractable` contract
- calls `Interact` on the selected gameplay object

The player does not know whether the target is an NPC or a mini-game. This keeps the test aligned with the existing interaction architecture.

### Player locking

`PortfolioPlayerLockService` implements the existing `IPlayerLockSystem` contract and registers itself through the Service Locator.

Dialogue and mini-game systems can therefore lock player movement without depending on a full production character controller.

### Dialogue

Teacher Ada uses the transferred dialogue system and demo dialogue node assets:

- `Dialogue_Teacher_Start.asset`
- `Dialogue_Teacher_Systems.asset`

Interacting with the NPC opens the real dialogue canvas. Closing or finishing the conversation returns control to the capsule player.

### Mini-games

Each colored station uses the existing quest mini-game launch path:

1. `PortfolioDemoPlayerController` discovers an `IInteractable`.
2. `MiniGameWorldInteractable` resolves its mini-game config.
3. `QuestMiniGameConfigSO` launches the matching UI through `MiniGameWorldLaunchHost`.
4. The mini-game reports completion through its existing callback.
5. `QuestObjectiveEventBus` publishes the completion event.
6. `PortfolioDemoHud` reports the completed game and restores the normal scene flow.

The three configured game ids are:

- `line_match`
- `letter_ordering`
- `word_ordering`

## Demo Data

Scene-specific ScriptableObjects are stored in:

- `Assets/_PortfolioSlice/Demo/Data`

They contain small test exercises only. Production portfolio content can later replace these assets without changing the integration code.

## Scene Builder

The scene can be regenerated from:

- `Tools > Portfolio Demo > Rebuild Test Scene`

The builder is located at:

- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Editor/PortfolioDemo/PortfolioDemoSceneBuilder.cs`

It creates the scene objects, demo data, UI wiring, service registration, camera, lighting, build settings entry, and interaction stations.

This makes the integration scene reproducible instead of relying on undocumented manual Inspector setup.

## Controls

- `WASD` - move the capsule
- `E` - interact with the nearest available target
- Mouse - select dialogue choices and interact with mini-game UI

The HUD displays the nearest interaction prompt and the latest completed system.

## Manual Verification Checklist

1. Open `PortfolioDemo.unity` and enter Play Mode.
2. Confirm that no dialogue or mini-game panel is visible at startup.
3. Walk to Teacher Ada and press `E`.
4. Confirm that the dialogue opens, choices work, and player movement is locked during the conversation.
5. Finish the dialogue and confirm that movement is restored.
6. Walk to the Line Match station and press `E`.
7. Complete the line connection exercise and confirm that the UI closes.
8. Repeat the process for Letter Ordering.
9. Repeat the process for Word Ordering.
10. Confirm that the HUD reports completion after each mini-game.
11. Exit Play Mode and confirm that Unity reports no teardown exceptions.

## Integration Fixes Made During This Procedure

- Added Input System assembly references required by the demo player and editor builder.
- Closed all mini-game panels during bootstrap so the world scene remains visible at startup.
- Replaced the missing third-party Flow Layout components in the portfolio Word Ordering prefab with built-in `HorizontalLayoutGroup` components.
- Restored the nested Word Reveal popup prefab required by the Line Match presentation.
- Added an explicit `ServiceLocatorGlobal` to the generated scene.
- Made quest world target deregistration tolerate locator teardown during Play Mode exit.

## Validation Result

The scene was regenerated from the editor menu and run in Unity.

The final `Play -> Stop` smoke test completed without:

- C# compiler errors
- missing-script messages
- `MissingComponentException`
- `NullReferenceException`
- missing global Service Locator warnings

The project is now ready for manual gameplay verification and for replacing the primitive test environment with a new portfolio visual direction.
