# English Quest Online - Manual Verification Checklist

## Purpose

This checklist is the last manual pass for the portfolio slice after code, tests, and editor validation are already green.

Use it before:

- a portfolio recording
- a recruiter demo
- a technical interview walkthrough
- a release-style milestone commit for the current slice

## Test environment

Verify in:

1. single-player Play Mode
2. two-player Multiplayer Play Mode with an Additional Editor Instance

Verification order:

1. complete the single-player pass first
2. only then verify the shared-session pass

The slice is considered healthy only if the solo pass already succeeds before a second player is introduced.

Shortcut:

- if you only need a fast yes/no answer for the MVP contract, use `SoloFirstVerification.md` first;
- after that, use `SoloRuntimeSignoff.md` for the direct one-player scene pass;
- record the final solo pass result with `SoloRuntimeSignoff_Record_Template.md`;
- use this full checklist when you need the broader Sprint 3/4 sign-off for shared session behavior, tooling, and presentation.

Scene:

`Assets/_PortfolioSlice/Demo/Scenes/PortfolioDemo.unity`

## Pass 1 - Single-player core flow

### Scene boot

- [ ] Scene opens without missing-reference noise
- [ ] Local player spawns correctly
- [ ] Camera follows the local player
- [ ] Open-world HUD is visible in the world state
- [ ] Player panel clearly reads as solo session

### Quest chain

- [ ] Teacher Ada is the first available NPC
- [ ] Coach Ben is locked before Ada is completed
- [ ] Guide Nora is locked before Ben is completed
- [ ] Mini-games cannot be started out of order

### Lesson 1

- [ ] Ada dialogue opens correctly
- [ ] Cursor/input state behaves correctly during dialogue
- [ ] Line Match opens on top of the world
- [ ] Line Match can be completed normally
- [ ] Ada can finish the lesson after the mini-game
- [ ] Coach Ben unlocks after Ada

### Lesson 2

- [ ] Ben dialogue opens correctly
- [ ] Letter Ordering opens correctly
- [ ] Letter Ordering can be completed normally
- [ ] Ben can finish the lesson after the mini-game
- [ ] Guide Nora unlocks after Ben

### Lesson 3

- [ ] Nora dialogue opens correctly
- [ ] Word Ordering opens correctly
- [ ] Word Ordering can be completed normally
- [ ] Final completion panel appears after the lesson is finished
- [ ] Replay/reset flow still works

### Solo-first multiplayer guardrail

- [ ] The player can complete the full slice alone
- [ ] No quest requires a second player to activate
- [ ] No quest requires a second player to complete
- [ ] Optional co-op text does not imply blocking progression

## Pass 2 - Shared multiplayer flow

This pass must confirm added multiplayer value, not rescue the main quest path:

- use it only after the solo pass above is green;
- do not treat a second player as a requirement for activating or unblocking any mission.

### Session boot

- [ ] Both instances join the same Photon Fusion room
- [ ] Each player spawns at a different spawn point
- [ ] Each instance keeps its own camera
- [ ] Players can see each other move
- [ ] Player collision works

### Ownership rules

- [ ] One player can start dialogue without forcing the other into it
- [ ] One player can open a mini-game without forcing the other into it
- [ ] Quest progress stays per-player
- [ ] One player's lesson completion does not auto-complete the other player's lesson

### Shared visibility

- [ ] Player panel shows both players
- [ ] Local player is identified clearly
- [ ] Player statuses update when one player enters dialogue
- [ ] Player statuses update when one player enters a mini-game
- [ ] Solo/shared wording remains understandable

### Optional Study Circle

- [ ] Study Circle is visible in the scene
- [ ] Study Circle does not interfere with the quest path
- [ ] Ignoring the Study Circle entirely does not change the result of the full solo quest run
- [ ] When both players stand inside it, the shared moment becomes active
- [ ] HUD/debug surfaces reflect the active shared moment
- [ ] Leaving the circle returns it to a non-active state

## Pass 3 - Debug and tooling

### Debug overlay

- [ ] Debug overlay toggles correctly
- [ ] Overlay shows local object ownership
- [ ] Overlay shows session player list
- [ ] Overlay shows ownership rules
- [ ] Overlay shows optional co-op state

### Validator

- [ ] `Tools > English Quest > Validate Portfolio Demo` runs successfully
- [ ] No unexpected errors are reported
- [ ] Any warnings are understood and acceptable
- [ ] Validator would reject any accidental attempt to author optional co-op as a required quest objective

## Pass 4 - Presentation quality

- [ ] Top-of-screen instructions are readable
- [ ] Briefing text is understandable in solo mode
- [ ] Briefing text is understandable in shared mode
- [ ] Completion panel text reads clearly
- [ ] The world feels readable enough for a quick portfolio demonstration

## Exit rule

The current Sprint 3/4 slice is ready to be treated as stable when:

- the full solo path passes
- the shared session path passes
- Study Circle remains optional
- debug and validator tooling still work
- nothing in the UI suggests that a second player is required for mission activation
- the shared-session pass adds confidence to the demo instead of hiding a solo-flow dependency
