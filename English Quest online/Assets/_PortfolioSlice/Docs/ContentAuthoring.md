# English Quest Online - Content Authoring Guide

## Purpose

This document explains how to extend the current portfolio slice without breaking its sequencing rules.

The MVP is intentionally narrow. New content should respect that narrowness.

## Authoring principle

For this project, authoring should follow this rule:

- one NPC
- one quest line
- one ordered lesson purpose
- one active mini-game per step

Do not add disconnected activities that bypass the lesson flow.

## Main authored assets

Core ScriptableObject layer:

- `QuestLineSO`
- `QuestLineRegistrySO`
- quest definitions linked through `QuestInfo` / `QuestDefinitionLink`
- dialogue nodes used by NPCs

Scene authoring layer:

- NPC object
- `NpcQuestGiver`
- dialogue references
- quest line reference
- mini-game station reference
- status indicator above the NPC

## How to add or adjust a lesson

### 1. Define the learning intent first

Before touching the scene, answer:

- what should the player learn here?
- what is the single skill of this lesson?
- which mini-game expresses that skill best?

Example:

- Ada = first letters
- Ben = missing letter inside words
- Nora = final answer or sentence logic

### 2. Update the quest line asset

Inside the quest line asset:

- keep the NPC ID correct
- keep the display name readable
- set the prerequisite line ID if this lesson should unlock later
- assign the correct quest assets in order

### 3. Update the NPC wiring

On the NPC object:

- assign the correct `Quest Line`
- keep the `Npc Id` aligned with the quest line
- only assign fallback dialogue if it is intentionally needed

If a field is empty and the current UX works without it, do not fill it just to make the inspector look busy.

### 4. Verify station gating

The mini-game station must only become playable when its quest step is active.

Always verify:

- the station is locked before the lesson starts
- the station opens during the correct step
- the station is no longer the active objective after completion

### 5. Validate the full chain

After editing content, test the full local chain:

1. Ada starts first
2. Ben stays locked
3. Ada completes
4. Ben unlocks
5. Nora stays locked
6. Ben completes
7. Nora unlocks
8. level completion appears after Nora

## Multiplayer authoring rule

Do not author lessons in a way that requires a second player just to make the MVP function.

Allowed:

- showing another player's current lesson in the HUD
- seeing another player in the world
- future optional co-op interactions

Not allowed for the current slice:

- "mission starts only if two players stand here"
- "lesson progress only works if both players accept it"
- "player B can block player A from finishing the path"

Solo completion must always stay valid.

## Safe extension ideas

Extensions that fit the current architecture well:

- better lesson text and clearer teaching prompts
- stronger final lesson data/content
- better NPC presentation and landmarking
- more polished feedback on quest state changes
- optional multiplayer showcase UI

Extensions that should be deferred until after the MVP showcase is stable:

- shared party quest progress
- networked co-op puzzle requirements
- large inventory or combat systems
- extra worlds/scenes
- backend persistence

## Debug workflow

Use the scene debug/progress controls when testing content:

- reset progress
- start from a selected quest
- jump to a selected step
- replay the full slice quickly

This is the fastest safe way to verify quest authoring changes without replaying the full chain every time.
