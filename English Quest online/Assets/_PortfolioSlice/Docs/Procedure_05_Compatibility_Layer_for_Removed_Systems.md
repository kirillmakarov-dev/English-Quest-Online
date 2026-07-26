# Procedure 05: Add a Compatibility Layer for Removed Systems

## What changed

I added a small compatibility layer under:
- `Assets/_PortfolioSlice/SourceMirror/_OurAssets/Scripts/Core/Compatibility/PortfolioCompatStubs.cs`

It provides minimal, project-local stand-ins for the external systems that were causing namespace errors after we removed the old packages from the portfolio slice:

- `EnglishKingdom.LevelSystem`
- `EnglishKingdom.RewardSystem`
- `TargetIndicators`
- `DTT.WordConnect`
- `EnglishKingdom.UI.LevelUp`

## Why this was needed

The goal of the portfolio project is to keep the interesting architecture visible while trimming the parts we do not want to show, such as the old inventory stack and unrelated third-party dependencies.

If we simply delete those systems, the quest layer still points at their types and Unity stops compiling. The compatibility layer lets the slice stay buildable while those features behave as no-ops.

## What the compatibility layer contains

- A simple `LevelManager` plus `LevelCurveSO`
- A lightweight `RewardDefinition` and `RewardBundle`
- A minimal `TargetIndicatorManager`
- A placeholder `WordConnectManager`
- A stub `LevelUpCelebrationPresenter`

## Result

The quest, dialogue, save, and networking code can remain in the slice without dragging the full legacy packages back in.

This keeps the project closer to the intended portfolio story:
- show the architecture that matters
- hide the systems we do not want to present
- keep the slice compiling while we continue pruning

## Next step

Open Unity again and check the console. The remaining errors, if any, should now point to a much smaller set of real dependencies instead of the removed packages.
