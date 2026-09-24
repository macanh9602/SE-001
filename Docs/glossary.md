# Glossary — SE-001

## 1. Gameplay

| Term | Nghĩa chính xác | Tên trong code | Không phải là |
|---|---|---|---|
| Logical grain | Đơn vị material được simulation/accounting theo dõi | `LogicalGrain` | Visual crumb/particle |
| Sand | Material rơi và repose trên grid | `Sand` | ParticleSystem |
| Source | Entity phát finite material stream | `Source` | VFX emitter vô hạn |
| Cup | Entity nhận material và phát semantic collection result | `Cup` | Collider authority |
| Source Scale | Hệ số kích thước thân mọi Source trong level; cùng chi phối visual và tap hit-area | `SE001LevelJson.sourceScale` | Emission Rate, Stream Width |
| Bowl Scale | Hệ số kích thước mọi Bowl trong level; cùng chi phối visual và vùng nhận cát | `SE001LevelJson.bowlScale` | Required Amount |
| Emission Rate | Tốc độ phát cát chung cho Source trong level, theo grain mỗi simulation step | `SE001LevelJson.sourceEmissionRate` | Kích thước Source |
| Stream Width | Bề rộng dòng cát chung cho Source trong level, theo sand cell | `SE001LevelJson.sourceStreamWidth` | Kích thước Source |
| Player obstacle | Obstacle rasterize từ path người chơi vào mask | `PlayerObstacle` | Collider per cell |
| Static obstacle | Obstacle authored trong level data | `StaticObstacle` | Generated mesh |
| Settled | Trạng thái simulation không còn movement vượt threshold contract | `Settled` | Một frame không render |

## 2. Authoring / architecture

`Level name (GD)`: tên `Level_XX` hiển thị theo vị trí trong `PhaseCLevelSequence`; level cũ giữ `levelId` nội bộ để bảo toàn reference. `levelId` là ID ổn định dùng cho JSON filename và runtime load.

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

| Term | Meaning | Code |
|---|---|---|
| LayoutDefinition | Runtime reference to a baked layout prefab and mask asset | `LayoutDefinition` |
| LayoutMaskAsset | Bit-packed baked valid/static grid consumed by runtime | `LayoutMaskAsset` |
| layoutId | Stable level reference to a `LayoutDefinition` resource | `SE001LevelJson.layoutId` |
| Layout (GD) | Baked board geometry (walls + static obstacles) that levels pick by Layout ID; edited only via SVG + Layout Bake | `LayoutDefinition` |
| Layout ID | GD-facing name of a baked layout; lowercase, digits, `_` | `LayoutDefinition.layoutId` |
| Bake / Rebake | Turn an SVG into layout prefab + mask; Rebake repeats it for an existing Layout ID and keeps asset identity | `LayoutBaker.TryBakeSvg` |
| Ready / Needs Rebake / Source Missing / Invalid SVG / Bake Failed | Layout Bake status shown to GD; Needs Rebake may block levels from loading | `LayoutBakeState` |
| contourHash | Baked geometry identity; definition and mask must match or runtime refuses to load | `LayoutDefinition.contourHash` |

## Phase B terms

| Term | Meaning | Code |
|---|---|---|
| Board-space | Canonical gameplay XY coordinates after authoring conversion | `BoardSpace` |
| Canonical contour | Finite, filled, closed simple polygon in board-space | `PolygonContourData` |
| Valid mask | Generated cells where sand may exist inside the board | `validMask` |
| Static mask | Generated cells blocked by authored wall/obstacles | `staticObstacleMask` |
| Rotating Obstacle / Cross | Per-level X-shaped continuously rotating obstacle that pushes sand; its bars share the static obstacle material | `RotatingObstacleData`, `RotatingObstacleSystem` |
| Rotating mask | Transient occupied cells of all Cross obstacles, separate from the player's stroke mask | `SandSimulationState.RotatingMask` |

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
