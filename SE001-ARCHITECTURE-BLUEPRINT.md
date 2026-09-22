# SE-001 Architecture Blueprint — alignment from sample project

Status: **ACCEPTED — project architecture source-of-truth**.
Adoption baseline: `main` at commit `6f7d08f`.

This blueprint governs SE-001 architecture and supersedes the retired Story 000A and the former
Story 001–014 execution pack. `standards/system-design.md` remains the generic guardrail;
`Docs/runtime-architecture.md` and `Docs/data-model.md` are the implementation-facing contracts
derived from this blueprint.

## 1. Audit inputs

### SE-001 baseline
- Canonical script root: `Assets/_Core/4_Scripts`.
- Existing scaffold: `Creation`, `Elements`, `System/Bootstrapper`, `System/Creation`, `System/Management`, `Editor/Level/C#`, `Editor/Level/USS`, `Editor/Level/UXML`.
- Foundation currently present: `LevelManager`, `LevelContext`, `LevelRuntimeState`, `LevelReadinessGate`, `ILevelLifecycleParticipant`.
- `PrefabProfile` is still a placeholder; production factories/spawner/entities are not implemented yet.

### Reference project (`Assets(3).zip`)
Relevant audited files:
- `Assets/_Core/Scripts/System/Management/LevelManager.cs`
- `Assets/_Core/Scripts/System/Creation/CakeLevelSpawner.cs`
- `Assets/_Core/Scripts/System/Bootstrapper/PrefabProfile.cs`
- `Assets/_Core/Scripts/Creation/CakeStripFactory.cs`
- `Assets/_Core/Scripts/Creation/CakeQueueSlotFactory.cs`
- `Assets/_Core/Scripts/Creation/CakeTrayLineFactory.cs`
- `Assets/_Core/Scripts/Creation/CakeStripCreateParameters.cs`
- `Assets/_Core/Scripts/Elements/Strip/CakeStripDomain.cs`
- `Assets/_Core/Scripts/Elements/Strip/CakeStripVisual.cs`
- `Assets/_Core/Prefabs/Cake/CakeStrip.prefab`
- `Assets/_Core/Prefabs/Cake/CakeQueueSlot.prefab`
- `Assets/_Core/Scripts/Editor/Level/C#/CakeLevelEditorWindow*.cs`
- `Assets/_Core/Scripts/Editor/Level/UXML/CakeLevelEditorWindow.uxml`
- `Assets/_Core/Scripts/Editor/Level/USS/CakeLevelEditorWindow.uss`
- `Assets/_Core/Scripts/Editor/Level/C#/CakeProjectSetup.cs`

### Level Editor skill
The existing skill correctly specifies Document/ViewState/DerivedState separation, `ApplyEdit != ReloadDocument`, stable-id selection, responsive split panes, five-surface validation, unsaved Play Test, and the delivery checklist. Its current weakness is that it contains contracts/checklists but no reusable C#/UXML/USS code recipes.

---

## 2. Main conclusion

SE-001 should keep its existing folder convention but adopt the reference project's composition pattern:

```text
GameScene
    ↓
LevelManager                     lifecycle / level selection only
    ↓
LevelSpawner                     composition + spawn/unload owner
    ↓
LevelJson → Validate/Resolve
    ↓
LevelRuntimeState + SandSimulation + roots
    ↓
Factories
    ├── SourceFactory
    ├── CupFactory
    ├── StaticObstacleFactory
    └── DrawStrokeFactory
            ↓
      pooled/prefab entities
      Domain + Visual
    ↓
GameplayManager                  per-level orchestration
    ├── Sources → emit commands
    ├── SandSimulation.Step
    ├── Cups → collection/accounting
    └── End-condition evaluation
    ↓
GameplayInputController / HUD
```

`LevelManager` must not be the gameplay/spawn god object. `LevelSpawner` owns creation and cleanup. Factories own prefab acquisition and binding. Domain decides gameplay. Simulation owns sand pose/state. Visual is replaceable presentation.

---

## 3. What from commit 6f7d08f is kept vs changed

| Current foundation item | Decision | Target |
|---|---|---|
| `LevelContext` lifetime/cancellation concept | KEEP + ADAPT | Context receives roots/runtime references created by Spawner; it should not invent gameplay content itself. |
| `LevelRuntimeState` per-level/non-static concept | KEEP + EXPAND CAREFULLY | Stable-id/index registry and per-level records; no sand cell buffer inside it. |
| `LevelReadinessGate` | KEEP | Close before unload/load; open only after factories + gameplay binding complete. |
| `ILevelLifecycleParticipant` | KEEP | Useful for cleanup/unbind contracts. |
| `LevelManager.Begin/Reload/Unload` API | KEEP CONCEPT | Delegate actual load/spawn to `LevelSpawner`. |
| `RuntimeInitializeOnLoadMethod` auto-create manager | REMOVE for production | Scene-authored manager and dependencies are explicit. No hidden manager creation if `GameScene` already owns it. |
| `foundation_smoke` automatic level in `Awake` | REMOVE from production flow | Smoke harness/test may remain editor/test-only. Actual game loads authored level data. |
| LevelContext creating `LevelRoot/BoardRoot/SimulationRoot/VisualRoot` | MOVE OWNERSHIP | `LevelSpawner` creates/owns runtime hierarchy, then passes references into context. |

Recommended scene component relation:

```text
LevelManager GameObject
├── LevelManager
├── LevelSpawner
├── GameplayManager
├── GameplayInputController
├── Pool adapter/provider
└── HUD bridge/provider
```

`LevelManager` should require/get `LevelSpawner` similarly to the reference project's `[RequireComponent(typeof(CakeLevelSpawner))]` pattern.

---

## 4. Canonical SE-001 folder ownership

Do not introduce another `Assets/_Core/Scripts` tree. Use the existing project scaffold.

```text
Assets/_Core/4_Scripts/
├── Commons/
│   ├── ICreateParameters.cs
│   ├── IFactory.cs
│   ├── IRuntimeCreatable.cs
│   └── IPendingCleanup.cs
│
├── Data/
│   ├── Level/
│   │   ├── SE001LevelJson.cs
│   │   ├── BoardData.cs
│   │   ├── SourceData.cs
│   │   ├── CupData.cs
│   │   ├── StaticObstacleData.cs
│   │   └── LevelValidator.cs
│   └── Profiles/
│       ├── SandSimulationProfile.cs
│       ├── SandVisualProfile.cs
│       ├── SourceProfile.cs
│       ├── CupProfile.cs
│       └── DrawPathProfile.cs
│
├── Creation/
│   ├── SourceCreateParameters.cs
│   ├── SourceFactory.cs
│   ├── CupCreateParameters.cs
│   ├── CupFactory.cs
│   ├── StaticObstacleCreateParameters.cs
│   ├── StaticObstacleFactory.cs
│   ├── DrawStrokeCreateParameters.cs
│   └── DrawStrokeFactory.cs
│
├── Elements/
│   ├── Source/
│   │   ├── SandSourceDomain.cs
│   │   └── SandSourceVisual.cs
│   ├── Cup/
│   │   ├── CupDomain.cs
│   │   └── CupVisual.cs
│   ├── Obstacle/
│   │   ├── StaticObstacleDomain.cs   (only if semantic state is needed)
│   │   ├── StaticObstacleVisual.cs
│   │   └── ObstacleRasterizer.cs
│   ├── Draw/
│   │   ├── DrawStrokeDomain.cs       (if persistent gameplay state is needed)
│   │   └── DrawStrokeVisual.cs
│   └── Sand/
│       └── SandFieldVisual.cs
│
├── Simulation/
│   └── Sand/
│       ├── SandSimulation.cs
│       ├── SandSimulationState.cs
│       └── SandSemanticEvents.cs
│
├── System/
│   ├── Bootstrapper/
│   │   ├── PrefabProfile.cs
│   │   └── GameplayRuntimeProfile.cs
│   ├── Creation/
│   │   └── LevelSpawner.cs
│   └── Management/
│       ├── LevelManager.cs
│       ├── LevelContext.cs
│       ├── LevelRuntimeState.cs
│       ├── GameplayManager.cs
│       ├── GameplayInputController.cs
│       └── LevelReadinessGate.cs
│
└── Editor/Level/
    ├── C#/
    │   ├── LevelEditorWindow.cs
    │   ├── LevelEditorWindow.State.cs
    │   ├── LevelEditorWindow.Serialization.cs
    │   ├── LevelEditorWindow.Navigation.cs
    │   ├── LevelEditorWindow.Inspector.cs
    │   ├── LevelEditorWindow.Validation.cs
    │   ├── LevelEditorWindow.Modes.cs       (only when needed)
    │   └── Views/
    │       ├── BoardCanvas.cs
    │       └── ValidationPanel.cs
    ├── UXML/
    │   └── LevelEditorWindow.uxml
    └── USS/
        └── LevelEditorWindow.uss
```

Do not create every listed class on day one. This tree defines ownership. Add only a class that has an actual responsibility.

---

## 5. Prefab architecture

Keep SE-001's current prefab root (`Assets/_Core/3_Prefabs`) rather than copying the sample path.

```text
Assets/_Core/3_Prefabs/Gameplay/
├── Source/SandSource.prefab
├── Cup/Cup.prefab
├── Obstacle/StaticObstacle.prefab
├── Obstacle/DrawStroke.prefab
└── Sand/SandField.prefab
```

### Source prefab

```text
SandSource                         Root = behavior/composition
├── SandSourceDomain
├── SandSourceVisual
├── View                           ART-ONLY replaceable child
│   ├── MeshFilter
│   └── MeshRenderer
└── Anchors
    └── EmitPoint                  presentation alignment anchor
```

Gameplay emission position/width is authored data. Replacing the shaker mesh/material must not change simulation truth.

### Cup prefab

```text
Cup
├── CupDomain
├── CupVisual
├── View                           ART-ONLY
│   ├── MeshFilter
│   └── MeshRenderer
├── FillView                       visual fill only
└── Anchors
    ├── Entry
    └── Feedback
```

Cup sink/collection geometry is authored data converted to simulation cells. It is not inferred from the visible cup mesh.

### Static obstacle prefab

```text
StaticObstacle
├── StaticObstacleVisual
└── View
    ├── MeshFilter                 procedural ribbon/shape mesh from authored polyline
    └── MeshRenderer
```

The obstacle's gameplay truth is `points + thickness` in level data. `ObstacleRasterizer` converts the same data to the static simulation mask. `StaticObstacleVisual` builds presentation from the same data but cannot decide collision.

### Player draw-stroke prefab

```text
DrawStroke
├── DrawStrokeVisual
└── View
    └── LineRenderer / generated mesh
```

A pooled visual represents the stroke. The dynamic obstacle mask is a separate simulation state generated through the same rasterization geometry rules.

### Sand field prefab

```text
SandField
├── SandFieldVisual
└── View
    └── Renderer
```

There is one/few render surfaces, never one GameObject per grain. Sand simulation remains pure/native data.

---

## 6. PrefabProfile contract

`PrefabProfile` should become the central presentation/composition profile, equivalent in role to the reference project's profile.

Minimum ownership:

```text
Elements
- SourcePrefab
- CupPrefab
- StaticObstaclePrefab
- DrawStrokePrefab
- SandFieldPrefab

Shared presentation
- source/cup/obstacle/draw materials as needed
- optional material sets by material ID

Pool hints
- source prewarm
- cup prewarm
- static obstacle prewarm
- draw stroke prewarm

No gameplay rules here.
```

Simulation/interaction tunables live in their own Profiles. `GameplayRuntimeProfile` may aggregate references to these profiles if the scene otherwise accumulates too many individual serialized fields.

---

## 7. Factory rules

Use Factory where object/prefab/pool lifecycle exists. Do not wrap every pure C# object in a Factory just to mimic the sample.

### Factory-owned
- Source prefab/domain/visual binding.
- Cup prefab/domain/visual binding.
- Static obstacle visual/entity creation.
- Player draw-stroke pooled visual creation.
- Any pooled VFX entity later.

### Direct construction is acceptable
- `SandSimulation` / native buffers.
- lightweight value/runtime records.
- resolver/validator/rasterizer services without Unity object lifetime.

Factory flow:

```text
LevelSpawner
    ↓
factory.Create(CreateParameters, levelToken)
    ↓
pool.Spawn(PrefabProfile.XPrefab)
    ↓
validate prefab contract
    ↓
Visual.Initialize(...)
Domain.OnCreated(...)
    ↓
register runtime state / gameplay manager
```

Every pooled entity must implement a deterministic cleanup/reset path before recycle.

---

## 8. Level spawn / unload lifecycle

### Load

```text
LevelManager.LoadLevel(id)
    ↓
close input/readiness
    ↓
LevelSpawner.SpawnLevelAsync(level)
    ↓
cancel + cleanup previous level
    ↓
load/validate/resolve data
    ↓
create level CancellationToken
    ↓
create LevelRuntimeState
    ↓
create SandSimulation
    ↓
create LevelRoot
    ├── BoardRoot
    ├── ObstacleRoot
    ├── SourceRoot
    ├── CupRoot
    ├── DynamicDrawRoot
    ├── SandVisualRoot
    └── VfxRoot
    ↓
seed board valid mask
    ↓
seed STATIC obstacle mask
    ↓
create static obstacle visuals through Factory
    ↓
create cups through Factory
    ↓
create sources through Factory
    ↓
create/bind SandField visual
    ↓
bind GameplayManager
    ↓
bind GameplayInputController
    ↓
LevelReady
    ↓
open readiness/input
```

Critical invariant: static obstacle simulation state exists before any source is allowed to emit.

### Unload

```text
close input/readiness
→ LevelWillUnload
→ input unbind
→ cancel level token
→ gameplay/domain cleanup
→ recycle pooled views/entities
→ dispose SandSimulation/native buffers
→ dispose LevelRuntimeState
→ destroy empty LevelRoot
```

Pool persists across levels.

---

## 9. Gameplay orchestration

`GameplayManager` is the one per-level orchestration owner; it is not the same as `LevelManager`.

Suggested responsibility:

```text
GameplayManager.Tick
    1. sources produce semantic emit requests
    2. SandSimulation accepts/emits pending material
    3. SandSimulation fixed-step update
    4. cup collection bridge consumes grains in authored sink masks
    5. accounting is updated
    6. end condition evaluates only semantic counters/state
    7. Visual/HUD receive events/state changes
```

Input callback does not mutate sand/domain directly. It emits draw commands/path intents; the gameplay/simulation bridge commits them in deterministic order.

---

## 10. Obstacle architecture — explicit contract

Obstacle must not be treated as a tiny field inside `LevelJson`; it is a first-class authored feature.

### Static obstacle
- Authoring SoT: stable ID, polyline/shape points, thickness, optional presentation style ID.
- Runtime: rasterized into `staticObstacleMask`.
- Presentation: prefab root + `View` child; generated mesh/line from the authored shape.
- Factory: creates/binds the presentation entity and registers it by stable ID.
- Editor: create/delete/select/move points, insert/remove polyline point, thickness edit, canvas handles, validation.

### Dynamic player obstacle
- Runtime SoT: accepted player stroke command/state; not serialized back to level JSON.
- Simulation: rasterized into `dynamicObstacleMask` using the same geometry contract.
- Presentation: pooled `DrawStroke` prefab.
- Retry/unload: clears both visual strokes and dynamic mask.

### Shared geometry
Runtime and Editor must share the same board-space mapper and obstacle rasterization rules. Editor preview must never reimplement a second approximation.

---

## 11. Source and Cup contracts

### Source

Data:
- stable ID
- material ID
- authored position
- logical grain/material amount
- stream width/shape override only when the product needs it

Domain:
- remaining/pending emission accounting
- source state
- issues semantic emit commands

Visual:
- shaker/source art and stream presentation
- child `View` is replaceable
- no control over logical emitted count

### Cup

Data:
- stable ID
- accepted material ID
- authored sink shape/position/size
- required count
- foreign-material tolerance if gameplay requires it

Domain:
- accepted/foreign collected counters
- completion/contamination state

Visual:
- replaceable cup art
- fill/feedback presentation
- no simulation query through Renderer/Collider

---

## 12. Sand simulation contract

The useful sand algorithm from the discarded implementation may later be salvaged selectively, but architecture is fixed first.

Simulation owns:
- grain/material buffers
- valid/static/dynamic masks
- fall velocity / horizontal momentum needed by Powder feel
- fixed-step update
- deterministic accounting
- semantic interaction/contact events

Simulation does not own:
- Source/Cup prefab appearance
- HUD
- win/lose UI
- ParticleSystem/VFX authority

Visual owns one/few render surfaces fed by simulation state; no GameObject-per-grain design.

---

## 13. Level Editor architecture

The current scaffold is already correct and must be used:

```text
Editor/Level/
├── C#/
├── UXML/
└── USS/
```

### State model

```text
Document
- SE001LevelJson
- current asset path
- dirty state

ViewState
- mode/tool
- stable selection IDs
- selected obstacle point index
- zoom/pan
- search/filter
- pane widths/collapse state

DerivedState
- validation issues
- rasterized obstacle preview
- simulation/route preview summaries
- stale flags/performance metrics
```

ViewState and DerivedState are never serialized into level JSON.

### Required update paths

```text
ApplyEdit(change)
- mutate targeted document state
- one undo intent
- preserve ViewState
- invalidate only relevant derived data

ReloadDocument(doc)
- replace full document
- controlled selection remap by stable ID
- full derived rebuild
- no edit undo entry

ChangeView(...)
- ViewState only
- no dirty document

Undo/Redo
- restore Document
- preserve view where semantically valid
```

### Window composition

Use UI Toolkit split panes similarly to the reference implementation, but keep only SE-001's needed complexity:

```text
┌ Levels ┬──────────── Board Canvas ───────────┬ Inspector ┐
│ list   │ toolbar: tools/options             │ entity    │
│ search │                                   │ fields    │
│ New... │                                   │           │
├────────┴────────────────────────────────────┴───────────┤
│ Status: file · dirty · blocking/warning counts          │
└─────────────────────────────────────────────────────────┘
```

Global Save / Undo / Redo / Play Test positions are stable. Panes are real `TwoPaneSplitView`/split views with resize behavior; no permanent floating overlay over the canvas.

### Authoring modes/tools needed for this game
- Board/draw-area editing.
- Source placement and properties.
- Cup placement/size/material properties.
- Static obstacle polyline authoring.
- Selection/move/delete/duplicate.
- Validation + click-to-focus.
- Preview masks using shared runtime rasterizer.
- Play Test current unsaved document.

Do not build convenience features until this complete workflow works.

### Unsaved Play Test

Harvest the sample project's pattern:

```text
current Editor Document
    ↓ serialize to temporary JSON
EditorPrefs/session one-shot override path
    ↓ enter Play Mode
LevelManager consumes override ONCE
    ↓ normal LevelSpawner path
```

This guarantees editor and runtime use the same loader/spawner pipeline.

---

## 14. Level Editor skill gap

The current `skills/level-editor` contract is conceptually good, but a low-reasoning worker still has to invent too much implementation detail. Before relying on it broadly, harvest generic code recipes from the reference project into the skill, not project-specific Cake code.

Useful generic recipes to add later:
- partial `EditorWindow` composition skeleton;
- `Document/ViewState/DerivedState` skeleton;
- `ApplyEdit` / `ReloadDocument` sample;
- `TwoPaneSplitView` UXML skeleton;
- delayed field + callback lifecycle recipe;
- stable selection remap recipe;
- validation panel/click-to-focus recipe;
- unsaved Play Test temp JSON recipe;
- session/view-state persistence recipe.

The skill should reference those recipes when a worker implements a Level Editor.

---

## 15. Patterns to harvest vs patterns NOT to copy

| Reference pattern | SE-001 decision |
|---|---|
| `LevelManager → LevelSpawner` | HARVEST |
| central `PrefabProfile` | HARVEST |
| pooled Factory entity creation | HARVEST |
| Root behavior + child `View` | HARVEST |
| explicit CreateParameters/dependencies | HARVEST |
| reverse cleanup/recycle discipline | HARVEST |
| Level Editor partial class separation | HARVEST |
| Document/ViewState/DerivedState | HARVEST |
| unsaved Play Test override | HARVEST |
| Cake-specific queue/tray/strip rules | DO NOT COPY |
| Cake-specific graph/bake semantics | DO NOT COPY |
| sample's exact folder names (`_Core/Scripts`, `_Core/Prefabs`) | DO NOT COPY; keep SE-001 `4_Scripts` / `3_Prefabs` |
| large mature editor's difficulty/tray features | DO NOT COPY until SE-001 needs them |

---

## 16. Architecture gates before feature implementation resumes

Feature implementation should not resume until these contracts are approved:

| Gate | Must be explicit |
|---|---|
| Runtime ownership | LevelManager vs LevelSpawner vs GameplayManager responsibilities |
| Entity composition | Source, Cup, StaticObstacle, DrawStroke each has Data / Domain / Visual / Factory policy |
| Prefab contract | Root behavior + replaceable child `View`; gameplay geometry independent from art mesh |
| Profiles | PrefabProfile and simulation/interaction profile ownership |
| Sand authority | simulation-driven 2D, renderer/VFX presentation only |
| Obstacle parity | shared geometry/rasterizer between runtime and editor preview |
| Editor architecture | C#/UXML/USS scaffold + state model + Play Test path |
| Lifecycle | load/readiness/input and unload/cancel/recycle order |

Only after these are accepted should the roadmap be rewritten into implementation stories.

---

## 17. Suggested implementation order (not stories yet)

```text
A. Align foundation
   LevelManager → LevelSpawner ownership
   PrefabProfile + scene dependency wiring

B. One vertical entity slice
   StaticObstacle Data → Factory → Prefab/View → Simulation mask → Editor preview
   (best proof because it crosses every architecture layer)

C. Sand technical slice
   Simulation + SandField visual using approved roots/profiles

D. Source vertical slice
   Data → Domain → Factory → prefab/View → simulation emission

E. Cup vertical slice
   Data → Domain → Factory → prefab/View → accounting

F. Player draw vertical slice
   Input command → dynamic rasterizer → pooled DrawStroke View

G. GameplayManager/end-state integration

H. Level Editor production flow
   New/Open/Edit/Obstacle/Source/Cup/Validate/Save/unsaved Play Test

I. Feel/VFX and device performance
```

StaticObstacle is intentionally an early vertical slice: it proves Data, shared geometry, Factory, prefab View separation, Simulation and Editor parity before the project expands.

---

## 18. Salvage policy for the backup branch

Do not cherry-pick old feature commits wholesale. Later inspect and port only isolated implementation pieces that satisfy the new architecture, such as:
- proven Powder grid algorithm;
- conservation tests;
- obstacle segment/capsule rasterization math;
- board-space conversion math;
- source/cup accounting algorithms.

Do not salvage their old composition/orchestration/folder placement if it conflicts with this blueprint.
