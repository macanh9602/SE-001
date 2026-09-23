# SE-001 — Phase V1b: Bowl Receiver Visual
## Cup retained as a game-wide switchable backup

C2C flow: **INIT → PLAN → EXECUTED → DONE | PLAN | BLOCKED**

Workspace: **SE-001 only**  
Implementation owner: **Codex**  
Product/manual verification: **Ducan in Unity**

Do not commit or push unless explicitly requested.

---

# 0. Entry protocol

1. Verify `workspace_info` first. Workspace must be exactly `SE-001`; stop on mismatch.
2. Read current source/status/diffs through the `Codex with ChatGPT · SE-001` connector. Never ask Ducan to paste files, diffs or logs.
3. Read and preserve the existing V1 contracts:
   - `handoff/phase-V1/PHASE-V1-JAR-VISUAL.md`
   - `handoff/phase-V1/implementation-notes.html`
   - `handoff/phase-V1/jar-tint-tuning.json`
   - current `JarVisualProfile`, `JarVisualAssetSetup`, `JarPreviewUtility`
   - current Cup domain/factory/visual/prefab/profile/tests
4. Inspect the real bowl textures before implementation:
   - `Assets/_Core/0_Texture2D/Env/T_Bowl_Main.png`
   - `Assets/_Core/0_Texture2D/Env/T_Bowl_Spec.png`
5. Use the supplied reference image:
   - `handoff/phase-V1b/reference/bowl-vs-cup.png`
6. Preserve all unrelated project files and user-authored level JSON. Do not bulk-rewrite or re-author levels.
7. Report `INIT`, then a concise `PLAN`, then implement. Ask Ducan only at the explicit size/data checkpoint in §8 if a level-data change is actually required.

Current re-review snapshot (2026-09-24):
- branch `main`
- HEAD `f2c4b2b`
- upstream `origin/main`, ahead 0 / behind 0
- working tree is intentionally dirty with ongoing **Level Editor UX authoring** and level-data edits listed below

Re-check rather than assuming this state is still current.

## 0.1 Pre-existing dirty work — preserve and merge around it

At re-review time the following changes predate V1b and are **not cleanup targets**:

```text
M Assets/_Core/4_Scripts/Editor/Level/C#/LevelEditor/LevelEditorWindow.Document.cs
M Assets/_Core/4_Scripts/Editor/Level/C#/LevelEditor/LevelEditorWindow.Layout.cs
M Assets/_Core/4_Scripts/Editor/Level/C#/LevelEditor/LevelEditorWindow.cs
M Assets/_Core/4_Scripts/Editor/Level/USS/LevelEditorWindow.uss
M Assets/_Core/Resources/Levels/Level_01.json
M Assets/_Core/Resources/Levels/Level_02.json
M handoff/phase-D/evidence/level-editor/ux-review.md
M handoff/phase-D/implementation-notes.html
?? Docs/visualizers/current-level-entity-explorer-options.html
?? Docs/visualizers/level-browser-tab-options.html
```

These Level Editor changes introduce/iterate the current workflow:
- `Edit` workspace tab;
- `Level Browser` tab;
- current-level `Entities` drawer;
- General/Add Source/Add Cup/Add Obstacle authoring action row;
- responsive browser/entity UX.

**V1b must not revert, replace, or redesign that navigation.**

When integrating Bowl into the Level Editor:
- prefer `LevelEditorCanvas.cs`, `LevelEditorWindow.Panels.cs`, `LevelEditorValidation.cs`,
  `LevelEditorGeometry` and `JarPreviewUtility`;
- touch the dirty `LevelEditorWindow.Layout.cs`, `LevelEditorWindow.cs`,
  `LevelEditorWindow.Document.cs` or `LevelEditorWindow.uss` only when Bowl support
  strictly requires it;
- if one of those dirty files must be touched, preserve all pre-existing tab/browser/entity-drawer behavior and make a surgical merge;
- do not restore an older three-pane/Levels layout from historical Phase D documentation.

`Level_01.json` and `Level_02.json` are current user-authored dirty data. Do not use them as disposable test fixtures and do not rewrite them for Bowl sizing.

---

# 1. Locked product decisions — do not reopen

Ducan, 2026-09-24.

| ID | Decision | Locked choice |
|---|---|---|
| B-1 | Sand-holding geometry | **Art contour.** Bowl geometry comes from the real `T_Bowl_Main` alpha silhouette and measured glass thickness, not a rectangle/trapezoid. |
| B-2 | Size mapping | **Uniform by width.** Height is derived from art aspect. Bowl exposes one scalar `Size` concept in the Level Editor; do not independently stretch X/Y. |
| B-3 | Backup path | **Game-wide switch**: `Receiver Style = Cup | Bowl`. Default = **Bowl**. Existing level JSON does not select style. |
| B-4 | Bowl tint | **Seven dedicated `MAT_BowlMain_<Color>` materials**, seeded from the current cap tint parameters and tuned for the Bowl texture. Do not reuse the Cup material assets directly. |

Cup remains a supported production fallback and must still pass its tests when `receiverStyle = Cup`.

Do not modify existing level data merely to turn Cup into Bowl.

---

# 2. Current architecture facts to respect

Current project facts at packet creation:

- `GameplayRuntimeProfile` has no receiver-style switch yet.
- `PrefabProfile` has `cupPrefab` but no bowl prefab.
- `CupDomain` owns receiver gameplay geometry, sink/capacity/full logic.
- `CupDomain` currently builds rectangle/taper grid geometry from `CupProfile`.
- `GameplayManager.Configure(...)` stamps Cup walls once through `CupDomain.RegisterWalls(...)`.
- `CupFactory` currently always spawns `PrefabProfile.cupPrefab`.
- `PhaseCCupVisual` owns `View/{Shadow,Body,CapTop,CapBottom}`.
- `JarVisualProfile` stores measured Source/Cup art data.
- `JarPreviewUtility` currently supports `Source | Cup`.
- the Level Editor preview and color palette use `JarPreviewUtility`.
- current `CupProfile.bodySize = (2,2)`, taper is 0 in the production asset.
- current Cup size is global/profile-derived; `CupData` does not contain a per-Cup size field.
- the Phase D smooth vector layout preview is now already implemented via
  `LevelEditorLayoutPreviewCache` + `LevelEditorLayoutPreviewElement`; do not regress or replace it.
- current logical sand scale is `SandSimulationProfile.grainsPerUnit = 115`, with
  Source/Cup logical amounts converted through that one profile scale. Bowl capacity/Required
  logic must preserve this current semantic contract and must not restore older 30-grains/unit behavior.
- Phase D is currently `BLOCKED / IMPLEMENTED` because closure evidence is pending.
  V1b must not reopen or attempt to close Phase D as part of Bowl work.

Do not create a second receiver gameplay system. Refactor the existing receiver path so both styles share the same domain/accounting contract.

---

# 3. Target architecture

```text
GameplayRuntimeProfile.receiverStyle (Cup | Bowl)    ← one game-wide switch
        │
        ├── ReceiverShape / ReceiverGeometry
        │      Cup  = current rectangle path, behavior preserved
        │      Bowl = baked art-derived curved shape, uniform width scale
        │
        ├── CupDomain
        │      walls / sink / capacity / FillRatio / full-mouth close
        │      read ReceiverShape only
        │      no Renderer/Texture access
        │
        ├── ReceiverFactory
        │      Cup  → Cup.prefab
        │      Bowl → Bowl.prefab
        │
        └── Editor
               Level Editor + JarPreviewUtility draw active receiver style
```

The serialized gameplay entity may remain `CupData` for compatibility. Do not rename schema fields/classes just because the presentation is now Bowl.

Preferred new runtime/data types:

```text
ReceiverStyle { Cup, Bowl }

ReceiverShape
  style
  world bounds
  rimY
  inner/free contour or row spans
  wall/blocking contour/cells
  capacity rows/spans
  outer silhouette reference/data for validation tests

BowlVisualProfile
  measured art facts
  normalized outer silhouette
  normalized inner edge / blocking contour
  rim
  wall thickness
  spec placement
  uniform size/aspect information
  any baked row spans/LUT needed by preview/tests
```

Exact names may differ, but separation must remain:
- art measurement/bake = editor-time;
- receiver gameplay shape = data/runtime;
- presentation = prefab/View;
- no renderer access from `CupDomain`.

Do not put per-frame geometry extraction into presentation.

---

# 4. Real texture measurement — required, never guess

Inputs:

```text
Assets/_Core/0_Texture2D/Env/T_Bowl_Main.png
Assets/_Core/0_Texture2D/Env/T_Bowl_Spec.png
```

Measure programmatically from the actual imported pixels, not by looking at the screenshot.

Record in `handoff/phase-V1b/evidence/bowl-measurements.md`:

```text
Main texture width × height
Spec texture width × height
alpha bounding box
outer silhouette
rim Y
rim open interval / mouth width
measured visible glass-wall thickness
curved-bottom profile
native art aspect
Spec alpha bbox
Spec transform relative to Main:
  offset x/y
  scale
  whether the source canvases match 1:1
```

If importer readability must be changed for the editor bake:
- make the import change deterministic;
- do not leave accidental compression/filtering changes;
- do not sample texture pixels at runtime.

The reference image indicates Bowl roughly ~2.2× Cup width, but that is a visual reference, **not a measurement constant**.

---

# 5. Bowl contour bake

Implement as idempotent editor tooling inside `JarVisualAssetSetup` or a tightly related editor helper invoked by it.

## 5.1 Required bake chain

```text
T_Bowl_Main alpha
→ threshold/trace outer silhouette
→ identify/open the rim
→ measure glass wall thickness
→ derive inner glass edge
→ account for runtime grid safety
→ simplify deterministically
→ normalize to art coordinates
→ store in BowlVisualProfile
```

Outer silhouette and sand-blocking geometry are not the same thing. Store enough data to test both.

## 5.2 Contour rules

- Bowl bottom/sides follow the real art.
- Top is open at the rim.
- Simplified polygon: **≤ 40 points**.
- Use deterministic Douglas–Peucker or equivalent deterministic simplification.
- Preserve endpoints around the rim and high-curvature bottom regions.
- The bake must be idempotent.
- Rebuild must update profile data in place and preserve asset GUIDs.

Test:

```text
Bowl_Contour_Bake_IsDeterministic_AndClosedAtBottom
```

"Closed at bottom" means the receiver wall path cannot leak through the curved base; the rim remains the intentional opening.

---

# 6. Sand must visually remain inside the Bowl

Simulation grid cell is currently around 0.06 world, but never hardcode that number. Read the actual runtime `SandSimulationProfile.cellSize`.

A curved grid wall can stair-step outside the artwork if the blocking boundary is placed on the outer silhouette.

## 6.1 Required geometry safety

Keep three concepts distinct:

```text
outer silhouette
inner glass edge
runtime blocking boundary
```

Derive the inner glass edge by inset from the alpha outer silhouette using the measured wall thickness.

Then place the runtime blocking boundary safely inside the visible glass band, accounting for approximately half a simulation cell at the Bowl's actual uniform scale.

The goal:

- no blocking staircase protrudes outside the outer silhouette;
- sink/free cells considered "inside the Bowl" remain visually inside the glass;
- bottom curve does not leak;
- rim remains open until Full;
- the wall does not become so thick that it visibly destroys Bowl capacity.

Do not hardcode a pixel inset independent of world scale/cell size.

## 6.2 Minimum-size validation

At the smallest allowed Bowl size:

```text
effective visible wall thickness >= one usable simulation-cell safety requirement
```

If not, the Level Editor must warn GD:

> Bowl is too small for the current sand grid.

Use GD-facing language. Do not expose implementation jargon like array indices.

This should be Warning if the Bowl still functions but visual leakage risk exists; Blocking if correct collision cannot be guaranteed.

## 6.3 Required tests

```text
Bowl_BlockedCells_StayInsideOuterSilhouette
```

Run at minimum and maximum supported Bowl sizes.

Test the art/world transform precisely:
- cells considered receiver interior/free map inside the outer silhouette;
- wall/blocking cells stay within the intended glass band/safe silhouette region;
- no free receiver cell extends through the curved bottom.

Also:

```text
Bowl_FillToRim_NoGrainOutsideGlass
```

Run deterministic simulation-style filling and sample grains attributed to the Bowl interior against the outer silhouette transform.

This test is about receiver leakage, not unrelated falling sand elsewhere in the level.

---

# 7. ReceiverShape and CupDomain refactor

The gameplay contract remains named `CupDomain` unless a rename is clearly lower risk. A broad Cup→Receiver schema rename is forbidden in this packet.

Refactor CupDomain so geometry comes from a shape object rather than renderer/art assumptions.

Required common behavior:

```text
AcceptedMaterialId
Position
RequiredLogical
Required grains
Collected
Capacity
FillRatio
ForeignDetected
RegisterWalls
Collect
Full
CloseMouth
```

## 7.1 Cup path

`receiverStyle = Cup` must preserve current production behavior:
- current rectangle geometry;
- current art-aligned inner width/bottom;
- current capacity semantics;
- current mouth close rule;
- existing Cup tests adapted only where necessary.

Do not "improve" Cup while adding Bowl.

## 7.2 Bowl path

`receiverStyle = Bowl`:
- uniform world scale from Bowl width;
- height derived from measured art aspect;
- curved wall geometry from baked profile;
- precompute/stamp wall cells once during receiver setup;
- capacity comes from actual interior grid/polygon area below rim;
- no per-frame polygon rasterization;
- `FillRatio` continues to use collected vs required target;
- validation still blocks Required > physical Capacity;
- Full closes the open rim span;
- additional falling sand may pile on top of the closed rim; that is expected.

Test:

```text
Bowl_Capacity_FromPolygonArea
Bowl_Walls_MatchArtContour
```

The capacity test should compare against the baked/grid interior, not a rectangular approximation.

---

# 8. Size mapping and the only expected product checkpoint

Locked rule:

> Bowl scales uniformly by width; height follows art aspect.

Current data has no per-Cup size field. Therefore **do not add a JSON/schema size field silently**.

First measure the real Bowl art and compare it against current Cup appearance at current `CupProfile.bodySize.x`.

Preferred path if it satisfies the reference:
- keep level JSON unchanged;
- add a Bowl-specific global/profile width or `cupWidth → bowlWidth` factor in `BowlVisualProfile`;
- derive height from art aspect;
- Level Editor shows a single Bowl `Size` concept, not X/Y stretch.

If implementation concludes that existing levels must be re-authored or `CupData` must gain a serialized size field:

**STOP before modifying level JSON/schema and ask Ducan.**

Report:

```text
Measured Cup width:
Measured Bowl native width/aspect:
Proposed visual width:
Proposed factor:
Why a profile-only factor is insufficient:
Exact JSON/schema change proposed:
Which existing levels would change:
```

Do not touch level data until Ducan answers.

If a profile-only factor works, proceed without rewriting levels and record the chosen measured factor in evidence.

---

# 9. Bowl prefab and presentation

Create production prefab:

```text
Assets/_Core/3_Prefabs/Gameplay/Bowl/Bowl.prefab

Bowl
└── View
    ├── Shadow
    ├── Main
    └── Spec
```

No extra decorative/runtime-debug children in production.

Recommended component:
`BowlVisual` / `PhaseCBowlVisual`.

## 9.1 Main

- texture: `T_Bowl_Main`
- shader: existing `SE001/JarTint`
- color-specific shared material:
  - `MAT_BowlMain_Red`
  - `MAT_BowlMain_Cobalt`
  - `MAT_BowlMain_Yellow`
  - `MAT_BowlMain_Purple`
  - `MAT_BowlMain_Pink`
  - `MAT_BowlMain_Green`
  - `MAT_BowlMain_White`
- no `renderer.material`
- material selected by `acceptedMaterialId`

Extend `ColorProfileEntry` with a Bowl material reference if that is the cleanest single source of truth.

## 9.2 Spec

- texture: `T_Bowl_Spec`
- shared untinted material
- fixed measured transform relative to Main
- not colorized by colorId
- render above Main

Do not guess Spec offset from the screenshot. Measure texture canvases/alpha bbox.

## 9.3 Shadow

- fake drop shadow consistent with V1
- silhouette generated from `T_Bowl_Main` alpha
- shared shadow material
- no shadowmap
- offset/profile-driven, no per-frame allocation

## 9.4 Draw order

Back → front:

```text
board/layout
sand field
Bowl Shadow
Bowl Main (tinted semi-transparent glass)
Bowl Spec
```

Sand must appear through Main, never above Spec.

Preserve V1 Source/Cup ordering.

Use stable render queues/depths/shared materials. Do not add per-frame sorting code unless technically unavoidable and justified.

---

# 10. Materials and tuning

Bowl gets **seven dedicated Main materials**.

Generator requirements:
- create/update in place;
- preserve GUIDs;
- deterministic filenames;
- rerun idempotent;
- no duplicates.

Seed tint parameters from the matching existing Cup-cap color:
`MAT_CupCap_<Color>` → `MAT_BowlMain_<Color>`.

Then tune against Bowl art, without modifying the Cup materials.

Preferred source-of-truth:
`handoff/phase-V1b/bowl-tint-tuning.json`

If generated, seed it from current cap tuning first. Do not overwrite deliberate Bowl retuning on every rebuild.

Required test:

```text
BowlMaterials_Rebuild_Idempotent_PreservesGuids
```

Run across all seven materials, not Red only.

The Bowl should continue to use `sandColor` from the existing `ColorProfile`; do not create a second gameplay color identity.

---

# 11. Factory and switch safety

Add:

```csharp
public enum ReceiverStyle
{
    Cup,
    Bowl
}
```

Add to `GameplayRuntimeProfile`:

```text
receiverStyle
```

Production asset default = **Bowl**.

Add `bowlPrefab` to `PrefabProfile`.

Runtime factory behavior:

```text
Cup  → existing Cup prefab + PhaseCCupVisual
Bowl → Bowl prefab + BowlVisual
```

Prefer one receiver-selection factory used by `LevelSpawner`.

Do not duplicate domain construction or gameplay rules in two factories.

Existing `CupFactory` can remain as a thin Cup-specific helper for compatibility/tests if useful, but the production spawn path must have one clear style switch.

Test:

```text
ReceiverStyle_Switch_SpawnsCupOrBowl_WithoutDataChange
```

Test must prove:
- same `CupData`;
- same acceptedMaterialId/required amount/position;
- only style/profile changes;
- no JSON mutation;
- switch back to Cup works.

---

# 12. Level Editor integration

**Merge constraint:** the current Level Editor navigation is being actively refined in the
pre-existing dirty work (§0.1). Bowl support is an entity-preview/geometry/validation concern;
it is not permission to restructure tabs, toolbar order, Level Browser, or the Entities drawer.

The existing smooth vector **layout** background preview (`LevelEditorLayoutPreview*`) is separate
from receiver art. Keep it unchanged.

When `GameplayRuntimeProfile.receiverStyle = Bowl`:

- Level Editor canvas draws Bowl, not Cup art.
- color palette previews Bowl in all 7 colors.
- placement footprint/bounds reflect the actual active Bowl shape/bounds.
- validation capacity uses Bowl receiver shape.
- `Required Amount > Capacity` message uses Bowl capacity.
- min-size/grid warning from §6.2 is surfaced.
- one scalar `Size` concept is shown for Bowl; do not show independent Width/Height controls.

When style = Cup:
- current Cup preview/editor path remains available.

Do not serialize receiver style into each level.

### Size UI caveat

Because current schema has no per-Cup size, do not invent per-level editability silently.

If Bowl Size is profile-global, make the UI clear that it is the active/global receiver size, not a hidden level field.

If Ducan later approves a per-level size schema, handle it in a follow-up change.

---

# 13. JarPreviewUtility

Extend:

```text
JarPreviewKind.Source
JarPreviewKind.Cup
JarPreviewKind.Bowl
```

Bowl preview must composite:
- shadow;
- colorized Main using the same CPU mirror of `SE001/JarTint`;
- untinted Spec on top.

Cache by all relevant Bowl/profile/material identities.

No allocation/rebuild per repaint.

Existing cache test remains valid; add Bowl coverage.

The Level Editor must not use a visually different ad-hoc Bowl renderer from runtime.

---

# 14. Asset setup

Extend `JarVisualAssetSetup` as the idempotent source for Bowl generated assets.

Expected responsibilities:
- configure Bowl texture import settings;
- measure T_Bowl_Main/T_Bowl_Spec;
- bake silhouette/contours/profile data;
- bake Bowl shadow mask/mesh;
- create/update Bowl materials;
- create/update Bowl meshes;
- create/update Bowl prefab;
- bind Bowl profile/material/prefab references;
- keep existing Source/Cup assets working.

Do not expose new GD-facing menu clutter under `SE001`. Phase D intentionally reduced the production menu to Level Editor + Layout Bake.

Bowl setup can remain callable through development code/tests or existing dev-only setup paths without a new production `MenuItem`.

---

# 15. Tests

Mandatory new tests:

```text
Bowl_Contour_Bake_IsDeterministic_AndClosedAtBottom
Bowl_Uniform_Size_HeightFollowsAspect
Bowl_Walls_MatchArtContour
Bowl_Capacity_FromPolygonArea
ReceiverStyle_Switch_SpawnsCupOrBowl_WithoutDataChange
BowlMaterials_Rebuild_Idempotent_PreservesGuids
Bowl_BlockedCells_StayInsideOuterSilhouette
Bowl_FillToRim_NoGrainOutsideGlass
```

Also add/extend:

```text
JarPreview_Bowl_Cache_NoRebuildWithoutChange
JarPreview_Bowl_UsesCorrectColorAndUntintedSpec
LevelEditor_BowlPreview_UsesActiveReceiverStyle
LevelEditor_BowlValidation_UsesBowlCapacity
```

Regression:
- existing Cup tests with explicit style = Cup;
- Source tests unchanged;
- Level Editor tests;
- playthrough/reload tests;
- visual factory tests;
- full EditMode suite.

Do not remove old Cup assertions just to make Bowl pass.

---

# 16. Evidence

Create:

```text
handoff/phase-V1b/evidence/
```

Required:

1. `bowl-measurements.md`
2. Play Mode screenshots:
   - all 7 Bowl colors;
   - empty;
   - filling;
   - full/closed rim;
3. close-up curved bottom while filling:
   - smallest supported Bowl size;
   - largest supported Bowl size;
4. Cup ↔ Bowl style switch before/after using the same level data;
5. Level Editor canvas showing Bowl;
6. Level Editor color palette with Bowl;
7. draw-call comparison Cup vs Bowl;
8. no steady-state GC allocation from Bowl presentation;
9. evidence that rebuild preserves material/prefab/profile GUIDs;
10. reference comparison with `reference/bowl-vs-cup.png`.

If screenshots or profiler evidence require Ducan's live Unity interaction, report exactly which captures remain manual. Do not invent them.

---

# 17. Performance constraints

Mobile-first constraints remain.

Forbidden:
- per-frame texture alpha analysis;
- per-frame polygon simplification;
- per-frame wall rasterization;
- per-frame mesh/material/texture creation;
- `renderer.material`;
- GameObject-per-grain;
- simulation resolution increase just for Bowl visuals.

Expected:
- contour/art bake only in Editor;
- receiver grid geometry built/stamped once per level load;
- steady-state Bowl visual GC alloc = 0;
- shared materials/meshes;
- MPB only if needed for per-instance feedback;
- draw-call impact measured vs Cup.

---

# 18. Do not expand scope

Do not:
- alter/revert the current Edit / Level Browser / Entities Level Editor UX;
- use the current Phase D blocked status as a reason to run a Phase D closure project inside V1b;
- change Source art/behavior;
- redesign sand simulation;
- alter logical amount semantics;
- change level progression;
- rewrite level JSON for style;
- delete Cup assets;
- delete old V1 materials;
- retune Cup colors while tuning Bowl;
- change Layout Bake;
- reopen unrelated Phase D work;
- start new gameplay gimmicks.

This packet changes receiver geometry/presentation behind one global style switch.

---

# 19. Likely file ownership

Inspect first; this is a guide, not a blind patch list.

Likely touched/new:

```text
Assets/_Core/4_Scripts/Data/GameplayRuntimeProfile.cs
Assets/_Core/4_Scripts/Data/PrefabProfile.cs
Assets/_Core/4_Scripts/Data/ColorProfile.cs
Assets/_Core/4_Scripts/Data/JarVisualProfile.cs              // only shared facts
Assets/_Core/4_Scripts/Data/BowlVisualProfile.cs             // preferred Bowl-specific facts
Assets/_Core/4_Scripts/System/Management/PhaseCEntities.cs
Assets/_Core/4_Scripts/Creation/ReceiverFactory.cs
Assets/_Core/4_Scripts/Creation/CupFactory.cs                // compatibility/refactor as needed
Assets/_Core/4_Scripts/Presentation/BowlVisual.cs
Assets/_Core/4_Scripts/System/Creation/LevelSpawner.cs
Assets/_Core/4_Scripts/Editor/JarVisualAssetSetup.cs
Assets/_Core/4_Scripts/Editor/JarPreviewUtility.cs
Assets/_Core/4_Scripts/Editor/Level/C#/LevelEditor/*
Assets/_Core/4_Scripts/Tests/Editor/*
Assets/_Core/4_Scripts/Tests/PlayMode/*                      // if project convention uses this
Assets/_Core/Resources/Profiles/BowlVisualProfile.asset
Assets/_Core/Resources/Profiles/PhaseCGameplayRuntimeProfile.asset
Assets/_Core/Resources/Profiles/PhaseBPrefabProfile.asset
Assets/_Core/Resources/Profiles/PhaseCColorProfile.asset
Assets/_Core/1_Materials/Jar/MAT_BowlMain_*.mat
Assets/_Core/1_Materials/Jar/MAT_Jar_BowlSpec.mat
Assets/_Core/1_Materials/Jar/MAT_Jar_BowlShadow.mat
Assets/_Core/3_Prefabs/Gameplay/Bowl/Bowl.prefab
Assets/_Core/3_Prefabs/Gameplay/JarVisualMeshes/*
handoff/phase-V1b/*
```

Do not modify level JSON unless Ducan explicitly approves at §8 checkpoint.

---

# 20. Verification gate

Before `DONE`:

```text
[ ] real Bowl texture measurements recorded
[ ] Bowl contour bake deterministic
[ ] outer / inner / blocking geometry separated correctly
[ ] ≤ 40 point simplified contour
[ ] Bowl profile generated idempotently
[ ] receiverStyle exists and production default = Bowl
[ ] Cup switch remains functional
[ ] Bowl factory/prefab uses View/{Shadow,Main,Spec}
[ ] seven dedicated Bowl Main materials exist
[ ] Spec is untinted
[ ] Bowl material rebuild preserves GUIDs
[ ] curved walls/capacity/full use art-derived shape
[ ] no per-frame receiver rasterization
[ ] min/max blocked-cell silhouette test PASS
[ ] fill-to-rim leak test PASS
[ ] Level Editor uses active Bowl art
[ ] Bowl one-dimensional uniform Size rule honored
[ ] no level JSON/schema change without Ducan approval
[ ] existing Cup tests PASS with style Cup
[ ] targeted Bowl tests PASS
[ ] Level Editor tests PASS
[ ] full EditMode regression PASS or unrelated failures proven
[ ] compile clean
[ ] console clean
[ ] scoped style gate PASS
[ ] git diff --check PASS
[ ] steady-state Bowl GC alloc evidence
[ ] draw-call Cup vs Bowl evidence
[ ] screenshots/manual verification recorded honestly
[ ] pre-existing dirty Level Editor UX files preserved/merged without regression
[ ] dirty Level_01.json and Level_02.json preserved byte-for-byte unless Ducan explicitly edits them during verification
[ ] current LevelEditorLayoutPreview vector background remains functional
[ ] current grainsPerUnit=115 logical amount contract remains intact
[ ] unrelated files preserved
```

---

# 21. C2C reporting

## INIT

Report only:
- workspace name;
- HEAD/branch;
- dirty state;
- V1 receiver-related files inspected;
- actual Bowl texture presence;
- expected owned areas.

## PLAN

Use:

```text
Requirement
→ owner/file
→ invariant
→ verification
```

Include explicit rows for:
- texture measurement;
- contour bake;
- inner/blocking geometry;
- ReceiverShape/CupDomain refactor;
- switch;
- Bowl visual/prefab/materials;
- editor preview;
- size checkpoint;
- leak/capacity tests;
- regression/perf/evidence.

## EXECUTED

Use:

```text
Item | Claimed | Verified by | Evidence
```

Required rows:
- Measurements
- Bowl contour
- Grid-safe wall inset
- Capacity
- Full/mouth close
- Materials 7/7
- Spec transform
- Shadow
- Receiver switch
- Cup backup regression
- Level Editor preview
- Size rule / whether level data changed
- No outside-glass leak
- Draw calls
- GC
- compile/tests/style/diff
- screenshots

## Final state

Exactly one:

```text
DONE
```

only when all implementation and verifiable gates pass and any required Ducan manual acceptance is recorded.

or:

```text
BLOCKED — <specific blocker + evidence>
```

If the only blocker is the size/data decision, use:

```text
BLOCKED — Bowl size measurement requires Ducan decision before any level-data/schema change.
```

Do not use `PARTIAL`.
