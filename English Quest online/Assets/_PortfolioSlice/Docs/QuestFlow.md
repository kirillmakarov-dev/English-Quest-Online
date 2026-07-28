# English Quest Online - Quest Flow

## Goal of the slice

The portfolio demo is a guided learning path built around three NPCs.

Each NPC represents one lesson stage:

1. Teacher Ada
2. Coach Ben
3. Guide Nora

The player always understands what comes next:

- talk to the current NPC;
- complete the assigned mini-game;
- unlock the next lesson;
- finish the full level after the final lesson.

## Current lesson order

### Lesson 1 - Teacher Ada

Focus:

- first letter pairing;
- simple recognition;
- first contact with the learning flow.

Mini-game:

- Line Match

Outcome:

- the player learns the first two letters;
- Coach Ben becomes available for that same player.

### Lesson 2 - Coach Ben

Focus:

- missing letter recognition;
- vocabulary reinforcement;
- using letters inside full words.

Mini-game:

- Letter Ordering

Outcome:

- the player reinforces vocabulary through word completion;
- Guide Nora becomes available for that same player.

### Lesson 3 - Guide Nora

Focus:

- final structured language step for the MVP;
- choosing or assembling the correct final answer in the last lesson.

Mini-game:

- Word Ordering

Outcome:

- the learning path is complete;
- the level enters the completed state;
- replay/reset options become available.

## State progression per player

Quest progression is per-player, not global.

That means:

- one player can still be on Teacher Ada;
- another player can already be on Guide Nora;
- neither player blocks the other;
- solo play remains fully valid.

This is the correct MVP behavior for a portfolio prototype. Multiplayer presence is demonstrated, but the educational flow remains stable and individually testable.

That also means a second player may join later without becoming part of the unlock logic for the first player. The lesson chain must already work in a complete and readable way for a single player session.

## Optional multiplayer moment

Outside the quest chain, the scene also includes an optional `Study Circle`.

This is not part of the lesson unlock path.

Its job is only to show that:

- two players can react to the same shared world marker;
- the demo has one intentional co-op beat;
- the main learning flow still remains solo-first.

If the circle is never used, the full MVP quest chain must still play exactly the same way.

This is also treated as a content safety rule, not just a design preference:

- optional co-op activities must stay outside the mandatory lesson chain;
- the validator is expected to fail if an authored quest objective tries to use the `Study Circle` as a required progression step.

## Unlocking rules

The sequence is strict for each player:

1. Teacher Ada is available first
2. Coach Ben is locked until Ada is completed
3. Guide Nora is locked until Ben is completed
4. The final completion state appears only after Nora is completed

Mini-games are also sequential:

- a player cannot start later activities early;
- a completed mini-game is not meant to be replayed as the active quest step;
- quest availability and station availability must always agree.

## Runtime quest states

The quest system uses explicit local states:

- `REQUIREMENTS_NOT_MET`
- `CAN_START`
- `IN_PROGRESS`
- `CAN_FINISH`
- `FINISHED`

These states drive:

- NPC availability
- station availability
- objective progress
- completion gating
- multiplayer status text in the HUD

## UX expectation during the flow

When the slice is behaving correctly:

- only the correct NPC feels available
- the player cannot accidentally skip ahead
- mini-games open cleanly on top of the world
- cursor/input locking behaves consistently
- the final level completion screen clearly closes the loop
- the presence or absence of a second player does not change whether the active lesson can be started

## Demo expectation for reviewers

A reviewer should be able to understand the full project loop in one short session:

1. enter the scene
2. meet Ada
3. finish the first educational task
4. see Ben unlock
5. finish the second educational task
6. see Nora unlock
7. complete the final lesson
8. reach a clear end state

If a second player is shown afterward, that should read as an extra multiplayer layer on top of an already complete solo loop, not as a prerequisite for understanding the slice.

That makes the slice easy to read as both a game system demo and a portfolio case study.
