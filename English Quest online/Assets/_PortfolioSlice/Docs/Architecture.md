# English Quest Online - Architecture Overview

## Purpose

This document explains how the current portfolio slice is assembled at runtime.

The project is intentionally built as a focused vertical slice:

- one open-world scene;
- three sequential NPC quest lines;
- three educational mini-games;
- Photon Fusion Shared Mode for two players;
- independent quest progression per player.

The goal is not a full game framework. The goal is a clean, explainable gameplay slice with visible architectural discipline.

## High-level runtime model

The scene is composed from a few cooperating runtime layers:

1. Open-world interaction layer
2. Dialogue layer
3. Quest progression layer
4. Mini-game layer
5. Multiplayer layer
6. Portfolio-specific UX/debug layer

Each layer owns a narrow responsibility and communicates through events, service lookup, or explicit scene references.

## Composition root

Primary scene:

`Assets/_PortfolioSlice/Demo/Scenes/PortfolioDemo.unity`

Core runtime anchors in the scene:

- `Quest Manager`
- `Quest Line Registrar`
- `Dialogue Manager`
- `Quest Objective Event Bus`
- `Portfolio Demo HUD`
- `Network`

These objects form the slice composition root. The player, NPCs, mini-game stations, and HUD all resolve behavior from these scene-level systems.

## Service boundary

The project uses `UnityServiceLocator` as a lightweight composition mechanism.

Why it is used here:

- scene systems can register shared contracts once;
- gameplay components stay decoupled from hard singleton lookups;
- the slice can keep inspector-driven authoring without collapsing into one global manager.

Typical services resolved at runtime:

- `IQuestService`
- player lock/input control contracts
- mini-game binding helpers

Important rule:

Service Locator is used as a composition boundary, not as a replacement for every dependency. Direct scene references are still used when a dependency is explicitly part of the scene setup.

## Quest architecture

Main files:

- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/QuestSystem/Core/QuestManager.cs`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/QuestSystem/Core/QuestInfo.cs`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/QuestSystem/Authoring/QuestLineSO.cs`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/QuestSystem/Authoring/QuestLineRegistrar.cs`

Responsibilities:

- `QuestLineSO` defines an authored quest line for one NPC.
- `QuestLineRegistrar` resolves and registers quest-line content into the scene.
- `QuestInfo` stores runtime quest state, step progress, requirements, and objective progress.
- `QuestManager` is the authoritative local progression service for the slice.

The quest system is event-driven. `QuestManager` emits:

- `OnQuestStarted`
- `OnQuestUpdated`
- `OnQuestCompleted`
- `OnQuestStateChanged`
- `OnObjectiveProgressChanged`
- `OnLevelCompleted`

This keeps quest consumers simple:

- NPC indicators react to quest state changes;
- mini-game binders react to current objective state;
- the HUD reacts to completion flow;
- debug and validation tools can inspect the same data source.

## Dialogue and interaction

Main files:

- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Dialogue/DialogueManager.cs`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Player/PlayerInteraction.cs`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Player/PlayerInteractionController.cs`

The player interacts with NPCs and stations through the same interaction contract.

Flow:

1. Player enters interaction range
2. Prompt is shown by the HUD
3. Interaction opens dialogue or gameplay
4. Input/cursor state is locked through the player lock system
5. On completion or close, control returns to open-world mode

This prevents mini-games and dialogue from fighting for control at the same time.

## Game flow coordination

Main file:

- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/PortfolioDemo/PortfolioGameFlowCoordinator.cs`

The portfolio slice uses explicit high-level states instead of scattered UI toggles:

- `OpenWorld`
- `Dialogue`
- `MiniGame`
- `LevelCompleted`

The coordinator drives HUD visibility and the final completion flow so that:

- open-world HUD disappears while a mini-game is active;
- completion UI always sits on top and owns cursor interaction;
- scene behavior stays predictable during demo playthroughs.

## Mini-game integration

Current mini-games in the MVP slice:

- Line Match
- Letter Ordering
- Word Ordering

They are not free-floating activities anymore. They are bound to quest objectives and only become available when the player reaches the correct step.

Integration pattern:

- quest step becomes active;
- matching station becomes playable;
- on completion, an objective completion event is published;
- `QuestManager` advances the current quest;
- next NPC or station becomes available for that same player.

## Multiplayer architecture

Core mode:

- Photon Fusion `GameMode.Shared`

Important design choice:

- multiplayer presence is shared;
- quest progression is local per player;
- no mission in the MVP requires a second player to activate it.

This is intentional. The slice demonstrates multiplayer coexistence and synchronization without sacrificing solo demo reliability.

Main multiplayer responsibilities:

- `GameNetworkManager` boots the network session
- `EnglishQuestNetworkSceneManager` manages scene ownership/load flow
- `PlayerSpawnCoordinator` spawns players into scene spawn points
- `NetworkStarterAssetsPlayer` bridges Starter Assets movement with Fusion ownership
- `PlayerQuestStatusSync` exposes each player's current lesson/progress to the shared HUD
- `PortfolioSessionPlayerUtility` resolves player objects across co-session runners for HUD/debug visibility and late-join-safe inspection
- `PortfolioOptionalCoopStudyCircle` adds one optional shared-world beat for the portfolio demo without changing quest ownership or lesson gating

The optional Study Circle is intentionally outside the quest progression path:

- one player can ignore it and still finish the full slice;
- two players can use it to demonstrate an intentional shared multiplayer moment;
- HUD and debug surfaces can reference it without turning it into a progression dependency.

This is also protected at the tooling level:

- the portfolio validator is expected to fail if an optional co-op activity such as the `Study Circle` is authored as a required quest objective;
- this keeps future content edits from accidentally breaking the solo-first contract of the MVP slice.

## Player architecture

Runtime player setup combines:

- Starter Assets third-person movement
- local interaction logic
- player lock service
- Fusion network object ownership
- per-scene camera binding

The multiplayer player object supports:

- local controlled movement for the owning player
- replicated transform and animation for remote peers
- per-player camera ownership
- collision presence against other players
- independent quest status reporting

## Debug and validation layer

Portfolio-specific developer tooling is part of the slice, not an afterthought.

Current support includes:

- quest progress debug controller
- runtime overlay/debug helpers
- validation entry points in the editor pipeline

The purpose is to make the slice fast to demo, fast to reset, and safe to evolve.

## Architectural boundary summary

What this slice does well on purpose:

- keeps quest logic centralized in one authoritative local service
- keeps content authoring data-driven with ScriptableObjects
- keeps scene wiring explicit and readable
- keeps multiplayer ownership isolated from quest authoring concerns
- keeps demo UX under a dedicated portfolio layer

What it deliberately does not try to solve yet:

- persistent backend or account saves
- shared co-op quest completion logic
- network-synchronized dialogue state
- production-grade content pipeline tooling across many scenes

That tradeoff is correct for this portfolio slice: the architecture is focused, readable, and proportionate to the demo scope.
