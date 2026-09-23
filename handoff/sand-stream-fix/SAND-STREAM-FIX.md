# Sand Stream Fix (B+) — Story + Execute prompt

Guardrails: `AGENTS.md`, `CLAUDE.md`. Không commit/push nếu user chưa yêu cầu. Giao tiếp tiếng Việt, report theo format cuối file.
Reference hành vi (source of truth cho feel): `Docs/visualizers/sand-stream-lab.html` — panel **B+**, slider default
(air drag 0.50, streak 1.00, độ dày ±1, nối khe 10, texture hạt 0.8). JS trong file đó là port 1:1 của `SandSimulation.Step()` + phần fix; dùng nó làm đặc tả, không cần đoán.

## Vì sao

Sand rời obstacle bị bắn xéo và tản thành bụi (video repro: 65% hạt đi lệch khỏi dòng chính tại probe). Rule đã chốt: **1 hạt vào sai cup = thua ngay**
→ spray gây thua oan, người chơi không kiểm soát được. Mục tiêu: sand rời mép obstacle/path **rơi thẳng đứng**, và người chơi **thấy một dòng liền, dày ~3 px**.

## Quyết định đã chốt (không tự đổi)

1. Hướng **B+** = sim fix "A" + render stream trong `SandFieldVisual`. Không giảm `maxFallCellsPerStep` (không đổi pacing).
2. Độ dày visual **±1 cell** (3 px). Visual không bao giờ đi vào collision / cup count.
3. **Mở lại** freeze sand của Phase C (decision-log 2026-09-23). Append decision ở mục cuối file này vào `Docs/decision-log.md`.
4. Không tự rebalance level JSON. Level nào đổi kết quả → báo evidence, GD/Dev chốt.

## Repo facts đã verify

- `Assets/_Core/4_Scripts/Simulation/Sand/SandSimulation.cs` — `Step()` duyệt **bottom-up, board y hướng lên** (rơi = `cy-1`). Hàng dưới đã được xử lý trước trong cùng step → `Velocity[below]` là giá trị step hiện tại.
- Nguyên nhân spray (line hiện tại):
  - `:170-178` nhánh airborne (`fell > 0`): drift ngang xác suất `|m|·0.35` + `m *= 0.985f` → momentum giữ gần nguyên suốt quãng rơi.
  - `:125-129` splash và `:144` roll (`m*0.9 + dd*0.35`) bơm momentum lên trần 2.0 khi lăn qua obstacle.
  - Nhánh `fell == 0` cũng chạy khi hạt bị chặn bởi **một hạt khác đang rơi** → splash/roll giữa không trung → dòng tự nổ.
- `SandSimulationProfile.cs`: asset `Assets/_Core/Resources/Profiles/PhaseBSandSimulationProfile.asset` chỉ serialize tới `enableLateralSlide`; field mới nhận default từ code.
- `Assets/_Core/4_Scripts/Elements/Sand/SandFieldVisual.cs` — `UpdateTexture()` ghi thẳng `NativeArray<Color32>` 1 pixel/cell, texture row y = board y (above = `i + width`). Presentation-only, không được ghi `SandSimulationState`.
- `SandSimulationState` đã có `Velocity`, `ValidMask`, `StaticMask`, `CupWallMask`, `DynamicMask` public.
- Regression gate gameplay: `Tests/Editor/PhaseCPlaythroughTests.cs` (level 01–03 win/lose + determinism).

---

## PACKET S1 — Sim: rơi thẳng (fix "A")

### Profile (`SandSimulationProfile`, header "Stream")
```csharp
[Tooltip("Momentum kept per step while airborne. 0 = drop dead-straight off edges.")]
[Range(0f, 1f)] public float airDrag = 0.5f;
[Tooltip("A grain blocked by a grain that is itself falling follows it instead of splashing/rolling mid-air.")]
public bool followFallingGrain = true;
```

### `Step()`
1. Nhánh airborne (`fell > 0`): **xoá** block drift ngang (`if (am > 0.4f && NextFloat() < am * 0.35f) {...}`); thay `m *= 0.985f` bằng `m *= profile.airDrag`. Giữ nguyên dòng `if (fell < n) v = ...`.
2. Đầu nhánh `fell == 0`, trước splash:
   ```
   below = (cx, cy - 1)
   nếu followFallingGrain && below trong grid && Cells[below] != 0 && Velocity[below] >= 1f:
       v = min(v, Velocity[below]); m *= airDrag; không splash / roll / slide / disperse; hạt đứng yên step này
   ```
   Hạt không move → đi nhánh `else` cuối (ghi `vel[i]`, `mom[i]`, `stamp[i]`) như hiện tại.
3. Không đổi gì khác (splash/roll/slide/dispersion trên mặt đất giữ nguyên để pile vẫn chảy như cũ).
4. Cập nhật XML summary của class: bỏ ý "momentum carries grains through the air", thêm 2 rule mới.

Lưu ý: số lần gọi RNG thay đổi → quỹ đạo khác bản cũ nhưng vẫn deterministic (cùng seed + cùng command = cùng grid).

### Test mới (`SandSimulationTests`)
- `AirborneGrain_WithMomentum_FallsStraight`: mask cột trống ~20×60, emit 1 hạt ở đỉnh, set `State.Momentum[i] = 2f`, step tới khi chạm đáy → x không đổi.
- `FallingGrain_BlockedByFallingGrain_DoesNotSplash`: 2 hạt cùng cột, hạt trên có `Velocity` lớn hơn; step cho tới khi cả hai chạm đáy → không hạt nào đổi x trong lúc airborne.
- Test cũ phải pass nguyên trạng.

## PACKET S2 — Visual: dòng dày + liền (B+)

### Field (`SandFieldVisual`, header "Falling stream")
```csharp
[SerializeField, Range(0f, 2f)] private float streamStreak = 1f;     // vệt = ceil(v * streak) cell phía trên hạt
[SerializeField, Range(0, 16)] private int streamBridgeCells = 10;   // nối khe tới hạt cùng màu phía trên nếu khe <= vệt + N
[SerializeField, Range(0, 2)] private int streamHalfWidth = 1;       // nong ngang ±N cell
[SerializeField, Range(0f, 1f)] private float streamGrain = 0.8f;       // texture hạt: tone jitter + khe tối + mép vỡ
[SerializeField] private int streamScrollCellsPerFrame = 2;          // pattern trôi xuống theo dòng
[SerializeField, Min(0)] private int maxStreamGrains = 8192;         // quá cap → bỏ phần overlay còn lại, không crash
```

### Thuật toán (khớp `Sim.prototype.draw` trong visualizer, panel B+)
- **Airborne grain**: `Cells[i] != 0 && Velocity[i] >= 1f && y > 0`, ô dưới **không** phải geometry (`!Valid || Static || CupWall || Dynamic`), và ô dưới trống **hoặc** là hạt có `Velocity >= 1f`. (Hạt trượt trên ramp/pile bị loại → không vẽ vệt đè lên mặt pile.)
- Trong vòng base-pass hiện có: ghi index hạt airborne vào `int[] streamGrains` cấp 1 lần ở `Bind()` (capacity `maxStreamGrains`).
- Overlay pass sau base-pass, chỉ trên danh sách đó:
  1. Vệt: `len = ceil(v * streamStreak)`; k = 1..len lên trên; dừng ở ô có hạt hoặc geometry; mark ô với material của hạt.
  2. Nối khe: `reach = len + streamBridgeCells`; quét lên trong vùng trống; nếu gặp hạt **cùng material** trong `reach` → mark toàn bộ khe.
  3. Mark cả ô của chính hạt làm seed cho bước nong.
  4. Nong: mỗi ô đã mark → paint `x ± 1..streamHalfWidth` nếu ô đó trống, không phải geometry, chưa mark.
- Màu — **phải đọc ra là hạt, không phải thanh màu phẳng** (thanh màu đặc 1 tone đã bị user reject; so B+ với texture hạt 0 vs 0.8 trong visualizer):
  - `r = hash01(x, y + scroll)` với `scroll = renderFrame * streamScrollCellsPerFrame` (`renderFrame` = counter riêng của `SandFieldVisual`, tăng mỗi lần `UpdateTexture`) (board y-up nên pattern trôi xuống = cộng). Hash integer ổn định, không dùng `UnityEngine.Random`.
  - Tone: `l = (r - 0.5) * 2 * 26 * streamGrain` cộng vào RGB (B nhân 0.95) như base-pass.
  - Alpha cơ sở: ô vệt/khe 0.95; ô nong `0.92 - 0.25*(|dx|-1)`.
  - Khe tối: ô trong (vệt/khe/nong không ở mép ngoài) nếu `r < 0.22*streamGrain` → alpha ×0.45; ô mép ngoài (`|dx| == streamHalfWidth`) nếu `r < 0.45*streamGrain` → alpha ×0.15 (mép vỡ).
  - Pixel = `lerp(emptyColor, palette[m] + l, alpha)`.
- Mark dùng `ushort[] streamStamp` + frame id (tăng mỗi frame, wrap thì clear 1 lần) thay vì clear mảng W·H mỗi frame. Base-pass đã ghi lại toàn bộ pixel mỗi frame nên không cần xoá overlay.
- **0 GC/frame**; buffer cấp ở `Bind()`, giải phóng cùng texture. Không đọc/ghi state ngoài đọc.

---

## Verification (tự chạy)

1. Compile + console sạch.
2. EditMode: `SandSimulationTests` (cũ + 2 mới) pass.
3. `PhaseCPlaythroughTests`: chạy toàn bộ. **Không sửa test hoặc level JSON để cho pass.** Test nào đổi kết quả → ghi vào evidence: tên test, kết quả cũ/mới, `LastLoseReason`, step count, và nhận định vì sao (ví dụ: script stroke đang dựa vào spray).
4. Nếu `PhaseCPerfCapture` chạy được rẻ: so `UpdateTexture` ms và GC alloc trước/sau ở level nặng nhất.

Evidence ghi vào `handoff/sand-stream-fix/evidence/verification.md`.

## Manual verify (PENDING MANUAL — user)

- Chơi 3 level Phase C trên Android: sand rời circle/ramp/path rơi thẳng, thấy một dòng liền; không có hạt lạc vào cup sai.
- Nhìn sát miệng cup: dòng dày 3 px không gây hiểu nhầm "trông như vào mà không tính".

## Decision-log entry (append nguyên văn vào `Docs/decision-log.md`)

```
## 2026-09-23 — Mở lại sand: stream rơi thẳng + render dòng (B+)

### Bối cảnh
Sand rời obstacle bị bắn xéo và tản thành bụi (video repro: 65% hạt đi lệch khỏi dòng chính). Với rule wrong-cup = thua ngay, spray gây thua oan. Visualizer: `Docs/visualizers/sand-stream-lab.html`.

### Quyết định
1. Supersede điểm 1 của entry "Đóng Phase C: freeze sand": mở lại sand cho fix này.
2. Sim: bỏ lateral drift khi airborne, `airDrag` 0.5, hạt bị chặn bởi hạt đang rơi thì bám theo, không splash giữa không trung.
3. Visual: `SandFieldVisual` vẽ vệt + nối khe + nong ±1 cell cho hạt airborne, phủ texture hạt trôi theo dòng (không để thanh màu phẳng). Chỉ là hình, không vào collision/cup count.
4. Không đổi `maxFallCellsPerStep` (giữ pacing).

### Đánh đổi đã chấp nhận
Hình dòng rộng hơn collision ±1 cell (0.06 unit). 3 level Phase C phải re-check; level nào đổi kết quả do GD/Dev chốt rebalance.

### Xem lại khi
GD thấy dòng quá dày/mảnh trên device, hoặc level mới cần sand bắn ngang có chủ đích.
```

## Report format

- Files changed
- Đã implement gì (S1, S2)
- Evidence tự verify (test pass/fail, playthrough diff, perf nếu có)
- Deviation / open risk
- Manual verify còn lại
