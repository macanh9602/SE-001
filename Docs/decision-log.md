# Decision Log — SE-001

## D-009 — Manual acceptance of V1 and D-A closure

Status: Accepted — 2026-09-23
Stories: `handoff/phase-V1/PHASE-V1-JAR-VISUAL.md`, `handoff/phase-D/PHASE-D-A-LAYOUT-BAKE.md`

### Chốt

- Ducan confirmed the V1 and D-A workflows are manually acceptable in the Unity Editor.
- V1 and D-A are marked `DONE — MANUAL ACCEPTANCE`.
- Missing screenshot UX packets and numeric profiler evidence are explicit accepted deviations; they are not claimed as executed evidence.
- Existing full-suite baseline failures remain recorded and are accepted as non-blocking for these scoped visual/editor packets.

### Hệ quả

The later device/performance gate may still collect authoritative draw-call and GC numbers. It does not reopen these closures unless a new regression is found.

## D-008 — Phase V1 jar visual contract

Status: Accepted — 2026-09-23
Story: `handoff/phase-V1/PHASE-V1-JAR-VISUAL.md`

### Chốt

- Source/Cup presentation uses authored shared quad meshes, shared materials, and reusable MPBs; runtime does not generate per-instance meshes or materials.
- `JarVisualProfile` is the single source for measured art composition, masks, Source fill LUT, and 9-slice constants.
- `CupProfile.taper = 0` is the locked art-aligned geometry contract. `fillLine` remains `0.85` because it participates in gameplay accounting; the visual FillLine is removed only.
- Color ids 1–7 use 14 generated cap/mouth materials referenced by `ColorProfile`; the old visual materials remain as non-destructive deletion candidates.

### Hệ quả

Editor preview and runtime presentation share the same measured profile and tint math. The visual change does not authorize recalibration of the existing gameplay simulation or level data; that remains a separate decision/story.

> Append-only. Decision mới đặt ở trên cùng. Project-level decision ghi ở đây; micro-decision ghi trong story implementation notes.

## D-007 — Baked layout asset is the runtime geometry source

Status: Accepted — 2026-09-23
Story: `handoff/phase-D/PHASE-D-EXECUTE-D0-D05.md`

### Chốt

- SVG contours are imported in Editor and baked through the existing `LayoutRasterizer` into a
  bit-packed `LayoutMaskAsset` plus a prefab containing baked mesh references.
- Schema 3 level JSON stores `layoutId` and entity data; it does not store wall contours or static
  obstacle contours. Existing schema 2 levels migrate once through the Phase D menu command.
- Runtime loads the baked mask and blocks on cell-size or data-length mismatch; it never silently
  rasterizes a stale layout.

### Hệ quả

Runtime load no longer pays contour rasterization or layout mesh generation. Rebake is an Editor
operation and preserves the `LayoutDefinition` asset identity. Layout prefabs contain presentation
only; Source/Cup and simulation state remain level-owned runtime composition.

## D-006 — Source body size có thể author theo từng level

Status: Accepted — 2026-09-22
Story: Phase C level 02 layout/video pass

### Chốt

- `SourceData.size` là optional width/height theo board-space cho từng source.
- `(0,0)` resolve về `SourceProfile.bodySize`, giữ backward compatibility cho JSON cũ mà không bump `schemaVersion`.
- Resolved size dùng chung cho presentation và tap hit-area; `SourceData.position` vẫn là nozzle/emission position.

### Hệ quả

Level có thể match scale từ layout reference mà không đổi source ở level khác. Resolve chỉ xảy ra khi load level; không thêm allocation hoặc work trong gameplay loop.

## D-005 — Architecture Blueprint supersedes the former execution roadmap

Status: Accepted — 2026-09-22

### Chốt

- `SE001-ARCHITECTURE-BLUEPRINT.md` là project architecture source-of-truth của SE-001.
- Story 001–014 cũ bị xóa khỏi active handoff; Git history là historical record của chúng.
- Story 000A cũ được archive dưới `handoff/archive/` và không được execute.
- Roadmap mới chỉ materialize story kế tiếp sau khi gate trước đã PASS. Story executable đầu tiên là
  `handoff/story-001-architecture-foundation-alignment/story.md`.

### Hệ quả

- D-004 và mọi reference Story 001–014 trước decision này chỉ còn giá trị lịch sử; device gate mới
  thuộc phase I và chỉ được materialize sau các architecture gate trước.
- Product scope sand, Source, Cup, obstacle, draw path, editor, progression và performance không bị
  loại bỏ; chỉ execution decomposition bị thay thế.
- `LevelManager` giữ lifecycle/selection, `LevelSpawner` sở hữu composition + spawn/unload,
  `GameplayManager` sở hữu per-level gameplay orchestration.

## D-004 — Device gates deferred to Story 014

Status: Accepted — 2026-09-22

### Chốt

- Mọi acceptance cần đo trên device thật trong Story 001–013 (p95, Redmi 9A, device smoke, video) được thay bằng Editor proxy và đánh dấu `DEFERRED-DEVICE → 014`; không chặn Story DONE.
- Editor proxy dùng Burst-enabled Profiler để ghi sim ms avg/p95, xác nhận GC Alloc = `0 B/frame` sau warm-up bằng ProfilerRecorder hoặc PlayMode test, và đo draw call bằng Frame Debugger/Stats.
- Visual/feel acceptance (khớp visualizer, fine powder) ghi `PENDING FEEL REVIEW (user)` và không chặn Story DONE.

### Phạm vi

Áp dụng cho Story 001–013. Story 014 là gate tập trung cho device performance, quality scaling và device evidence.

## D-003 — Giữ `Assets/_Core/4_Scripts` làm canonical script structure

Status: Accepted — 2026-09-22
Story: `handoff/archive/story-000A-core-runtime-foundation/story.md` (historical, superseded)
Supersedes: D-001 code-root, assembly-placement và legacy-folder clauses

### Vấn đề

Story 000/000A đã hiểu “tách code mới khỏi legacy semantics” thành tạo cây `Assets/_Core/Scripts/SE001`, làm trùng framework và bỏ qua flow quen thuộc đã có trong `4_Scripts`.

### Chốt

- `Assets/_Core/4_Scripts` là canonical script structure của SE-001.
- Audit existing code theo từng file: generic code reuse trực tiếp sau khi đổi về namespace/contract SE001; CH013 gameplay-specific code replace hoặc adapt tại đúng owner.
- Không giữ class `SE001LevelManager` song song; lifecycle foundation nằm ở `System/Management/LevelManager.cs`.
- Không tạo `Assets/_Core/Scripts` cho code production mới. Nếu cần asmdef, đặt theo module trong canonical tree và chỉ khi dependency thực tế yêu cầu.

### Loại phương án nào, vì sao

Loại cây framework song song vì tăng coupling chuyển tiếp, tạo hai nơi tìm code và làm flow project khác cấu trúc quen thuộc mà không đem lại gameplay value.

### Đánh đổi đã chấp nhận

Một số file CH013 cùng nằm trong canonical tree trong thời gian migration; vì vậy mọi story phải audit theo file, không được coi toàn folder là reusable hoặc legacy.

### Hệ quả

Story 000/000A và canonical docs dùng `Assets/_Core/4_Scripts`; code foundation đã port về `System/Management` và Commons generic về `SE001.Commons`.

### Xem lại khi

Một module thật sự cần assembly isolation vì compile dependency, Editor/runtime separation hoặc build-time boundary; khi đó thêm asmdef tại chỗ, không dựng root song song.

## D-002 — Dùng GameScene duy nhất cho bootstrap và gameplay

Status: Accepted — 2026-09-22
Story: `handoff/story-000-project-contract-bootstrap/story.md`
Supersedes: D-001 scene-flow clause

### Vấn đề

User chốt dùng `GameScene` thay vì tách `MainScene` bootstrap và `GameScene` gameplay.

### Chốt

`GameScene` là scene duy nhất trong Build Settings và sở hữu cả bootstrap entry cùng gameplay lifecycle.

### Loại phương án nào, vì sao

Loại bỏ scene bootstrap riêng để giữ flow hiện tại đơn giản và không tạo thêm scene ownership không cần thiết.

### Đánh đổi đã chấp nhận

Bootstrap code và gameplay code cùng scene boundary; các layer vẫn phải giữ ownership tách biệt trong component/module contract.

### Hệ quả

Các story sau tham chiếu `GameScene` cho bootstrap/load level; không tạo hoặc yêu cầu `MainScene`.

### Xem lại khi

Có yêu cầu meta flow hoặc scene loading độc lập làm thay đổi project-level scene ownership.

## D-004 — Phase C finish contract: authored profiles and cup geometry

Status: Accepted — 2026-09-22
Story: `handoff/PHASE-C-FINISH-PLAN.md`

### Chốt

- Cup giữ sand thật trong sink; `Collected` là số hạt đúng màu đang nằm trong sink và `Required` tính theo hình học dưới `fillLine`.
- Cup taper và effective wall thickness được dùng chung cho mask/domain; position là giữa đáy ngoài, size là kích thước ngoài.
- Runtime bắt buộc dùng authored profiles trong `Resources/Profiles`; thiếu profile là lỗi rõ ràng, không `CreateInstance` fallback.
- Palette là source cho material ID validation; visual palette rendering vẫn là phần closure sau.

### Hệ quả

LevelSpawner, GameplayManager và GameplayInputController nhận tune values từ profile; JSON level chỉ giữ level-authored data. Profile assets được tạo bằng menu `SE001/Phase C/Create default profiles`.

## D-001 — SE-001 dùng Simulation-driven 2D và feature-scoped contract

Status: Accepted — 2026-09-22
Story: `handoff/story-000-project-contract-bootstrap/story.md`

### Vấn đề

Story sau cần identity, scene ownership, physics authority và data source of truth nhất quán trước khi thêm sand runtime.

### Chốt

- Internal code: `SE-001`.
- Namespace root: `SE001`, không dùng class prefix.
- Unity `6000.0.70f1`, URP, mobile.
- Code mới ban đầu được định hướng feature-scoped dưới `Assets/_Core/Scripts/SE001`; clause này đã bị D-003 supersede.
- `GameScene` là scene duy nhất cho bootstrap và gameplay; quyết định này được supersede chi tiết tại D-002.
- Physics authority là Simulation-driven, dimension 2D, determinism tolerance-based.
- Level JSON tại `Assets/_Core/Resources/Levels/` là source of truth; generated/runtime state không serialize.
- Low-end target là Redmi 9A; Creative mode để future story định nghĩa.

### Loại phương án nào, vì sao

- Loại bỏ Rigidbody-authoritative 2D/3D vì sand grid là gameplay authority.
- Việc loại `4_Scripts` khỏi source of truth đã bị D-003 supersede; guardrail còn hiệu lực là không resurrect CH013 gameplay semantics mù quáng.
- Loại bỏ việc serialize scene hierarchy/generated grid vì không đảm bảo authoring portability.

### Đánh đổi đã chấp nhận

Simulation và assembly boundaries cần thêm contract/bridge, nhưng giảm coupling và cho phép đo performance/quality riêng trên mobile.

### Hệ quả

Story 001 trở đi phải tuân thủ `SE001` namespace, feature assembly, JSON source-of-truth và Simulation semantic signals.

### Xem lại khi

Physics authority hoặc serialization contract thay đổi ở story cấp project; khi đó phải tạo decision mới và supersede entry này.

## 2026-09-23 — Đóng Phase C: freeze sand, descope juice cup/stroke

### Bối cảnh

Sand feel đã được tune rồi revert hai lần trong ngày. Panel Win không hiện do prefab root-Canvas bị instantiate làm con (chi tiết: `handoff/phase-C-complete-playable-core/evidence/win-panel-invisible-rootcause.md`).

### Quyết định

1. Sand simulation trả về hành vi `6dee3cb` và **freeze** trong Phase C. Mọi thay đổi feel chuyển sang phase sau, kèm rebalance level.
2. Stream density lấy từ `PhaseCSourceProfile`; level JSON không override `emissionRate`/`streamWidth`.
3. Juice cup (`cupPunchDuration`) và stroke-grow (`strokeExtrudeDuration`) **descope** khỏi Phase C; giữ field trong `JuiceProfile` nhưng không dùng.
4. Panel prefab kiểu canvas được chuẩn hoá RectTransform ở `UIPanels.NormalizePanelRect` thay vì sửa từng prefab.

### Đánh đổi đã chấp nhận

Phase C đóng với ít juice hơn dự kiến; đổi lại lấy được thời gian cho Phase D và tránh rebalance lại 3 level.

### Hệ quả

Phase D bắt đầu từ D0 và D0.5 (pipeline SVG → prefab mesh + mask). Các mục PENDING MANUAL của C4 phải chạy trước khi coi là CLOSURE PASS đầy đủ.

### Xem lại khi

GD yêu cầu feel khác cho sand, hoặc khi thêm juice trở lại sau Phase D.

## 2026-09-23 — Mở lại sand: stream rơi thẳng + render dòng (B+)

### Bối cảnh

Sand rời obstacle bị bắn xéo và tản thành bụi (video repro: 65% hạt đi lệch khỏi dòng chính). Với rule wrong-cup = thua ngay, spray gây thua oan. Visualizer: `Docs/visualizers/sand-stream-lab.html`. Spec: `handoff/sand-stream-fix/SAND-STREAM-FIX.md`.

### Quyết định

1. Supersede điểm 1 của entry "Đóng Phase C: freeze sand": mở lại sand cho fix này.
2. Sim: bỏ lateral drift khi airborne, `airDrag` 0.5. Không thêm rule bám theo hạt đang rơi (đã đo: gây kẹt source, chỉ đổ được 44–77%).
3. Visual: `SandFieldVisual` vẽ vệt + nối khe + nong ±1 cell cho hạt airborne, phủ texture hạt trôi theo dòng (không để thanh màu phẳng). Chỉ là hình, không vào collision/cup count.
4. Không đổi `maxFallCellsPerStep` (giữ pacing).
5. Sand texture dùng **Point filter** (`SandField.prefab` → `softRender: 0`) thay vì Bilinear: 1 texel/cell phóng ~5× trên màn hình, Bilinear làm dòng và hạt bị mờ. Pile cũng thành pixel khối, giống visualizer. Không đổi render scale 0.8 của Mobile_RPAsset.

### Đánh đổi đã chấp nhận

Hình dòng rộng hơn collision ±1 cell (0.06 unit). Tốc độ đổ của source giảm ~3–15% vì không còn splash ngang ở miệng jar. 3 level Phase C phải re-check; level nào đổi kết quả do GD/Dev chốt rebalance.

### Xem lại khi

GD thấy dòng quá dày/mảnh trên device, hoặc level mới cần sand bắn ngang có chủ đích.
## D-010 — Level Editor naming and progression on Save

Status: Accepted — 2026-09-23

### Bối cảnh

GD yêu cầu tên level dạng `Level_01` theo thứ tự progression; Level Editor trước đó tạo `new_level` và không cập nhật `PhaseCLevelSequence` khi Save.

### Quyết định

- Level Editor hiển thị `Level_XX` theo thứ tự trong `PhaseCLevelSequence`. Giữ nguyên `levelId` của level cũ để không phá reference và file đã authored.
- New Level lấy ID `Level_XX` kế tiếp. Save JSON thành công thì thêm ID mới vào `PhaseCLevelSequence`; filename phải khớp `levelId` để runtime load được.
- Entity và Layout được nhóm, tìm kiếm và phân trang trong panel; stable ID nằm ở Technical ID, không là tên chính GD nhìn thấy.

### Phương án loại bỏ và trade-off

Không để GD tự nhập tên hoặc tự sửa progression sau Save: thao tác dễ lệch file JSON với runtime sequence. Thêm ghi asset khi Save trong Editor, không thêm chi phí CPU/GPU/GC/draw call/memory vào gameplay loop.

### Xem lại khi

GD cần thứ tự level khác thứ tự Save hoặc cần đổi tên/đổi vị trí level đã phát hành.

## D-011 — Rotating Cross obstacle and global jar settings

Status: Accepted — 2026-09-23

GD chose a continuously rotating Cross obstacle with two 9-slice bars, shared static-obstacle material, per-level pivot/scale/bar length/initial angle/signed speed, and sand push sweep. The simulation uses a separate moving mask so player strokes remain intact; swept grains relocate to nearby free cells with no loss. Cross bar width and bounded push radius live in `RotatingObstacleProfile`.

Source body size, emission rate and stream width live only in `SourceProfile`; Cup outside size lives only in `CupProfile`, initially 2 × 1.5 board units. Schema 4 omits these jar values and adds Cross entities. Existing schema-3 levels load with global Profile values and are upgraded to schema 4 when saved from the Level Editor. The few legacy Cup shapes that differed therefore become the chosen global size.

GD revised the global Cup outside size to **2 × 2 board units** on 2026-09-23. `CupProfile` is the authoritative setting; Editor preview and runtime both read it.

Mobile cost: one moving bool mask and a scratch mask per level, bounded Cross footprint scans at the fixed sand step, and two SpriteRenderer draws per Cross. No per-step grain allocation or runtime material creation. Device profiling remains required before production closure.

## D-012 - Unsaved Play Test uses the normal runtime load path

Status: Accepted - 2026-09-24

The Level Editor passes the current document as a one-shot editor session override. `LevelManager`, `LevelSpawner` and `LevelDataLoader` continue to own normal runtime loading and validation; no temporary JSON is written to `Resources`. The override is consumed once, cleared on return to Edit Mode, and the editor document remains dirty/context-preserving.

Trade-off: this adds a small editor-only bridge and session state, but avoids a second preview runtime, keeps gameplay behavior aligned with production, and adds no runtime CPU, GPU, GC, draw-call or memory cost when the override is absent.

## D-013 — Per-level Source and Bowl tuning

Status: Accepted — 2026-09-24; supersedes the Source tuning and Bowl sizing ownership in D-011.

GD chose one uniform Source Scale and one uniform Bowl Scale shared by their respective entities in each level. Source scale affects art and tap area around the authored nozzle; Bowl scale affects art, sink geometry and capacity around the authored bottom centre. Source Emission Rate and Stream Width are independent per-level controls with Level Editor guidance. Changing Bowl scale does not change grains per logical unit, so the same Required Amount appears less full in a larger Bowl.

Schema 5 stores all four values in level JSON. Schema 3/4 remain runtime-readable using scale 1 and SourceProfile stream values. The Level Editor resolves those values when opening old files and writes schema 5 on save. Profile assets retain base art size and legacy stream defaults; level JSON owns these four values for schema 5. Resolution happens at level load or editor rebuild, with no per-frame allocation or extra draw calls. Large Bowl or stream settings still require Redmi 9A profiling.

2026-09-24 follow-up: a 12-unit Source/Cup level at Bowl Scale 1.5 overflowed before reaching the target even though the collision-cell capacity exceeded Required. Authoring and runtime now reject targets above `Capacity × fullFillFraction`, and `Level_01` uses Bowl Scale 1.7 after deterministic replay. This guard does not replace a level Play Test. Bowl-specific sand leveling remains a separate feel/gameplay decision.
