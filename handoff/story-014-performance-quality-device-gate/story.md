# Story 014 — Performance, quality scaling và device gate

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Sand grid + texture upload + VFX là workload mới lớn | Stories 001–010 | Prototype smooth trong Editor không đủ |
| Project target 60fps mid / 30fps low | performance budget | Cần gate trước SDK/final polish |

## 1. Kết quả mong đợi

> Sau story này, full gameplay level nặng nhất có measured budget trên device thật, quality fallback rõ và không còn allocation/leak blocker trước khi bước sang SDK/final art.

## 2. Ranh giới

**Làm:**
- Define representative heavy level từ editor.
- Profile Simulation, texture upload/render, draw input, cups, VFX, UI.
- Quality tier cho grid resolution/update cadence/VFX density nếu cần.
- Stress 2× và ghi break point.
- Memory/pool peak.
- 10–20 run soak/reload.
- Record canonical limits vào project context/runtime architecture.

**KHÔNG làm trong story này:**
- Không SDK.
- Không final art optimization pass cho asset chưa tồn tại.
- Không gameplay redesign để cứu performance nếu chưa escalate.

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
| measurement | Performance |
| quality resolver | Profile/System |
| downgrade visual | Visual |
| hard simulation limits | Architecture decision nếu cần |

### 5.2 Số liệu tune được

Quality tier chỉ được đổi visual/resolution/cadence trong bounds không phá gameplay semantics; mọi threshold ở profile/canonical limits.

### 5.3 Required contracts

- Low quality không được đổi material counts, cup result, obstacle collision semantics.
- VFX là degrade-first.
- Nếu giảm grid resolution ảnh hưởng collision materially, đó là project-level decision, không tự làm.

## 6. Acceptance criteria

- [ ] Target device thật đạt ≥30fps p95-compatible frame budget ở heavy level.
- [ ] Mid device đạt 60fps target nếu đã chỉ định.
- [ ] Gameplay loop 0 B/frame GC sau warm-up.
- [ ] Draw calls gameplay trong project budget.
- [ ] 10× load/unload không memory trend; 20 runs không pool growth vô hạn.
- [ ] Quality tier parity tests cho Win/Lose/accounting.
- [ ] Canonical max grid/source/VFX counts được ghi lại.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Grid resolution cần giảm quá mạnh | line/sand semantics đổi | escalate; optimize upload/sim trước |
| VFX overdraw | GPU bottleneck | density/lifetime/size quality scaling |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- Unity Profiler device captures avg/p95.
- Frame Debugger.
- Memory Profiler.
- GC Alloc.
- Parity replay/high-level deterministic scenario across quality tiers.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 014: Performance, quality scaling và device gate

Source of truth:
handoff/story-014-performance-quality-device-gate/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
