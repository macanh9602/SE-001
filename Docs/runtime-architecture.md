# Runtime Architecture — SE-001

Đây là instance của `standards/system-design.md` cho SE-001. Story 000 chỉ chốt ownership và module boundary; feature behavior được implement ở các story sau.

## 1. Layer map

| Layer | Owner / module contract |
|---|---|
| Bootstrap | `GameScene` là scene duy nhất; bootstrap và gameplay lifecycle cùng ownership |
| Profile | Feature profiles trong module tương ứng; tunables không nằm trong Domain |
| Level Data | `SE001LevelJson` từ `Assets/_Core/Resources/Levels/` |
| Save / Progress | Chưa implement; sẽ là module progression riêng, không thuộc Story 000 |
| Spawner | Source feature phát lệnh spawn/pour; không quyết định sand movement |
| Factory | Feature-scoped factory tạo/bind prefab hoặc pooled view |
| Domain | Cup, Source, level rules và accounting; không reference Visual |
| Simulation | Sand grid Simulation-driven 2D; là authority cho sand pose/transition |
| RuntimeState | Per-level state: stable IDs, masks, counts, occupancy và semantic signals |
| Scheduler | Chỉ thêm khi behavior kéo dài nhiều nhịp; timing đọc từ Profile |
| Visual | Sand renderer, cup/source views, HUD presentation; đọc state/signal |
| Bridge | Adapter giữa Domain/Simulation và Visual/HUD |
| HUD | `IHudPresenter` + bridge; gameplay không phụ thuộc UI framework cụ thể |
| Editor | Level authoring/validation/preview; document state tách view state |

## 2. Feature boundaries

| Feature | Domain responsibility | Non-responsibility |
|---|---|---|
| Sand | Material state, movement commands/signals, count conservation | UI, ParticleSystem, win/lose |
| Level | Authored entities, stable IDs, load/validate/unload | Generated grid serialization |
| Input | Chuyển pointer/touch thành command/path intent | Tự sửa sand state hoặc gameplay result |
| Cup | Collection intent và semantic collection result | Raw Renderer/Collider query |
| Source | Finite material emission intent và source state | Tự resolve sand physics |
| HUD | Present counters/result/feedback qua bridge | Gameplay decisions |

## 3. Placement contract

- Gameplay plane: 2D XY.
- Z/depth chỉ dành cho presentation ordering; không dùng để quyết gameplay.
- Camera ownership thuộc Bootstrap/scene setup; feature không tự ghi camera.
- Scene root và prefab scale là authored data; runtime không tạo GameObject per grain/cell.

## 4. Spawn / unload lifecycle

Theo blueprint `standards/system-design.md §5`: cancel token cũ → cleanup level cũ → load/validate JSON → resolve Profile/override → tạo RuntimeState → seed static obstacle → tạo source/cup/view qua factory/pool → build generated simulation state → phát `OnLevelSpawned`.

## 5. Query contract

| Query | Complexity | Source |
|---|---|---|
| Stable ID lookup | O(1) | Per-level RuntimeState index |
| Obstacle/mask lookup | O(1) | Simulation mask owner |
| Cup/source state | O(1) | Domain RuntimeState |
| Neighbor movement | O(out-degree) | Simulation grid |

## 6. Physics / Simulation contract

- Physics authority mode: Simulation-driven.
- Physics dimension: 2D board XY.
- Determinism: Tolerance-based; emitted/in-field/collected/spilled/pending accounting deterministic.
- Simulation owner: persistent sand grid buffers và deterministic single-writer step.
- Domain → Simulation: semantic commands (emit, draw obstacle, collect), không raw Rigidbody dependency.
- Simulation → Domain: semantic state/signals (settled, collected, spilled, edge-leave).
- Runtime pose authoritative: có, trong Simulation; renderer chỉ present state.

## 7. Generated data

```text
SE001LevelJson (authoring source)
    ↓ validate / resolve
RuntimeState + obstacle masks + sand grid (generated/runtime)
    ↓
Visual/HUD presentation
```

Generated grid/cache không serialize trong Story 000; regenerate khi load. Parity tests thuộc story implement data/simulation tương ứng.
