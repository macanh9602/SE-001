# D0.5 layout load evidence

Measured in Unity `6000.0.70f1` through the active `SE-001` Editor session on 2026-09-23.

Input was the real `TrashStuff/test_tool.svg`, parsed by the shared
`PhaseBSvgImporter`. The measured layout contained 5 contours and 1,635 points.
The runtime grid was `180x320` at `cellSize=0.06`.

| Stage | Operation | Time | JSON bytes |
|---|---|---:|---:|
| Before | One legacy `LayoutRasterizer.Rasterize` call, the operation previously done during runtime load | 7,660.497 ms | 241,274 |
| After | One `LayoutMaskAsset.TryBuildMaskSet` bit-unpack call | 0.465 ms | 310 migrated schema-3 bytes |
| After | `LevelManager.BeginLevel("phase_c_level_02")` through the normal `LevelSpawner` path | 3.260 ms | n/a |

The before/after operation comparison used the same parsed SVG-derived contour data
and the same cell size. The production load measurement includes the normal manager,
spawner, baked layout prefab, simulation, sand field, and gameplay composition.
The parity check returned `maskOk=True`; the dedicated test also compares every
cell with the rasterizer output.

The legacy and migrated JSON byte counts are serialized snapshots of the same SVG
input before and after contour extraction into the baked layout asset.
