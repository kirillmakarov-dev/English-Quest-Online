# Procedure 15 - MVP Name Normalization and Scope Table

## Goal

Align the visible project naming with the current MVP slice:

- one open world map
- quest lines tied to the selected mini-games
- multiplayer via Photon Fusion
- only the mini-games that are part of the current flow

Internal C# identifiers such as `WordGame*`, `DuolingoWordGame`, and `LetterConnection*` are kept for compatibility. This pass normalizes the public-facing labels, menu paths, and docs so they match the MVP language.

## Final Scope Table

| Что оставляем | Что удаляем | Что переписываем |
|---|---|---|
| Open world map, NPCs, quest system, reward flow, Photon Fusion networking | Legacy demo flows that do not affect world/quest/multiplayer flow | Public labels and menu paths so they use MVP names instead of old demo names |
| `LineMatch` | Unused mini-games outside the chosen quest lines | `Word Game` -> `Word Ordering` in editor-facing text |
| `Letter Ordering` | `Drawing`, `SpeakAloud`, `SentenceCompletion`, `ButtonPuzzle`, `JumpCounter`, `Phonics` | `Letter Connection` -> `Letter Ordering` in create/menu paths |
| `Word Ordering` | `DailyQuests` and other removed questline/content tails | `MiniGamesWordGame` / `MiniGamesLetterConnection` constants -> MVP-aligned names |
| Quest data needed for the three selected games | Inventory/action-bar tails that are not needed for rewards or quests | Docs and inspector labels that still mention the old slice |

## Result

The live slice now reads like the intended portfolio MVP:

- one open world
- several NPC quest givers
- quest lines built from the three selected mini-games
- reward flow tied to quest completion
- multiplayer foundation ready for expansion

The implementation keeps the stable runtime code intact and only changes what the player or editor sees.
