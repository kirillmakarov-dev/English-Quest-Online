# English Quest Online - Solo-First Verification

## Purpose

This is the fastest manual sign-off path for the main MVP rule:

- one player must be able to complete the full lesson chain alone;
- a second player must never be required to activate, start, or complete a mission;
- optional co-op may add presentation value, but it must not change progression ownership.

Use this document when you do not need the full checklist and only want to prove the most important Sprint 3/4 contract in a few minutes.

Fastest editor path for the full proof flow:

- `Tools > English Quest > Docs > Open Solo-First Proof Pack`

Recommended order:

1. use this file for the fastest yes/no check
2. then run `SoloRuntimeSignoff.md` for the live one-player scene pass
3. record the result with `SoloRuntimeSignoff_Record_Template.md`
4. only after that move to `ManualVerificationChecklist.md` if shared-session and presentation checks are needed

## Fast 5-minute proof

Scene:

`Assets/_PortfolioSlice/Demo/Scenes/PortfolioDemo.unity`

### Pass A - Solo proof

Run the scene with only one local player.

Expected result:

1. Teacher Ada is available first.
2. Coach Ben stays locked until Ada is completed.
3. Guide Nora stays locked until Ben is completed.
4. The player can complete:
   - Ada -> Line Match
   - Ben -> Letter Ordering
   - Nora -> Word Ordering
5. The final completion panel appears without any second player joining.

Hard fail conditions:

- any mission refuses to start because no partner is present;
- any UI text implies that the player must wait for another player;
- any optional co-op element blocks the next lesson.

### Pass B - Shared proof

Only after Pass A is green, run a two-instance session.

Expected result:

1. Both players join the same room and spawn correctly.
2. One player can still complete the lesson chain without the other participating.
3. The second player can stay idle and does not become part of the unlock chain.
4. If both players use the `Study Circle`, it behaves like an optional shared moment only.
5. Leaving the `Study Circle` does not affect quest availability.

Hard fail conditions:

- the second player is needed to unlock or finish a lesson;
- talking to an NPC on one player forces the other player into the same state;
- the optional shared moment changes the quest chain result.

## Sign-off statement

Sprint 3/4 can be treated as solo-first verified only if both statements are true:

1. A complete Ada -> Ben -> Nora run works in a single-player session.
2. A shared session adds visibility and optional co-op only, without becoming mission gating.

## If a regression appears

Check these systems first:

- `QuestLineRegistrySO` and quest prerequisites
- `NpcQuestGiver` line binding
- mini-game station `gameId` and fallback config wiring
- `Portfolio Demo HUD` wording
- `PortfolioOptionalCoopStudyCircle`
- `PortfolioDemoValidationAnalyzer`

If the regression is real, it should be reflected in one of three places:

- validator output
- edit-mode tests
- the live Unity scene during the two passes above
