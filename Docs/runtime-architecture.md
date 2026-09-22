# Runtime Architecture — SE-001

Đây là runtime contract áp dụng từ `SE001-ARCHITECTURE-BLUEPRINT.md`. Blueprint là project
architecture source-of-truth; file này mô tả ownership cần giữ ổn định khi implement.

## 1. Runtime ownership

```text
GameScene
  → LevelManager             lifecycle + level selection
  → LevelSpawner             validate/resolve + composition + spawn/unload
  → Factories                prefab/pool acquisition + binding
  → GameplayManager          per-level gameplay orchestration
  → Domain / SandSimulation  semantic rules / authoritative sand state
  → Visual / HUD             replaceable presentation
```

| Owner | Trách nhiệm | Không sở hữu |
|---|---|---|
| `LevelManager` | Begin/load/reload/unload, readiness coordination | Spawn details, roots, gameplay rule |
| `LevelSpawner` | Load/validate/resolve, create runtime objects and roots, factory order, cleanup | Level selection, win/lose |
| `LevelContext` | Carry level ID, token and references created by Spawner | Tự tạo roots hoặc gameplay content |
| `LevelRuntimeState` | Per-level stable-ID indexes and semantic records | Sand cell/native buffers, global singleton state |
| `GameplayManager` | Ordered per-level emit → simulate → collect → account → end-state flow | Scene selection, prefab acquisition |
| `SandSimulation` | Sand buffers, masks, fixed-step state and semantic contact results | HUD, prefab appearance, end UI |
| Factory | Acquire prefab/pool instance, validate and bind create parameters | Gameplay decisions |
| Visual/HUD | Render state and feedback | Gameplay authority hoặc raw simulation decisions |

## 2. Scene and dependency contract

- `GameScene` là scene duy nhất cho bootstrap và gameplay.
- Scene-authored `LevelManager` có explicit dependency tới `LevelSpawner`, `GameplayManager`, input,
  pool provider và HUD bridge/provider.
- Không production auto-create manager bằng `RuntimeInitializeOnLoadMethod`.
- `foundation_smoke` không tự chạy trong production; smoke harness chỉ nằm ở Tests/Development.
- Canonical code root là `Assets/_Core/4_Scripts`; không tạo tree `Assets/_Core/Scripts` song song.

## 3. Entity composition

Source, Cup, StaticObstacle và DrawStroke tuân contract Data / Domain nếu có semantic state /
Visual / Factory. Root behavior giữ composition; child `View` thay art được mà không đổi gameplay
geometry.

- Source emission geometry và amount đến từ authored data/domain, không từ mesh.
- Cup sink geometry đến từ authored data, không infer từ Renderer/Collider.
- StaticObstacle truth là stable ID + points/shape + thickness; visual và simulation cùng đọc data đó.
- DrawStroke visual được pool; dynamic obstacle mask là simulation state riêng.
- Sand dùng một hoặc ít render surfaces, không một GameObject/Transform cho mỗi grain/cell.

## 4. Profiles and factories

- `PrefabProfile` sở hữu prefab references, shared presentation assets và pool prewarm hints; không
  chứa gameplay rules.
- Simulation/interaction tunables thuộc feature Profiles; level override chỉ tồn tại khi data contract
  cho phép và phải đi qua một resolver.
- Factory chỉ dùng cho Unity object/prefab/pool lifecycle. Pure services, validators, rasterizers,
  runtime records và `SandSimulation` được construct trực tiếp bởi owner phù hợp.
- Mọi pooled entity có deterministic release/reset và full acquire/bind.

## 5. Load lifecycle

```text
LevelManager.LoadLevel(id)
→ close readiness/input
→ LevelSpawner cleanup previous level
→ load + validate + resolve authored data
→ create level token + LevelRuntimeState + SandSimulation
→ create LevelRoot and Board/Obstacle/Source/Cup/DynamicDraw/SandVisual/Vfx roots
→ seed board valid mask
→ seed static obstacle mask
→ create obstacle visuals → cups → sources → SandField
→ bind GameplayManager → bind input
→ LevelReady → open readiness/input
```

Static obstacle simulation state phải tồn tại trước khi Source có thể emit.

## 6. Unload lifecycle

```text
close readiness/input
→ LevelWillUnload
→ input unbind
→ cancel level token
→ gameplay/domain cleanup
→ recycle pooled views/entities
→ dispose SandSimulation/native buffers
→ dispose LevelRuntimeState
→ destroy empty LevelRoot
```

Pool sống qua level; chỉ clear khi đổi scene/application lifecycle yêu cầu.

## 7. Simulation and data flow

- Physics authority: Simulation-driven 2D trên board XY.
- Determinism: tolerance-based cho motion; accounting/count phải deterministic.
- Input callback chỉ tạo command/path intent. Gameplay/simulation bridge commit theo deterministic order.
- Domain nhận semantic signals; không ad-hoc query raw Renderer/Collider/Physics.
- Accounting invariant:
  `emitted = inField + collected + spilled/lost + pending`.

## 8. Shared obstacle geometry

Runtime và Level Editor dùng chung board-space mapper và obstacle rasterizer. Editor preview không có
implementation xấp xỉ riêng. Rasterizer tạo static/dynamic masks từ cùng geometry contract; visual
mesh/line chỉ là presentation.

## 9. Mobile performance guardrails

## 10. Phase B runtime boundary

The normal runtime path is `LevelManager -> LevelSpawner -> LevelDataLoader -> shared geometry ->
SandSimulation -> Visual`. `LevelManager` never parses SVG or generates meshes. SVG is editor/import input
only; runtime loads canonical JSON from `Resources/Levels`. `LevelSpawner` validates data before readiness,
creates per-level state and roots, seeds masks from canonical geometry, and owns partial-load cleanup.

Gameplay geometry is board-space XY. Presentation may extrude it along Z and add a top-to-side bevel, but
visual height/depth/bevel/material changes must not alter the gameplay polygon or generated masks.

- Không GameObject-per-grain/cell; không procedural mesh rebuild mỗi frame.
- Không allocation/LINQ/scene search trong hot path.
- Runtime query O(1) hoặc O(out-degree); buffers được reuse và dispose theo level.
- Pool Source, Cup, obstacle/draw views và VFX lặp lại.
- CPU, GPU, GC, draw call và memory được đo theo `standards/performance-budget.md`; device gate cuối
  thuộc roadmap phase I.
