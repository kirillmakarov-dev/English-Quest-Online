# Procedure 02: Technical Base and Source Mirror

## What is in the new project now

- The Unity project keeps the original Unity version: `6000.3.3f1`.
- `Packages/manifest.json` has been updated with the core dependencies needed by the source code:
  - `UniTask`
  - `Photon/Fusion-related tooling already present through the copied source assets`
  - `Cinemachine`
  - `Input System`
  - `Cloud Save`, `Cloud Code`, `Authentication`, and `Unity Services Core`
  - `UI Extensions`
  - `uLipSync`
  - `Behavior`, `Splines`, `ProBuilder`, `Recorder`, `ShaderGraph`, `Terrain Tools`, `Mathematics`, and `Multiplayer PlayMode`
- The Fusion vendor folder has been copied into `Assets/Photon`.
- The Feel feedback package has been copied into `Assets/_ThirdParty/Feel`.
- A source mirror now exists at `Assets/_PortfolioSlice/SourceMirror/_OurAssets` with the authored project folders that matter for gameplay logic:
  - `Scripts`
  - `Data`
  - `Input`
  - `Resources`
  - `Settings`

## What this means

The new project now has the technical spine of the original game, but still does not carry the old world art or scene layout. That is intentional.

We are preserving the systems that make the project interesting from a portfolio point of view:

- Service Locator architecture.
- Photon Fusion networking.
- Quest and dialogue flow.
- Save infrastructure.
- Mini-game and teacher-facing systems.

At the same time, we are keeping the visual identity free for a new presentation layer.

## What has not been copied yet

- The original gameplay scenes.
- The old open-world environment.
- Human character models.
- Animal character models.
- Any visual content that would force the new project to look like a clone of the old one.

## Next step

Begin selective scene and prefab reconstruction for the new portfolio slice, using the copied code and data as the source of truth.
