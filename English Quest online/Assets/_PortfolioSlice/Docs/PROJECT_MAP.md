# English Quest Online - Project Map

## Purpose

This document is the practical map of the current repository.

Use it to answer three questions quickly:

1. what is active in the portfolio slice right now;
2. where the important code, data, and scene wiring live;
3. where the next developer should work without digging through the entire mirrored codebase.

This file is intentionally operational. It is not a design essay.

## Recommended reading order

If you are onboarding into the project, read in this order:

1. `README.md`
2. `Assets/_PortfolioSlice/Docs/Architecture.md`
3. `Assets/_PortfolioSlice/Docs/QuestFlow.md`
4. `Assets/_PortfolioSlice/Docs/Multiplayer.md`
5. this file

For implementation planning, then continue to:

- `Assets/_PortfolioSlice/Docs/MVP_SENIOR_ROADMAP.md`

## Repository root

```text
English Quest online/
|-- Assets/
|   |-- _PortfolioSlice/
|   |-- Photon/
|   |-- StarterAssets/
|   `-- Settings/
|-- Packages/
`-- ProjectSettings/
```

### Working rule

Most authored portfolio work should begin in:

```text
Assets/_PortfolioSlice
```

`Assets/Photon` and `Assets/StarterAssets` are dependencies, not the primary place for new slice features.

## Portfolio slice structure

```text
Assets/_PortfolioSlice/
|-- Art/
|-- Demo/
|-- Docs/
`-- SourceMirror/
```

### `Art`

Contains selected visual assets used by the slice:

- gameplay/UI prefabs
- sprites
- materials
- portfolio HUD prefab outputs

Relevant folder:

```text
Assets/_PortfolioSlice/Art/Prefabs/UI/PortfolioHUD/
```

Current HUD prefab outputs:

- `PortfolioHUD_Header.prefab`
- `PortfolioHUD_DemoBriefing.prefab`
- `PortfolioHUD_MultiplayerPlayers.prefab`
- `PortfolioHUD_Completion.prefab`
- `PortfolioHUD_DialoguePanel.prefab`

### `Demo`

Contains the active playable portfolio slice:

- the main scene
- quest-line ScriptableObjects
- local demo prefabs
- scene-specific data

Main scene:

```text
Assets/_PortfolioSlice/Demo/Scenes/PortfolioDemo.unity
```

Quest-line content:

```text
Assets/_PortfolioSlice/Demo/Data/QuestLines/
|-- 01_TeacherAda_FirstQuestline/
|-- 02_CoachBen_SecondQuestline/
|-- 03_GuideNora_ThirdQuestline/
`-- Shared_MVP_Catalogs/
```

### `Docs`

Contains the implementation narrative and the current source-of-truth documentation for the slice:

- architecture
- multiplayer rules
- content authoring guidance
- showcase flow
- verification checklists
- cleanup procedures
- senior roadmap

### `SourceMirror`

Contains the mirrored runtime/editor/test code used by the portfolio slice.

Important note:

`SourceMirror` still contains more systems than the active MVP currently showcases. Do not assume every mirrored feature is part of the active playable slice.

## Active playable slice

The current playable slice is:

- one open-world map;
- one local lesson chain;
- three NPCs;
- three mini-games;
- optional two-player multiplayer presence.

### NPC flow

1. `Teacher Ada`
2. `Coach Ben`
3. `Guide Nora`

Unlock order is enforced by quest-line prerequisites.

### Mini-game flow

1. `Line Match`
2. `Letter Ordering`
3. `Word Ordering`

Mini-games are tied to quest progression and are not meant to be freely launched out of sequence.

## Main scene hierarchy anchors

The exact scene can evolve visually, but these anchors matter architecturally:

- `Quest Manager`
- `Quest Line Registrar`
- `Dialogue Manager`
- `Quest Objective Event Bus`
- `Portfolio Demo HUD`
- `Network`
- player spawn points
- NPC objects
- mini-game stations

If one of these is missing or disconnected, the slice is likely broken at a structural level, not just visually.

## Main code areas

### Portfolio-specific scene and tooling

```text
Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/PortfolioDemo/
Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Editor/PortfolioDemo/
```

Key responsibilities here:

- scene HUD flow
- portfolio-specific presentation logic
- scene rebuild tooling
- validation tools
- progress/debug tools
- prefab materialization support

### Quest system

```text
Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/QuestSystem/
```

Owns:

- runtime quest state
- quest authoring types
- objective handling
- quest/NPC integration

### Dialogue

```text
Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Dialogue/
```

Owns:

- dialogue panel lifecycle
- dialogue content display
- interaction with player locking

### Interaction and player control

```text
Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Player/
Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/InteractionSystem/
```

Owns:

- interaction scanning
- player-world interaction contract
- prompt display inputs
- player-side lock/control transitions

### Mini-games

```text
Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/GamePlay/MiniGames/
```

Current relevant implementations:

- `LineMatch/`
- `DuolingoWordGame/`

This area now contains the shared mini-game lifecycle rollout work and the game-specific runtime logic.

### Networking

```text
Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Network/
Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/PortfolioDemo/Network/
```

Use this area for:

- session boot
- scene participation
- player spawn coordination
- ownership-safe local player setup
- multiplayer HUD/status sync

## Editor tools that matter

### Scene rebuild

```text
Tools > Portfolio Demo > Rebuild Test Scene
```

Use when:

- the demo scene needs to be reconstructed;
- baseline scene wiring was lost;
- you need a known structural starting point.

### HUD materialization

```text
Tools > English Quest > UI > Materialize Portfolio HUD Prefabs
```

Use when:

- the scene-authored HUD hierarchy was updated;
- prefab outputs must be refreshed from the scene;
- serialized HUD references must be rewired safely.

Important rule:

This tool supports scene-authored UI. It should not be turned into a generator that hides important visual structure from the hierarchy.

### Docs menu

```text
Tools > English Quest > Docs
```

Use to open the project proof and reference docs from inside Unity.

## Current authoring model

### Quest content

Quest lines are authored per NPC in dedicated folders.

Each line should keep together:

- quest-line asset
- related dialogue nodes
- mini-game config
- lesson-specific ScriptableObject content

This keeps ownership readable and reduces cross-project hunting.

### UI content

The preferred direction is now:

- visual hierarchy exists in the scene;
- reusable pieces can be stored as prefabs;
- runtime logic binds to authored UI instead of generating it invisibly.

This is especially important for:

- dialogue panel
- portfolio HUD
- completion panel
- mini-game root panels

## Verification documents

For day-to-day checks:

- `ManualVerificationChecklist.md`
- `SoloFirstVerification.md`
- `SoloRuntimeSignoff.md`
- `SoloRuntimeSignoff_Record_2026-07-28.md`

These documents matter because the slice has a hard rule:

the full Ada -> Ben -> Nora lesson chain must remain playable by one player even when multiplayer support is present.

## What is intentionally out of scope

The repository still contains traces of broader systems and prior production-oriented content, but the active slice does not currently try to showcase:

- large inventory systems;
- combat loops;
- production save backend flow;
- broad live-service feature sets;
- large-scale environment scope.

When in doubt, prefer preserving the clarity of the current slice over reactivating older mirrored systems.

## Safe working zones

Best areas for ongoing slice work:

- `Assets/_PortfolioSlice/Demo/`
- `Assets/_PortfolioSlice/Art/`
- `Assets/_PortfolioSlice/Docs/`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/PortfolioDemo/`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Editor/PortfolioDemo/`

Use extra care when editing:

- core quest system runtime
- generic mini-game lifecycle code
- networking ownership code
- mirrored code that may still be shared by more than one slice concern

## Suggested next development path

From a project-health perspective, the most valuable next steps are:

1. strengthen validator coverage for scene/runtime references;
2. increase automated tests around progression and multiplayer spawning/state;
3. continue the scene-authored UI cleanup across all mini-games;
4. improve final presentation consistency across HUD, dialogue, transitions, and completion moments;
5. extend authoring UX so new quest lines can be assembled faster and more safely.

## Short rule for future contributors

If you want to add something new, first ask:

- does it belong to the active MVP slice;
- can it be explained clearly in the scene and docs;
- does it preserve solo-first lesson completion;
- does it avoid hidden runtime magic;
- does it keep ownership and responsibilities readable.

If the answer is yes, it probably belongs in this project.
