# English Quest Online - Solo Runtime Sign-off

## Purpose

This is the final live Unity check for the main Sprint 3/4 rule:

- one player can enter the scene and finish the full MVP lesson chain alone;
- no second player is required to activate, start, unlock, or complete any mission;
- optional multiplayer elements stay optional during the full run.

Use this after code tests and validator checks are already green.

If you need the short evidence summary first, read:

- `SoloFirst_Status.md`

Fastest editor path for the full proof flow:

- `Tools > English Quest > Docs > Open Solo-First Proof Pack`

Optional automated pre-check before the live runtime pass:

- close any open Unity editor instance
- run:
  `powershell -ExecutionPolicy Bypass -File .\scripts\Run-SoloFirstUnityEditMode.ps1`

After the live pass is done, copy:

- `SoloRuntimeSignoff_Record_Template.md`

and save the completed record next to this file so the result stays attached to the repository.

Fastest editor path:

- `Tools > English Quest > Docs > Create Solo Runtime Record From Template`

Scene:

`Assets/_PortfolioSlice/Demo/Scenes/PortfolioDemo.unity`

## Runtime pass

Run Play Mode with one local player only.

### Boot

- [ ] The scene loads without blocking errors
- [ ] The player spawns correctly
- [ ] The camera follows the player correctly
- [ ] Open-world HUD is visible
- [ ] The player panel reads as a solo session

### Lesson chain

- [ ] Teacher Ada is available immediately
- [ ] Coach Ben is locked before Ada is finished
- [ ] Guide Nora is locked before Ben is finished
- [ ] No lesson station can be started out of order

### Ada

- [ ] Ada dialogue opens
- [ ] Dialogue can be finished normally
- [ ] Line Match opens correctly
- [ ] Line Match can be completed
- [ ] Ada's lesson finishes
- [ ] Coach Ben unlocks

### Ben

- [ ] Ben dialogue opens
- [ ] Letter Ordering opens correctly
- [ ] Letter Ordering can be completed
- [ ] Ben's lesson finishes
- [ ] Guide Nora unlocks

### Nora

- [ ] Nora dialogue opens
- [ ] Word Ordering opens correctly
- [ ] Word Ordering can be completed
- [ ] Final completion panel appears

## Hard fail conditions

- any NPC refuses to progress because no second player is present
- any mini-game refuses to launch because no second player is present
- any UI text suggests waiting for a partner
- any optional co-op object interferes with the solo chain

## Sign-off

The solo runtime pass is complete only when:

1. `Ada -> Ben -> Nora` is finished by one player in one session
2. the final completion panel appears
3. no multiplayer-only requirement appears anywhere in the flow
