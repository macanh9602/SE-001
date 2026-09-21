# Story 007 — Cups collect đúng material và audit contamination

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Objective là salt/pepper vào respective cup, không mix | official store description | Cần sink semantics chính xác |
| Cellular grid dễ double-consume/mất grain ở boundary | simulation nature | End condition phụ thuộc accounting |

## 1. Kết quả mong đợi

> Sau story này, cup sink consume grain đi vào vùng cup đúng một lần, phân biệt own/foreign material và báo state thu thập cho Domain.

## 2. Ranh giới

**Làm:**
- Runtime cup entity + sink region raster/lookup.
- Simulation semantic signal/consume contract tại sink.
- Track ownCollected, foreignCollected theo cup/material.
- RequiredCount/tolerance lấy level data.
- Visual fill indicator tối thiểu/prototype.
- Audit total material conservation.

**KHÔNG làm trong story này:**
- Không quyết Win/Lose trong Cup Visual.
- Không final cup art.
- Không progression.

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
| collection counters/rule inputs | Domain/RuntimeState |
| sink detection/consume | Simulation |
| cup feedback | Visual |
| audit | Diagnostics |

### 5.2 Số liệu tune được

requiredGrainCount và allowedForeignGrains là level data; cup visual fill timing/profile nếu có.

### 5.3 Required contracts

- Grain được remove khỏi field chỉ qua consume path có accounting.
- Cup accepts any incoming grain for accounting; matching/foreign classification là Domain data.
- `wrong grain` không tự trigger lose trong Simulation/Visual; Domain Story 008 quyết.

## 6. Acceptance criteria

- [ ] Một grain crossing sink boundary chỉ được consume 1 lần.
- [ ] Own/foreign counters đúng với scripted sequence.
- [ ] Two cups cùng material hoặc khác material không cross-count.
- [ ] Total accounting conservation pass.
- [ ] Visual disable không đổi collection result.
- [ ] Sink query không scan toàn grid per cup per frame nếu có thể index theo region.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Boundary oscillation double-count | counter > emitted | consume clears/moves ownership atomically |
| Huge sink scan | CPU tăng theo cup×grid | precomputed cell mask/index |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- EditMode scripted sink tests.
- Randomized accounting/property test.
- Profiler cup collection frame.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 007: Cups collect đúng material và audit contamination

Source of truth:
handoff/story-007-cups-accounting/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
