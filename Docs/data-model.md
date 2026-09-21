# Data Model — SE-001

## 1. Ba loại data

| Loại | Ví dụ | Save? |
|---|---|---|
| Source of truth (authoring) | `SE001LevelJson`, entity stable IDs, authored layout/mask/source/cup data | Có |
| Generated / baked | Validated lookup, resolved overrides, generated obstacle/simulation buffers | Không; regenerate được |
| Runtime state | Per-level occupancy, sand cells, counters, pending emissions, semantic signals | Không; tạo khi load, hủy khi unload |

Scene hierarchy, Mesh, Collider, debug geometry và ParticleSystem không phải source of truth.

## 2. Level schema contract

### `SE001LevelJson`

| Field | Type | Ý nghĩa | Bắt buộc |
|---|---|---|---|
| `schemaVersion` | int | Version của serialized contract | Có |
| `levelId` | string | Stable level identifier | Có |
| `board` | object | Authored board bounds/cell contract | Có |
| `staticObstacles` | array | Stable obstacle definitions | Có, có thể rỗng |
| `sources` | array | Stable material source definitions | Có, có thể rỗng |
| `cups` | array | Stable collection target definitions | Có, có thể rỗng |

Schema owner là Level Data feature. File nằm tại `Assets/_Core/Resources/Levels/`; JSON là authoring source of truth.

### Entity data

Mọi entity dùng `stable id` dạng string; array index không phải identity. Reference tới ID không hợp lệ phải bị validator chặn.

## 3. Global vs level override

| Giá trị | Global | Level override | Resolver |
|---|---|---|---|
| Simulation/feel tunable | Profile ScriptableObject | Chỉ khi story cho phép | Một resolver duy nhất |
| Entity placement/count | Không | Level JSON | Level loader |
| Presentation prefab/material | PrefabProfile | Không trong Story 000 | Factory/profile |

Override phải đi qua một resolver; không đọc `profile.X` và `level.X` rải rác. Field override null phải được phân biệt với object rỗng khi serializer/deserializer được implement.

## 4. Save / load policy

- Bắt buộc save: authored level JSON và stable IDs.
- Không save: generated grid/cache, scene hierarchy, runtime sand cells, occupancy hoặc transient signals.
- RuntimeState tạo mới mỗi lần load level và bị cleanup khi unload.
- Migration field mới có default an toàn không bắt buộc bump schema; đổi nghĩa field bắt buộc bump.

## 5. Invariants

- `schemaVersion` tồn tại.
- `levelId` và entity IDs unique.
- Không dangling reference.
- Graph/adjacency deterministic, không duplicate edge hoặc cycle trái contract.
- Sand accounting bảo toàn: `emitted = inField + collected + spilled/lost + pending`.
- Static obstacle seed trước dynamic/player obstacle và trước source emission.

## 6. Editor view state

Selection, mode/tab, active layer, zoom, pan và panel width nằm trong `SessionState`/`EditorPrefs`, không serialize vào level JSON.
