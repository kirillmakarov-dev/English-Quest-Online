# Procedure 06 - Trimmed to Line Match and Duolingo Only

## Goal

This procedure locked the portfolio slice down to the systems we actually want to show in a recruiter-facing demo:

- Service Locator
- Photon Fusion
- quest flow, dialogue, and save architecture
- the selected mini-games:
  - Line Match
  - Letter Ordering
  - Word Ordering

Everything else was treated as supporting baggage and moved out of the live slice so the project stays focused.

## What Changed

### Kept in the live slice

- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/GamePlay/MiniGames/DuolingoWordGame`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/GamePlay/MiniGames/LineMatch`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/QuestSystem/Objectives/MiniGames`
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Compatibility/PortfolioCompatStubs.cs`

### Moved out to archive

The following groups were moved to the archive area because they are not part of the portfolio demo:

- inventory-specific gameplay and UI code
- button puzzle and jump counter mini-games
- museum, scales, drawing, speak aloud, sentence completion, and word platform systems
- quest authoring/editor tooling that only exists to support the removed systems
- extra visual helpers tied to removed gameplay
- old mini-game data folders that are no longer part of the final slice

Archive root:

- `C:\Portfolio Projects\English-Quest-Online\Archive\PortfolioTrimmed`

## Current Runtime Shape

The slice is now structured around a much smaller live surface:

- `MiniGameWorldLaunchHost` only exposes the mini-game bootstraps that still matter.
- `QuestMiniGameBinder` remains as the glue layer for connecting quest steps to mini-game implementations.
- `PortfolioCompatStubs` keeps a few minimal compatibility types alive so the remaining quest and save code can still compile against older references.

This means the portfolio slice can still communicate the core architecture, but without dragging in the unrelated systems we do not want to present.

## Notes

- I have not run the Unity editor or a full compile from this pass.
- The source tree is now much smaller, but there are still a few legacy references in comments and save documentation that may need a final cleanup pass.
- Inventory is intentionally excluded from the portfolio target and should be rebuilt later as a separate simple system if needed.

## Next Step

Do one final dependency sweep for any remaining live references to removed systems, then clean up the inventory-related files that still point at the old package boundaries.
