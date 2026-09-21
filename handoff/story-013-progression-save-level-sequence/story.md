# Story 013 — Progression/save và ordered level sequence

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Playable loop hiện Next/Retry nhưng chưa persist | Story 009 | App restart mất progress |
| SDK bị loại scope nhưng local save vẫn cần | project scope | Meta cơ bản phải độc lập SDK |

## 1. Kết quả mong đợi

> Sau story này, game nhớ level hiện tại/completed levels và Next/Retry hoạt động qua app restart mà không phụ thuộc SDK.

## 2. Ranh giới

**Làm:**
- Ordered level catalog/list.
- Persist current/unlocked/completed using project Save infrastructure.
- Clamp/migrate invalid saved index.
- Next advances only after Won.
- Retry không mutate progress.
- Dev reset-progress command.

**KHÔNG làm trong story này:**
- Không cloud save.
- Không ads/reward/economy.
- Không leaderboard.

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
| save/progress | Save |
| level catalog | Data/Profile |
| flow bridge | Bootstrap/Flow |

### 5.2 Số liệu tune được

Level order/catalog là data; debug reset editor/dev only.

### 5.3 Required contracts

- Save không là source-of-truth trong active run.
- Progress update từ result event qua bridge, không Domain gọi PlayerPrefs trực tiếp.

## 6. Acceptance criteria

- [ ] Win level N → restart app → level N+1 selected.
- [ ] Lose/retry không unlock next.
- [ ] Corrupt/out-of-range progress fallback an toàn.
- [ ] Reset progress về first level.
- [ ] No SDK dependency.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Save writes trong hot path | hitch | write on result/flow transition only |
| Level catalog/file mismatch | next missing | startup validation |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- EditMode save adapter tests.
- Manual app restart.
- Catalog validation.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 013: Progression/save và ordered level sequence

Source of truth:
handoff/story-013-progression-save-level-sequence/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
