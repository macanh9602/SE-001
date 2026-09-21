# Story 009 — Full playable loop với minimal HUD

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Domain có result nhưng chưa có vòng tròn người chơi | P3 core loop gate | Chưa playable end-to-end |
| Project đã có HUDSystem/Win/Lose prefab nhưng template cũ | current `_Core` | Có thể reuse UI infrastructure, không reuse old gameplay |

## 1. Kết quả mong đợi

> Sau story này, trên device thật có thể MainScene → load level → vẽ/đổ → Win/Lose → Retry/Next và lặp lại không leak.

## 2. Ranh giới

**Làm:**
- Bootstrap vào GameScene/level loader theo Story 000.
- Bind gameplay input chỉ sau level spawn.
- Minimal HUD: level label, restart, clear-line nếu reference flow cần, result panels.
- Win → Next, Lose → Retry.
- Unbind input/cancel/recycle/dispose đúng unload order.
- 10× load/reload loop.

**KHÔNG làm trong story này:**
- Không final UI skin.
- Không SDK/ads.
- Không progression unlock persistence (Story 013).

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
| scene/level lifecycle | Bootstrap/Spawner |
| UI bridge | Bridge/HUD |
| retry/next | Flow |
| cleanup | all owners |

### 5.2 Số liệu tune được

UI timings ở Timing/MotionProfile; gameplay result không chờ tween.

### 5.3 Required contracts

- HUD read-only từ bridge/domain snapshot.
- Input unbind trước cancel/unload.
- VFX/visual cleanup không giữ level references.
- Next tạm dùng ordered level list/runtime service; persistence sau.

## 6. Acceptance criteria

- [ ] Cold launch vào level playable.
- [ ] Win path → Next level; Lose path → Retry.
- [ ] Load→unload→load 10 lần không tăng instance/memory trend.
- [ ] Không input lọt trong teardown/result state.
- [ ] 60/30 target không cần final pass nhưng không có obvious spike > frame budget do lifecycle.
- [ ] Minimal UI dùng prefab/TMP infrastructure, không runtime-created text/material.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Template HUD namespace coupling | compile/reference old CH013 | bridge/adapt existing infrastructure, không resurrect gameplay |
| Async teardown race | object callback sau unload | level CancellationToken + generation guard |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- Device full-loop capture.
- 10× reload memory/instance count.
- Console zero new errors.
- Input teardown repro spam.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 009: Full playable loop với minimal HUD

Source of truth:
handoff/story-009-playable-loop-minimal-ui/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
