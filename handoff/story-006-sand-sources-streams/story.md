# Story 006 — Salt/pepper sources phát finite material streams

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Core game có salt và pepper source rơi xuống board | official reference | Cần material routing + finite payload |
| Visual stream không được trở thành gameplay authority | particle-system rule | Phải tách logical emit và visual crumbs |

## 1. Kết quả mong đợi

> Sau story này, mỗi source trong level phát đúng số logical grains theo material id, có pending delivery/accounting rõ và visual falling stream không làm thay đổi quantity.

## 2. Ranh giới

**Làm:**
- Source runtime entity đọc level data.
- Emit finite payload vào Sand Simulation theo rate/width.
- Source start theo `pourStartDelay`, được vẽ line trước và trong lúc pour.
- Pending emit reservation/accounting.
- Stream visual presentation theo pattern dense falling crumbs; logical grain != visual crumb.
- Source cleanup/restart deterministic.

**KHÔNG làm trong story này:**
- Không cup logic.
- Không win/lose.
- Không final shaker model/art.

## 3. Context cần đọc

- `AGENTS.md`
- `Docs/project-context.md`
- `standards/system-design.md`
- `standards/performance-budget.md`
- `Docs/runtime-architecture.md`
- `Docs/data-model.md`
- `Docs/particle-system-rule.md` khi story có VFX
- `Docs/visualizers/sand-feel-lab.html` khi story có sand feel


## 4. Đầu vào đã có

- Unity `6000.0.70f1`, URP.
- Burst `1.8.x` có trong package lock.
- UniTask, DOTween, `VTLTools.ObjectPool`, `EffectsProfile`.
- `GameScene` và `Docs/visualizers/sand-feel-lab.html` đã tồn tại trong workspace.
- **Không có và không được yêu cầu `.zip` legacy.** Mọi behavior cần thiết nằm trong story/spec hiện tại.

## 5. Việc cần làm

### 5.1 Layer assignment

| Việc | Layer |
|---|---|
| source quantity/state | Domain/RuntimeState |
| deposit API | Simulation |
| source view/stream | Visual |
| timing | Profile |

### 5.2 Số liệu tune được

pourStartDelay, logicalRate, streamWidth, stream visual density/jitter/fall speed là profile/level override đúng owner.

### 5.3 Required contracts

- Domain owns remaining logical grain count.
- Visual backlog/capacity không được reject logical gameplay silently.
- Nếu visual budget đầy: degrade/drop visual crumbs, KHÔNG mất logical grain.
- Pending state được tính trong end-condition idle.

## 6. Acceptance criteria

- [ ] Source emit tổng đúng `grainCount` per material.
- [ ] 2 source đồng thời không swap material/color.
- [ ] Restart reset remaining/pending đúng.
- [ ] Visual particle cap bounded; không Instantiate per grain.
- [ ] Disable Visual vẫn emit logical grains y hệt.
- [ ] Accounting audit khớp emitted/pending/active.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Visual queue coupling logic | sand dừng vì particle cap | one-way presentation notifications |
| Source overfills top cell | pending bị mất | admission/pending retry bounded |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- EditMode source count tests.
- PlayMode two-source simultaneous.
- Toggle visual off parity.
- Particle count/draw-call capture.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 006: Salt/pepper sources phát finite material streams

Source of truth:
handoff/story-006-sand-sources-streams/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
