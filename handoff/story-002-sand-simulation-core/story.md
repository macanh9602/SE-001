# Story 002 — Production sand simulation core

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Slice chỉ để prove feasibility | P2 technical-slice contract | Không được bê prototype hardcode vào production |
| Gameplay cần sand source-of-truth độc lập Visual | system blueprint | Win/lose/accounting phải test headless |

## 1. Kết quả mong đợi

> Sau story này, project có production Sand Simulation module sở hữu grid state, movement, obstacle masks, emit/consume API và accounting, test được không cần final visual.

## 2. Ranh giới

**Làm:**
- Chuyển kết luận Story 001 thành production simulation.
- Tạo persistent native buffers/lifecycle explicit.
- Implement movement phases: acceleration → multi-cell fall → impact → diagonal/slide → settle.
- Implement static + dynamic obstacle mask API.
- Implement deterministic overlap resolution khi obstacle mới đè grain.
- Implement emit/query/consume/count APIs theo material id.
- Implement Sand Audit counters cho dev build.
- Dispose/reload sạch.

**KHÔNG làm trong story này:**
- Không render beauty shader.
- Không ParticleSystem.
- Không gameplay win/lose.

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
| grid/state/step | Simulation |
| accounting queries | Simulation/RuntimeState boundary |
| audit | Diagnostics |
| lifecycle | Simulation |

### 5.2 Số liệu tune được

Tunable motion đọc từ `SandRuntimeProfile`/resolved level override; technical epsilon/limits có thể const nếu không tune.

### 5.3 Required contracts

- Single owner của grain state.
- `emitted = active + collected + spilled + pending` có thể audit theo material.
- `Step(dt)`/fixed cadence không phụ thuộc Visual.
- Obstacle update là command rõ, không Visual tự sửa grid.
- No `Instantiate/Destroy` trong sim.

## 6. Acceptance criteria

- [ ] Headless/EditMode simulation tests pass cho fall, diagonal, ramp slide, edge-leave, obstacle overlap, settle.
- [ ] Count conservation pass qua ≥10k simulation steps seeded deterministic.
- [ ] Load/unload simulation 10 lần không leak NativeArray.
- [ ] 0 B/frame managed allocation trong production step sau warm-up.
- [ ] Không reference Renderer/ParticleSystem/HUD từ simulation.
- [ ] Static + player obstacle dùng cùng collision contract nhưng mask owner tách biệt.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Float momentum làm khó reproducible | test flaky | tolerance-based asserts + fixed seed/order |
| Dispose race | NativeArray disposed while job running | Complete job trước mutate/dispose |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- Unity compile.
- EditMode tests.
- Burst enabled verification.
- Memory/Native leak check.
- `git diff --check`.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 002: Production sand simulation core

Source of truth:
handoff/story-002-sand-simulation-core/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
