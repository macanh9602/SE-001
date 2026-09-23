# Phase D — GD Authoring Workflow & Level Editor

Nguồn duy nhất cho Phase D. Gộp plan gốc (C2C INIT → PLAN) + các sửa đổi trong `PHASE-D-REVIEW.md` (đã áp thẳng vào đây).
Viết 2026-09-23, đối chiếu repo tại `c5727d1`.

Workspace: SE-001, Unity/C#. Architecture source of truth: `SE001-ARCHITECTURE-BLUEPRINT.md`.
Skill chính: `skills/level-editor/`. Hỗ trợ: `skills/gd-communication/`, `skills/debug-audit/`, `skills/presentation-lifecycle/`, `skills/technical-slice/`. Skill review mới: `skills/editor-ux-review/`.

Trạng thái cuối: `DONE / CLOSURE PASS` hoặc `BLOCKED — blocker cụ thể + evidence`. Không có PARTIAL.

---

## 0. Entry gate (đã sửa so với plan gốc)

Plan gốc cấm mọi thứ trước khi Phase C closure. Sửa lại theo phụ thuộc thật:

| Packet | Phụ thuộc thật | Được bắt đầu khi |
|---|---|---|
| D0 | không | ngay |
| D0.5 | `LayoutRasterizer`, `LevelSpawner` (đã ổn định) | ngay |
| D1 | schema sau D0.5 | D0.5 xong |
| D2 | D1 | D1 xong |
| D3 | layout asset, mapper/rasterizer dùng chung | D0.5 + D2 |
| D4 | validator Phase C, mask đã bake | D0.5 + D3 |
| D5 | D3, D4 | — |
| D6 | `LevelManager`/`LevelSpawner` ổn định | **Phase C tick đủ 4 ô PENDING MANUAL** |
| D7 | sequence/progression Phase C | **Phase C tick đủ 4 ô PENDING MANUAL** |
| D8 | tất cả | — |

Ngoài ra cần: git tree sạch và một commit mốc trước khi vào D6/D7.

Phase D **không** sửa gameplay core còn dở của Phase C. Phase D sở hữu: GD authoring workflow, progression/save production, Level Editor, SVG workflow, validation, runtime-parity preview, unsaved Play Test, GD-facing UX, editor usability/performance/closure.

---

## D0 — Skill + UX system bootstrap

Mục tiêu: đủ hướng dẫn tái dùng để một worker mức thấp xây được editor mà không phải tự phát minh architecture/UI/UX. **Chưa implement editor.**

### D0.A — làm giàu `skills/level-editor/`
Đọc trước: `skills/AUTHORING.md`, `skills/level-editor/SKILL.md`, `refs/checklist.md`, `refs/anti-patterns.md`, `refs/workflow-details.md`, `knowledge/editor-ux/*`, `playbooks/p4-level-editor.md`.

Thêm `recipes/`: `editor-window-partials.md`, `document-view-derived-state.md`, `apply-edit-vs-reload.md`, `split-workspace-uitk.md`, `delayed-fields-and-callbacks.md`, `stable-selection-remap.md`, `validation-focus-flow.md`, `unsaved-play-test.md`.

Recipe được chứa skeleton C#/UXML/USS generic. **Không** copy data/gameplay của game vào skill generic.

### D0.B — tạo `skills/editor-ux-review/`
`SKILL.md` + `refs/`: `usability-review.md`, `visual-hierarchy.md`, `editor-visual-system.md`, `first-use-review.md`, `review-checklist.md`.

Skill này **review** tool sau khi implement, không thay cho guidance implement. 14 chiều bắt buộc: task completion, discoverability, information architecture, interaction quality, error recovery, visual hierarchy, control sizing/density, semantic color, icon usage, frequency-based emphasis, responsive layout, accessibility/readability, GD terminology, first-use không cần tài liệu.

### D0.C — mini editor visual system
Control roles: primary action, secondary action, danger action, toolbar tool toggle, icon action, section header, inline validation, status badge, empty state.
Semantic roles: primary, active/selected, success, warning, danger, info, muted.
Emphasis tiers: high (thường dùng / task hiện tại / trạng thái chặn), normal (authoring thường), low (metadata / advanced / hiếm).
Frequency tiers: always visible, contextual, advanced/collapsible.

Rules: Save / Play Test / trạng thái validate chặn **không bao giờ được biến mất**; primary phải nổi hơn secondary; danger không đặt sát primary mà không có ngăn cách; action hay dùng có vị trí cố định; icon-only chỉ cho action hiển nhiên; action quan trọng dùng icon + label; tool đang active và entity đang chọn phải nhận ra ngay; field advanced/raw không được lấn inspector; canvas là trọng tâm thị giác; màu semantic để truyền trạng thái, không trang trí; tránh nhiều accent color cạnh tranh; control disabled phải giải thích lý do qua tooltip/status; empty state phải nói GD làm gì tiếp.

### Exit
Recipes tồn tại và generic; `editor-ux-review` có quy trình review rõ; visual-system dùng được mà không phải tự chế design rule; checklist cũ được tham chiếu chứ không nhân bản; không nhúng gameplay SE-001 vào skill generic.
Evidence: danh sách file skill thay đổi + bảng map `Blueprint requirement → recipe/review rule`.

---

## D0.5 — Layout Asset Pipeline (SVG → prefab mesh + mask) — **packet mới**

Lý do: `LayoutRasterizer.Rasterize()` chạy mỗi lần load, quét từng cell (board 10.8×19.2 ở `cellSize 0.06` = 57.600 cell), mỗi cell test point-in-polygon qua **toàn bộ** contour. Layout SVG vài nghìn điểm → hàng chục tới hàng trăm triệu phép test mỗi lần load → lag/treo. Mesh layout cũng dựng runtime qua `LayoutVisualFactory`. `PhaseBSvgImporter` hiện chỉ là MenuItem fixture cứng, ghi thẳng contour vào JSON (`phase_b_svg_test.json` từng dài 6.600 dòng).

### Input / Output
- Input: file `.svg` do GD chọn (panel chọn file, không hardcode fixture).
- Output mỗi layout:
  - `LayoutPrefab`: mesh đã triangulate + material, pivot tại gốc board.
  - `LayoutMaskAsset`: static mask đã bake, kèm `cellSize`, `boardSize`, `contourHash`, `importerVersion`.
  - `LayoutDefinition` (ScriptableObject): `layoutId`, tham chiếu prefab + mask, board size, thumbnail.

### Quy tắc
- Mask bake bằng **đúng** `LayoutRasterizer` (chạy trong Editor). Không viết bộ rasterize thứ hai — đây là điều kiện parity.
- Runtime: `cellSize` của `SandSimulationProfile` khác giá trị đã bake, hoặc `contourHash` lệch → **chặn load, lỗi rõ ràng**, không âm thầm rasterize lại.
- Có `Rebake` cho một layout và `Rebake All`.
- Layout prefab không chứa logic gameplay; source/cup không nằm trong layout.
- Level JSON **chỉ** reference `layoutId`; bỏ `wallContours`/`staticObstacles`; bump `schemaVersion`; có migration cho level cũ. Validator từ chối JSON có cả `layoutId` lẫn contour.

### Tests
`LayoutBake_MaskMatchesRuntimeRasterizer` (so từng cell), `LayoutBake_StaleCellSize_BlocksLoad`, `LayoutBake_ReimportSameSvg_IsDeterministic`, `LevelLoad_UsesBakedMask_NoRasterizeCall` (đếm call = 0), `LevelJson_MigratesContoursToLayoutId`.

### Acceptance đo được
Thời gian load level có layout SVG thật: ghi số **trước / sau** vào evidence; phần layout gần như bằng 0 và không phụ thuộc số điểm contour. File level JSON giảm rõ rệt.

---

## D1 — Data / Document / File lifecycle

Mục tiêu: GD tạo, mở, lưu, mở lại một level document mà không cần code hay sửa JSON tay.

Kiến trúc bắt buộc tách 3 tầng:
- **Document**: `SE001LevelJson`, current path, dirty state, schema/version.
- **ViewState**: tool/mode, stable selected IDs, zoom/pan, search/filter, pane dimensions.
- **DerivedState**: validation issues, preview caches, stale flags, metrics.

**ViewState và DerivedState không bao giờ serialize vào level JSON.**

Implement: New, Open, Save, Save As, version/schema handling, dirty state, safe close/open guard, round-trip serialization, xử lý file hỏng/không hỗ trợ. `ApplyEdit != ReloadDocument` ngay từ đầu.

Tests: `LevelDocument_New_IsValid`, `LevelDocument_SaveLoad_RoundTripsAllFields`, `LevelDocument_ViewState_NotSerialized`, `LevelDocument_SaveAs_DoesNotOverwriteWrongFile`, `LevelDocument_MalformedFile_DoesNotDestroyOpenDocument`, `LevelDocument_DirtyState_TracksEdits`, `LevelDocument_Reload_PreservesValidViewState`.

Evidence: tạo document trắng → save → reopen → so sánh hierarchy/data; screenshot trạng thái window đầu tiên.
UX review: first-use có nhận ra New/Open/Save không; dirty/saved thấy ngay; empty state nói bước tiếp theo.

---

## D2 — Production editor shell

Mục tiêu: mở Level Editor ra là một workspace GD chuyên nghiệp. Dùng UI Toolkit.

```
┌ Levels ┬──────────── Board Canvas ───────────┬ Inspector ┐
│ list   │ tool / options / context bars       │ entity    │
│ search │                                     │ fields    │
│ New... │                                     │           │
├────────┴─────────────────────────────────────┴───────────┤
│ file · dirty · validation · status                       │
└──────────────────────────────────────────────────────────┘
```

Bắt buộc: split pane thật, resize được, nhớ kích thước pane, canvas nhận phần không gian còn lại, Save/Undo/Redo/Play Test có vị trí cố định, tool đang active nhìn ra ngay, status bar luôn lộ trạng thái document, inspector scroll độc lập, responsive quanh ~640 px.

Hierarchy: **high** = canvas, tool đang active, Save, Play Test, validation chặn; **normal** = inspector entity đang chọn, field hay dùng; **low** = raw ID, metadata advanced, action hiếm. Button/icon/màu theo visual system D0.

Tests: workspace state persistence, pane size persistence, số callback sau domain reload ổn định, vị trí/state của global action test được ở mức khả thi.
Bắt buộc chạy `editor-ux-review` sau khi implement.
Screenshot: default width, narrow width, entity đang chọn, document dirty, action bị disable kèm lý do.

Shell chạy được nhưng nhìn như debug panel = **chưa DONE**.

---

## D3 — Vertical slice edit đầu tiên (StaticObstacle)

Mục tiêu: GD author trọn một StaticObstacle qua editor production.

Flow: tạo → chọn → move → sửa point → insert/remove point → sửa thickness/style nếu có → undo/redo → delete → validation → preview đúng runtime.

Rules: selection theo stable ID; dùng chung board-space mapper; dùng chung obstacle rasterizer; không có hình học xấp xỉ riêng cho editor; một gesture kéo = một Undo entry; preview nặng không rebuild theo từng pixel chuột.

**Thao tác canvas (bổ sung từ review):**
1. Chọn layout trước: panel Levels có ô chọn `LayoutDefinition` (thumbnail + tên). Đổi layout → canvas đổi nền, mask đổi theo, entity giữ nguyên nhưng **re-validate vị trí ngay**.
2. Snap theo cell của sim (`cellSize`), giữ `Shift` để bỏ snap.

Tests: obstacle create/delete, point insert/remove, move giữ đúng selection, một gesture một undo, parity rasterization editor/runtime, add/remove/sort không chọn nhầm entity.
UX review: tool active state, handle nhìn rõ ở mọi zoom, action huỷ diệt tách riêng, control hay dùng nằm trên metadata advanced, tương tác canvas không đánh nhau với pan/zoom.
Evidence: screenshot trước/sau, walkthrough undo, bằng chứng parity mask.

---

## D4 — Source + Cup + Validation

Mục tiêu: GD author đủ entity cho một level SE-001 thật.

Source: đặt/di chuyển, color id, amount, size/profile override ở mức được phép.
Cup: đặt/di chuyển, size, color id chấp nhận, các field bắt buộc.

**Thêm entity và kéo thả (bổ sung từ review):**
- Nút `Add Source` / `Add Cup` spawn entity vào **giữa vùng canvas đang nhìn**, ở trạng thái selected, sẵn sàng kéo. Không dùng modal nhập toạ độ.
- Kéo bằng chuột trên canvas; một gesture = một Undo.
- **Check vị trí hợp lệ ngay trong lúc kéo**, mỗi frame kéo, dùng mask đã bake (không rasterize lại): nằm trong board; không chồng static mask; không chồng entity khác (kể cả thân jar và miệng cup); miệng cup không bị bịt; dưới vòi source phải có khoảng trống.
- Hợp lệ = ghost bình thường; không hợp lệ = viền đỏ + badge lý do ngắn ("miệng cup bị chặn"). Thả ở vị trí không hợp lệ → **trả về vị trí hợp lệ cuối cùng**, không tạo data sai.
- Inspector vẫn cho nhập toạ độ chính xác, nhưng là đường phụ.

Validation thành feature production. Mức: blocking / warning / info. Issue phải hiện ở: canvas, inspector, validation panel, Save, Play Test/load path.
Click vào issue → đổi mode nếu cần → select theo stable ID → frame tới target → focus đúng field khi khả thi.
Message format: **WHAT sai · WHERE · HOW to fix**. Không dùng tên class / serialized-property trong message cho GD.

Tests: unique ID, color id hợp lệ, bounds, static overlap, supply ≥ required, geometry không hợp lệ, click-to-focus mapping, sửa xong thì issue biến mất, issue blocking disable đúng Save/Play Test, `Drag_OneGesture_OneUndo`, `Drag_InvalidDrop_RevertsToLastValid`, `Drag_UsesBakedMask_NoRasterize`, `Overlap_SourceCup_Detected`, `CupMouthBlocked_Detected`, `ChangeLayout_RevalidatesEntities`.
UX review: lỗi đủ nổi, warning khác blocker rõ ràng, field kỹ thuật thô bị hạ nhấn, field Source/Cup hay dùng lên trước, metadata hiếm nằm trong foldout Advanced.

---

## D5 — Complete GD authoring workflow

Mục tiêu: GD dựng một level hợp lệ từ trắng, không đụng JSON.

Hỗ trợ: board, Source, Cup, StaticObstacle, select/move, delete, duplicate, Undo/Redo, search/filter nếu hữu ích, validation, Save. Tính năng tiện lợi chỉ được thêm khi core workflow đã đủ.

Walkthrough bắt buộc: trắng → cấu hình board → thêm source → thêm cup → thêm obstacle → chỉnh vị trí/tune → sửa hết issue → Save.
Đo friction: số modal thừa, số lần phải đổi pane, disabled state không rõ lý do, field đòi kiến thức dev.
`editor-ux-review` chạy 3 pass: first use không tài liệu; core workflow; stress/recovery.
Format issue: `Severity | Task | Friction | Expected | Evidence | Fix direction`. Không nhận issue kiểu "nhìn xấu" nếu không kèm hậu quả usability quan sát được.

---

## D6 — Preview + unsaved Play Test

Mục tiêu: GD test **document đang mở, chưa save**, qua đúng đường runtime thật.

```
Editor Document → serialize temp JSON → one-shot session override → Play Mode
→ LevelManager tiêu thụ override ĐÚNG MỘT LẦN → LevelSpawner thường → GameplayManager/simulation
```

Không được: tạo loader runtime thứ hai; ghi temp data vào file level trong Resources; bỏ qua validation/spawner thường; để sót override sau khi Play.
Preview dùng chung mapper, validator, rasterizer khi trả lời cùng một câu hỏi. Không có xấp xỉ trùng lặp.

Tests: `UnsavedPlayTest_LoadsCurrentDocument`, `UnsavedPlayTest_ConsumesOverrideOnce`, `UnsavedPlayTest_NormalLoadRestoredAfterward`, `UnsavedPlayTest_BlockingValidationPreventsPlay`, `EditorRuntime_ObstaclePreviewParity`, editor context khôi phục sau khi thoát Play.
Evidence: sửa mà không Save → Play Test → screenshot runtime chứng minh bản chưa save được load → quay lại Editor với document vẫn dirty.
UX: Play Test là primary action; nếu disable phải thấy ngay lý do chính xác.

---

## D7 — Progression / Save + UX production pass

Mục tiêu: tool và progression hành xử như phần mềm production, không phải tiện ích dev.

Progression: sequence dùng stable level identity; progression save do production sở hữu; editor inspect/chọn được level đã author mà không hardcode; Level Editor không âm thầm ghi đè progression. Không nhân bản logic sequence của Phase C.

**Nơi tune global (bổ sung từ review):**
- Một cửa sổ/asset tuning duy nhất, mở được từ Level Editor, gom: sand feel, source, cup, ink, juice.
- Sửa xong preview và Play Test dùng ngay, không cần restart Editor.
- Cảnh báo khi sửa field ảnh hưởng toàn bộ level đã balance — đặc biệt `cellSize`, vì nó **làm hỏng toàn bộ mask đã bake**, phải Rebake All.
- **Không** override feel theo level. Level JSON chỉ giữ level data (`logicalAmount`, `requiredAmount`, `drawInkBudget`…). Nếu sau này cần override, mở bằng whitelist tường minh.

Final UX/visual review: button sizing (primary > secondary, click target đủ lớn, icon dày đặc chỉ cho thao tác quen và lặp); màu (semantic nhất quán, selection/active tool rõ, blocker/warning/success phân biệt được, không rainbow UI); icon (vocabulary nhất quán, action quan trọng icon + text, tool mode nhận ra nhanh, tooltip gồm action + shortcut); information hierarchy (field hay dùng trên field hiếm, data advanced collapsible, canvas vẫn chiếm ưu thế, entity đang chọn rõ, issue blocking nổi hơn metadata).
States phân biệt được: hover, active, selected, disabled, dirty, valid, warning, blocking.
Empty states phải hướng dẫn bước tiếp: chưa chọn level, chưa chọn entity, không có issue, level rỗng.
Screenshot review ở: normal width, ~640 px, inspector nội dung dài, validation chặn, chọn Source, chọn Cup, chọn Obstacle.

---

## D8 — GD closure

Mục tiêu: chứng minh tool dùng được cho người dùng thật.

Walkthrough end-to-end, làm **bằng chính tool**, không sửa JSON tay, không lách bằng Inspector:
mở editor → new level → cấu hình board → thêm Source → thêm Cup → thêm obstacle → tạo ra ít nhất một lỗi validation → dùng validation để điều hướng và sửa → Save → sửa tiếp mà không save → unsaved Play Test → quay lại Editor → Save bản cuối → load file cuối qua đường game thường.

Chạy `skills/level-editor/refs/checklist.md`, mỗi mục `PASS / FAIL / N/A / PENDING`. Không bao giờ suy đoán PASS.
Sản phẩm kèm theo: GD guide ngắn có screenshot; mục "tool này **không** làm được gì"; bảng shortcut/tool; known limitations.
Performance: level hợp lệ nhỏ nhất; level production lớn nhất dự kiến; độ mượt tương tác canvas; thời gian cập nhật validation; không AssetDatabase scan / allocation trong repaint hot path; không tích tụ callback qua reopen/domain reload.
Technical gates: compile sạch, console sạch, tests PASS, style gate PASS, `git diff --check` PASS, không sót Play Test override, không dirty temp asset, docs + roadmap cập nhật, diff cuối được review độc lập.

---

## Anti-shortcuts

Phase D FAIL nếu: editor ghi schema khác runtime đọc; editor tự implement rasterization riêng; tool chỉ dùng được nếu biết kiến thức dev; GD vẫn phải sửa JSON tay; có Save/Open nhưng không test round-trip; mọi edit đều gọi `ReloadDocument`; selection dùng array index; validation chỉ tồn tại ở một panel; unsaved Play Test thực ra test file đã save; lỗi quan trọng chỉ thấy ở console; toolbar là một hàng phẳng các control ngang nhau; primary/secondary/danger không phân biệt được; stable ID thô lấn át workflow; icon không có label/tooltip hiểu được; layout vỡ ở cửa sổ hẹp; agent tự review UX của chính mình chỉ bằng đọc code; nói có screenshot nhưng không lưu; walkthrough cuối không thực sự chạy.

---

## DONE gate

```
[ ] Skill bootstrap xong.
[ ] Level Editor recipes dùng được.
[ ] editor-ux-review tồn tại và đã được dùng.

[ ] Layout bake pipeline: SVG → prefab mesh + mask asset.
[ ] Runtime không rasterize mask khi load level (đếm call = 0).
[ ] Mask bake stale (cellSize/hash lệch) bị chặn với lỗi rõ ràng.
[ ] Level JSON chỉ chứa layoutId, migration level cũ chạy được.
[ ] Số đo thời gian load layout trước/sau có trong evidence.

[ ] Document/ViewState/DerivedState tách bạch, đã verify.
[ ] New/Open/Save/Save As/version/round-trip verified.
[ ] Workspace UI Toolkit production hoàn chỉnh.
[ ] StaticObstacle full vertical edit flow xong.
[ ] Source authoring xong.
[ ] Cup authoring xong.
[ ] Add Source/Cup spawn vào canvas, drag to move, check vị trí hợp lệ khi kéo.
[ ] Thả ở vị trí không hợp lệ không tạo được data sai.
[ ] Production validation xong.
[ ] Parity hình học runtime/editor verified.
[ ] Walkthrough trắng → level hoàn chỉnh xong.
[ ] Unsaved Play Test dùng đúng pipeline runtime.
[ ] Progression/save production integration xong.
[ ] Cửa sổ tuning global mở được từ Level Editor; cảnh báo khi đổi field cần rebake.

[ ] visual hierarchy review PASS.
[ ] button/color/icon/state hierarchy review PASS.
[ ] ~640px responsive review PASS.
[ ] first-use review PASS.
[ ] error-recovery review PASS.

[ ] level-editor delivery checklist xong.
[ ] GD guide tồn tại.
[ ] known limitations đã ghi.

[ ] performance với level lớn nhất dự kiến chấp nhận được.
[ ] lifecycle/domain reload sạch.
[ ] compile sạch.
[ ] console sạch.
[ ] tests PASS.
[ ] style gate PASS.
[ ] git diff --check PASS.

[ ] screenshot/evidence cuối tồn tại.
[ ] ROADMAP cập nhật.
[ ] Docs Phase D cập nhật.
```

Chỉ khi đó: **Phase D = DONE / CLOSURE PASS**. Không bắt đầu Phase E trước gate này.

---

## Rủi ro đã biết

- **`cellSize` trở nên rất đắt để đổi** sau khi bake mask. Phải ghi trong docs và chặn bằng cảnh báo ở cửa sổ tuning.
- **Migration level cũ**: bỏ contour khỏi JSON thì phải bake ngược contour hiện có thành layout asset. Script migration một lần, có test.
- **Hai nguồn hình học** có thể lén quay lại nếu ai đó thêm contour vào JSON. Validator phải từ chối.
- **`ToolSelectLevelUI`** đang dùng `_levelIdFormat = "level_{0:00}"`, không khớp id `phase_c_level_02`. Phải sửa format hoặc dùng danh sách id tường minh, nếu không GD chọn level sẽ không load được.
- Phase D đòi rất nhiều screenshot/evidence. Cần người thao tác thật trong Unity; agent không tự claim PASS.

---

## Report format

Mỗi packet: `Item | Claimed | Verified by | Evidence`. Dòng cuối bắt buộc: `DONE` hoặc `BLOCKED — <blocker cụ thể + evidence>`.
