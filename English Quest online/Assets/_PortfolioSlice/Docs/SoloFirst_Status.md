# English Quest Online - Solo-First Status

## Purpose

This file is the short status page for the main Sprint 3/4 MVP rule:

- one player must be able to enter the scene and complete the full lesson chain alone;
- no second player is required to activate, start, unlock, or complete any mission;
- optional multiplayer remains optional presentation value only.

Use this document when you need a fast answer to two questions:

1. What is already proven by code, tests, and validation?
2. What was confirmed by the live Unity pass?

## Current status

- Code guardrails: in place
- Editor validation guardrails: in place
- Documentation guardrails: in place
- Live Unity one-player pass: completed
- Final solo-first sign-off record: present

## What is already proven

### Quest chain ownership

The authored MVP chain is locked to:

1. `Teacher Ada`
2. `Coach Ben`
3. `Guide Nora`

and the unlock order is driven by quest-line prerequisites rather than player count.

Relevant systems:

- `QuestLineRegistrar`
- `QuestLineRegistrySO`
- `NpcQuestGiver`

### Mini-game gating

The required lesson activities remain:

- `line_match`
- `letter_ordering`
- `word_ordering`

Optional co-op targets such as `study_circle` are treated as invalid for the core learning path by the portfolio validator.

### Solo-first documentation and validation

The project now has dedicated proof-path documents:

- `SoloFirstVerification.md`
- `SoloRuntimeSignoff.md`
- `SoloRuntimeSignoff_Record_Template.md`
- `ManualVerificationChecklist.md`

These documents are wired into the portfolio validator so they cannot silently disappear from the repository without creating warnings.

### Tested evidence

The current codebase has edit-mode coverage that protects the solo-first contract in these areas:

- first NPC is available without a second player;
- second and third NPCs stay locked because of quest order, not player count;
- mini-game launch can happen in solo conditions;
- required quest-line assets preserve the Ada -> Ben -> Nora chain;
- required quest definitions do not hide direct prerequisite quest gates inside the MVP lines.

## Live Unity evidence

The dated runtime record confirms the following in the current scene:

- one local player spawns and controls correctly in `PortfolioDemo`;
- the player can complete `Ada -> Ben -> Nora` in one session;
- all three mini-games open correctly in the live scene;
- the final completion panel appears;
- no HUD or dialogue text implies waiting for a second player.

## Required final proof path

Run these in order:

1. close any open Unity editor instance if you want to use batchmode automation
2. in Unity, you can open the full proof pack from:
   `Tools > English Quest > Docs > Open Solo-First Proof Pack`
   this opens the status page, the fast verification doc, the live runtime sign-off doc, the dated record template, and the batchmode preflight script in one place
3. optional automated guardrail pass:
   `powershell -ExecutionPolicy Bypass -File .\scripts\Run-SoloFirstUnityEditMode.ps1`
   this preflight now checks the committed solo-first quest chain, the committed `PortfolioDemo` scene wiring and HUD copy, the committed quest/registry/network assets, and the required reviewer-facing docs before the live Unity pass
4. `SoloFirstVerification.md`
5. `SoloRuntimeSignoff.md`
6. `Tools > English Quest > Docs > Create Solo Runtime Record From Template`
7. complete the generated `SoloRuntimeSignoff_Record_YYYY-MM-DD.md`
8. if needed, finish with `ManualVerificationChecklist.md`

## Completion rule

Sprint 3/4 can be treated as fully solo-first verified only when both are true:

1. the code/tests/validator remain green;
2. a real dated solo runtime sign-off record exists and confirms that one player completed the full slice alone.
