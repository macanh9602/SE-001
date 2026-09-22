# Phase C C4 runtime evidence

Captured from Unity `GameScene` through Unity MCP on 2026-09-23.

## Production hierarchy

- Scene: `Assets/_Core/Scenes/GameScene.unity`
- Root `HUDSystem` exists with `HUDSystem` component.
- Level 02 production level was spawned through `LevelManager`.
- `PhaseCDebugView` was absent from the spawned `LevelRoot`.

## Screenshots

- Before draw: `level02-before-draw.png`
- After one committed draw: `level02-after-committed-draw.png`
- The committed stroke returned `committed=True`; remaining ink was `2.395538`.

## Reload x10

The Unity PlayMode probe counted active runtime ownership after every synchronous reload.

| Cycle | Ready | Active Phase C HUD canvases | Active LevelRoots |
|---:|---|---:|---:|
| 1 | true | 1 | 1 |
| 2 | true | 1 | 1 |
| 3 | true | 1 | 1 |
| 4 | true | 1 | 1 |
| 5 | true | 1 | 1 |
| 6 | true | 1 | 1 |
| 7 | true | 1 | 1 |
| 8 | true | 1 | 1 |
| 9 | true | 1 | 1 |
| 10 | true | 1 | 1 |

The EditMode lifecycle test additionally verified fixed child/source/cup counts and no debug view.

Console check after playthrough and reload cycles: 0 error/warning entries.
