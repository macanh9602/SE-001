# Phase D — Review + sửa đổi bắt buộc

Viết 2026-09-23, đối chiếu plan Phase D (bản C2C STATE: INIT → PLAN) với repo tại `c5727d1`.
Kết luận: plan **giữ nguyên phần UX/closure**, nhưng thiếu 1 packet hạ tầng và 2 nhóm chức năng GD. Bổ sung bên dưới.

## 1. Đo thực tế trên repo

| Điểm | Bằng chứng trong repo | Hệ quả |
|---|---|---|
| Rasterize mask mỗi lần load | `Geometry/LayoutRasterizer.cs`: 2 vòng lặp qua toàn bộ cell, mỗi cell gọi `ContainsPoint` cho **mọi** contour của wall + obstacle | Board 10.8×19.2 ở `cellSize 0.06` = **57.600 cell**. Layout SVG vài nghìn điểm → hàng chục tới hàng trăm triệu phép test mỗi lần load → lag/treo. Đây là lý do note của GD là đúng. |
| Mesh layout dựng lúc runtime | `LevelSpawner.SpawnLayoutVisuals()` → `LayoutVisualFactory.Create()` → triangulate + extrude | Thêm chi phí load và GC, lặp lại mỗi lần reload/retry. |
| Importer SVG mới chỉ ra JSON | `Editor/Level/C#/PhaseBSvgImporter.cs`, một `MenuItem` fixture cứng `Import test_tool.svg`, ghi `level.board.wallContours` | Chưa phải pipeline production; GD không chọn được file, không ra prefab. |
| Level JSON nhúng toàn bộ hình học | `phase_b_svg_test.json` từng dài 6.600 dòng | File nặng, khó diff, và là nguồn hình học thứ hai song song với mesh. |
| Schema hiện tại | `board{size, wallContours}`, `staticObstacles[]`, `sources[]`, `cups[]`, `drawInkBudget`, `requiresDrawing` | Chưa có `layoutId`. |

## 2. Quyết định đã chốt

1. **Bake prefab mesh + static mask.** Runtime chỉ Instantiate prefab và nạp mask; bỏ hẳn rasterize và triangulate lúc load.
2. **Level JSON chỉ reference `layoutId`**, bỏ `wallContours`/`staticObstacles` khỏi JSON; bump `schemaVersion`, có migration cho level cũ.
3. **Tuning: chỉ global.** `GlobalTuningProfile` (hoặc các profile hiện có) là nguồn duy nhất cho feel. Level JSON vẫn giữ **level data** sẵn có (`logicalAmount`, `emissionRate`, `streamWidth`, `requiredAmount`, `drawInkBudget`) — đây là data của màn chơi, không phải override feel.
4. **D0 và packet pipeline chạy sớm**, không chờ Phase C closure, vì pipeline phục vụ chính việc author 3 level của Phase C. Các packet D1 trở đi vẫn chờ Phase C DONE.

## 3. Packet mới: D0.5 — Layout Asset Pipeline (SVG → prefab + mask)

Chèn trước D1. Đây là packet hạ tầng, không phải UX.

### Input / Output
- Input: file `.svg` do GD chọn (panel chọn file, không hardcode fixture).
- Output cho mỗi layout:
  - `LayoutPrefab`: mesh đã triangulate + material, pivot tại gốc board.
  - `LayoutMaskAsset`: static mask đã bake, lưu kèm **`cellSize`, `boardSize`, `contourHash`, `importerVersion`**.
  - `LayoutDefinition` (ScriptableObject): `layoutId`, tham chiếu prefab + mask, board size, thumbnail.

### Quy tắc
- Mask bake bằng **đúng** `LayoutRasterizer` hiện tại (chạy trong Editor), không viết bộ rasterize thứ hai. Đây là điều kiện giữ parity mà plan Phase D yêu cầu.
- Runtime load: nếu `cellSize` của `SandSimulationProfile` khác `cellSize` đã bake, hoặc `contourHash` lệch → **lỗi rõ ràng, chặn load**, không âm thầm rasterize lại.
- Có nút `Rebake` cho một layout và `Rebake All`.
- Prefab mesh không chứa logic gameplay; source/cup không nằm trong layout.

### Tests
- `LayoutBake_MaskMatchesRuntimeRasterizer` — mask bake == mask rasterize trực tiếp, từng cell.
- `LayoutBake_StaleCellSize_BlocksLoad`.
- `LayoutBake_ReimportSameSvg_IsDeterministic` (hash không đổi).
- `LevelLoad_UsesBakedMask_NoRasterizeCall` — đếm call, phải bằng 0 ở runtime.
- `LevelJson_MigratesContoursToLayoutId`.

### Acceptance đo được
- Thời gian load một level có layout SVG thật: ghi số **trước / sau** vào evidence. Mục tiêu: phần layout gần như bằng 0, không còn phụ thuộc số điểm contour.
- Kích thước file level JSON giảm rõ rệt (level SVG cũ 6.600 dòng → vài chục dòng).

## 4. Bổ sung cho D3/D4 — thao tác GD trên canvas

Plan gốc có "place/move" nhưng chưa nói cách đặt và cách kiểm tra vị trí hợp lệ. Chốt lại:

1. **Chọn layout trước**: panel Levels có ô chọn `LayoutDefinition` (thumbnail + tên). Đổi layout → canvas đổi nền, mask đổi theo, entity giữ nguyên nhưng phải **re-validate vị trí** ngay.
2. **Thêm entity**: nút `Add Source` / `Add Cup` spawn entity vào **giữa vùng canvas đang nhìn**, ở trạng thái selected, sẵn sàng kéo. Không dùng modal nhập toạ độ.
3. **Drag to move**: kéo bằng chuột trên canvas, snap theo cell của sim (`cellSize`), giữ `Shift` để bỏ snap. Một gesture kéo = **một** entry Undo.
4. **Check vị trí hợp lệ ngay trong lúc kéo**, cập nhật mỗi frame kéo (dùng mask đã bake, không rasterize lại):
   - nằm trong board;
   - không chồng static mask (tường/obstacle);
   - không chồng entity khác (kể cả vùng thân jar và miệng cup);
   - miệng cup không bị bịt bởi static mask;
   - source phải có khoảng trống phía dưới vòi.
5. **Phản hồi khi kéo**: vị trí hợp lệ = ghost bình thường; không hợp lệ = viền đỏ + badge nêu lý do ngắn ("miệng cup bị chặn"). Thả ở vị trí không hợp lệ thì **trả về vị trí hợp lệ cuối cùng**, không tạo data sai.
6. Vẫn cho phép nhập toạ độ chính xác trong Inspector, nhưng đó là đường phụ.

Tests: `Drag_OneGesture_OneUndo`, `Drag_InvalidDrop_RevertsToLastValid`, `Drag_UsesBakedMask_NoRasterize`, `Overlap_SourceCup_Detected`, `CupMouthBlocked_Detected`, `ChangeLayout_RevalidatesEntities`.

## 5. Bổ sung cho D7 — nơi tune global

- Một cửa sổ/asset tuning duy nhất, mở được từ Level Editor, gom: sand feel, source, cup, ink, juice.
- Sửa giá trị → preview và Play Test dùng ngay giá trị mới, không cần restart Editor.
- Hiển thị cảnh báo khi sửa field ảnh hưởng toàn bộ level đã balance (ví dụ `cellSize`, vì nó **làm hỏng toàn bộ mask đã bake** — phải rebake all).
- Không có override theo level cho các field feel. Nếu sau này cần, mở bằng whitelist tường minh, không mở tự do.

## 6. Sửa Entry Gate

Gate gốc cấm mọi thứ trước khi Phase C closure. Sửa thành:
- **Được phép trước Phase C closure**: D0 (skill bootstrap), D0.5 (pipeline bake).
- **Chờ Phase C DONE**: D1 → D8.
- Lý do: D0.5 phục vụ trực tiếp C2A (author 3 level) và bỏ được nguy cơ treo khi load.

## 7. Bổ sung vào DONE GATE của Phase D

```
[ ] Layout bake pipeline: SVG → prefab mesh + mask asset.
[ ] Runtime không rasterize mask khi load level (đếm call = 0).
[ ] Mask bake stale (cellSize/hash lệch) bị chặn với lỗi rõ ràng.
[ ] Level JSON chỉ chứa layoutId, migration level cũ chạy được.
[ ] Số đo thời gian load layout trước/sau có trong evidence.
[ ] Add Source/Cup spawn vào canvas, drag to move, check vị trí hợp lệ khi kéo.
[ ] Thả ở vị trí không hợp lệ không tạo được data sai.
[ ] Cửa sổ tuning global mở được từ Level Editor; cảnh báo khi đổi field cần rebake.
```

## 8. Rủi ro

- **`cellSize` trở thành thứ cực kỳ đắt để đổi** sau khi bake mask. Cần ghi rõ trong docs và chặn bằng cảnh báo.
- **Migration level cũ**: khi bỏ contour khỏi JSON, phải bake ngược từ contour hiện có thành layout asset. Làm script migration một lần, có test.
- **Hai nguồn hình học** vẫn có thể lén quay lại nếu ai đó thêm contour vào JSON. Validator phải từ chối JSON có cả `layoutId` lẫn contour.
- Plan Phase D yêu cầu screenshot/evidence rất nhiều. Việc này cần người thao tác thật trong Unity; agent không tự claim PASS được.
