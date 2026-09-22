# Glossary — SE-001

## 1. Gameplay

| Term | Nghĩa chính xác | Tên trong code | Không phải là |
|---|---|---|---|
| Logical grain | Đơn vị material được simulation/accounting theo dõi | `LogicalGrain` | Visual crumb/particle |
| Sand | Material rơi và repose trên grid | `Sand` | ParticleSystem |
| Source | Entity phát finite material stream | `Source` | VFX emitter vô hạn |
| Cup | Entity nhận material và phát semantic collection result | `Cup` | Collider authority |
| Player obstacle | Obstacle rasterize từ path người chơi vào mask | `PlayerObstacle` | Collider per cell |
| Static obstacle | Obstacle authored trong level data | `StaticObstacle` | Generated mesh |
| Settled | Trạng thái simulation không còn movement vượt threshold contract | `Settled` | Một frame không render |

## 2. Authoring / architecture

| Term | Nghĩa | Tên trong code |
|---|---|---|
| Source of truth | Data authored duy nhất được save | `AuthoringData` |
| Generated data | Data có thể regenerate từ authoring | `GeneratedData` |
| RuntimeState | State sinh theo level load, chết khi unload | `RuntimeState` |
| LevelSpawner | Owner composition tạo/dọn per-level runtime state và roots | `LevelSpawner` |
| LevelContext | Lifetime/token và references của đúng một level generation; không tạo hierarchy | `LevelContext` |
| Simulation-driven | Simulation custom là authority cho sand pose/transition | `SimulationDriven` |
| Semantic signal | Kết quả domain/simulation truyền qua contract | `SemanticSignal` |
| Feature-scoped assembly | Assembly tách theo feature và vai trò Runtime/Editor/Tests | `*.Runtime`, `*.Editor`, `*.Tests` |

## 3. Metrics / modes

## Phase B terms

| Term | Meaning | Code |
|---|---|---|
| Board-space | Canonical gameplay XY coordinates after authoring conversion | `BoardSpace` |
| Canonical contour | Finite, filled, closed simple polygon in board-space | `PolygonContourData` |
| Valid mask | Generated cells where sand may exist inside the board | `validMask` |
| Static mask | Generated cells blocked by authored wall/obstacles | `staticObstacleMask` |

| Term | Đo bằng gì | Nói lên gì | Không nói lên |
|---|---|---|---|
| Low-end target | Redmi 9A device capture | Baseline performance thấp | Mọi Android device |
| Mid target | Project default 60 fps | Mục tiêu trải nghiệm mid device | Đã đo nếu chưa capture |
| Creative mode | Future quality/feel mode | Hướng sản phẩm cần chốt sau | Semantics hiện tại |

## 4. Cặp dễ nhầm

| A | B | Khác nhau |
|---|---|---|
| Logical grain | Visual crumb | Gameplay quantity vs presentation detail |
| Static obstacle | Player obstacle | Authored level data vs runtime input |
| RuntimeState | Source of truth | Transient runtime vs persisted authoring |
