# Sphere Corridor

Sphere Corridor is a finite procedural 2.5D vehicle action-platformer for
Windows PC. The current production target is a geometric MVP built with Unity
6.6 and the Universal Render Pipeline.

## Current milestone

**M0 — Foundation** establishes the project structure, scene flow, input asset,
logging policy, automated validation, and a repeatable Windows development
build. It intentionally contains only temporary geometric presentation.

Setup and verification instructions are documented in
[`Docs/Development/M0-Foundation.md`](Docs/Development/M0-Foundation.md).

## Repository contents

The source-controlled Unity project consists primarily of:

- `Assets` — authored game assets and source code.
- `Packages` — the exact Unity package manifest and lock file.
- `ProjectSettings` — project-wide Unity configuration.

Generated directories such as `Library`, `Temp`, `Logs`, `UserSettings`, and
`Builds` must not be committed.
