# Procedure 03: Restore Root Assembly Boundary

## What was broken

After copying the source mirror into the new portfolio project, most gameplay code ended up without the original root assembly boundary. The tests were compiled as separate asmdef assemblies, but they could not reliably see the default-assembly runtime code.

That surfaced as namespace resolution errors in the editor, especially around the currency test surface.

## What was fixed

- The original root assembly definition was restored into the new project:
  - `Assets/_PortfolioSlice/SourceMirror/_OurAssets/_Project.asmdef`
  - its matching `.meta` file so the original GUID stays intact
- This re-creates the source project's central assembly boundary for the mirrored runtime code.
- The test asmdefs can now resolve the runtime namespaces and types through the same reference graph the source project used.

## Why this matters

This project is not meant to be a rewrite. It is a controlled transfer of the working gameplay slice into a new portfolio shell.

Keeping the original assembly graph intact is important because:

- namespaces line up with the code that already exists,
- tests can compile against runtime systems,
- and future copying steps can stay focused on scene and content wiring instead of fighting assembly visibility.

## Next step

Reopen the project and let Unity reimport the assemblies. If any remaining namespace errors appear, they should now be isolated to specific files rather than the whole source mirror.
