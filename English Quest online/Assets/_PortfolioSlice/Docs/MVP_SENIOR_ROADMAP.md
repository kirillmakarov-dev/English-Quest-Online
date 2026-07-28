# English Quest Online - MVP to Senior Roadmap

## Purpose

This document is the working roadmap for taking the current English Quest portfolio slice from a solid MVP to a senior-looking showcase project.

It exists to answer four practical questions:

1. What is already finished and stable enough to keep?
2. What still needs to be upgraded for a stronger portfolio presentation?
3. In what order should the upgrades happen?
4. What does "done" mean for each phase?

This roadmap is intentionally scoped to the current prototype:

- one open-world map;
- three NPC-driven quest lines;
- three educational mini-games;
- Photon Fusion Shared Mode for two players;
- portfolio-first presentation rather than commercial-game scope.

## Current baseline

The current project already demonstrates:

- a playable open-world demo scene;
- sequential NPC quest-line unlocking;
- three integrated mini-games;
- local dialogue, interaction, and player locking flow;
- Photon Fusion two-player presence in Shared Mode;
- per-player quest progression by design;
- project cleanup, naming normalization, and a portfolio-facing README.

This means the project is no longer in "prototype chaos." It is already a focused MVP slice. The next work is about reliability, scale-readiness, and presentation quality.

## Target outcome

The target is not "more features."

The target is a portfolio slice that clearly shows:

- reliable game flow;
- maintainable architecture;
- controlled multiplayer ownership;
- content authoring discipline;
- developer tooling and validation;
- a polished, easy-to-demo presentation.

## Delivery strategy

Work in order. Do not skip ahead to larger multiplayer or polish tasks before the core MVP foundation is hardened.

Priority order:

1. Save/Load
2. Game Flow State
3. Final completion screen
4. Validation Tool
5. Debug Overlay
6. One real co-op mechanic
7. Late-join and authority polish
8. Portfolio docs and showcase pass

## Phase overview

| Phase | Focus | Status | Outcome |
|---|---|---|---|
| Phase 0 | Current MVP baseline | Done | Playable portfolio slice with local quest flow and Shared multiplayer presence |
| Phase 1 | Clean production prototype | Implemented in code, pending Unity verification | Reliable progression, explicit flow states, and proper level completion |
| Phase 2 | Engineering upgrade | Implemented in code, pending Unity verification | Validation, debugging tools, and better failure handling |
| Phase 3 | Multiplayer showcase upgrade | Planned | Multiplayer becomes meaningful, not just visible |
| Phase 4 | Presentation and portfolio pass | Planned | Clear demonstration package for recruiters, leads, and reviewers |

## Progress log

### 2026-07-28

- Roadmap created inside the project docs.
- README and PROJECT_MAP linked to the roadmap for clearer navigation.
- Sprint 1 implemented in code:
  local quest progress persistence, explicit flow coordination, final completion UI.
- Sprint 2 implemented in code:
  editor validation entry point, runtime debug overlay, additional setup warnings.
- Manual Unity verification still required after the coding pass:
  play through the slice, inspect scene wiring, and confirm there are no inspector errors.

## Phase 1 - Clean production prototype

### Goal

Turn the current demo into a stable vertical slice with persistence, explicit gameplay states, and a real ending.

### Tasks

#### 1. Save/Load progression

Persist at minimum:

- unlocked quest line;
- completed mini-games;
- current NPC availability;
- full level completion state.

Recommended user-facing support:

- automatic save on meaningful progress;
- reset progress action for demo use;
- safe load on scene start.

Definition of done:

- a player can complete part of the learning path;
- the scene can be reloaded;
- progress is restored correctly;
- completed activities remain completed;
- locked/unlocked NPC states restore correctly.

#### 2. Explicit Game Flow State

Introduce a formal flow model for at least:

- `OpenWorld`
- `Dialogue`
- `MiniGame`
- `QuestCompleted`
- `LevelCompleted`
- `NetworkConnecting`
- `NetworkInSession`

Definition of done:

- cursor state is controlled through the flow state, not scattered logic;
- player movement/input lock behavior is predictable;
- gameplay UI opens and closes only through valid state transitions;
- no overlapping interaction and mini-game activation paths remain.

#### 3. Proper final completion

After the Guide Nora line is finished:

- show a final completion screen;
- confirm that the learning path is complete;
- provide replay or restart progress option.

Definition of done:

- the level has a clear end state;
- the player understands that the prototype loop is complete;
- the demo no longer ends in an ambiguous "nothing happens next" state.

### Exit criteria for Phase 1

Phase 1 is complete only when:

- progress survives reload;
- flow states are explicit and stable;
- the last quest line ends in a real completion screen;
- the scene behaves like a deliberate demo, not a temporary test setup.

## Phase 2 - Engineering upgrade

### Goal

Show senior-minded development discipline through validation, debugging, and safer system boundaries.

### Tasks

#### 1. Content validation tool

Add an editor entry point such as:

```text
Tools/English Quest/Validate Portfolio Demo
```

The validator should check at minimum:

- duplicate IDs;
- missing object references;
- broken prerequisite chains;
- NPCs without valid quest-line wiring;
- mini-games not mapped to the expected objective.

Definition of done:

- one editor action validates the portfolio slice;
- errors are reported clearly enough to fix without tracing runtime failures;
- content issues are caught before play mode.

#### 2. Developer debug overlay

Expose runtime information such as:

- local player / remote player;
- network mode and authority;
- current quest line;
- current quest step;
- NPC state;
- active mini-game;
- session or room information.

Optional but valuable:

- complete current step;
- reset progress;
- teleport to NPC.

Definition of done:

- a developer can understand the current runtime state in seconds;
- basic flow debugging does not require deep inspector hunting.

#### 3. Error handling cleanup

Make failure cases explicit and safe:

- missing dialogue assignment;
- missing objective assignment;
- already completed mini-game;
- invalid or busy interaction point;
- missing network or profile configuration.

Definition of done:

- bad setup paths fail clearly;
- common mistakes do not silently break the slice;
- the project is easier to hand off or revisit later.

#### 4. Separate runtime configuration from content data

Keep these concerns distinct:

- quest data;
- dialogue data;
- mini-game lesson data;
- network config;
- scene bindings.

Definition of done:

- content changes do not require runtime rewiring unless intended;
- scene-specific composition is easy to inspect;
- future quest lines can be added with less risk.

### Exit criteria for Phase 2

Phase 2 is complete only when:

- validation exists and is useful;
- runtime state is inspectable through a debug surface;
- obvious content and setup failures are handled intentionally;
- data boundaries are clearer than in a pure prototype.

## Phase 3 - Multiplayer showcase upgrade

### Goal

Upgrade multiplayer from "two players exist in the same room" to "multiplayer adds visible design value."

### Tasks

#### 1. Shared social presence

Extend the current player panel with meaningful state such as:

- `In Dialogue`
- `In Mini-Game`
- `Finished Lesson 1`
- `Completed Level`

Definition of done:

- the other player's activity is visible at a glance;
- multiplayer feels present even when players are not standing next to each other.

#### 2. One real co-op mechanic

Add one small but intentional cooperative interaction. Good MVP options:

- both players must enter a zone to unlock a shared activity;
- one player finishing a lesson updates a visible team status;
- a ready-check before the final activity;
- a shared trigger that requires both players to confirm.

Definition of done:

- Photon Fusion is demonstrating gameplay value, not only transform sync;
- the multiplayer slice can be described as a mechanic, not just a technical feature.

#### 3. Formalize network ownership rules

Write down and enforce:

- who can start an NPC dialogue;
- who can start a mini-game;
- what progress is local versus shared;
- what the second player sees while the first player is busy in an activity.

Definition of done:

- ownership behavior is predictable;
- interaction conflicts do not feel random;
- the multiplayer rules can be explained clearly in docs and during a demo.

#### 4. Late-join handling

Verify and polish:

- join after the first player already progressed;
- player status panel sync;
- NPC states and activity availability;
- spawn, camera, and UI behavior for the joining player.

Definition of done:

- a late-joining player enters a stable session;
- no major presentation or state sync issues appear on join.

### Exit criteria for Phase 3

Phase 3 is complete only when:

- multiplayer communicates player state;
- at least one cooperative mechanic exists;
- ownership rules are intentional and documented;
- late join no longer feels like an edge case.

## Phase 4 - Presentation and portfolio pass

### Goal

Make the project easy to understand, easy to review, and easy to demo in a few minutes.

### Tasks

#### 1. Unify quest-line quality

All three NPC lines should present:

- a clear learning goal;
- short onboarding text;
- clear completion text;
- consistent tone of voice.

#### 2. Expand project documentation

Recommended docs:

- `Docs/Architecture.md`
- `Docs/QuestFlow.md`
- `Docs/Multiplayer.md`
- `Docs/ContentAuthoring.md`

These can live either in the existing slice docs area or in a root docs folder if the project later grows beyond one scene.

#### 3. Prepare a showcase flow

A 3-5 minute demo should clearly show:

- open world;
- NPC progression;
- three learning stages;
- multiplayer join and presence;
- final completion.

Definition of done:

- someone new to the project can understand what it demonstrates without a long explanation;
- the showcase can be repeated consistently.

### Exit criteria for Phase 4

Phase 4 is complete only when:

- the project reads clearly as a portfolio case study;
- docs support both technical review and continued development;
- the demo flow is strong enough for a recruiter, lead, or technical interviewer.

## Recommended sprint breakdown

### Sprint 1

- Save/Load
- Game Flow State
- Final completion screen

### Sprint 2

- Validation Tool
- Debug Overlay
- Error handling cleanup

### Sprint 3

- One co-op mechanic
- Late-join and authority polish
- Multiplayer status polish

### Sprint 4

- Architecture and flow docs
- README pass
- Showcase polish

## Minimum bar for a senior-looking slice

If time is limited, the minimum upgrade set is:

- Save/Load;
- explicit state machine for the main flow;
- validation tool;
- debug overlay;
- one meaningful co-op multiplayer mechanic;
- clean final UX and documentation.

If those six pieces are done well, the project will read much closer to a senior-minded portfolio slice than to a simple Unity prototype.

## Working rules for future sessions

When continuing this roadmap, follow these rules:

1. Do not expand scope before the current phase is stable.
2. Prefer system quality over more content.
3. Validate every new multiplayer behavior in a two-player session.
4. Keep docs updated when a phase meaningfully changes the project architecture.
5. If a scene rebuild can overwrite manual wiring, move the source of truth into the builder, prefabs, or ScriptableObjects.

## Immediate next action

The best next implementation step is Phase 1:

```text
Save/Load
-> explicit Game Flow State
-> final completion screen
```

That sequence gives the highest quality gain for the lowest scope risk and should be treated as the current top-priority roadmap block.
