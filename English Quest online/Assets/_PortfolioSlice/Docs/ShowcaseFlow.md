# English Quest Online - Showcase Flow

## Purpose

This document is the short demo runbook for presenting the portfolio slice to:

- recruiters
- leads
- technical interviewers
- collaborators joining the project later

The goal is to show the strongest parts of the slice in 3 to 5 minutes without drifting into setup noise.

## Demo message

The project should read as:

- a small but intentional open-world learning prototype
- a data-driven quest pipeline
- a clean local gameplay flow
- a readable Photon Fusion Shared multiplayer slice
- a portfolio project where solo play stays valid even when multiplayer is present

Presentation guardrail:

- if the slice is shown in a solo run, the viewer should still understand the full product idea;
- if the slice is shown in a two-player run, the second player should strengthen the presentation, not explain missing solo behavior.

## Core talking points

When presenting the project, keep returning to these points:

1. The quest flow is strict, readable, and data-driven.
2. Each player owns their own lesson progression.
3. Multiplayer adds presence and one optional shared world beat, but never blocks the main path.
4. Validation, debug tooling, and content organization were built as part of the slice, not added at the very end.

## Recommended 3-5 minute walkthrough

### Part 1 - Open the scene

Open:

`Assets/_PortfolioSlice/Demo/Scenes/PortfolioDemo.unity`

Say:

"This is a focused MVP slice: one map, three NPCs, three learning stages, and a Photon Fusion Shared session."

## Part 2 - Show the solo-first loop

In a single-player run:

1. Spawn into the scene
2. Walk to Teacher Ada
3. Start the first lesson
4. Complete the first mini-game
5. Show that Coach Ben unlocks next

Say:

"The main path is always solo-playable. Multiplayer is optional presence, not a requirement for activating missions."

Also make explicit if needed:

"A second player can join later, but the lesson chain never waits for a partner."

## Part 3 - Show the quest chain

Continue the flow:

1. Start Coach Ben
2. Complete the missing-letter lesson
3. Unlock Guide Nora
4. Complete the final lesson
5. Reach the completion state

Say:

"The slice moves from letters to words to a final structured answer, and the level ends in a clear completion state."

## Part 4 - Show multiplayer value

In a two-instance run:

1. Spawn both players at separate spawn points
2. Show that both cameras remain local
3. Show that one player can talk to an NPC without forcing the other into the same flow
4. Show the player panel and debug overlay
5. Walk both players into the optional `Study Circle`

Say:

"Quest ownership stays local, but multiplayer still has visible design value through shared presence, synced status, and one optional shared world moment."

Add explicitly if needed:

"Even if we never enter the Study Circle, the full lesson chain still works from start to finish for one player."

## Part 5 - Show engineering discipline

Open or mention:

- `Tools > English Quest > Validate Portfolio Demo`
- the debug overlay
- the slice docs in `Assets/_PortfolioSlice/Docs`

Say:

"This is not just a playable prototype. It also has validation, debug visibility, rebuildable scene composition, and slice-specific documentation."

## What to avoid during the demo

Avoid spending time on:

- Photon account setup
- package import history
- legacy systems removed from the slice
- long explanations of every class
- features that are intentionally out of scope

The showcase should stay on the value of the current slice.

## If only 90 seconds are available

Use this compressed path:

1. Spawn into the world
2. Show Teacher Ada and the first mini-game
3. Mention the locked-then-unlocked NPC sequence
4. Show the player panel or debug overlay
5. State that multiplayer is optional and per-player
6. Mention validator and docs

## Verification checklist before a live demo

Fastest option:

- if you only need to confirm the core MVP rule before a short demo, run `SoloFirstVerification.md` first;
- then run `SoloRuntimeSignoff.md` for the live one-player scene pass before moving to the full checklist;
- then return to this checklist only if you also need the wider shared-session and presentation pass.

Before presenting, verify:

1. the correct scene is open
2. Photon App ID is configured
3. the local player spawns correctly
4. the first NPC is available
5. mini-game UI opens correctly
6. completion UI still works
7. if using two instances, both players join the same room
8. if using two instances, the `Study Circle` reacts when both players stand inside
9. if the `Study Circle` is ignored, the solo lesson path still behaves exactly the same
10. if only one player is present for the whole demo, the project still reads as complete and intentional

## Showcase summary

If the slice is shown correctly, the viewer should leave with four conclusions:

1. the project has a clear gameplay loop
2. the architecture is controlled and explainable
3. multiplayer ownership rules are intentional
4. the prototype is small by scope, but senior in presentation and discipline
