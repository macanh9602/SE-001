# Phase C — Finish Plan (5 packets)

Base: HEAD `c5727d1`. Viết ngày 2026-09-23. Owner thực thi: Codex. Người duyệt: Ducan.
Plan này thay cho mọi to-do Phase C rải rác; F0–F6 cũ không dùng nữa.

> **Trạng thái 2026-09-23:** C1-FINAL, C2A, C2B, C3 đã xong. Juice cup (`cupPunchDuration`) và stroke-grow
> (`strokeExtrudeDuration`) **bị bỏ khỏi scope** theo quyết định của Ducan; chỉ còn juice valve. C4 còn các mục
> PENDING MANUAL phải chạy trong Unity — xem `PHASE.md`. Plan này giữ lại làm hồ sơ, không còn là to-do.

## 0. Trạng thái đã xác minh trên repo

| Claim | Kết quả kiểm tra |
|---|---|
| Sand màu sai do thiếu binding | ĐÚNG. `SandFieldVisual.SetPalette()` chỉ có định nghĩa (`Elements/Sand/SandFieldVisual.cs:31`), không nơi nào gọi. `LevelSpawner.SpawnSandField()` chỉ `Bind(simulation)` → rơi về `sandColor` vàng mặc định. |
| LevelManager ép level | ĐÚNG. `LevelManager.cs:17` `RequiredLevelId`, `:81` `levelId = RequiredLevelId`. |
| Next không chạy | ĐÚNG. `CanBeginNextLevel()` và `BeginNextLevel()` `return false` cứng. |
| Chỉ có 1 level | ĐÚNG. `Resources/Levels/` chỉ có `phase_c_level_02.json`. |
| Test matrix giả | ĐÚNG. Test tên Level01/03 nhưng đều load level02. |
| Sand sim đã có surface-flow + edge release | ĐÚNG, đã commit (`6fe7e4b` trở đi). |

## 1. Quyết định đã chốt

1. **3 level thật + progression**: author `phase_c_level_01/02/03`, bỏ force id, sequence 01→02→03, Next chạy thật.
2. **487/534**: đo thất thoát trước (đếm hạt theo số phận), có số rồi mới quyết sửa level hay sửa sim.
3. **Sand feel: freeze.** Chỉ mở lại nếu C2B chứng minh root cause nằm ở sim.
4. **Juice: tối thiểu để chơi được.** HUD + panel kết quả + 4 feedback lõi.
   - HUD stack: **HUDSystem + prefab có sẵn**; `PhaseCHudView` chỉ còn HUD trong game.
   - `WinPanel`/`LosePanel`: viết lại cho SE-001, gán onClick trong prefab.
5. **ColorProfile theo colorId** (yêu cầu mới): thay `MaterialPalette`/`materialId` bằng một profile màu khóa theo `colorId`, dùng chung cho sand, source, cup, UI.

## 2. C1-FINAL — ColorProfile + Sand Color Closure

Mục tiêu: một nguồn màu duy nhất theo `colorId`, và sand thực sự lấy màu từ đó.

- Tạo `ColorProfile : ScriptableObject`, entry `{ int colorId; Color32 sandColor; Color32 uiColor; string displayName; }`, kèm `GetSandColor(byte colorId)`, `GetUiColor`, `Contains`, và `BuildSandLookup()` trả `Color32[]` index theo colorId cho renderer.
- Thay `MaterialPalette` ở: `GameplayRuntimeProfile.materialPalette` → `colorProfile`; `LevelDataLoader`, `LevelDataValidator`, `SourceFactory`, `CupFactory`, `PhaseCSourceVisual`, `PhaseCCupVisual`, `PhaseCAssetSetup`. Xóa `MaterialPalette.cs` sau khi không còn reference.
- **Fix binding**: trong `LevelSpawner.SpawnSandField()` gọi `visual.SetPalette(colorProfile.BuildSandLookup())` **trước** `visual.Bind(simulation)`.
- `SandFieldVisual.sandColor` chỉ còn là fallback khi profile null; log warning nếu fallback bị dùng trong production.
- Asset: `PhaseCColorProfile.asset` với colorId 1 = Coral `#E8414F`, colorId 2 = Blue `#2F6BFF`.
- **Schema JSON**: giữ nguyên tên field `materialId`/`acceptedMaterialId` trong level JSON ở packet này (tránh phải migrate data giữa chừng), map thẳng sang `colorId`. Nếu muốn đổi tên field thì làm ở C2A cùng lúc author 3 level mới, bump `schemaVersion`.

Acceptance:
- Test: colorId 1 ra coral, colorId 2 ra blue, level 2 màu không lẫn nhau; test fail nếu `SetPalette` không được gọi (assert lookup khác fallback).
- 7 test C1 cũ xanh lại; screenshot level02 đúng 2 màu, lưu vào `handoff/.../evidence/`.
- Không đụng `SandSimulation`.

## 3. C2A — Real Level Data + Progression

- Author thật `phase_c_level_01` (dạy tap + 1 nét vẽ), `phase_c_level_02` (2 màu, đang có), `phase_c_level_03` (2 màu + obstacle chéo, cần 2 nét).
- `LevelManager`: bỏ `RequiredLevelId` và dòng ép `levelId = RequiredLevelId`; `firstLevelId` thành field bình thường; `CanBeginNextLevel()`/`BeginNextLevel()` chạy theo `PhaseCLevelSequence`.
- `PhaseCLevelSequence.asset`: 01 → 02 → 03.
- Validator bổ sung: supply ≥ required cho từng colorId; source/cup/obstacle không chồng lên static mask; mọi colorId trong level phải có trong ColorProfile; mỗi cup phải reachable (ít nhất tồn tại một đường rơi tự do từ source tới miệng cup khi chưa vẽ — hoặc ghi rõ level cần vẽ).
- Mỗi level phải load độc lập được (test load từng id).

Acceptance: load riêng từng level không lỗi; Next từ 01 sang 02 sang 03 chạy; validator bắt được 4 loại lỗi trên bằng fixture sai cố ý.

## 4. C2B — Playthrough Closure + Balance (điều tra 487/534)

Bước 1 — đo, chưa sửa. Thêm audit đếm hạt theo số phận, ghi vào `AgentAudit/phase-c-supply.md`:

| Nhóm | Ý nghĩa |
|---|---|
| inCorrectCup | vào đúng cup |
| inWrongCup | vào sai cup |
| restingOnObstacle | đọng trên obstacle/stroke khi sim đã đứng yên |
| onFloor | rơi xuống sàn/ngoài cup |
| stillMoving | còn đang chuyển động khi hết thời gian |
| neverEmitted | source còn hạt chưa phát |

Bước 2 — kết luận theo số:
- Nếu `restingOnObstacle` lớn → level layout hoặc sim transport. Xem lại `slopeAccel`, `maxSlideCells` (đây là cửa duy nhất được mở lại sand feel, và phải chạy lại toàn bộ playthrough sau đó).
- Nếu `onFloor` lớn → layout: cup lệch, miệng cup hẹp, hoặc dòng cát lệch khỏi mép.
- Nếu `neverEmitted` lớn → logic valve/amount.
- Nếu tất cả đều nhỏ mà vẫn thiếu → required derive từ hình học cup đang cao hơn supply → sửa authoring supply hoặc kích thước cup.

Bước 3 — 5 test playthrough thật trên 3 JSON khác nhau: `Level01_TapOnly_Wins`, `Level02_TwoColors_Wins`, `Level02_NoDraw_Loses`, `Level03_WrongCup_Loses`, `Level03_TwoStrokes_Wins`.

Cấm: hạ `requiredAmount` xuống đúng con số đang đạt để test xanh.

Acceptance: 5 test xanh; file audit có số liệu trước/sau; nếu có đổi sim thì ghi rõ tham số cũ → mới và lý do.

## 5. C3 — HUD + Result + Juice tối thiểu

### 5.1 Hiện trạng (đã kiểm tra)

Project đang có **hai hệ HUD song song**:
- `HUDSystem : UIPanels<HUDSystem>` — flow panel đầy đủ: `Show<T>(ShowType)`, `Hide<T>()`, `HideAllPanelExclude<T>`, `BlockByPanel()`, `blockTouched`, `tapToHide`, `IsLock`. Prefab: `HUDSystem.prefab`, `GameWin_Canvas.prefab`, `GameLose_Canvas.prefab`.
- `PhaseCHudView` — tự dựng canvas bằng code lúc runtime, có Retry/Next riêng, **không** dùng HUDSystem.

Và: `WinPanel`/`LosePanel` vẫn là stub của project cũ (`using CH013.Gameplay`, thân hàm comment hết); button trong hai prefab **không có onClick nào được gán**; không code nào ngoài thư mục HUD tham chiếu tới hai prefab đó.

### 5.2 Quyết định

- **HUDSystem + prefab là hệ chính** cho panel kết quả. `PhaseCHudView` thu lại chỉ còn HUD trong game (ink bar, cup count, source count) và **bỏ hai nút Retry/Next tự dựng**.
- **Viết lại `WinPanel`/`LosePanel` cho SE-001**, bỏ `using CH013`.
- Giữ prefab cũ, FX thừa (confetti/firework/star/coin) dọn ở packet juice hoặc phase sau.

### 5.3 Flow bật tắt UI (bắt buộc, đây là phần trước đây plan còn thiếu)

1. `GameplayManager.GameStateChanged` là nguồn duy nhất kích hoạt panel. Không panel nào tự poll state.
2. `Playing` → không panel kết quả nào mở; nếu đang mở thì `Hide<WinPanel>()` / `Hide<LosePanel>()`.
3. `Won` → `HUDSystem.Instance.Show<WinPanel>(ShowType.DissmissCurrent)`, truyền data `{ levelId, cupSummary, canNext }`.
4. `Lost` → `Show<LosePanel>(ShowType.DissmissCurrent)`, truyền data `{ levelId, loseReason }`; `LosePanel` hiển thị lý do bằng chữ (`WrongCup` / `NotFilled`).
5. Panel kết quả đặt `blockTouched = true` và `tapToHide = false`: chặn tap vào jar/vẽ phía sau, và không cho tap ra ngoài để tắt.
6. Gameplay input phải hỏi `HUDSystem.Instance.BlockByPanel()` trước khi nhận tap/draw. Đây là điều kiện cho guard test `Input_DecorativeUiDoesNotBlock` (HUD trang trí không chặn, panel kết quả thì chặn).
7. Khi đổi level (`LevelWillUnload`) phải `HideAllPanelExclude<HudPanel>()` hoặc hide tường minh cả hai panel, tránh panel sống sót qua reload. `HUDSystem` có `DontDestroyOnLoad` nên đây là lỗi dễ xảy ra.
8. Sim vẫn tiếp tục settle sau khi panel mở (guard `GameOver_SimKeepsSettling`); panel chỉ là lớp trình bày.

### 5.4 Wiring button

- `WinPanel.NextLevel()` → `LevelManager.Instance.BeginNextLevel()`; disable/ẩn nút Next khi `CanBeginNextLevel() == false` (level cuối) và thay bằng nút Home hoặc Replay.
- `LosePanel.RestartLevel()` → `LevelManager.Instance.ReloadCurrentLevel()`.
- `GameLose_Canvas` đang có sẵn `Home_Button` và `Retry_Button`; `GameWin_Canvas` cần nút Next.
- Gán `onClick` **trong prefab** (persistent listener) trỏ tới method của panel script, không `AddListener` bằng code lúc runtime, để tránh double-subscribe khi panel được show lại.
- Panel gọi lệnh lifecycle qua một flag xử lý ở `Update` (giống `PhaseCHudView` đang làm), không gọi thẳng trong callback của button, vì `BeginLevel` sẽ destroy object đang xử lý sự kiện.
- Chặn double-click: sau lần bấm đầu, khóa nút cho tới khi level mới `IsReady`.

### 5.5 HUD trong game

- Ink/energy bar thật theo `drawInkBudget`, cập nhật khi vẽ và khi commit stroke.
- Cup count theo logical unit (không phải số hạt), source còn lại.
- Feedback lõi theo `JuiceProfile`: valve đóng/mở, cup nhận cát, cup đầy, vào sai cup. Các mục F5 còn lại ghi vào backlog phase sau.
- Xóa hẳn `PhaseCDebugView` và mọi reference.

### 5.6 Acceptance

- Test: `Won` → WinPanel active và LosePanel không active; `Lost` → ngược lại; `Playing` → cả hai tắt.
- Test: panel kết quả mở thì `BlockByPanel()` true và gameplay không nhận được tap/draw; HUD thường thì false.
- Test: reload level khi panel đang mở → panel đóng, không còn instance sống sót.
- Test: ink bar giảm đúng theo chiều dài stroke; lý do thua đúng loại.
- Test: bấm Next ở level cuối không crash; bấm Retry hai lần nhanh chỉ reload một lần.
- Console sạch, không warning missing reference từ hai prefab.

## 6. C4 — Closure

- Full regression EditMode; bổ sung guard test còn thiếu: `Stroke_TruncatedAtInk`, `Input_DecorativeUiDoesNotBlock`, `GameOver_SimKeepsSettling`, `Sim_ReachesStableState`.
- Lifecycle reload ×10 trên cả 3 level.
- Perf capture mới trên 3 level thật (file perf cũ đang stale, còn ghi level01/03 không tồn tại): frame time, GC alloc trong sim loop, số draw call của sand.
- `git diff --check`, style pass, console sạch.
- Screenshot + walkthrough thủ công lưu trong evidence folder.
- Sync `Docs/PHASE.md` + `ROADMAP.md` + `implementation-notes.html` về trạng thái thật, đánh dấu Phase C `DONE / CLOSURE PASS`.

## 7. Thứ tự và phụ thuộc

C1-FINAL → C2A → C2B → C3 → C4. Không đảo thứ tự: C2B phải chạy trên data thật của C2A, và mọi quyết định về sand feel chỉ được mở sau khi C2B có số.

## 8. GD check được gì sau mỗi packet

Nguyên tắc: mỗi packet phải để lại **thứ GD tự mở/tự chỉnh được, không cần dev**.

### Sau C1-FINAL
- Mở `Resources/Profiles/PhaseCColorProfile.asset`, đổi màu theo `colorId` → Play thấy **cát, jar, cup và số trên HUD cùng đổi màu**. Đây là cách check nhanh binding có đúng không: nếu cát không đổi màu theo profile thì binding hỏng.
- Thêm một colorId thứ 3 vào profile để thử bảng màu mới, chưa cần sửa level.
- Evidence: screenshot level02 hai màu trong `handoff/.../evidence/`.

### Sau C2A
- Chỉnh trực tiếp trong JSON level (`Resources/Levels/phase_c_level_0X.json`): vị trí source/cup, `logicalAmount`, `emissionRate`, `streamWidth`, `requiredAmount`, `drawInkBudget`, obstacle.
- Bấm Play → level đầu load theo `firstLevelId`; qua màn bằng nút Next theo sequence 01→02→03.
- `ToolSelectLevelUI` (dev level select trong game) phải chọn được 3 level. **Lưu ý:** `_levelIdFormat` hiện là `level_{0:00}`, không khớp id `phase_c_level_02`; C2A phải sửa format hoặc đặt danh sách id tường minh, nếu không GD bấm chọn level sẽ không load được.
- Validator: nếu GD chỉnh sai (supply < required, source/cup chồng tường, colorId không có trong ColorProfile) thì **báo lỗi rõ ràng ngay khi load**, kèm tên level và field sai — không im lặng rồi chơi hụt cát.

### Sau C2B
- Mở `AgentAudit/phase-c-supply.md` thấy bảng số hạt theo số phận cho từng level: vào đúng cup / sai cup / đọng trên obstacle / rơi xuống sàn / còn chuyển động / chưa phát.
- Từ bảng đó GD tự biết phải tăng `logicalAmount` bao nhiêu, hay layout đang làm rơi cát ra ngoài — chỉnh JSON rồi chạy lại, không cần dev.

### Sau C3
- **Chơi thật được từ đầu tới cuối**: mở valve, vẽ, thắng/thua, bấm Retry hoặc Next, qua màn.
- Ink bar tụt đúng theo nét vẽ; cup count theo logical unit; thua thì panel hiện đúng lý do (`WrongCup` / `NotFilled`).
- Chỉnh `JuiceProfile`, `SourceProfile`, `CupProfile` trong Inspector rồi Play lại để cảm feedback (valve, cup nhận cát, cup đầy, sai cup).
- Kiểm tra nhanh flow UI: panel kết quả mở thì không vẽ/tap được vào jar phía sau; reload level thì panel không sót lại.

### Sau C4
- Số perf trên 3 level thật (frame time, GC alloc trong sim loop, draw call của sand) trong file perf mới.
- Screenshot + walkthrough trong evidence folder; `PHASE.md`/`ROADMAP.md` khớp trạng thái thật.

### GD **không** tự check được (cần dev)
- Nội bộ sim: `flowOrderedRows`, `airDropProbe`, `momentumCarry`… Các field này nằm trong `PhaseBSandSimulationProfile` và **đang freeze** theo quyết định ở mục 1.
- Determinism, GC, và số bước sim mỗi frame.

## 9. Rủi ro đã biết

- **Đổi ColorProfile chạm nhiều file** (8 file + asset). Làm nguyên packet C1-FINAL, không trộn với C2A.
- **Test playthrough hiện tại sẽ đỏ sau C2A** vì chúng đang mô tả state cũ. Đây là kết quả mong muốn, sửa trong C2B.
- **HUDSystem có `DontDestroyOnLoad`**: panel kết quả dễ sống sót qua reload level nếu không hide tường minh ở `LevelWillUnload`. Đã đưa thành acceptance test ở C3.
- **Sand feel vừa đổi tối 22/09** (surface flow + edge release). Nếu C2B phải mở lại sim thì toàn bộ balance của 3 level phải chạy lại.
- **Perf chưa đo lại** sau khi thêm `flowOrderedRows` (quét thêm 1 lần mỗi hàng có cát mỗi step). Bắt buộc đo ở C4, nếu tốn thì cho phép tắt theo profile.
