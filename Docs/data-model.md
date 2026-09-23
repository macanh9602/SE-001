# Data Model — SE-001

Data contract này triển khai architecture trong `SE001-ARCHITECTURE-BLUEPRINT.md`.

## 1. Data categories

| Loại | Ví dụ | Save? |
|---|---|---|
| Authoring source-of-truth | `SE001LevelJson`, board, stable entity IDs, Source/Cup/RotatingObstacle data | Có |
| Generated/baked | Resolved lookup, valid/static masks, editor preview cache | Không; regenerate được |
| Runtime state | Dynamic draw mask, sand buffers, counters, pending emissions, signals | Không; tạo/hủy theo level |

Scene hierarchy, Mesh, Collider, visual line, debug geometry và ParticleSystem không phải authoring
source-of-truth.

## 2. `SE001LevelJson`

| Field | Type | Contract |
|---|---|---|
| `schemaVersion` | int | Serialized contract version |
| `levelId` | string | Stable unique level identifier |
| `layoutId` | string | Stable reference to a baked `LayoutDefinition` |
| `board` | object | Authored bounds/cell/valid-area contract |
| `staticObstacles` | array | Legacy schema 2 input only; removed from schema 3 JSON |
| `sources` | array | Stable material source definitions; có thể rỗng |
| `cups` | array | Stable collection target definitions; có thể rỗng |
| `rotatingObstacles` | array | Per-level Cross obstacles with position, scale, length, initial angle and signed rotation speed |

Schema owner là Level Data feature. JSON nằm tại `Assets/_Core/Resources/Levels/`. Array index không
phải identity; mọi entity dùng stable string ID.

## 3. Entity contracts

### StaticObstacle

- Stable ID.
- Authored polyline/shape points trong board space.
- Thickness.
- Optional presentation style ID.
- Cùng data đi vào shared `ObstacleRasterizer` và presentation builder.

Static obstacle mask là generated runtime data. Collider hoặc generated mesh không được serialize làm
truth thay thế.

### Source

- Stable ID, material ID, authored position.
- Body size, emission rate and stream width resolve only from `SourceProfile`.
- Logical material/grain amount.
- Level JSON does not store body size, emission rate or stream width.

Visual anchor có thể hỗ trợ alignment nhưng không quyết định logical emission position/count.

### Cup

- Stable ID, accepted material ID.
- Authored position; outside width/height resolve only from `CupProfile.bodySize`.

### Phase C position and material contract

`SourceData.position` is the authored nozzle position. `SourceProfile.bodySize` drives both presentation bounds and tap hit-area. `CupData.position` is the centre of the cup's outside bottom; `CupProfile.bodySize` defines the outside mouth/height rectangle. `materialId` must resolve in `ColorProfile` before a level can spawn. Runtime sand, masks and collected counts are generated state and are not serialized back into level JSON.
- Required count.
- Foreign-material tolerance chỉ khi gameplay contract yêu cầu.

Visual cup mesh không quyết định sink geometry.

### RotatingObstacle (Cross)

- Stable ID, board-space pivot position, scale, unscaled bar length, initial angle and signed degrees per second are authored in schema-4 level JSON.
- `RotatingObstacleProfile` owns global bar width and bounded sand push search radius. Two 9-slice SpriteRenderers use the shared `MAT_Obstacle` material; art can replace the placeholder sprite in the prefab.
- Runtime rotates at the fixed sand step. A separate transient rotating mask blocks grains and pushes grains swept by the bars without erasing player strokes or changing emitted/occupied counts.

### Dynamic DrawStroke

- Runtime source-of-truth là accepted player stroke command/state.
- Không serialize ngược vào level JSON.
- Dynamic mask dùng cùng board-space mapping/rasterization rules với StaticObstacle.
- Retry/unload xóa cả visual stroke và dynamic mask.

## 4. Runtime ownership

- `LevelRuntimeState`: stable-ID index và semantic per-level records; non-static, không serialize.
- `SandSimulation`: grain/material buffers, valid/static/dynamic masks và authoritative physical state.
- `RotatingObstacleSystem`: per-level moving mask and sand sweep; transient and not serialized.
- `LevelContext`: lifetime/token và references do `LevelSpawner` tạo; không phải data source.
- Editor `Document` chứa `SE001LevelJson`; `ViewState` và `DerivedState` không serialize vào JSON.

## 5. Profiles and overrides

| Giá trị | Global | Level override | Resolver |
|---|---|---|---|
| Simulation/feel tunable | Feature Profile | Chỉ khi story/data contract cho phép | Một resolver duy nhất |
| Entity placement/count/geometry | Không | Level JSON | Level loader |
| Presentation prefab/material | `PrefabProfile` | Không | Factory/profile |
| Source/Cup body size, Source emission/stream, Cross bar width | Feature Profiles | Không | Runtime/editor profile lookup |
| Cross placement, scale, length, phase and speed | Không | Level JSON | RotatingObstacleSystem |

Không giữ hai nguồn cho cùng một sự thật. Optional override phải phân biệt rõ unset với giá trị hợp lệ.

## 6. Validation and invariants

- `schemaVersion` tồn tại và được hỗ trợ.
- `levelId` và mọi entity stable ID unique; không dangling reference.
- Board-space entity geometry hữu hạn, nằm trong contract của `LayoutDefinition`.
- Schema 3 có `layoutId`, không chứa `board.wallContours` hoặc `staticObstacles`.
- Schema 4 adds `rotatingObstacles` and removes Source/Cup size plus Source emission/stream overrides. The runtime can load schema 3 with current global Profile values; the Level Editor upgrades schema 3 in memory and saves schema 4.
- Runtime load đọc bit-packed `LayoutMaskAsset`; cell size/hash lệch phải block với lỗi rõ ràng.
- Static mask seed trước dynamic mask và trước Source emission.
- Runtime và Editor preview dùng cùng board mapper/rasterizer, có parity tests.
- Accounting bảo toàn:
  `emitted = inField + collected + spilled/lost + pending`.
- Generated cache/runtime state không được save thay authoring data.

## 7. Schema evolution

## 8. Phase B canonical geometry clarification

Schema 2 is the historical Phase B contour contract. It contains `schemaVersion`, `levelId`, `board`
with wall contours, `staticObstacles`, and entity arrays. The editor migration converts it to schema 3.
Schema 3 contains `schemaVersion`, `levelId`, `layoutId`, `board.size`, and entity arrays; the layout
geometry is stored in `LayoutDefinition` and its baked `LayoutMaskAsset`/prefab.

Contours are closed simple polygons represented by finite board-space points. Duplicate terminal points,
fewer than three unique vertices, degenerate contours, missing IDs, and duplicate IDs are validation errors.
Concave contours and multiple independent contours are supported. Holes/compound boolean contours are not
silently approximated in Phase B.

Meshes, triangulation, raster masks, bevel geometry, colliders, and SVG commands are generated/baked data
and are never serialized as level JSON authoring truth. A `LayoutDefinition` is the reviewed generated
asset that owns the baked prefab and bit-packed mask for one layout identity.

- Field mới có safe default có thể không bump schema khi backward compatibility được test.
- Đổi nghĩa/xóa/đổi type field bắt buộc bump schema và có migration/validation rõ.
- Schema 2 → schema 3 được xử lý bằng workflow migration/editor; runtime không đọc schema 2.
- Story thay serialization contract phải được Product Owner approve trước implementation.
