# Phase D — Execute prompt: D0 + D0.5

Spec/acceptance: `handoff/phase-D/PHASE-D-PLAN.md`. File này là **hướng dẫn thực thi**, không thay plan.
Guardrails: `AGENTS.md`, `CLAUDE.md`. Không commit/push nếu user chưa yêu cầu. Giao tiếp tiếng Việt, report theo format ở cuối.

Repo facts đã verify (đừng giả định khác):
- Không có `.asmdef` trong `Assets/_Core` → toàn bộ script nằm ở Assembly-CSharp; code Editor **phải** nằm dưới thư mục `Editor/` để vào Assembly-CSharp-Editor.
- `LayoutRasterizer.Rasterize(SE001LevelJson level, float cellSize, int maxCells) → LayoutMaskSet` (`Assets/_Core/4_Scripts/Geometry/LayoutRasterizer.cs`). `LayoutMaskSet(int width,int height)` có `Width`, `Height`, `bool[] ValidMask`, `bool[] StaticMask`, `Index(x,y)`.
- `LevelSpawner.SpawnLevel` gọi rasterize tại đúng một chỗ:
  `LayoutMaskSet masks = LayoutRasterizer.Rasterize(levelData, profile.cellSize, profile.maxCells);`
  rồi `activeContext.AttachSimulation(new SandSimulation(profile, masks, SandSimulation.SeedFrom(levelId)));`
- `SE001LevelJson`: `schemaVersion` (hiện 2), `levelId`, `board{ Vector2 size; List<PolygonContourData> wallContours }`, `List<StaticObstacleData> staticObstacles{ stableId, contours, styleId }`, `sources`, `cups`, `drawInkBudget`, `requiresDrawing`.
- `LevelDataLoader.Load(levelId)` đọc `Resources/Levels/<id>`, nạp `ColorProfile` + `SandSimulationProfile`, gọi `LevelDataValidator.Validate(level, colorProfile, cellSize, maxCells)`.
- `PhaseBSvgImporter` (`Assets/_Core/4_Scripts/Editor/Level/C#/PhaseBSvgImporter.cs`) hiện là MenuItem fixture cứng, ghi contour vào `level.board.wallContours`.

---

## PACKET D0 — Skills (không đụng code game)

### D0.A — `skills/level-editor/recipes/`
Tạo 8 file. Mỗi file theo đúng khung: `## Khi nào dùng` · `## Vấn đề` · `## Cách làm` · `## Skeleton` (C#/UXML/USS generic) · `## Bẫy thường gặp` · `## Cách kiểm chứng`. Mỗi file 60–150 dòng, có ít nhất một skeleton chạy được về mặt cú pháp.

| File | Nội dung bắt buộc |
|---|---|
| `editor-window-partials.md` | Tách EditorWindow thành partial theo vùng (toolbar/canvas/inspector/status); nơi đặt state; thứ tự `CreateGUI` |
| `document-view-derived-state.md` | 3 tầng Document/ViewState/DerivedState; cái nào serialize, cái nào không; ai invalidate cái nào |
| `apply-edit-vs-reload.md` | ApplyEdit cục bộ vs ReloadDocument; khi nào được reload; cách giữ selection/scroll |
| `split-workspace-uitk.md` | TwoPaneSplitView lồng nhau, lưu pane size, canvas nhận phần còn lại, responsive ~640px |
| `delayed-fields-and-callbacks.md` | delayed field, unregister callback, tránh tích tụ callback qua domain reload |
| `stable-selection-remap.md` | Selection theo stable ID; remap sau add/remove/sort; không dùng array index |
| `validation-focus-flow.md` | Click issue → đổi mode → select ID → frame target → focus field; format WHAT/WHERE/HOW |
| `unsaved-play-test.md` | temp JSON + one-shot session override; tiêu thụ đúng một lần; khôi phục sau Play |

Cấm: nhúng data/gameplay SE-001 (sand, cup, jar, colorId…) vào recipe generic. Không copy nội dung skill Unity official.

### D0.B — `skills/editor-ux-review/`
`SKILL.md` + `refs/usability-review.md`, `refs/visual-hierarchy.md`, `refs/editor-visual-system.md`, `refs/first-use-review.md`, `refs/review-checklist.md`.
`SKILL.md` phải có **quy trình review 3 pass** (first use không tài liệu / core workflow / stress-recovery) và bảng 14 chiều từ plan §D0.B.
`review-checklist.md`: mỗi dòng một mục kiểm, cột `PASS/FAIL/N/A/PENDING`.
`editor-visual-system.md`: chép **đủ** control roles, semantic roles, emphasis tiers, frequency tiers và 13 rule ở plan §D0.C, thêm ví dụ USS class name generic (`.btn-primary`, `.badge-warning`…).

### D0 — Exit
- Self-review theo `skills/AUTHORING.md`.
- Bảng map `Blueprint requirement → recipe/review rule` đặt ở `handoff/phase-D/evidence/d0-mapping.md`.
- Không file nào chứa logic gameplay SE-001.

---

## PACKET D0.5 — Layout Asset Pipeline

### Quyết định đã chốt (không tự đổi)
- Mask lưu **ScriptableObject + `byte[]` bitpack**, 1 bit/cell.
- Migration: **MenuItem chạy tay một lần**, sau đó runtime **chỉ** đọc schema mới. Không giữ đường đọc schema cũ.

### D0.5.1 — Data assets (runtime, `Assets/_Core/4_Scripts/Data/`, namespace `SE001.Data`)

```csharp
public sealed class LayoutMaskAsset : ScriptableObject
{
    public int width, height;              // số cell
    public float cellSize;                 // cellSize lúc bake
    public Vector2 boardSize;              // board size lúc bake
    public string contourHash;             // hash của contour nguồn
    public int importerVersion;            // đổi khi thuật toán bake đổi
    public byte[] staticBits;              // bitpack, 1 = static, index = y*width+x
    public byte[] validBits;               // bitpack, 1 = valid

    public bool TryBuildMaskSet(float runtimeCellSize, int maxCells, out LayoutMaskSet masks, out string error);
}

public sealed class LayoutDefinition : ScriptableObject
{
    public string layoutId;
    public GameObject layoutPrefab;        // mesh đã bake
    public LayoutMaskAsset mask;
    public Vector2 boardSize;
    public Texture2D thumbnail;            // optional
}
```
- `TryBuildMaskSet` trả `false` + `error` mô tả rõ khi `runtimeCellSize != cellSize`, `width*height > maxCells`, hoặc mảng bit sai độ dài. **Không** tự rasterize lại.
- Bitpack: `staticBits[i >> 3]`, bit `i & 7`. Giải nén một lần ra `LayoutMaskSet`, không giải nén mỗi frame.
- `LayoutDefinition` đặt tại `Assets/_Core/Resources/Layouts/<layoutId>.asset` để runtime `Resources.Load` được.

### D0.5.2 — Level schema
- `SE001LevelJson`: thêm `public string layoutId = string.Empty;`, **xoá** `board.wallContours` và `staticObstacles` khỏi runtime path; `schemaVersion = 3`.
- `LevelDataLoader.Load`: sau khi parse, nếu `schemaVersion != 3` → ném lỗi rõ ràng kèm hướng dẫn chạy MenuItem migration. Nạp `LayoutDefinition` theo `layoutId` từ `Resources/Layouts/`; thiếu → lỗi rõ.
- `LevelDataValidator`: thêm rule `layoutId` bắt buộc và tồn tại; **từ chối** file có đồng thời `layoutId` và contour; giữ nguyên các rule cũ về colorId/supply/bounds. Bounds check dùng `LayoutDefinition.boardSize`.

### D0.5.3 — Runtime consume
Trong `LevelSpawner.SpawnLevel`, thay đúng dòng rasterize bằng: nạp `LayoutDefinition` → `mask.TryBuildMaskSet(profile.cellSize, profile.maxCells, out masks, out error)` → `false` thì `throw new InvalidOperationException(error)`. Giữ nguyên `AttachSimulation(...)` phía sau.
`SpawnLayoutVisuals` đổi thành `Instantiate(layout.layoutPrefab, activeContext.BoardRoot, false)`; **không** gọi `LayoutVisualFactory` nữa ở đường production.
Thêm counter để test được: `public static int RasterizeCallCount` trong `LayoutRasterizer`, tăng 1 mỗi lần `Rasterize` chạy, có `ResetCallCount()`.

### D0.5.4 — Editor bake (`Assets/_Core/4_Scripts/Editor/Level/C#/`, namespace `SE001.Editor.Level`)
- `LayoutBakeWindow : EditorWindow`, menu `SE001/Phase D/Layout Bake`. UI Toolkit. Chức năng: chọn file `.svg` (`EditorUtility.OpenFilePanel`), nhập `layoutId`, preview số contour/điểm, nút `Bake`, danh sách layout đã có + `Rebake`, `Rebake All`.
- `LayoutBaker` (static): parse SVG bằng logic sẵn có trong `PhaseBSvgImporter` (refactor phần parse ra dùng chung, **không** copy-paste), rồi:
  1. Dựng `SE001LevelJson` tạm chỉ để rasterize (board size + contour) → gọi **đúng** `LayoutRasterizer.Rasterize`.
  2. Bitpack sang `LayoutMaskAsset`, ghi `cellSize` lấy từ `PhaseBSandSimulationProfile`, `contourHash` = SHA1 của chuỗi toạ độ đã làm tròn 4 chữ số, `importerVersion` hằng số trong code.
  3. Bake mesh: dùng `PolygonTriangulator`/`ExtrudedBevelMeshBuilder` sẵn có, lưu `Mesh` vào asset và tạo prefab dưới `Assets/_Core/3_Prefabs/Gameplay/Layout/Baked/<layoutId>.prefab`, material lấy theo `PhaseBLayoutVisualProfile`.
  4. Tạo/ghi đè `LayoutDefinition`.
- `Rebake All` phải giữ nguyên GUID asset cũ (dùng `AssetDatabase.LoadAssetAtPath` + ghi đè, không Delete/Create).

### D0.5.5 — Migration
`MenuItem("SE001/Phase D/Migrate levels to layoutId")`: với mỗi file trong `Resources/Levels/`: bake layout từ contour đang có (đặt `layoutId = <levelId>_layout`), ghi `LayoutDefinition`, xoá contour khỏi JSON, set `layoutId`, `schemaVersion = 3`, ghi lại file. In ra Console bảng tổng kết. Chạy một lần cho 3 level hiện có.

### D0.5.6 — Tests (`Assets/_Core/4_Scripts/Tests/Editor/`)
| Test | Nội dung |
|---|---|
| `LayoutBake_MaskMatchesRuntimeRasterizer` | Bake từ một contour fixture → `TryBuildMaskSet` → so **từng cell** với `LayoutRasterizer.Rasterize` trên cùng data |
| `LayoutBake_StaleCellSize_BlocksLoad` | `TryBuildMaskSet` với cellSize khác → `false`, `error` chứa cả hai giá trị |
| `LayoutBake_ReimportSameSvg_IsDeterministic` | Bake 2 lần cùng input → `contourHash` và `staticBits` giống hệt |
| `LevelLoad_UsesBakedMask_NoRasterizeCall` | `ResetCallCount()` → spawn level → `RasterizeCallCount == 0` |
| `LevelJson_MigratesContoursToLayoutId` | Chạy migration trên fixture schema 2 → ra schema 3, có layoutId, không còn contour |

### D0.5.7 — Evidence bắt buộc
`handoff/phase-D/evidence/d05-load-time.md`: thời gian load **trước/sau** cho một level có layout SVG thật (đo bằng `Stopwatch` quanh `SpawnLevel`), kèm số điểm contour và kích thước file JSON trước/sau.

### D0.5 — Cấm
- Viết bộ rasterize thứ hai.
- Fallback âm thầm rasterize khi mask stale.
- Nhét source/cup vào layout prefab.
- Alloc mỗi frame; giải nén bitpack trong Update.

---

## Report format

Mỗi packet:

```
Item | Claimed | Verified by | Evidence
```

Kết thúc bằng đúng một dòng: `DONE` hoặc `BLOCKED — <blocker cụ thể + evidence>`.
Nếu cấu trúc repo khác phần "Repo facts" ở đầu file → **DỪNG và báo**, không tự đổi kiến trúc.
