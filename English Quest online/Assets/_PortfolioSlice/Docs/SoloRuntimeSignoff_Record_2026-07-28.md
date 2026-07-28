# English Quest Online - Solo Runtime Sign-off Record

## Purpose

Live one-player Unity verification record for the solo-first MVP rule from:

- `SoloRuntimeSignoff.md`

## Verification record

- Date: 2026-07-28
- Verified by: Kiril (manual local runtime pass)
- Unity version: 6000.3.3f1
- Branch or commit:
  - `main`
- Scene:
  - `Assets/_PortfolioSlice/Demo/Scenes/PortfolioDemo.unity`
- Session type:
  - one local player only

## Result

- [x] PASS
- [ ] FAIL

## Core proof

- [x] One player finished `Ada -> Ben -> Nora` in one session
- [x] No second player was required to activate any mission
- [x] No second player was required to start any mini-game
- [x] No second player was required to unlock the next NPC
- [x] Final completion panel appeared

## Runtime notes

### Boot

- Scene load: Passed without blocking issues.
- Player spawn: Local player spawned correctly.
- Camera follow: Camera followed the player correctly.
- Open-world HUD: HUD displayed correctly in open world.
- Solo-session wording: Solo-first wording read correctly and did not imply a partner requirement.

### Ada

- Dialogue: Opened and completed normally.
- Mini-game launch: `Line Match` launched correctly.
- Mini-game completion: Completed successfully.
- Unlock result: `Coach Ben` unlocked after completion.

### Ben

- Dialogue: Opened and completed normally.
- Mini-game launch: `Letter Ordering` launched correctly.
- Mini-game completion: Completed successfully.
- Unlock result: `Guide Nora` unlocked after completion.

### Nora

- Dialogue: Opened and completed normally.
- Mini-game launch: `Word Ordering` launched correctly.
- Mini-game completion: Completed successfully.
- Final completion panel: Appeared correctly at the end of the slice.

## Regressions found

- None

## Follow-up actions

- None

## Final sign-off statement

The current build satisfies the solo-first MVP rule: one player can complete the entire Ada -> Ben -> Nora lesson chain alone without any second-player requirement.
