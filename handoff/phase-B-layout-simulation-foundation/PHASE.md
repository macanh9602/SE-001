# Phase B — Layout + Simulation Foundation

Status: CURRENT / EXECUTABLE

## Goal

Deliver the vertical foundation `TrashStuff/test_tool.svg -> editor-only importer -> canonical JSON -> shared
board-space geometry/masks -> generated 3D wall/obstacles -> SandSimulation -> SandField`.

## Boundaries

- Runtime source of truth is canonical JSON, never SVG, mesh, collider, or raster cache.
- Board gameplay geometry is 2D XY; presentation extrudes along Z and may bevel the top-to-side junction.
- LevelManager owns lifecycle/selection only. LevelSpawner owns validation, composition, roots, masks, and cleanup.
- Sand is custom Simulation-driven data with reused buffers; no Rigidbody per grain and no GameObject per grain.
- Source, Cup, Draw, win/lose, progression, and full production Level Editor are Phase C/D scope.

## Packets

B01 canonical level data; B02 editor-only SVG import; B03 shared geometry and masks; B04 3D layout view;
B05 SandSimulation; B06 SandField visual; B07 full integration and ten-cycle lifecycle verification.
