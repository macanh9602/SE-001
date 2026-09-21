# Story 000A — Core Runtime Foundation theo flow LevelManager quen thuộc

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Canonical architecture đã chốt nhưng runtime foundation chưa tồn tại đúng chỗ | `Docs/project-context.md`, `Docs/runtime-architecture.md`; foundation tạm từng được generate dưới `Assets/_Core/Scripts/SE001/` | Story Sand phải tự invent lifecycle/load-unload hoặc phụ thuộc framework tree song song nếu không port |
| `Assets/_Core/4_Scripts` có flow cũ quen thuộc: `LevelManager`, `GameplayManager`, `GameplayInputController`, `LevelRuntimeState`, Factory/Common/HUD | Các script tồn tại trong workspace; phần gameplay cũ chủ yếu đang comment hoặc mang namespace `CH013` | Có thể tái sử dụng pattern tốt, nhưng copy/uncomment mù sẽ kéo semantics project cũ sang SE-001 |
| Story 000 chưa hoàn tất post-change Unity compile evidence do MCP mất kết nối | `handoff/story-000-project-contract-bootstrap/implementation-notes.html` | Cần baseline guard trước khi tạo foundation mới để không nhầm lỗi cũ với lỗi Story 000A |

## 1. Kết quả mong đợi

> Sau story này, `GameScene` có một core runtime lifecycle ổn định theo flow quen thuộc: **LevelManager load → tạo LevelContext/RuntimeState → mở gameplay gate → unload/reload sạch**, để Story 001+ chỉ cắm feature vào mà không tự quản lifecycle.

Observable smoke flow:

```text
GameScene
  ↓
Bootstrap
  ↓
LevelManager
  ↓
Create LevelContext
  ├─ level lifetime / CancellationToken
  ├─ per-level RuntimeState
  ├─ LevelRoot
  │   ├─ BoardRoot
  │   ├─ SimulationRoot
  │   └─ VisualRoot
  └─ lifecycle participants
  ↓
OnLevelSpawned / Input-ready gate

Reload / Unload
  ↓
unbind input → unsubscribe → cancel level token → cleanup participants
  → dispose RuntimeState → destroy empty LevelRoot
```

## 2. Ranh giới

### Làm

- Audit `Assets/_Core/4_Scripts` trước khi viết code mới và phân loại từng module liên quan: **REUSE DIRECT / ADAPT-PORT / REFERENCE ONLY / IGNORE**.
- Port runtime foundation vào `Assets/_Core/4_Scripts/System/Management/`, tuân namespace `SE001`; không giữ framework tree song song.
- Dựng `LevelManager`/level lifecycle coordinator theo flow quen thuộc của project cũ nhưng không mang gameplay semantics cũ.
- Dựng per-level `LevelContext` + `LevelRuntimeState` foundation và explicit level lifetime/cancellation.
- Dựng root ownership cho `LevelRoot`, `BoardRoot`, `SimulationRoot`, `VisualRoot`.
- Có lifecycle hook đủ để Story 001–010 đăng ký simulation/domain/visual về sau mà không sửa lại ownership nền.
- Có bind/unbind gate cho input/hud lifecycle; chưa implement input gameplay hoặc win/lose.
- Có reload/unload smoke flow chạy được không cần sand.
- Cập nhật `Docs/runtime-architecture.md` nếu tên owner/class thực tế khác layer map hiện tại.
- Chỉ promote legacy generic module thành dependency thật nếu audit chứng minh nó không mang `CH013`/gameplay semantics; project-level promotion phải ghi `Docs/decision-log.md`.

### KHÔNG làm

- Không implement sand simulation, Powder profile, renderer hoặc ParticleSystem sand.
- Không implement draw path, source, cup, collection, win/lose.
- Không implement level editor hoặc progression/Next Level ordering.
- Không SDK/ads/analytics/IAP.
- Không final art polish.
- Không rewrite toàn bộ `Assets/_Core/4_Scripts`; chỉ thay các file đã audit trong scope foundation.
- Không uncomment hàng loạt legacy code rồi search/replace namespace.
- Không tạo dependency từ code mới `SE001.*` sang namespace `CH013.*`.
- Không tự thay schema/gameplay contract của Story 004+.

## 3. Context bắt buộc đọc

Theo thứ tự:

1. `AGENTS.md`
2. `Docs/project-context.md`
3. `standards/system-design.md` — đặc biệt §1, §2, §5, §6, §7
4. `Docs/runtime-architecture.md`
5. `Docs/data-model.md`
6. `handoff/story-000-project-contract-bootstrap/implementation-notes.html`
7. `handoff/ROADMAP.md`

### Legacy/Core audit scope — đọc có mục đích, không scan toàn repo

- `Assets/_Core/4_Scripts/System/Management/LevelManager.cs`
- `Assets/_Core/4_Scripts/System/Management/GameplayManager.cs`
- `Assets/_Core/4_Scripts/System/Management/GameplayInputController.cs`
- `Assets/_Core/4_Scripts/System/Management/LevelRuntimeState.cs`
- `Assets/_Core/4_Scripts/Commons/ICreateParameters.cs`
- `Assets/_Core/4_Scripts/Commons/IRuntimeCreatable.cs`
- `Assets/_Core/4_Scripts/Commons/IFactory.cs`
- `Assets/_Core/4_Scripts/Commons/IPendingCleanup.cs`
- `Assets/_Core/4_Scripts/Creation/DemoFactory.cs`
- `Assets/_Core/4_Scripts/HUD/HUDSystem.cs`
- `Assets/_Core/4_Scripts/HUD/Panel.cs`
- `Assets/_Core/4_Scripts/HUD/UIPanels.cs`
- `Assets/_Core/4_Scripts/Data/EffectsProfile.cs` chỉ để hiểu shared infrastructure; Story này không làm VFX.

Đồng thời inspect:

- nguồn port tạm `Assets/_Core/Scripts/SE001/Bootstrap/` (xóa sau khi port + compile)
- `Assets/_Core/Scenes/GameScene.unity`
- `ProjectSettings/EditorBuildSettings.asset`

## 4. State hiện tại cần giữ nguyên

- Project: `SE-001`, namespace root `SE001`.
- Unity `6000.0.70f1`, URP.
- Canonical code root: `Assets/_Core/4_Scripts`.
- Assembly boundary: giữ project assembly hiện tại; chỉ thêm asmdef tại canonical tree khi dependency thực tế yêu cầu.
- `GameScene` là scene duy nhất cho bootstrap + gameplay.
- Simulation authority sau này: custom Simulation-driven 2D grid.
- Low-end target: Redmi 9A.
- Level source-of-truth sau này: `SE001LevelJson` trong `Assets/_Core/Resources/Levels/`.
- Existing code trong `Assets/_Core/4_Scripts` được audit theo file: generic reuse, game-specific replace/adapt.
- Không có `.zip` ngoài project; Story không được yêu cầu file ngoài workspace.

## 5. Việc cần làm

### Phase A — Baseline guard

1. Đọc Story 000 implementation notes.
2. Nếu Unity MCP/editor đang active: compile + console check **trước khi sửa code**.
3. Nếu baseline có lỗi, ghi `KNOWN BASELINE FAILURE` với evidence; không sửa lỗi ngoài scope trừ khi trực tiếp chặn 000A.
4. Inspect git diff hiện tại; giữ nguyên pre-existing Story 000 changes/untracked assets, không cleanup hộ.

### Phase B — Legacy audit trước implementation

Tạo bảng trong `implementation-notes.html`:

| Module/file | Classification | Reuse cái gì | Không reuse cái gì | Lý do |
|---|---|---|---|---|

Classification:

- **REUSE DIRECT** — generic, compile-stable, không `CH013`, không gameplay semantics; có thể reference trực tiếp nếu assembly boundary sạch.
- **ADAPT-PORT** — flow/pattern tốt nhưng mang project cũ; viết implementation mới trong `SE001`, không tạo dependency tới legacy namespace.
- **REFERENCE ONLY** — chỉ đọc để hiểu flow/API quen thuộc.
- **IGNORE** — không liên quan Story 000A.

Bắt buộc audit tối thiểu: LevelManager, GameplayManager, GameplayInputController, LevelRuntimeState, Commons lifecycle/factory contracts, HUD base.

### Phase C — Core runtime lifecycle

Required behavior, không bắt buộc tên class nội bộ ngoài những public concepts sau:

#### `LevelManager`

Owner của level lifecycle, không chứa gameplay rule.

Public capability tối thiểu:

- load/begin một level context theo stable `levelId` hoặc load request;
- reload current level;
- unload current level;
- expose `IsLoading`/`IsReady` tương đương để input/feature không chạy giữa teardown/spawn;
- không quyết định `NextLevel` sequence — Story 013 sở hữu progression.

`LevelManager` **không**:

- chứa sand movement;
- kiểm win/lose;
- đọc ParticleSystem;
- hardcode level layout;
- tự tìm feature dependency mỗi frame.

#### `LevelContext`

Per-level ownership object/container tối thiểu giữ:

- stable level id;
- level-scoped cancellation/lifetime;
- per-level `LevelRuntimeState`;
- root references: Level / Board / Simulation / Visual;
- registry/list lifecycle participant cần cleanup nếu dùng pattern này.

Context mới tạo mỗi lần load/reload. Context cũ không được reuse sau unload.

#### `LevelRuntimeState`

Story 000A chỉ dựng **foundation**, không chứa cup/source/sand rule.

Tối thiểu:

- per-level, non-static;
- chỗ để Story sau đăng ký/index stable runtime IDs nếu cần;
- explicit dispose/reset;
- không serialize/save.

#### Lifecycle participants

Feature sau này phải có một cách explicit để:

```text
Bind/Initialize with LevelContext
→ become ready
→ CleanupForLevelUnload / Dispose
```

Ưu tiên reuse/adapt `IPendingCleanup`/lifecycle pattern nếu audit thấy phù hợp. Không tạo framework lớn chỉ để phòng xa.

### Phase D — Scene/bootstrap wiring

- `GameScene` boot core runtime đúng một lần.
- Không duplicate `LevelManager` sau reload.
- Bootstrap khởi tạo manager/dependency theo explicit references/profile, không `Find*` trong hot path.
- Input/HUD gate chỉ mở sau level ready; unbind trước cancellation khi unload.
- Story chưa cần HUD gameplay. Existing HUD base có thể giữ nguyên nếu không cần chạm.

### Phase E — Smoke harness

Tạo smoke flow tối thiểu để chứng minh lifecycle mà **không cần Story 004 level schema**:

```text
Start GameScene
→ Begin empty/dev level context "foundation_smoke"
→ verify roots/context/runtime state exist
→ Reload
→ old context invalid/cleaned; new context created
→ Unload
→ no active level context; input gate closed
```

Smoke harness phải là dev/test-only hoặc temporary clearly-owned code; không biến fake level data thành production source-of-truth.

## 6. Layer assignment

| Việc | Layer | Ghi chú |
|---|---|---|
| GameScene boot | Bootstrap | Scene entry only |
| Load/reload/unload orchestration | Level/System | No gameplay rule |
| LevelContext | Runtime lifecycle | Per-level ownership |
| LevelRuntimeState foundation | RuntimeState | Non-static, no feature semantics yet |
| Input enable/disable | Input bridge | Gate only |
| HUD lifecycle readiness | Bridge/HUD | Optional wiring, no result rule |
| Smoke level | Tests/Development | Không thành level schema production |

## 7. Contract mới / giữ ổn định

| Contract | Ai dùng sau này | Rule |
|---|---|---|
| Level lifecycle | Story 001–014 | Feature không tự teardown scene/level |
| LevelContext | Sand/Input/Cup/Source/Visual | Dependency được truyền/bind explicit |
| Per-level lifetime token | Async feature | Mọi async gameplay/presentation về sau phải cancel theo level |
| RuntimeState ownership | Domain/Simulation | Không static, không save |
| Input-ready gate | Story 005+ | Input không mutate state khi load/unload |

## 8. Acceptance criteria

### Audit

- [ ] `implementation-notes.html` có bảng audit legacy với ít nhất các module bắt buộc ở Phase B.
- [ ] Không có new `SE001.*` runtime code reference namespace `CH013.*`.
- [ ] Không uncomment hàng loạt legacy gameplay code.
- [ ] Mọi direct legacy dependency mới (nếu có) đều được chứng minh generic và ghi canonical decision nếu thay policy Story 000.

### Runtime

- [ ] `GameScene` tạo core runtime đúng một lần.
- [ ] Begin/load smoke context tạo `LevelContext`, `LevelRuntimeState`, `LevelRoot`, `BoardRoot`, `SimulationRoot`, `VisualRoot`.
- [ ] Reload tạo context mới; context cũ đã cancel/cleanup/dispose trước khi context mới ready.
- [ ] Unload đóng input gate trước cancel, cleanup participant, dispose runtime state và bỏ active context.
- [ ] Reload/unload không phụ thuộc sand, cup, source, renderer hoặc final level JSON implementation.
- [ ] `LevelManager` không chứa gameplay rule và không quyết định NextLevel ordering.
- [ ] Không `Find*`/raycast/allocation per-frame chỉ để giữ lifecycle.

### Verification

- [ ] Unity compile sạch sau implementation, hoặc only known-baseline failures được phân biệt rõ.
- [ ] Console không có exception mới từ load → reload → unload smoke flow.
- [ ] EditMode/PlayMode test hoặc deterministic smoke verification chứng minh context generation/lifetime không bị old callback ghi vào context mới.
- [ ] Load → reload/unload lặp **10 lần**: active LevelManager count không tăng; active context/root count không tăng dần.
- [ ] `git diff --check` PASS.
- [ ] Final diff không chứa Sand gameplay, SDK, final art hoặc cleanup legacy ngoài scope.

## 9. Performance

Story này chưa đo sand performance. Foundation guardrail:

```text
CPU:              lifecycle work chỉ ở load/unload; không thêm per-element Update.
GPU / draw call:  N/A ngoài placeholder smoke nếu có.
GC:               gameplay idle sau load không alloc do LevelManager lifecycle.
Memory:           reload/unload không tăng dần active context/root/lifecycle participant.
Measurement:      Unity Profiler/Memory/Hierarchy smoke evidence; Redmi 9A chưa bắt buộc cho Story 000A.
```

## 10. Rủi ro và cách xử lý

| Rủi ro | Dấu hiệu | Xử lý |
|---|---|---|
| Port quá nhiều CH013 | diff lớn, nhiều type game cũ sống lại | dừng; quay lại audit classification |
| LevelManager thành god-class | manager biết input details/gameplay/sand/HUD rule | tách owner đúng layer, giữ manager orchestration-only |
| Tạo abstraction phòng xa | nhiều interface không có consumer thật | cắt; chỉ giữ contract cần cho Story 001–009 |
| Fake smoke data trở thành production schema | Story 004 phải migrate workaround | giữ smoke provider trong Tests/Development-only |
| Async callback từ context cũ ghi vào level mới | reload ngẫu nhiên state | cancellation + generation/context identity check |

## 11. Khi implement

- Vừa implement vừa cập nhật `implementation-notes.html`.
- Decision local/reversible tự quyết và ghi notes.
- Nếu muốn promote một module legacy thành dependency production, đây là project-level decision: ghi `Docs/decision-log.md`; nếu semantics không rõ thì dừng hỏi user.
- Không sửa Story 001+ trừ dependency reference/ROADMAP cần phản ánh 000A.
- Không đánh dấu Story 000 DONE thay user; chỉ ghi baseline state mà 000A quan sát được.

## 12. Verification / closure

Required evidence:

1. Pre-change baseline compile/console hoặc lý do không chạy được.
2. Legacy audit table.
3. Post-change compile.
4. Smoke `load/begin → reload → unload`.
5. 10-cycle lifecycle evidence.
6. Scoped diff.
7. `git diff --check`.

Story chỉ DONE khi required acceptance PASS/N/A có evidence thật và semantic deviations = NONE hoặc đã được user approve.

Sau closure PASS mới update `handoff/ROADMAP.md` để chèn 000A và chuyển milestone Foundation thành `000 + 000A`.

## 13. Implementation handoff

```text
Chạy Story 000A — Core Runtime Foundation.

Source of truth:
`handoff/story-000A-core-runtime-foundation/story.md`

Đây là story foundation, không phải gameplay story.

BẮT BUỘC:
- Đọc canonical docs + Story 000 implementation notes trước.
- Audit `Assets/_Core/4_Scripts` theo bảng REUSE DIRECT / ADAPT-PORT / REFERENCE ONLY / IGNORE trước khi viết code.
- Giữ flow quen thuộc LevelManager → LevelContext/RuntimeState → Gameplay/Input readiness → cleanup/reload.
- Code production thuộc namespace `SE001` và canonical `Assets/_Core/4_Scripts`; không dependency tới `CH013`.
- Không implement sand/cup/source/draw/win-lose.
- Không dùng hoặc hỏi `.zip` ngoài project.
- Không cleanup unrelated dirty tree.
- Tự verify đến khi có evidence thật; không claim PASS/DONE từ suy luận.

DỪNG chỉ khi:
- muốn thay project-level contract đã chốt;
- muốn promote legacy module mang semantics không rõ thành dependency production;
- baseline compile/runtime blocker không thể tách khỏi story;
- semantic deviation khỏi story != NONE.

Final report:
- Legacy audit summary
- Observable runtime result
- Files changed
- Verification evidence
- Semantic deviations
- Open risk/debt
- Final status: DONE / DOING / BLOCKED
```
