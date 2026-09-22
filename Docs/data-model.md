# Data Model — SE-001

Data contract này triển khai architecture trong `SE001-ARCHITECTURE-BLUEPRINT.md`.

## 1. Data categories

| Loại | Ví dụ | Save? |
|---|---|---|
| Authoring source-of-truth | `SE001LevelJson`, board, stable entity IDs, Source/Cup/StaticObstacle data | Có |
| Generated/baked | Resolved lookup, valid/static masks, editor preview cache | Không; regenerate được |
| Runtime state | Dynamic draw mask, sand buffers, counters, pending emissions, signals | Không; tạo/hủy theo level |

Scene hierarchy, Mesh, Collider, visual line, debug geometry và ParticleSystem không phải authoring
source-of-truth.

## 2. `SE001LevelJson`

| Field | Type | Contract |
|---|---|---|
| `schemaVersion` | int | Serialized contract version |
| `levelId` | string | Stable unique level identifier |
| `board` | object | Authored bounds/cell/valid-area contract |
| `staticObstacles` | array | First-class authored obstacle definitions; có thể rỗng |
| `sources` | array | Stable material source definitions; có thể rỗng |
| `cups` | array | Stable collection target definitions; có thể rỗng |

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
- Logical material/grain amount.
- Stream width/shape override chỉ khi product contract yêu cầu.

Visual anchor có thể hỗ trợ alignment nhưng không quyết định logical emission position/count.

### Cup

- Stable ID, accepted material ID.
- Authored sink shape/position/size.
- Required count.
- Foreign-material tolerance chỉ khi gameplay contract yêu cầu.

Visual cup mesh không quyết định sink geometry.

### Dynamic DrawStroke

- Runtime source-of-truth là accepted player stroke command/state.
- Không serialize ngược vào level JSON.
- Dynamic mask dùng cùng board-space mapping/rasterization rules với StaticObstacle.
- Retry/unload xóa cả visual stroke và dynamic mask.

## 4. Runtime ownership

- `LevelRuntimeState`: stable-ID index và semantic per-level records; non-static, không serialize.
- `SandSimulation`: grain/material buffers, valid/static/dynamic masks và authoritative physical state.
- `LevelContext`: lifetime/token và references do `LevelSpawner` tạo; không phải data source.
- Editor `Document` chứa `SE001LevelJson`; `ViewState` và `DerivedState` không serialize vào JSON.

## 5. Profiles and overrides

| Giá trị | Global | Level override | Resolver |
|---|---|---|---|
| Simulation/feel tunable | Feature Profile | Chỉ khi story/data contract cho phép | Một resolver duy nhất |
| Entity placement/count/geometry | Không | Level JSON | Level loader |
| Presentation prefab/material | `PrefabProfile` | Không | Factory/profile |

Không giữ hai nguồn cho cùng một sự thật. Optional override phải phân biệt rõ unset với giá trị hợp lệ.

## 6. Validation and invariants

- `schemaVersion` tồn tại và được hỗ trợ.
- `levelId` và mọi entity stable ID unique; không dangling reference.
- Board-space geometry hữu hạn, nằm trong contract cho phép và rasterize được.
- Static mask seed trước dynamic mask và trước Source emission.
- Runtime và Editor preview dùng cùng board mapper/rasterizer, có parity tests.
- Accounting bảo toàn:
  `emitted = inField + collected + spilled/lost + pending`.
- Generated cache/runtime state không được save thay authoring data.

## 7. Schema evolution

## 8. Phase B canonical geometry clarification

`SE001LevelJson` is the runtime authoring source of truth for Phase B. It contains `schemaVersion`,
`levelId`, `board`, `staticObstacles`, and empty/default `sources` and `cups` arrays. `BoardData` owns
`size` and wall polygon contours. `StaticObstacleData` owns a stable ID, one or more filled polygon
contours, and an optional presentation `styleId`.

Contours are closed simple polygons represented by finite board-space points. Duplicate terminal points,
fewer than three unique vertices, degenerate contours, missing IDs, and duplicate IDs are validation errors.
Concave contours and multiple independent contours are supported. Holes/compound boolean contours are not
silently approximated in Phase B.

Meshes, triangulation, raster masks, bevel geometry, colliders, and SVG commands are generated/runtime data
and are never serialized as authoring truth.

- Field mới có safe default có thể không bump schema khi backward compatibility được test.
- Đổi nghĩa/xóa/đổi type field bắt buộc bump schema và có migration/validation rõ.
- Story thay serialization contract phải được Product Owner approve trước implementation.
