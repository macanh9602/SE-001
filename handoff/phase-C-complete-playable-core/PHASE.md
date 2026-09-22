# Phase C — Complete Playable Core

Status: DONE — scope closed 2026-09-23. Verification below is split into *recorded* (C-R1/C4 run) and *pending re-run* (changes made 2026-09-23).

## Delivered

- Three authored JSON levels: `phase_c_level_01`, `phase_c_level_02`, and `phase_c_level_03`.
- `ColorProfile` is the single color source for sand, Source, Cup, and UI bindings.
- Source, Cup, Draw preview, committed DrawStroke, and sand visuals are factory/prefab-owned production presentation.
- `PhaseCDebugView` is not registered by production `LevelSpawner`.
- Level progression is authored `01 → 02 → 03`; `GameScene` starts at level 01.
- Validator covers color IDs, per-color supply, static overlap, and no-draw reachability.
- HUD result flow uses `HUDSystem` and authored Win/Lose prefabs; result panels block gameplay input and clear on reload.
- Post-result sand settling remains active; no gameplay/simulation tuning was changed for the C2B closure.
- HUD shows a real ink/energy bar (track + fill + remaining/budget) and the lose reason inline with the state line.
- Sand simulation is reverted to the `6dee3cb` behaviour and frozen for Phase C (see `Docs/decision-log.md`).
- Source stream density comes from `PhaseCSourceProfile` (`emissionRate 16`, `streamWidth 6`); the three levels carry no per-level override.

## Verification

### Recorded (C-R1 / C4 run, before the 2026-09-23 fixes)

- Unity EditMode full regression: 59/59 passed.
- Targeted C1/C2 suite: 18/18 passed.
- HUD and closure guard suite: 7/7 passed.
- PlayMode level 02: before/after committed-draw screenshots and hierarchy evidence under `evidence/`.
- PlayMode reload probe: 10/10 cycles ready with one active HUD canvas and one active LevelRoot per cycle.
- C4 performance capture over all three authored levels in `evidence/phase-c-perf-c4.md`.
- `git diff --check`: PASS. Scoped style gate: PASS.

### Verified live on 2026-09-23

- Win panel now renders on `Won` (root cause + fix: `evidence/win-panel-invisible-rootcause.md`), confirmed by the user in Play Mode.

### PENDING MANUAL re-run (code changed after the recorded run)

Changed on 2026-09-23: `UIPanels.NormalizePanelRect`, `PhaseCHudView` ink bar + lose reason, `PhaseCResultHudController` error surfacing, `PhaseCDebugView` deleted (2 tests updated to assert by component name), 3 level JSONs, `SourceProfile` default, sand files reverted to `6dee3cb`.

- [ ] EditMode full regression re-run.
- [ ] Reload probe ×10 on all three levels.
- [ ] Performance capture refresh on all three levels.
- [ ] `git diff --check` + console clean.

## Descoped from Phase C

- Cup juice (`cupPunchDuration`) and stroke-grow juice (`strokeExtrudeDuration`) are **not implemented**; the fields stay in `JuiceProfile` unused. Moved to the post-Phase-C backlog by decision on 2026-09-23. Valve open/close juice is implemented.

See `implementation-notes.html` and `evidence/` for packet-level evidence.
