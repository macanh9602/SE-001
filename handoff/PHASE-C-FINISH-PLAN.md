# SE-001 — Phase C FINISH plan (C-finish)
rev 1 — 2026-09-22. Thay thế phần còn lại của PHASE-C-EXECUTE-PROMPT rev 3. Rev 3 vẫn là nguồn cho §8A/8B/8C
(units, sand feel, juice) trừ những chỗ bị override ở §2 dưới đây.

---------------------------------------------------------------------
## 0. Retro lần chạy trước — sai ở đâu, plan này sửa thế nào
---------------------------------------------------------------------

| # | Chuyện đã xảy ra | Nguyên nhân gốc (ở plan / prompt) | Sửa trong plan này |
|---|---|---|---|
| R1 | Codex làm logic-only, không có GameObject/visual → Play không thấy gì, không test được | Prompt 1 cục ~1000 dòng, 26 section; packet xếp theo layer (data→domain→…→visual cuối) nên hết "sức" trước khi tới phần nhìn thấy được | **Vertical slice**: mỗi packet kết thúc bằng một trạng thái **Play được và nhìn thấy được**. Visual/HUD không để cuối |
| R2 | "EditMode 18/18 PASS" nhưng 0 test Phase C | Gate ghi "tests pass" chung chung | Mỗi packet có **danh sách tên test bắt buộc**; report phải liệt kê test mới thêm. Không có test mới = packet FAIL |
| R3 | Lọ bị bịt kín cả 4 mặt → level không thể thắng; vị trí lọ lúc là tâm, lúc là đáy | Toạ độ/hình học chỉ mô tả bằng lời | **Geometry contract có số cụ thể** (§3) + test assert đúng tập ô (cell set) |
| R4 | Không ai phát hiện level không thắng được | Không có kiểm tra "chơi thử" tự động | **Scripted playthrough test** cho từng level: chạy N step với chuỗi tap/draw cố định → assert WIN (hoặc LOSE đúng lý do) |
| R5 | Input bị chặn toàn bộ vì Image `BG` phủ màn hình có Raycast Target | Rule "UI blocking check" không nói rõ chặn theo UI nào | Rule: chỉ chặn khi con trỏ nằm trên **UI tương tác được** (đã fix, không được regress) + test |
| R6 | Đơn vị mơ hồ: `emissionRate` lúc hạt/giây, lúc hạt/step; `FixedUpdate` 50 Hz nhưng mỗi step lại tính như 60 Hz | Tên field không mang đơn vị | Tên field **phải chứa đơn vị** (`grainsPerStep`, `...Seconds`, `...Cells`, `...Units`); một clock duy nhất |
| R7 | Win thì simulation đóng băng → cột cát đứng giữa không trung; cát rung liên tục; chóp nhọn 45° tràn ra ngoài lọ | Spec không nói về hành vi sau game over, về ổn định, về độ sạt | Đã fix và ghi vào **DO-NOT-REGRESS** (§2) + test |
| R8 | Code viết dồn nhiều lệnh một dòng, dùng legacy Input API, `ScriptableObject.CreateInstance` fallback bị leak | `standards/` chỉ được "đọc", không có check | **Style gate** kiểm được bằng lệnh (§6): cấm nhiều statement/dòng, dòng >160 ký tự trong file mới/sửa, cấm CreateInstance ở runtime path |
| R9 | Report "Play smoke: LevelRoot tạo đúng" = bằng chứng yếu | Không định nghĩa thế nào là evidence | Report dùng bảng **Claimed vs Verified** + evidence cụ thể (tên test, dump hierarchy, audit file) |
| R10 | Luật thay đổi giữa chừng (lọ chứa cát thật, vạch fill, lọ thuôn, dispersion) chỉ nằm trong chat/code | Không có nơi ghi decision | Mọi thay đổi luật phải vào `Docs/decision-log.md` trước khi code |
| R11 | Chính mình (Claude) patch mà không compile được → gây 1 lỗi compile (`PointerEventData.eventSystem`) | Cloud không có Unity | Codex **compile + chạy test sau từng packet**; patch nào chưa compile phải ghi rõ "UNCOMPILED" |

---------------------------------------------------------------------
## 1. Baseline hiện tại (đã có trong code, Codex phải giữ)
---------------------------------------------------------------------

Đang chạy được (dev placeholder):
- Tap source → van mở/đóng, source có vòi xoay 180°; cát vàng/đỏ/xanh theo material.
- Draw: kéo >18 px → preview → thả tay → nét extrude 3D, mask động 3+ ô, trừ mực, cắt nét khi hết mực.
- Lọ thuôn (taper 0.2), thành nghiêng thật trong mask, cát nằm lại trong lọ, đầy theo vạch fill 85%, đầy thì đóng miệng.
- WrongCup → LOSE ngay; hết cát + đứng yên 30 step + lọ chưa đầy → LOSE NotFilled.
- Sau game over sim vẫn settle (không emission/rule/input).
- Sạt lở `dispersion = 5`, không rung.
- `PhaseCDebugView` (OnGUI + visual tạm) và audit hook `phase-c-draw` — **tạm**, sẽ bị thay/gỡ ở F4/F6.

---------------------------------------------------------------------
## 2. DO-NOT-REGRESS + luật đã chốt (override rev 3)
---------------------------------------------------------------------

1. **Cup chứa cát thật (override §8A/§9 rev 3):** hạt KHÔNG bị xoá khi vào lọ. `Collected` = số hạt đúng màu đang nằm trong sink.
   `Required` = số ô sink dưới vạch fill (`CupProfile.fillLine`, mặc định 0.85) — **tính từ hình học**.
   `requiredAmount` của GD = số hiển thị (logical). HUD hiện `CollectedLogical/RequiredLogical`.
2. **Full** → hàng trên cùng của sink thành cup-wall (đóng miệng). Sau Full không còn collect/foreign check.
3. **Lọ thuôn:** đáy rộng `size.x*(1-taper)`, miệng rộng `size.x`; thành dày `max(wallThickness, 2.5*cellSize)` ở cả mask lẫn visual.
4. **Input:** chỉ chặn khi pointer trên UI có `Selectable` / click handler / drag handler. Decorative UI không được chặn.
5. **Nét vẽ:** bán kính ≥ 1.5 ô; nét bị cắt tại chỗ hết mực, không bỏ cả nét.
6. **Sim:** không trượt ngang trên mặt phẳng (chống rung); sạt lở `dispersion` đi 1 ô/step về phía chỗ thấp; ưu tiên chéo theo chiều quét.
7. **Sau game over:** tiếp tục `Step()` tới khi đứng yên; không emission, không rule, không input.
8. **Clock:** một accumulator 60 Hz trong GameplayManager, `maxStepsPerFrame = 4`, không dùng `FixedUpdate`.
9. **Lệnh reload/Next không được gọi trong OnGUI hay trong callback của object sẽ bị huỷ** — defer sang frame sau.
10. Camera fitter bám `BoardRoot`; extrude về -Z; profile/level warning khi thiếu cát theo material.

---------------------------------------------------------------------
## 3. Geometry & unit contract (có số để test)
---------------------------------------------------------------------

- Board-space XY, gốc (0,0) góc dưới-trái board, y hướng lên. `cellSize` hiện 0.1 (F1 có thể đổi 0.06 — mọi thứ dưới đây tính theo biến).
- Cell (x,y) phủ `[x*cell,(x+1)*cell) × [y*cell,(y+1)*cell)`, tâm `(x+0.5)*cell`.
- **SourceData.position** = miệng vòi (điểm phát cát). Phát ở hàng `floor(y/cell)` và hàng ngay dưới, bề rộng `streamWidthCells`.
- **CupData.position** = **giữa đáy ngoài** của lọ; `size.x` = bề rộng miệng ngoài; `size.y` = chiều cao ngoài.
- Ví dụ test bắt buộc (cell 0.1, wall 0.12→0.25, taper 0.2): cup pos (5.4,1.0) size (2.0,1.5):
  hàng y=10 toàn wall trong khoảng |xc-5.4| ≤ 0.8; hàng y=24 wall khi |xc-5.4| ∈ (0.75,1.0]; sink hàng 24 nằm trong |xc-5.4| ≤ 0.75; không có ô wall nào ở trên y=24.
- Đơn vị field: `grainsPerStep`, `streamWidthCells`, `valveOpenDelaySeconds`, `drawThicknessUnits`, `inkBudgetUnits`, `grainsPerUnit`.

---------------------------------------------------------------------
## 4. Packets — vertical slices, mỗi packet kết thúc Play được
---------------------------------------------------------------------

Chạy tuần tự. Sau MỖI packet: compile → EditMode tests → style gate → cập nhật implementation-notes → packet tiếp.
Mỗi packet ghi "Manual check (PENDING MANUAL)" để Product Owner tự bấm.

### F0 — Baseline & guard (nhỏ)
- Xác nhận compile sạch trên baseline hiện tại; chạy toàn bộ test; ghi `git rev-parse HEAD`.
- Thêm test khoá DO-NOT-REGRESS hiện có (§2): `CupGeometry_MatchesContractExample`, `Cup_SandStaysAndCountsTowardFillLine`,
  `Cup_FullClosesMouth`, `Sim_NoLateralJitterOnFlatSurface`, `Sim_DispersionFlattensPile`, `Stroke_TruncatedAtInk`,
  `Input_DecorativeUiDoesNotBlock` (có thể test logic filter tách hàm), `GameOver_SimKeepsSettling`.
- Exit: toàn bộ test trên PASS trên code hiện tại **trước khi đổi gì thêm**.

### F1 — Sand feel "Bột mịn" (rev 3 §8B) — nhìn thấy ngay
- Port model lab: per-cell velocity/momentum/shade (SoA, cấp phát 1 lần), gravity+vmax, multi-cell fall, splash→momentum, momentum slide trên ramp, flow; **giữ** dispersion/không rung (§2.6) — flow ngẫu nhiên phải qua PRNG và chỉ đi về phía thấp hơn hoặc có budget để không rung vô hạn.
- PRNG seed theo levelId; cùng input → cùng grid.
- SandFieldVisual: palette + jitter + highlight, bilinear, không alloc (`GetPixelData`).
- Chọn `cellSize` 0.06 nếu step ≤ 2 ms trên Editor profiler ở level đầy cát; nếu không, giữ 0.1 và ghi số đo.
- Tests: `Sim_Deterministic_SameSeedSameGrid`, `Sim_ConservesGrains_WithMomentum`, `Sim_MomentumSlidesFurtherOnShallowRamp`, `Sim_ReachesStableState` (hết rung), + toàn bộ F0 vẫn PASS.
- Manual check: đặt cạnh `sand-feel-lab.html` preset "Bột mịn" — dòng mềm, đống thoải, trượt trên nét vẽ.

### F2 — Visual thật cho Source / Cup / DrawStroke
- Shader `SE001/SpriteSurface` (lit-lite + alpha clip + ShadowCaster), materials `MAT_Source`, `MAT_Cup`, `MAT_DrawPath`.
- Prefab `SandSource` (Pivot/View/EmitPoint), `Cup` (View thuôn + rim + back + fill-line), `DrawStroke` (mesh extrude) + Factory + CreateParameters theo rev 3 §6–7.
- Visual lọ lấy hình từ **CupDomain** (taper, effective wall) — không tự tính riêng.
- Bỏ phần visual trong `PhaseCDebugView` (giữ overlay tới F4).
- Tests: `SourceFactory_BindsAndCleansUp`, `CupFactory_VisualMatchesDomainGeometry` (so bounds), `DrawStrokeFactory_ReleasesMesh`, lifecycle 10 vòng không tăng object/mesh.
- Manual check: Play level 01–03, thấy source/lọ/nét đúng vị trí, có bóng.

### F3 — Data, profiles, validator, levels
- Tạo asset: `MaterialPalette`, `PhaseCSourceProfile`, `PhaseCCupProfile`, `PhaseCDrawPathProfile`, `PhaseCGameplayRuntimeProfile`, `PhaseCLevelSequence` — gán qua LevelSpawner/PrefabProfile. **Xoá mọi `CreateInstance` fallback ở runtime** (thiếu asset = lỗi rõ ràng).
- Input controller đọc DrawPathProfile (hết hardcode).
- Validator Phase C: ID unique; material ∈ palette; source/cup không đè static mask; **tổng hạt theo material ≥ tổng Required** (đổi Warning runtime thành validation error); requiredAmount > 0.
- Retune 3 level: 01 thắng bằng tap-only; 02 **bắt buộc** vẽ; 03 hai màu, có đường sai dẫn tới WrongCup.
- Tests: `Validator_*` cho từng luật; **`Playthrough_Level01_TapOnly_Wins`**, **`Playthrough_Level02_NoDraw_Loses`**, **`Playthrough_Level02_ScriptedDraw_Wins`**, **`Playthrough_Level03_WrongRoute_LosesWrongCup`**, **`Playthrough_Level03_Correct_Wins`** (chạy sim headless N step với tap/draw script cố định).
- Manual check: Next/Prev theo sequence, Next disabled ở level cuối.

### F4 — HUD thật + Win/Lose
- uGUI (project đang dùng uGUI): level label, Energy bar (ink), mỗi lọ logical count, trạng thái; panel Win (Next/Retry), Lose (lý do + Retry). Retry luôn có.
- HUD chỉ nghe event/snapshot GameplayManager. Nút → lệnh deferred qua LevelManager.
- Gỡ `PhaseCDebugView` hoàn toàn.
- Tests: `Hud_ReflectsSnapshot`, `Retry_ResetsAllState`, `Next_FollowsSequence`, `Next_DisabledOnLast`.

### F5 — Juice (rev 3 §8C) qua `JuiceProfile`
- Source: xoay ease-out-back đúng `valveOpenDelaySeconds`, pulse khi rót, lượng trong lọ source giảm mượt, lắc nhẹ khi tap lúc Empty.
- Cup: punch khi nhận (≤ 6/s), pop + flash khi Full, rung đỏ khi WrongCup.
- Stroke: extrude mọc 0→full khi commit. Ink bar mượt, đỏ khi thấp. Win: lọ nảy lần lượt.
- DOTween có sẵn; kill tween khi unload; không alloc per frame.
- Tests: `Juice_TweensKilledOnUnload` (không còn tween sống sau 10 vòng).
- Manual check: cảm giác "đã mắt" — PO chấm.

### F6 — Cleanup & closure
- Gỡ hook audit `phase-c-draw` (giữ `AgentDebugAudit` helper).
- Style gate PASS trên toàn bộ file Phase C (viết lại file one-liner của lần trước: `PhaseCProfiles.cs`, validator, LevelJson Phase C part).
- Docs: `decision-log.md` (tất cả luật §2), `data-model.md` (schema + ý nghĩa position/size), `runtime-architecture.md`, Phase C `PHASE.md` + implementation-notes, ROADMAP.
- Perf record: grid, số hạt đang động, ms/step, KB upload/frame, GC steady-state.
- 10-cycle lifecycle PASS; console sạch; `git diff --check`.

---------------------------------------------------------------------
## 5. Report bắt buộc (mỗi packet + cuối)
---------------------------------------------------------------------

| Item | Claimed | Verified by | Evidence |
|---|---|---|---|
| ví dụ: Level01 thắng tap-only | yes | `Playthrough_Level01_TapOnly_Wins` | test PASS, 412 steps |

- Liệt kê **tên test mới** + PASS/FAIL.
- Cái gì chưa chạy được → `PENDING MANUAL` hoặc `NOT RUN`, không được ghi PASS.
- File chưa compile → `UNCOMPILED`.

---------------------------------------------------------------------
## 6. Style gate (chạy được bằng lệnh)
---------------------------------------------------------------------

Trên các file .cs tạo/sửa trong Phase C:
- Không có dòng chứa ≥ 2 statement kết thúc bằng `;` ngoài `for(...;...;...)`.
- Không dòng > 160 ký tự.
- Không `ScriptableObject.CreateInstance` trong runtime path (chỉ tests/editor).
- Không `FindObjectOfType`/`Camera.main` trong hot path (cache ở Bind).
- Theo `standards/code-style.md` cho naming/braces.

---------------------------------------------------------------------
## 7. Stop conditions
---------------------------------------------------------------------

BLOCKED chỉ khi: luật gameplay chưa định nghĩa; F1 không giữ được determinism + ổn định cùng lúc; perf step > 4 ms không giảm được
mà không cần Burst (ghi lại, đề xuất, dừng F1 ở cell 0.1). Mọi quyết định local/reversible khác: tự chọn, ghi decision-log.

---------------------------------------------------------------------
## 8. Việc của Product Owner trước khi giao
---------------------------------------------------------------------

1. Compile + Play xác nhận bản hiện tại (dispersion, lọ thuôn) ổn → commit, điền hash vào F0.
2. Bỏ tick Raycast Target của `BG` trong GameScene.
3. Chốt: có đổi `cellSize` 0.06 ở F1 không nếu perf cho phép (mặc định: có).
