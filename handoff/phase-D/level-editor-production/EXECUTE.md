# D-B Level Editor — Hybrid execution supplement

This file supplements, but never replaces:

- `handoff/phase-D/PHASE-D-B-LEVEL-EDITOR.md`
- `handoff/phase-D/PHASE-D-PRODUCTION-TOOLING-SPEC.md`

C2C: `INIT → PLAN → EXECUTED → DONE | BLOCKED`

## 1. Entry gate

Before bootstrap or implementation:

- D-A must be genuinely DONE.
- V1 must be genuinely DONE.

If not, stop with `BLOCKED — entry gate`.

The bootstrap switch `-GateApproved` means the worker has already verified those two facts. It is not permission to waive them.

## 2. Required skills

Implementation:
- `skills/level-editor/SKILL.md`
- `skills/level-editor/refs/checklist.md`
- `skills/level-editor/refs/anti-patterns.md`
- `skills/level-editor/refs/workflow-details.md`
- all eight recipes under `skills/level-editor/recipes/`
- `skills/gd-communication/SKILL.md`

Review after the real workflow exists:
- `skills/editor-ux-review/SKILL.md`
- all five refs under `skills/editor-ux-review/refs/`

Use `debug-audit` only for an actual state/lifecycle bug whose cause is not statically proven.

## 3. Bootstrap contract

Run the supplied PowerShell with `-DryRun` first, then with `-GateApproved`.

The script intentionally creates only deterministic foundation:

- editor-only ScriptableObject document host;
- ViewState / DerivedState / validation issue types;
- stable-ID normalization helper;
- partial LevelEditorWindow shell;
- document New/Open/Save/Save As baseline;
- UI Toolkit chrome baseline;
- Level Editor USS visual roles;
- evidence / UX-review templates.

After apply, inspect all generated code before continuing.

The bootstrap is NOT acceptance-complete. In particular Codex still owns:

- real resizable split panes and the responsive table;
- authoritative board↔canvas transform;
- baked layout rendering/status and `Open Layout Bake`;
- Source/Cup canvas art via `JarPreviewUtility`;
- JarVisualProfile footprint/hit testing;
- color swatch selector;
- Source single Size and Cup Width/Height inspector;
- add/drag/snap/direct manipulation;
- one-drag-one-Undo;
- placement validity against baked mask;
- Source/Cup overlap, cup-mouth blocking, source clearance;
- production Blocking/Warning/Info validation;
- click issue → select/frame/focus;
- Save gating;
- full automated tests;
- end-to-end walkthrough;
- three-pass `editor-ux-review`;
- screenshots and GD guide.

Do not implement D-C unsaved Play Test here. Keep Play Test visible but disabled with GD-readable copy.

## 4. Architecture locks

- Schema 3 level JSON owns `levelId`, `layoutId`, Source, Cup and level values.
- Layout Bake owns SVG/wall/static layout geometry.
- Level Editor MUST NOT write `wallContours` or `staticObstacles` into schema-3 JSON.
- Layout geometry is read-only inside Level Editor.
- Selection identity is stable ID, never array index.
- `ApplyEdit != ReloadDocument`.
- ViewState and DerivedState never serialize into level JSON.
- Drag uses baked masks; zero `LayoutRasterizer.Rasterize` calls.
- Canvas uses `JarPreviewUtility`, not final flat debug shapes.
- Footprints derive from `JarVisualProfile`.
- New/legacy missing or duplicate Source/Cup IDs are repaired in memory and make the document dirty; nothing is silently written to disk.

## 5. C2C PLAN map required before feature implementation

Report:

```text
Requirement
→ planned class/file
→ skill/recipe
→ verification
```

Include at minimum:
- Document/ViewState/DerivedState
- SO Undo host
- file lifecycle
- responsive workspace
- stable selection
- layout selection
- canvas transform
- JarPreviewUtility art
- inspector fields
- drag + one Undo
- baked-mask placement validation
- validation focus flow
- end-to-end walkthrough
- three-pass UX review

## 6. Verification

Run exactly what D-B/shared spec requires.

Do not claim PASS from code inspection.

For UX:
1. first use without documentation;
2. core workflow;
3. stress/recovery including ~640 px, domain reload, malformed input and dirty close.

Every finding:
`Severity | Task | Friction | Expected | Evidence | Fix direction`

Keep before/after evidence.