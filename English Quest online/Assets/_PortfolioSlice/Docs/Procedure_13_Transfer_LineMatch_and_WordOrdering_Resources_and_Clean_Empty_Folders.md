# Procedure 13 - Transfer LineMatch and Word Ordering Resources and Clean Empty Folders

## Goal

Bring the visual and prefab resources for the selected mini-games into the portfolio slice, then clean out the empty folder shells left behind by the earlier pruning pass.

The selected content for this step was:

- `LineMatch`
- `Letter Ordering`
- `Word Ordering`

## What Was Copied

### LineMatch visuals

Copied the full LineMatch resource set from the source project into the portfolio slice:

- `Art/Materials/LineMatch`
- `Art/Prefabs/GamePlay/LineMatch`
- `Art/Sprites/LineMatch`

This brings over the UI art, word images, connection-line prefab pieces, and supporting material used by the LineMatch mode.

### Word Ordering UI

Copied the Word Ordering UI and data prefab set:

- `Art/Prefabs/UI/GamePlay/MiniGame/WordOrderingGame`

This folder carries the prefabs for the letter-order and word-order presentation layer, plus the local data assets used by that UI.

## What Was Cleaned

Removed 68 empty directories under `Assets/_PortfolioSlice` after the resource transfer.

That pass cleared the leftover empty shells from old systems and trimmed the portfolio tree down to the folders that still contain useful files.

## Current Result

The portfolio slice now has:

- the gameplay code we kept earlier
- the copied LineMatch and Word Ordering presentation assets
- a cleaner folder tree without empty legacy containers

## Notes

- I intentionally did not copy the broader old art environment or unrelated mini-game resources.
- The cleanup pass was limited to empty directories inside `Assets/_PortfolioSlice`, so nothing outside the portfolio slice was touched.
