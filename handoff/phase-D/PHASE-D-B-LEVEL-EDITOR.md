# Packet D-B — Production Level Editor (D1–D5)

C2C flow: **INIT → PLAN → EXECUTED → DONE | BLOCKED** · Workspace `SE-001` · Worker: Codex.

Spec: `handoff/phase-D/PHASE-D-PRODUCTION-TOOLING-SPEC.md` (read the revision note; all **[REV]** sections are binding).

## Entry gate

* D-A = DONE (Layout Bake statuses + `TryBuildMaskSet` exist).
* V1 = DONE (`JarVisualProfile`, `JarPreviewUtility`, 7-color ColorProfile exist).

If either is not DONE → report `BLOCKED — entry gate` in INIT and stop.

## Scope — spec sections

* §0 skills (`level-editor` + all listed recipes, `gd-communication`, `editor-ux-review` on Level Editor).
* §1 ownership (layout geometry read-only; “Open Layout Bake” action).
* §4 Level Editor — incl. [REV] Undo host (SO wrapper + Unity Undo), [REV] responsive table, [REV] canvas art via `JarPreviewUtility`, [REV] Source/Cup fields (Color swatch, Source single Size, Cup Width/Height, no taper), [REV] legacy stable IDs.
* §5 validation, §6 Save/Open tests, §7 interaction tests (incl. [REV] rows), §8 end-to-end walkthrough.
* §10 UX review — **Level Editor**, three passes; Play Test cases are deferred to D-C (mark them `N/A — D-C`).
* §11 GD guide (Play Test chapter: placeholder “coming in D-C”).
* §12–16.

## Out of scope

D6 unsaved Play Test (packet D-C). The Play Test button may be present but disabled with a GD-readable reason; no engineering-gate text in final UI.

## Evidence folder

`handoff/phase-D/evidence/level-editor/`

## Definition of DONE

```text
[ ] Document / ViewState / DerivedState separated; SO wrapper Undo host.
[ ] New / Open / Save / Save As round-trip; malformed Open keeps current doc.
[ ] Schema-3 JSON has layoutId, no wallContours / staticObstacles.
[ ] Split panes follow the responsive table; 640 px usable.
[ ] Layout selection + Open Layout Bake; layout change revalidates, never moves entities.
[ ] Canvas draws official Source/Cup art per color (JarPreviewUtility), overlays for selection/invalid.
[ ] Add Source / Add Cup → selected, draggable; one drag = one Undo; invalid drop reverts.
[ ] Drag uses baked mask; RasterizeCallCount == 0.
[ ] Color swatch dropdown; Source single Size; Cup Width/Height.
[ ] Legacy empty stable IDs handled per [REV].
[ ] Validation Blocking/Warning/Info with WHAT/WHERE/HOW; click-to-focus; Save gating explained.
[ ] §8 walkthrough executed with screenshots (or PENDING, never faked).
[ ] UX review pass 1/2/3 on Level Editor with before/after findings.
[ ] GD guide (Level Editor + Layout Bake chapters, known limitations).
[ ] Compile / console / tests / style gate / diff-check clean.
```

Final state: `DONE` or `BLOCKED — <specific blocker + evidence>`.
