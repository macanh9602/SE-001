# Story 001 — Architecture Foundation Alignment

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT → AUTO**  
> Size: **M**  
> Roadmap status: **DONE — CLOSURE PASS**

## Goal

Align foundation runtime với Architecture Blueprint để feature story sau dùng đúng ownership:

```text
LevelManager → LevelSpawner → LevelContext/RuntimeState/roots
                         ↘ GameplayManager/input/readiness
```

Sau story, `GameScene` load/reload/unload một empty authored/development level qua explicit scene
dependencies; `LevelSpawner` là owner duy nhất của composition và runtime root lifecycle.

## Why

| Vấn đề | Evidence | Ảnh hưởng |
|---|---|---|
| Foundation hiện để `LevelContext` tạo roots | Blueprint §3 và code baseline | Feature mới sẽ bind sai ownership và buộc refactor dây chuyền |
| Production auto-create manager/smoke flow còn tồn tại | Blueprint §3 | Scene dependency ẩn, khó kiểm soát lifecycle và test production load path |
| Chưa có production `LevelSpawner` contract | Blueprint §2/§8 | Sand/Source/Cup/Obstacle dễ đẩy composition vào `LevelManager` |

## Required context

Đọc theo thứ tự trong `AGENTS.md`, sau đó:

1. `SE001-ARCHITECTURE-BLUEPRINT.md` — §2–§8, §16–§18.
2. `handoff/ROADMAP.md`.
3. Historical evidence nếu cần: `handoff/archive/story-000A-core-runtime-foundation/`.
4. Existing foundation code trong `Assets/_Core/4_Scripts/System/Management/` và scene wiring liên quan.

Không dùng archived Story 000A làm execution contract.

## In scope

- Giữ public capability begin/load, reload và unload của `LevelManager`; chuyển composition work sang
  `LevelSpawner`.
- `LevelSpawner` load/validate/resolve request phù hợp foundation, cleanup level cũ, tạo lifetime token,
  `LevelRuntimeState`, runtime roots và `LevelContext`, rồi bind manager/input/readiness theo order.
- `LevelContext` chỉ giữ ID/token/references đã được Spawner tạo; không tự tạo gameplay roots.
- Giữ và align `LevelReadinessGate`, `ILevelLifecycleParticipant` và per-level cancellation.
- Dùng scene-authored explicit references/dependencies trong `GameScene`.
- Loại production manager auto-create bằng `RuntimeInitializeOnLoadMethod`.
- Loại automatic `foundation_smoke` khỏi production `Awake`/startup. Nếu còn cần, chuyển thành
  Tests/Development-only harness.
- Update tests, scene wiring và canonical docs khi implementation thực tế cần phản ánh tên/API đã chốt.

## Out of scope

- Không implement production StaticObstacle, rasterizer, SandSimulation, SandField visual.
- Không implement Source, Cup, DrawStroke, gameplay accounting/win/lose, HUD result hoặc progression.
- Không implement Level Editor.
- Không salvage/cherry-pick wholesale feature code cũ.
- Không đổi `SE001LevelJson` serialization contract ngoài foundation loader seam cần thiết; nếu seam buộc
  đổi schema, dừng và escalate.
- Không SDK/ads/analytics/IAP hoặc final art.

## Ownership and public contracts

### `LevelManager`

- Owner: lifecycle request, current level identity, load/reload/unload serialization và readiness.
- Public capability tối thiểu: begin/load theo stable level ID/request, reload current, unload current,
  expose loading/ready state tương đương.
- Delegate creation/cleanup cho `LevelSpawner`; không tạo roots hoặc feature objects.
- Không quyết định Next Level ordering và không chứa gameplay rule.

### `LevelSpawner`

- Một owner cho composition + spawn/unload.
- Nhận explicit dependencies: profiles/loader hoặc development provider, factories/services hiện có,
  gameplay/input/readiness bridges cần bind.
- Tạo roots theo blueprint; Story 001 có thể chỉ tạo roots rỗng nhưng ownership/order phải production-ready.
- Trả/construct một `LevelContext` chứa references hoàn chỉnh sau khi spawn thành công.
- Cleanup theo reverse ownership; pool không bị clear giữa reload.

### `LevelContext`

- Per-level và không reuse sau unload.
- Giữ stable level ID, cancellation/lifetime, `LevelRuntimeState` và root references.
- Không gọi `new GameObject`/tự tạo root; không load data hoặc quyết spawn order.

### Readiness/lifecycle

- Readiness/input đóng trước cleanup/load và chỉ mở sau khi context + binding hoàn tất.
- Unload order: close gate → notify will-unload → input unbind → cancel token → cleanup participants →
  dispose state → destroy empty roots.
- Callback/task từ generation cũ không được mutate context mới.

## Implementation sequence

1. Chạy baseline compile/console và inspect diff; ghi evidence trước thay đổi.
2. Audit foundation hiện tại, map từng responsibility vào `LevelManager`, `LevelSpawner`, `LevelContext`,
   readiness/lifecycle; ghi decision/deviation vào implementation notes.
3. Thêm/align `LevelSpawner`, chuyển root/state/context creation khỏi `LevelContext` và `LevelManager`.
4. Chuyển production bootstrap sang explicit scene wiring; remove production auto-create/smoke startup.
5. Align reload/unload cancellation, generation protection và cleanup order.
6. Tạo hoặc cập nhật EditMode/PlayMode tests và Development-only smoke harness.
7. Compile, chạy tests, console check, 10-cycle load/reload/unload verification và inspect scoped diff.

## Acceptance criteria

### Ownership

- [x] `LevelManager` không tạo runtime roots/feature objects và delegate composition cho `LevelSpawner`.
- [x] `LevelSpawner` là owner duy nhất của create/cleanup cho `LevelRoot` và child roots.
- [x] `LevelContext` chỉ nhận/giữ references; không tự dựng hierarchy.
- [x] `LevelRuntimeState` per-level, non-static; không chứa sand buffers.
- [x] Không production auto-create manager hoặc automatic `foundation_smoke` startup.
- [x] `GameScene` có explicit dependency wiring, không duplicate manager/spawner sau reload.

### Lifecycle

- [x] Load đóng readiness/input cho tới khi context và binding hoàn tất.
- [x] Reload cleanup/cancel/dispose context cũ trước khi context mới ready.
- [x] Unload theo order contract và bỏ active context/root.
- [x] Old async callback/generation không ghi được vào context mới (generation + cancelled lifetime; không có async loader trong slice này).
- [x] Pool provider N/A: foundation hiện không có pool provider để clear hoặc recreate.

### Verification

- [x] Unity compile sạch; console không có error/warning mới do story.
- [x] EditMode tests cho ownership/lifecycle logic PASS.
- [x] PlayMode hoặc deterministic smoke: load → reload → unload PASS.
- [x] Lặp load/reload/unload 10 lần: manager count, active context và root count không tăng dần.
- [x] `git diff --check` PASS; final diff không chứa feature implementation ngoài foundation.
- [x] `implementation-notes.html` có evidence thật, semantic deviations và closure status.

## Mobile performance

- Lifecycle work chỉ xảy ra ở load/reload/unload; không thêm per-frame manager polling ngoài owner cần thiết.
- Idle sau load không tạo GC allocation từ foundation lifecycle.
- Reload/unload không tăng dần native/managed memory hoặc root/component count.
- Không thêm draw call ngoài empty/dev smoke presentation cần thiết.
- Redmi 9A không phải gate của foundation story; device performance vẫn thuộc phase I.

## Stop conditions

Dừng và hỏi Product Owner nếu implementation yêu cầu:

- đổi serialization/source-of-truth;
- thay layer contract trong `standards/system-design.md` hoặc blueprint;
- thêm global singleton runtime state;
- thay gameplay semantics hoặc scope của phase B trở đi;
- giữ đồng thời hai production owners cho spawn/root lifecycle.

## Implementation handoff

```text
Chạy Story 001 — Architecture Foundation Alignment.

Source of truth:
handoff/story-001-architecture-foundation-alignment/story.md

- Đọc Architecture Blueprint và canonical contracts trước khi sửa code.
- Chạy baseline compile/console và ghi implementation notes trước thay đổi.
- Chỉ align foundation ownership/lifecycle; không implement phase B trở đi.
- LevelManager delegate composition; LevelSpawner sở hữu roots/spawn/unload; LevelContext chỉ giữ refs.
- Production dependency phải explicit trong GameScene; smoke chỉ Tests/Development.
- Tự compile/test/verify 10 lifecycle cycles; không claim PASS nếu chưa chạy.
- Không materialize phase B cho tới khi Story 001 closure PASS.

Final report: observable result, files changed, verification evidence, performance, deviations,
open risks và DONE/DOING/BLOCKED.
```

## Closure

Story chỉ DONE khi toàn bộ required acceptance PASS/N/A có evidence, semantic deviations được approve,
canonical docs khớp implementation và final diff đúng scope. Sau closure PASS mới materialize phase B
thành story kế tiếp và đổi status ROADMAP.
