# Story 008 — Gameplay rules đạt deterministic Win/Lose

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Components đã có input/source/cup nhưng chưa có ván chơi | Story 005–007 | Không thể gọi là clone gameplay |
| Win/Lose phải là state sau mutation, không nằm trong visual/collision callback | core-loop playbook | Tránh double event/race |

## 1. Kết quả mong đợi

> Sau story này, một level có state machine Planning/Active/Resolving/Won/Lost; kết quả chỉ dựa vào Domain + semantic simulation signals.

## 2. Ranh giới

**Làm:**
- Level run state machine.
- Evaluate end condition sau mọi relevant mutation và khi simulation settle.
- Win khi mọi cup đạt requiredCount, contamination trong tolerance, source/pending không còn điều kiện chưa resolve theo contract.
- Lose ngay khi contamination vượt tolerance; hoặc sau all sources exhausted + simulation settled mà requirements không thể đạt.
- Idempotent result event.
- Expose progress snapshots cho HUD bridge.
- Restart reset toàn bộ per-level state.

**KHÔNG làm trong story này:**
- Không UI final.
- Không SDK/analytics.
- Không thêm booster/gimmick.

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
| run state/end condition | Domain |
| settle/idle signal | Simulation → Domain |
| scheduler/timing | Scheduler |
| HUD snapshot | Bridge contract |

### 5.2 Số liệu tune được

contamination tolerance / result display delays từ data/profile; default clone path có thể là zero foreign tolerance.

### 5.3 Required contracts

- `EvaluateEndCondition()` idempotent.
- Simulation emits semantic `IsSettled/Idle`, Domain không đọc renderer/particle.
- All delays = 0 vẫn cho kết quả logic đúng.
- Visual removed → same result.

## 6. Acceptance criteria

- [ ] Correct routing level → Won exactly once.
- [ ] Wrong grain vượt tolerance → Lost exactly once.
- [ ] Stuck/insufficient grains: chỉ Lose sau source exhausted + pending=0 + simulation settled.
- [ ] Calling EvaluateEndCondition repeatedly không duplicate event.
- [ ] Restart from Won/Lost tạo RuntimeState mới.
- [ ] EditMode tests cover zero-delay property.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Never-settle | level treo Resolving | stable threshold/profile + hard diagnostic timeout chỉ log, không fake result |
| Early lose while grains pending | false loss | pending/reserved counts part of idle gate |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- Pure C# EditMode result tests.
- PlayMode one success/one fail/stuck case.
- Event count assertions.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 008: Gameplay rules đạt deterministic Win/Lose

Source of truth:
handoff/story-008-gameplay-rules-end-state/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
