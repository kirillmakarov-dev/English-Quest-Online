# English Quest Online - Multiplayer Slice

## Scope

This project uses Photon Fusion to demonstrate a clean two-player prototype slice, not a full live-service multiplayer game.

Current networking goal:

- two players can join the same open-world scene;
- both players see each other;
- both players move with their own camera and local input;
- both players can progress through the same lesson chain independently.

Portfolio reading rule:

- the project must still make complete sense when demonstrated by only one player;
- the second player adds multiplayer credibility, but does not unlock the main lesson flow.

## Network mode

The project currently targets:

- `Photon Fusion`
- `GameMode.Shared`

This keeps the MVP lightweight:

- no dedicated server flow;
- no custom relay/backend layer;
- minimal architecture change from the existing local prototype.

## Session model

Scene-level network boot lives in the `Network` object and related runtime services.

Important parts:

- `GameNetworkManager`
- `EnglishQuestNetworkSceneManager`
- `NetworkLifecycleHandler`
- `NetworkAuthorityService`
- `PlayerSpawnCoordinator`

Responsibilities:

- create or join the Shared session
- manage scene participation
- spawn players at predefined spawn points
- keep local ownership and camera binding correct

## Player spawn model

Players should not be placed as permanent controllable scene objects for runtime multiplayer.

Instead:

- the network session starts;
- Fusion spawns the player prefab;
- the player is placed at an authored spawn point;
- the local owning instance enables movement, input, interaction, and camera;
- remote instances only render replicated state and collision presence.

## Ownership model

The player prefab uses a Fusion `NetworkObject`.

The local view is driven by:

- state authority in Shared Mode
- the local runner/player association

The practical result:

- only the owning player controls movement and input;
- only the owning player owns the local follow camera;
- remote players replicate transform, animation, and collision presence.

## Quest progression rule

The MVP keeps quest progress independent per player.

That means:

- one player starting Ada does not force the other player into Ada
- one player finishing Ben does not unlock Nora for the other player
- a solo player can complete the entire slice without a partner

This is intentional and should not be treated as a missing feature.

For this portfolio slice, independent progress is the safer and more readable behavior.

## Ownership rules

The slice follows these practical ownership rules:

- each player can only start dialogue for themselves;
- each player can only open mini-games for themselves;
- quest progress is local to the player who performed the action;
- the second player sees presence and status, but does not get forced into the same activity;
- if one player is busy in dialogue or a mini-game, the other player remains free in the open world.

This keeps the demo stable and easy to explain:

- multiplayer shows shared presence;
- lesson progression remains individually testable;
- solo play is always valid.

Reviewer expectation:

- in a solo session, the slice should still read as complete and intentional;
- in a shared session, the extra player should add visibility and social presence, not quest gating.

These rules are also surfaced in the in-game debug overlay so the ownership model can be explained quickly during a live review.

## Shared visibility polish

Even though quest progression is not shared, the project still exposes useful multiplayer feedback:

- remote player presence
- player list in the HUD
- per-player current lesson/progress status in the player panel
- per-player activity state such as dialogue, mini-game, lesson completion, and full level completion
- explicit solo-session versus shared-session labeling in the HUD and debug surfaces, so one-player testing never reads like a blocked co-op state
- co-session-safe player lookup for HUD and debug surfaces, so late joins and multi-peer session views resolve the correct player object more reliably

This gives the demo a stronger multiplayer read without changing the educational flow model.

## Optional co-op study circle

To give Shared Mode one intentional world mechanic without breaking the solo-first quest flow, the slice now includes an optional `Study Circle`.

Behavior:

- it lives in the open world as a shared marker;
- if two players stand inside it at the same time, the shared world moment becomes active;
- the player HUD, synced player status, and debug overlay reflect that state;
- it never unlocks quests, blocks quests, or changes lesson ownership rules.

Design intent:

- solo completion remains the main path;
- multiplayer gets one readable cooperative beat;
- the feature is easy to demonstrate in under a minute during a portfolio walkthrough.

Implementation guardrail:

- the `Study Circle` is not allowed to become a required quest objective for the MVP lesson chain;
- the portfolio validator treats optional co-op activity IDs such as `study_circle` as invalid quest targets for the core learning flow;
- this protects the slice from future content edits that would accidentally make a second player mandatory.

## Testing setup

Recommended validation path:

1. open `PortfolioDemo`
2. configure the Photon Fusion App ID
3. use Multiplayer Play Mode / Additional Editor Instance
4. run one local player plus one extra editor instance
5. confirm both players spawn at different spawn points
6. confirm both cameras stay local to their owning instance
7. confirm remote movement looks acceptable
8. confirm each player can still progress quests independently
9. confirm the player panel updates correctly when one player is in dialogue or a mini-game
10. confirm late-joining session views still resolve names and activity states correctly
11. confirm that starting with only one player still allows a full Ada -> Ben -> Nora run
12. confirm that a second player joining later never becomes required for mission activation

## Known non-goals for the current slice

The following are intentionally out of scope for the MVP:

- shared co-op objective completion
- synchronized dialogue state between players
- shared quest acceptance
- party-wide lesson locks/unlocks
- backend account progression

These can be added later, but they are not required for the portfolio value of the current project.
