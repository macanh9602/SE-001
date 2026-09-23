# Phase D — Production Level Editor + Layout Bake GD UX

C2C flow: **INIT → PLAN → EXECUTED → DONE | BLOCKED**

Workspace: `SE-001` only.

Planning owner: ChatGPT.
Implementation + verification owner: Codex.

Primary Phase D source:

* `handoff/phase-D/PHASE-D-PLAN.md`
* `Docs/data-model.md`
* `Docs/glossary.md`
* `SE001-ARCHITECTURE-BLUEPRINT.md`

> **Revision 2026-09-23 (Claude review, approved by Ducan).**
> This file is now the **shared spec**, not an execution packet. Codex executes it through three packets, in order:
>
> | Packet | File | Spec sections | Depends on |
> |---|---|---|---|
> | D-A | `PHASE-D-A-LAYOUT-BAKE.md` | §0, §1, §2, §3, §10 (Layout Bake), §12–16 | — |
> | D-B | `PHASE-D-B-LEVEL-EDITOR.md` | §0, §1, §4–8, §10 (Level Editor), §11, §12–16 | D-A DONE, **V1 DONE** |
> | D-C | `PHASE-D-C-PLAY-TEST.md` | §9, §10.2/10.3 Play Test cases, §12–16 | D-B DONE + Phase C gate |
>
> V1 = `handoff/phase-V1/PHASE-V1-JAR-VISUAL.md` (official Source/Cup art, 7 colors, preview utility).
> Each packet has its own Definition of DONE; the §17 checklist below is the union across all three.
> Sections marked **[REV]** were changed by this revision.

This spec deliberately consolidates the next large Phase D work around the **two tools GD will use most**:

1. **Level Editor** — main level authoring workspace.
2. **Layout Bake** — layout/SVG import, review, bake and rebake workflow.

Do not create a third competing workflow for the same data.

Do not commit or push unless explicitly requested.

Preserve all unrelated dirty files. In particular, do not “clean up” pre-existing font/cache/Sirenix changes merely to make git clean.

---

# 0. Required skills

## Mandatory during implementation

Read and apply:

### `skills/level-editor/`

Mandatory entry:

* `skills/level-editor/SKILL.md`
* `skills/level-editor/refs/checklist.md`
* `skills/level-editor/refs/anti-patterns.md`
* `skills/level-editor/refs/workflow-details.md`

Mandatory recipes:

* `recipes/editor-window-partials.md`
* `recipes/document-view-derived-state.md`
* `recipes/apply-edit-vs-reload.md`
* `recipes/split-workspace-uitk.md`
* `recipes/delayed-fields-and-callbacks.md`
* `recipes/stable-selection-remap.md`
* `recipes/validation-focus-flow.md`
* `recipes/unsaved-play-test.md`

Core rule:

> GD must be able to create and maintain a complete level without editing JSON and without needing developer knowledge.

### `skills/gd-communication/`

Use for:

* labels;
* field names;
* empty states;
* tooltips;
* validation messages;
* disabled-action explanations;
* GD guide.

Use vocabulary from `Docs/glossary.md`.

Do not surface internal class names, serialized-property paths, implementation jargon or raw stable IDs as the primary interface.

## Mandatory after implementation

### `skills/editor-ux-review/`

Run this skill on **both**:

1. Production Level Editor.
2. Layout Bake.

This is not optional and is not satisfied by reading code.

Review must use the real Unity Editor UI and real task walkthroughs.

Required review dimensions: all 14 dimensions from `editor-ux-review/SKILL.md`.

Required three passes:

1. first use without documentation;
2. core workflow;
3. stress + recovery.

Review first, record findings, then fix, then rerun the affected review cases.

Do not silently fix while performing the first review pass; retain an auditable before/after finding list.

## Conditional skills

Use `skills/debug-audit/` only when there is a real intermittent/state/lifecycle bug such as:

* callback duplication after domain reload;
* dirty document unexpectedly lost;
* Play Test override consumed twice;
* reload/reopen state corruption.

Do not use `debug-audit` for obvious deterministic implementation mistakes.

`presentation-lifecycle` is not part of this packet unless a genuine presentation ownership/async race appears.

`technical-slice` is not mandatory: D0.5 already proved the expensive layout path. Use it only if a new unknown performance architecture appears.

---

# 1. Architecture correction — two GD tools, two ownership domains

The current schema-3 architecture is authoritative:

```text
Level JSON
  → levelId
  → layoutId
  → Source[]
  → Cup[]
  → level gameplay values

LayoutDefinition
  → baked prefab
  → baked valid/static mask
  → board geometry
  → SVG provenance
```

Therefore:

## Layout Bake owns

* SVG input.
* Board/layout geometry.
* Walls.
* Static obstacle geometry contained by the SVG/layout.
* Layout prefab.
* Baked valid/static mask.
* Layout identity.
* Stale/rebake state.

## Level Editor owns

* Selecting a `LayoutDefinition`.
* `levelId`.
* Source authoring.
* Cup authoring.
* Level gameplay values currently belonging to `SE001LevelJson`.
* Validation.
* Save/Open lifecycle.
* Unsaved Play Test when D6 gate is open.

## Important: supersede old D3 assumption

The old Phase-D D3 text describing direct `StaticObstacle` editing in the Level Editor is no longer compatible with schema 3 because schema-3 level JSON must not serialize `staticObstacles`.

Do **not** reintroduce `staticObstacles` or `wallContours` into schema-3 level JSON just to satisfy that old workflow.

For GD:

```text
Need to change wall/static layout geometry
        ↓
Layout Bake / source SVG

Need to place/tune Source/Cup or level values
        ↓
Level Editor
```

The Level Editor may show baked static geometry for context, but it is **read-only layout geometry**.

Provide a clear action from the Level Editor to open Layout Bake when the user needs to change layout geometry.

Update Phase-D documentation to record this ownership correction.

---

# 2. Preflight — close D0.5 acceptance gaps first

Do this before building new editor workflow on top of D0.5.

Current implementation already contains:

* `LayoutDefinition`
* `LayoutMaskAsset`
* schema 3
* `LayoutBaker`
* `LayoutBakeWindow`
* migration
* baked runtime loading
* deterministic/parity tests
* load-time evidence

Do not rewrite this pipeline.

## 2.1 Runtime stale contour-hash verification

Current mask records `contourHash`, but runtime does not have an authoritative hash comparison.

Fix this cleanly.

Recommended contract:

```csharp
LayoutDefinition
{
    string layoutId;
    GameObject layoutPrefab;
    LayoutMaskAsset mask;
    Vector2 boardSize;

    // expected baked geometry identity
    string contourHash;
}
```

Baker writes the same hash atomically to:

* `LayoutDefinition.contourHash`
* `LayoutMaskAsset.contourHash`

Create one runtime validation/build entry point, for example:

```csharp
LayoutDefinition.TryBuildMaskSet(...)
```

or an equivalent single shared helper.

It must reject:

* null mask;
* empty/missing expected hash;
* definition hash != mask hash;
* stale `cellSize`;
* invalid bit lengths;
* max-cell overflow;
* invalid board-size relationship if applicable.

Error must tell the user to rebake the layout.

Do not add runtime SVG parsing.

Do not silently rasterize.

`LevelSpawner` and the schema-3 validator must use this authoritative path instead of reaching directly into `layout.mask` in different ways.

## 2.2 Strengthen no-runtime-rasterize test

Current `LevelLoad_UsesBakedMask_NoRasterizeCall` only calls `LevelDataLoader.Load`.

That does not prove the production spawn path.

Add/replace with a test that exercises actual production composition:

```text
LayoutRasterizer.ResetCallCount
→ LevelManager.BeginLevel(valid schema-3 level)
→ LevelSpawner production path
→ assert RasterizeCallCount == 0
→ teardown
```

Retain loader-specific tests separately if useful.

Add stale-hash regression:

```text
valid baked LayoutDefinition
→ alter definition/mask hash relationship
→ production load/spawn
→ clear blocking error
→ no fallback rasterize
```

---

# 3. Layout Bake — convert from dev utility to GD production tool

The current `LayoutBakeWindow` is functional but structurally resembles a developer debug utility:

* flat stack of fields;
* flat stack of buttons;
* status communicated mostly by labels;
* no strong action hierarchy;
* existing layouts are plain text rows.

Preserve its pipeline; replace/refactor the UX.

Production menu:

`SE001/Layout Bake`

Window minimum remains usable around 640 px.

---

# 3.1 Primary workflow

The intended workflow is:

```text
Choose SVG
→ inspect parsed preview
→ choose/confirm Layout ID
→ see whether input is valid/stale
→ Bake
→ see resulting layout as Ready
```

For an existing layout:

```text
Find/select layout
→ see source + baked status
→ Rebake
→ Ready
```

Bulk maintenance:

```text
Rebake All
```

must remain available, but it is not the dominant primary action.

---

# 3.2 Information architecture

Use visually distinct regions.

Suggested structure:

```text
┌ Layout Bake ─────────────────────────────────────────────┐
│ Create or update baked layouts from SVG                 │
├─────────────────────────────────────────────────────────┤
│ SVG Source                                               │
│ [ path / filename                         ] [Browse]     │
│ Layout ID [...............................]              │
│                                                         │
│ Preview                                                 │
│ [thumbnail/preview area]   Board      10.8 × 19.2       │
│                            Contours   5                  │
│                            Points     1,635              │
│                            Grid       180 × 320          │
│                            Status     Ready / Stale/...  │
│                                                         │
│                                    [ Bake Layout ]      │
├─────────────────────────────────────────────────────────┤
│ Existing Layouts                          [Search...]    │
│ layout_a       Ready       source.svg       [Rebake]    │
│ layout_b       Stale       stage2.svg       [Rebake]    │
│ ...                                                     │
│                                                         │
│ [Rebake All...]                                         │
├─────────────────────────────────────────────────────────┤
│ status / actionable error                               │
└─────────────────────────────────────────────────────────┘
```

Exact visual composition may adapt to UI Toolkit limitations, but task hierarchy must remain.

---

# 3.3 Visual hierarchy

High emphasis:

* selected SVG/layout;
* current validity/staleness;
* primary `Bake Layout`;
* blocking error.

Normal:

* preview metrics;
* existing layout rows;
* per-layout `Rebake`.

Low:

* raw source path;
* asset metadata;
* hashes;
* importer version.

Do not show SHA/hash or technical asset paths as primary GD information.

Advanced metadata may live in a collapsible section.

---

# 3.4 Semantic states

Every layout should clearly resolve to a user-facing state such as:

* Ready
* Needs Rebake
* Source Missing
* Invalid SVG
* Bake Failed

Use semantic color consistently with `editor-ux-review/refs/editor-visual-system.md`.

Color is not the only state carrier; include icon/text.

---

# 3.5 Actions

### Bake Layout

Primary action.

Disable when:

* SVG cannot parse;
* Layout ID invalid;
* required profiles missing;
* another blocking precondition exists.

Disabled state must explain why.

### Browse SVG

Secondary.

After selection, automatically refresh parsed preview. GD should not have to discover that they must click an additional Preview button unless preview is expensive enough to justify it.

### Rebake

Per-row contextual action.

### Rebake All

Secondary/bulk-maintenance action.

Do not visually compete with `Bake Layout`.

If it can modify many assets, require an intentional confirmation and show progress/result.

---

# 3.6 Error surface

Do not make Console the only place to understand failure.

Show inline:

```text
WHAT happened
WHERE relevant
HOW to recover
```

Example:

```text
SVG could not be parsed.
Source: stage_07.svg
Fix the SVG path/geometry, then choose the file again.
```

Console logging may remain for developer diagnostics but is secondary.

---

# 3.7 Empty state

Opening Layout Bake for the first time must explain the next action.

Example intent:

```text
Choose an SVG layout to preview and bake it for use by levels.
```

Existing-layout empty state must not look broken.

---

# 3.8 Existing layout browser

At minimum:

* searchable;
* readable layout name;
* status;
* source presence;
* per-row Rebake;
* select/ping asset where useful.

Do not run `AssetDatabase.FindAssets` continuously from repaint.

Refresh only at explicit/lifecycle-safe boundaries.

---

# 3.9 Layout Bake tests

Keep existing D0.5 tests and add coverage for:

* stale definition/mask contour hash blocks runtime;
* source missing status;
* stale `cellSize` is surfaced as Needs Rebake;
* Rebake preserves asset identity/GUID;
* same SVG stays deterministic;
* production spawn performs zero rasterization.

UI logic should be factored so status computation can be tested without requiring pixel-based UI automation.

---

# 4. Production Level Editor

Production menu:

`SE001/Level Editor`

UI Toolkit only unless there is a proven technical reason otherwise.

Do not implement the whole tool as one `EditorWindow` class.

Use the `editor-window-partials` recipe or an equivalent cohesive decomposition.

---

# 4.1 Mandatory state architecture

Three separate layers.

## Document

Owns persisted level data:

```text
SE001LevelJson
current path
schema version
dirty state
```

Only level-authoring data serializes.

## ViewState

Owns editor navigation:

```text
selected stable ID + entity kind
active tool/mode
zoom
pan
search/filter
left pane width
right pane width
validation drawer state
```

Do not serialize into level JSON.

Persist appropriate workspace preferences separately.

## DerivedState

Owns regenerable state:

```text
LayoutDefinition reference
unpacked baked mask/cache
validation issues
placement validity
canvas caches
lookup dictionaries
metrics
```

Never serialize into level JSON.

## Undo host **[REV]**

`SE001LevelJson` is plain C#, so Unity `Undo` cannot record it directly.

Decision: the Document lives inside an editor-only `ScriptableObject` wrapper (`HideFlags.HideAndDontSave`) with the level data as a `[SerializeField]`.

* Every edit: `Undo.RecordObject(wrapper, "<GD-readable action name>")` before mutating.
* Drag: `Undo.IncrementCurrentGroup()` on pointer down, `Undo.CollapseUndoOperations(group)` on pointer up → one entry.
* `Undo.undoRedoPerformed` → refresh DerivedState + remap selection by stable ID; do **not** reload the document from disk.
* Wrapper survives domain reload; dirty flag + current path are serialized fields of the wrapper (not of the level JSON).
* ViewState changes never call `Undo.RecordObject`.

---

# 4.2 Document lifecycle

Implement first.

Mandatory:

* New
* Open
* Save
* Save As
* dirty state
* schema/version handling
* malformed-file handling
* safe close/open guard
* round-trip serialization
* current-path management

Rules:

* `ApplyEdit != ReloadDocument`.
* Editing a field does not recreate the entire document.
* Malformed Open must not destroy the currently open valid document.
* Save As must not overwrite an unrelated file accidentally.
* ViewState/DerivedState must not enter JSON.
* A schema-3 level requires a valid `layoutId`.
* Level JSON must not gain `wallContours` or `staticObstacles`.

New documents default to current schema.

---

# 4.3 Workspace layout

Target workspace:

```text
┌ Levels ┬────────────── Board Canvas ─────────────┬ Inspector ┐
│ list   │ global actions / tool / context         │ selected  │
│ search │                                         │ entity    │
│ New    │                                         │ fields    │
│ Open   │                                         │           │
├────────┴─────────────────────────────────────────┴───────────┤
│ document · dirty · validation · status                       │
└─────────────────────────────────────────────────────────────┘
```

Use real resizable split panes.

Requirements:

* canvas receives remaining space;
* inspector scrolls independently;
* hierarchy/list scrolls independently;
* pane widths persist;
* sane min widths;
* tool remains usable around 640 px;
* collapse/reflow instead of crushing controls.

Responsive rule **[REV]** (three panes cannot all keep their minimum at 640 px, so the rule is fixed here instead of left to the implementer):

| Window width | Levels pane | Canvas | Inspector |
|---|---|---|---|
| ≥ 960 px | visible, min 180 | min 360 | visible, min 260 |
| 700–959 px | collapsed to toggle button (opens as overlay drawer) | fills | visible, min 240 |
| < 700 px | collapsed | fills, min 320 | collapsible; opens as right overlay when an entity is selected |

Global actions (§4.4) and the status bar are never collapsed.

---

# 4.4 Global actions

Stable location across modes:

* New/Open
* Save
* Undo
* Redo
* Play Test when D6 is available
* validation status

`Save` and `Play Test` are high-emphasis actions.

`Save As` may be secondary/menu-level.

Global actions do not jump location when selection changes.

---

# 4.5 Level browser

Left pane should allow GD to:

* see authored levels;
* search/filter by level ID/name where available;
* create new;
* open an existing level.

Do not hardcode `level_{0:00}`.

Use actual discovered/authored level identities.

Selecting a level in a browser must not silently discard a dirty document.

---

# 4.6 Layout selection

Every level explicitly selects one `LayoutDefinition`.

Inspector/level settings show:

* layout thumbnail when available;
* layout name;
* Ready / Needs Rebake / Missing status;
* `Change Layout`;
* `Open Layout Bake`.

Changing layout:

1. updates canvas background/static geometry;
2. updates baked mask;
3. immediately revalidates Source/Cup positions;
4. does not modify Source/Cup data automatically;
5. marks document dirty only because `layoutId` changed.

If the new layout invalidates entities, surface issues rather than silently moving them.

---

# 4.7 Canvas is the visual focus

Canvas must show:

* board bounds;
* selected baked layout;
* static/baked blocked geometry;
* Source entities;
* Cup entities;
* selected entity;
* invalid-placement feedback.

Use one authoritative board↔canvas mapping.

Do not create a second approximate coordinate implementation.

Zoom/pan:

* zoom around pointer where practical;
* pan does not fight entity drag;
* Fit/Frame Selection action available;
* handle hit size remains usable across zoom range.

Heavy derived state must not rebuild for every repaint.

## Entity visuals on canvas **[REV]**

Source and Cup are drawn with the **official art per colorId**, using `JarPreviewUtility` from V1 (composed, tinted, with drop shadow). No flat colored rectangles/circles as the final UI.

* Source drawn in idle pose (mouth up), sand fill = authored amount (full).
* Cup drawn at its authored width/height using the 9-slice cap rule.
* Selection / invalid feedback is an overlay (outline/ghost) drawn on top of the art, not a recolor of the art.
* Preview textures come from the V1 cache; the canvas must not re-tint per repaint.
* Entity footprint used for hit-testing and placement comes from `JarVisualProfile`, the same source runtime uses.

---

# 4.8 Entity scope for schema 3

Production Level Editor directly authors:

## Source **[REV]**

Fields exposed in GD terminology:

* position;
* **Color** — swatch dropdown built from ColorProfile (swatch + `displayName`: Red, Cobalt, Yellow, Purple, Pink, Green, White). Stored as `materialId`; the raw number is Advanced only;
* amount;
* **Size** — one value (height). Visual width follows art aspect (V1-6). On save, `size` is written as (derived width, height) so runtime and editor agree. Legacy levels whose x/y aspect deviates > 10 % get a Warning, not a silent rewrite.

Advanced: `emissionRate`, `streamWidth`, `startsOpen`, stable ID.

## Cup **[REV]**

Fields:

* position;
* **Accepted Color** — same swatch dropdown;
* required amount;
* **Width** and **Height** (9-slice caps, V1-2). No taper field anywhere in the UI.

## Layout geometry

Read-only in Level Editor.

To change walls/static geometry:

`Open Layout Bake`

Do not add point editing for layout contours to this tool.

---

# 4.9 Add Source / Add Cup

Primary canvas workflow:

```text
Add Source
→ spawn near center of current visible board area
→ selected immediately
→ ready to drag

Add Cup
→ same
```

Do not use a modal asking for X/Y coordinates.

Inspector coordinate entry remains available as a precision path.

New stable IDs must be deterministic enough to avoid collision and remain stable after save/reopen.

Selection is by stable ID, never array index.

Legacy data **[REV]**: `SourceData.stableId` / `CupData.stableId` already exist but default to empty. On Open, entities with empty or duplicate stable IDs get a new ID assigned in memory, the document becomes dirty, and an **Info** issue says “N items received a new ID — save to keep them”. Never assign IDs silently on disk.

---

# 4.10 Drag workflow

Dragging Source/Cup:

* direct manipulation on canvas;
* snap to simulation cell by default if appropriate;
* Shift temporarily bypasses snap;
* one pointer drag = one Undo entry;
* avoid full document reload;
* avoid rasterizing layout;
* use already baked mask/cache.

During drag:

* validate prospective position;
* normal appearance when valid;
* clear danger outline/ghost + concise reason when invalid.

Invalid release:

* revert to last valid position;
* do not commit invalid data.

Do not fix invalid release after the fact by saving bad data and hoping global validation catches it.

---

# 4.11 Placement validity

At minimum, placement checks relevant to current gameplay geometry:

* entity footprint from `JarVisualProfile` (same dimensions runtime uses) **[REV]**;
* inside board;
* not in static blocked mask;
* no invalid Source/Cup overlap;
* cup mouth not blocked;
* required clear space under Source nozzle;
* finite valid coordinates.

Reuse shared authoritative geometry/data where a runtime rule already exists.

Do not maintain two conflicting versions of the same rule.

Do not call `LayoutRasterizer.Rasterize` during drag.

---

# 4.12 Inspector hierarchy

Selected entity first.

Frequent GD controls first.

Suggested hierarchy:

```text
Source
  Material
  Amount
  Position
  Size

  Advanced
    Stable ID
    rare/raw metadata
```

```text
Cup
  Accepted Material
  Required Amount
  Position
  Size

  Advanced
    Stable ID
    rare/raw metadata
```

Raw stable IDs must not dominate the interface.

Destructive actions must be separated from normal editing.

---

# 4.13 Undo / Redo

Mandatory:

* field edit → sensible undo;
* one drag → one undo;
* add entity → one undo;
* delete entity → one undo;
* duplicate → one undo if implemented;
* changing layout → one undo;
* undo preserves/remaps stable selection when possible.

Do not pollute undo with view-only changes such as zoom/pan.

---

# 5. Production validation

Validation is a feature, not a debug panel.

Severity:

* Blocking
* Warning
* Info

Every issue includes:

```text
WHAT is wrong
WHERE it is
HOW to fix it
```

Do not use class/property names as primary GD copy.

---

# 5.1 Validation surfaces

Issues must be visible where relevant:

* canvas;
* inspector;
* validation panel/drawer;
* status bar;
* Save;
* Play Test.

Clicking an issue should:

1. select entity by stable ID;
2. switch appropriate context if necessary;
3. frame entity on canvas;
4. focus the relevant inspector control where practical.

Fixing the error must remove the issue without reopening the document.

---

# 5.2 Blocking behavior

Blocking issue:

* blocks Save where saving invalid production data would violate the contract;
* blocks Play Test;
* explains the first blocker;
* exposes total blocking count.

Do not simply gray out a button.

---

# 6. Save/Open acceptance tests

Mandatory automated tests where practical:

* `LevelDocument_New_IsValid`
* `LevelDocument_SaveLoad_RoundTripsAllFields`
* `LevelDocument_ViewState_NotSerialized`
* `LevelDocument_SaveAs_DoesNotOverwriteWrongFile`
* `LevelDocument_MalformedFile_DoesNotDestroyOpenDocument`
* `LevelDocument_DirtyState_TracksEdits`
* `LevelDocument_Reload_PreservesValidViewState`

Additional:

* schema-3 output contains `layoutId`;
* schema-3 output contains no `wallContours`;
* schema-3 output contains no `staticObstacles`;
* stable IDs survive round-trip.

---

# 7. Interaction acceptance tests

At minimum:

* Add Source selects the new Source.
* Add Cup selects the new Cup.
* Add/remove entity does not select a different entity because of array reindex.
* Sort/reopen remaps selection via stable ID.
* Drag is one Undo.
* Invalid drop returns to last valid location.
* Drag uses baked mask and causes zero rasterizer calls.
* Source/Cup overlap detected.
* blocked cup-mouth detected.
* Source clearance issue detected.
* layout change revalidates all entities.
* fixing an issue removes it.
* blocking validation controls Save/Play Test correctly.
* **[REV]** legacy level with empty stable IDs → IDs assigned in memory, dirty + Info issue, nothing written until Save.
* **[REV]** Color dropdown lists exactly the ColorProfile entries; unknown `materialId` in a legacy file → Blocking issue naming the Source/Cup.
* **[REV]** Undo after drag restores position and keeps the same entity selected (stable ID).
* **[REV]** canvas uses `JarPreviewUtility` textures; no preview rebuild across 100 repaints without data change.

---

# 8. End-to-end GD workflow before Play Test integration

Codex must actually run:

```text
Open Level Editor
→ New
→ choose a Ready Layout
→ Add Source
→ drag Source
→ configure material + amount
→ Add Cup
→ drag Cup
→ configure accepted material + required amount
→ intentionally create a validation error
→ use validation issue to navigate back to entity
→ fix issue
→ Save
→ close/reopen
→ verify same level and selection/data
```

No hand-editing JSON.

Store screenshot/evidence.

If this walkthrough was not actually executed, mark it `PENDING`.

---

# 9. D6 — Unsaved Play Test, conditional gate

D6 remains part of the production Level Editor, but do not violate the existing dependency.

Before implementing D6, inspect:

`handoff/phase-C-complete-playable-core/PHASE.md`

The following four re-runs must be genuinely completed:

* EditMode full regression re-run.
* Reload probe ×10 on all three levels.
* Performance capture refresh on all three levels.
* `git diff --check` + console clean.

If those four items are still pending:

**do not silently bypass this gate.**

Finish D1–D5 work and report:

`BLOCKED — D6 requires the four Phase C pending re-runs.`

Do not invent PASS.

---

# 9.1 Unsaved Play Test architecture

When gate is open:

```text
Editor Document
→ validate
→ serialize current in-memory document to temporary/session payload
→ one-shot runtime level override
→ enter Play Mode
→ normal LevelManager
→ normal LevelSpawner
→ normal gameplay/simulation
→ consume override exactly once
→ clear override
→ return to Editor
→ restore editor context
```

Forbidden:

* second runtime loader;
* writing unsaved temp level into production Resources;
* bypassing normal validator/spawner;
* override surviving a later normal load.

Use the `unsaved-play-test.md` recipe.

---

# 9.2 Play Test UX

Play Test is a primary action.

If unavailable because of document validation:

* keep it visible;
* disable it;
* show exact user-facing blocker.

No internal engineering gate text should remain in the final production UI.

Engineering gate messages are acceptable only during development, not as final GD UX.

---

# 9.3 D6 tests

* `UnsavedPlayTest_LoadsCurrentDocument`
* `UnsavedPlayTest_ConsumesOverrideOnce`
* `UnsavedPlayTest_NormalLoadRestoredAfterward`
* `UnsavedPlayTest_BlockingValidationPreventsPlay`
* editor context restored after Play Mode
* unsaved document remains dirty after returning
* no temporary production asset left behind

Manual evidence:

```text
open saved level
→ change Source/Cup without Save
→ Play Test
→ runtime visibly uses unsaved change
→ exit Play
→ Editor still has dirty document
```

---

# 10. Editor UX review — mandatory for BOTH tools

Implementation is not complete before this section.

Use:

`skills/editor-ux-review/SKILL.md`

Do separate review reports for:

* Level Editor
* Layout Bake

Do not combine all findings into vague generic feedback.

Finding format:

```text
Severity | Task | Friction | Expected | Evidence | Fix direction
```

---

# 10.1 Review pass 1 — First use

Reviewer opens tool without reading guide.

## Level Editor tasks

Try to discover:

* New/Open;
* choose layout;
* Add Source;
* Add Cup;
* Save;
* validation state;
* Play Test if available.

Record every place where the next action is unclear.

## Layout Bake tasks

Try to discover:

* select SVG;
* understand whether it parsed;
* understand Layout ID;
* identify primary Bake action;
* find existing layouts;
* rebake an existing layout;
* understand stale/error state.

---

# 10.2 Review pass 2 — Core workflow

## Level Editor

Run blank → valid level → Save/Open → validation repair → Play Test where available.

Review:

* task completion;
* action prominence;
* drag quality;
* inspector order;
* validation navigation;
* terminology;
* primary/secondary/danger hierarchy.

## Layout Bake

Run:

* new SVG → bake;
* existing layout → rebake;
* invalid SVG;
* missing source;
* stale layout;
* Rebake All.

Check whether GD needs Console or Project window knowledge to recover.

If yes, UX is not done.

---

# 10.3 Review pass 3 — Stress/recovery

Both tools:

* resize around 640 px;
* long inspector/list content;
* scroll each pane;
* domain reload;
* close/reopen;
* malformed input;
* dirty document close for Level Editor;
* invalid SVG for Layout Bake;
* repeated button/selection changes;
* disabled actions;
* keyboard focus/tab where applicable.

No important action may become inaccessible or clipped.

---

# 10.4 Required visual review dimensions

Explicitly review all:

1. Task completion
2. Discoverability
3. Information architecture
4. Interaction quality
5. Error recovery
6. Visual hierarchy
7. Control sizing/density
8. Semantic color
9. Icon usage
10. Frequency-based emphasis
11. Responsive layout
12. Accessibility/readability
13. GD terminology
14. First-use without documentation

---

# 10.5 Visual requirements

For both tools:

* Primary action visually stronger than secondary.
* Danger/destructive action separated.
* Active/selected state immediately visible.
* Blocking/warning/success states distinguishable.
* Do not create rainbow UI.
* Icons use one vocabulary.
* Important actions use icon + label where useful.
* Icon-only actions require obvious meaning + tooltip.
* Repeated tool actions may be compact.
* Raw IDs/paths/hashes are low emphasis.
* Frequent fields precede advanced fields.
* Disabled controls explain why.
* Empty states say what to do next.

---

# 10.6 Screenshots required

## Level Editor

Capture at minimum:

* first-open/empty state;
* normal-width working level;
* ~640 px;
* Source selected;
* Cup selected;
* validation blocker;
* document dirty;
* disabled Play Test/Save with reason where applicable;
* validation issue focus target.

## Layout Bake

Capture:

* empty state;
* SVG selected + parsed preview;
* successful Ready layout;
* stale/error layout;
* existing layout list;
* ~640 px;
* Rebake result.

Do not claim screenshot evidence if files were not saved.

Capture method **[REV]**: use the Unity MCP screenshot capability described in `AGENTS.md` (or an editor script that captures the focused `EditorWindow`). Each screenshot is a PNG under the packet's `evidence/.../screenshots/` with the window width in the file name (e.g. `level-editor_640px_source-selected.png`). If Unity MCP is unavailable in the session, mark every screenshot and UX-review item `PENDING — no editor capture available`; do not substitute code reading.

---

# 11. GD guide

Use `skills/gd-communication/`.

Create a short guide, not developer documentation.

Suggested:

`handoff/phase-D/GD-LEVEL-AUTHORING-GUIDE.md`

Cover:

## Which tool do I use?

```text
Change walls/layout geometry → Layout Bake
Place/tune Source/Cup → Level Editor
```

## Layout Bake

5–8 steps.

## Level Editor

5–10 steps.

## Validation

Explain blocker/warning/info.

## Play Test

Explain unsaved Play Test when available.

## Tool does NOT do

Clearly state current limitations.

Use screenshots.

No source code discussion.

---

# 12. Evidence structure

Create:

```text
handoff/phase-D/evidence/level-editor/
handoff/phase-D/evidence/layout-bake-ux/
```

Recommended files:

```text
level-editor/
  implementation.md
  functional-verification.md
  ux-review-pass-1.md
  ux-review-pass-2.md
  walkthrough.md
  screenshots/...

layout-bake-ux/
  functional-verification.md
  ux-review-pass-1.md
  ux-review-pass-2.md
  screenshots/...
```

D0.5 regression evidence may stay in existing D0.5 files but link it from the new verification report.

---

# 13. Performance/editor lifecycle rules

Do not regress editor responsiveness.

Forbidden in hot/repaint paths:

* `AssetDatabase.FindAssets` every repaint;
* full document serialization every pointer move;
* layout rasterization during drag;
* full validation graph rebuild for unrelated hover;
* material/texture allocations;
* duplicated event callbacks after `CreateGUI`/domain reload.

Use caches/lookup dictionaries where needed.

A single entity edit should invalidate only relevant derived state.

---

# 14. Mandatory verification

Before report:

## Compile

* Unity compile clean.
* No new Console errors.
* No new warnings caused by this packet.

## Automated tests

Run:

* D0.5 layout tests;
* new document tests;
* interaction/validation tests;
* full EditMode suite.

If existing unrelated tests fail, identify them by exact test name and prove they predate/are outside the touched path before classifying.

Do not write “existing failure” without evidence.

## Static checks

* `git diff --check`
* scoped style gate for all touched C#
* repo-wide style gate if available, with unrelated pre-existing failures identified separately

## Manual

* Level Editor end-to-end workflow.
* Layout Bake new + rebake workflow.
* editor-ux-review three passes on both tools.
* D6 unsaved Play Test if gate open.

---

# 15. Anti-shortcuts

Packet fails if any of these happen:

* Level Editor writes schema-2 contours/static obstacles back into level JSON.
* Level Editor becomes a second layout geometry editor.
* Layout Bake remains a flat developer button panel and is declared production-ready.
* GD must inspect Console to understand a normal recoverable error.
* GD must manually type JSON.
* selection uses array indexes.
* every edit reloads the whole document.
* drag rasterizes SVG/layout again.
* invalid drag commits bad data.
* Save/Play Test silently disabled.
* stable IDs dominate normal inspector UI.
* all toolbar buttons have equal visual priority.
* primary/danger actions sit next to each other without hierarchy.
* 640px layout clips the core workflow.
* callback count grows after reopen/domain reload.
* UX review consists only of reading C#.
* UX review is run only on Level Editor but not Layout Bake.
* review findings are fixed without keeping before/after evidence.
* screenshots are claimed but not stored.
* unsaved Play Test bypasses `LevelManager`/`LevelSpawner`.
* Phase C D6 dependency is marked PASS without actual evidence.

---

# 16. Worker C2C reporting

## INIT

Report only:

* workspace;
* current commit;
* dirty files that predate this packet;
* Phase C D6 gate state;
* files/areas expected to be owned.

Do not modify unrelated dirty files.

## PLAN

Before implementation, provide concise mapping:

```text
Requirement
→ planned class/file
→ skill/recipe used
→ verification
```

Important rows:

* Document/ViewState/DerivedState
* split workspace
* stable selection
* drag + undo
* validation focus
* Layout Bake UX
* D0.5 stale hash
* actual-spawn no-rasterize test
* unsaved Play Test gate

Then execute without waiting for another micro-approval unless an architecture contradiction appears.

## EXECUTED

Report:

```text
Item | Claimed | Verified by | Evidence
```

Include:

* D0.5 closure;
* Layout Bake functional;
* Layout Bake UX review;
* Level Editor lifecycle;
* Level Editor Source/Cup authoring;
* validation;
* workflow walkthrough;
* Level Editor UX review;
* D6 state;
* tests;
* compile;
* style;
* diff check.

## Final state

Exactly one:

`DONE`

only when all scope including D6 is genuinely complete and verified.

or:

`BLOCKED — <specific blocker + evidence>`

If D1–D5 are complete but Phase C gate still prevents D6:

`BLOCKED — Level Editor core and both UX passes complete; D6 unsaved Play Test is gated by the four outstanding Phase C verification items.`

Do not report `PARTIAL`.

---

# 17. Definition of DONE

All boxes required:

```text
[ ] D0.5 contour-hash stale contract fixed.
[ ] Production spawn path proves RasterizeCallCount == 0.
[ ] Layout Bake no longer behaves like a dev/debug utility.
[ ] Layout Bake first-use/core/stress UX review completed.
[ ] Layout Bake review findings fixed and rerun.

[ ] Level Document/ViewState/DerivedState separated.
[ ] New/Open/Save/Save As round-trip verified.
[ ] Schema-3 JSON contains layoutId and no layout contours/staticObstacles.
[ ] Production split-pane Level Editor complete.
[ ] Layout selection + Open Layout Bake flow complete.
[ ] Source authoring complete.
[ ] Cup authoring complete.
[ ] Canvas add/drag authoring complete.
[ ] Invalid drop cannot create invalid data.
[ ] Stable selection + Undo/Redo verified.
[ ] Production validation complete.
[ ] Validation click-to-focus complete.
[ ] Blank → valid level → Save → reopen walkthrough complete.

[ ] editor-ux-review run on Level Editor.
[ ] editor-ux-review run on Layout Bake.
[ ] 14 UX dimensions reviewed for both.
[ ] ~640px reviewed for both.
[ ] first-use reviewed for both.
[ ] error recovery reviewed for both.
[ ] screenshot evidence exists.

[ ] Phase C D6 gate actually PASS.
[ ] Unsaved Play Test uses normal runtime path.
[ ] One-shot override verified.
[ ] Editor context restored after Play Test.

[ ] GD guide exists.
[ ] Known limitations documented.
[ ] Compile clean.
[ ] Console clean.
[ ] Tests PASS or unrelated failures proven.
[ ] Scoped style gate PASS.
[ ] git diff --check PASS.
[ ] Phase-D docs/roadmap updated to reflect actual state.
```

Only then:

**Phase D Level Editor + Layout Bake production tooling = DONE.**
