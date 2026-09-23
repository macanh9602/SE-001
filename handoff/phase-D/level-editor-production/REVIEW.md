# D-B Level Editor — Independent review sheet

Use after Codex reports `EXECUTED`.

## Architecture

- [ ] Level JSON remains schema 3.
- [ ] No schema-3 `wallContours` / `staticObstacles` output.
- [ ] Layout geometry is read-only in Level Editor.
- [ ] Document / ViewState / DerivedState are genuinely separated.
- [ ] View-only state does not call Unity Undo.
- [ ] Edit operations use SO Undo host; no disk reload for ordinary edits.
- [ ] Selection is stable-ID based.
- [ ] Legacy empty/duplicate IDs are repaired in memory + dirty + Info issue.
- [ ] Baked mask is used during placement; rasterizer call count stays zero.

## File lifecycle

- [ ] New valid document.
- [ ] Open valid schema-3 document.
- [ ] Malformed Open preserves current document.
- [ ] Save / Save As round-trip.
- [ ] Dirty close/open guard.
- [ ] Save blocker explains exact issue.
- [ ] ViewState not serialized.

## Workspace / UX

- [ ] Real resizable split panes.
- [ ] ≥960 px: Levels + Canvas + Inspector usable.
- [ ] 700–959 px: Levels collapses to drawer; Canvas + Inspector usable.
- [ ] <700 px: Levels collapsed; Inspector overlay/collapsible; Canvas remains ≥320 px.
- [ ] Global actions/status never disappear.
- [ ] Canvas is visual focus.
- [ ] Primary/secondary/danger hierarchy is obvious.
- [ ] Disabled controls explain why.
- [ ] Empty states give the next action.

## Source / Cup authoring

- [ ] Add Source / Cup at visible canvas center.
- [ ] Newly added entity is selected and immediately draggable.
- [ ] Official art comes from `JarPreviewUtility`.
- [ ] Color control is swatch + display name, raw numeric ID Advanced only.
- [ ] Source exposes single Size value; width follows art aspect.
- [ ] Cup exposes Width + Height; no taper UI.
- [ ] Inspector coordinate edit remains precision path.
- [ ] Invalid drag reverts to last valid position.
- [ ] One drag = one Undo.
- [ ] Undo keeps same stable entity selected.

## Placement / validation

- [ ] JarVisualProfile footprint.
- [ ] board bounds.
- [ ] static baked mask.
- [ ] Source/Cup overlap.
- [ ] cup mouth blocked.
- [ ] Source nozzle clearance.
- [ ] finite coordinates.
- [ ] Blocking / Warning / Info.
- [ ] WHAT / WHERE / HOW copy.
- [ ] issues visible in canvas, inspector, validation UI and status.
- [ ] click issue selects + frames + focuses when practical.
- [ ] fixing issue removes it without reload.

## Performance/lifecycle

- [ ] no AssetDatabase scan per repaint.
- [ ] no rasterize during drag.
- [ ] no preview texture rebuild across 100 unchanged repaints.
- [ ] no callback accumulation after reopen/domain reload.
- [ ] no full document serialization per pointer move.

## Evidence

- [ ] first-open/empty screenshot.
- [ ] normal width.
- [ ] ~640 px.
- [ ] Source selected.
- [ ] Cup selected.
- [ ] blocker state.
- [ ] dirty state.
- [ ] disabled action with reason.
- [ ] validation focus target.
- [ ] blank → valid → validation repair → Save → reopen walkthrough.
- [ ] editor-ux-review pass 1.
- [ ] editor-ux-review pass 2.
- [ ] editor-ux-review pass 3.
- [ ] all 14 review dimensions have a real verdict.

## Technical gate

- [ ] Unity compile clean.
- [ ] Console clean.
- [ ] D-B targeted tests pass.
- [ ] D0.5/Layout Bake regressions pass.
- [ ] full EditMode suite pass or unrelated failures are proven by exact evidence.
- [ ] scoped style gate pass.
- [ ] `git diff --check` pass.
- [ ] GD guide updated.