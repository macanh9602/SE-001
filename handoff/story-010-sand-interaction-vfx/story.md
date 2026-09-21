# Story 010 — Sand interaction VFX: stream, impact, slide, edge-leave

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Powder sim đúng nhưng obstacle interaction cần tactile feedback | user requirement + SandDropStream reference pattern | Đây là phần lớn perceived sand feel |
| Emit particle per collision cell sẽ nổ CPU/overdraw | mobile budget | Cần aggregate/sampling |

## 1. Kết quả mong đợi

> Sau story này, sand chạm/trượt/rời obstacle có micro VFX rõ nhưng restrained như fine powder, bounded và hoàn toàn presentation-only.

## 2. Ranh giới

**Làm:**
- Giữ/hoàn thiện falling stream visual.
- Simulation/bridge aggregate contact samples thành semantic VFX events.
- Impact micro crumbs.
- SurfaceSlide skim particles theo tangent.
- EdgeLeave crumbs mang momentum.
- HeavyImpact chỉ khi contact energy/count vượt profile threshold.
- Pool/prewarm theo EffectsProfile/particle contract.
- Expose Powder VFX profile.

**KHÔNG làm trong story này:**
- Không đổi simulation movement để chiều VFX.
- Không final art/SFX mastering.
- Không emit ParticleSystem cho từng grain/cell collision.

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
| contact aggregation | Simulation→Presentation bridge |
| effects | Visual/VFX |
| pool lifecycle | Visual infrastructure |
| intensity tuning | Profile |

### 5.2 Số liệu tune được

Powder baseline: impact chance/sample thấp; 1–2 crumbs/event; low up velocity; short lifetime ~micro reaction. Exact production values tune qua visualizer/device, không hardcode.

### 5.3 Required contracts

- VFX event carries position, normal, tangent, material/color, intensity; không gameplay result.
- Contact events có fixed per-frame/per-region budget.
- One logical grain may map to multiple crumbs.
- VFX drop under budget pressure is allowed; logical state unaffected.

## 6. Acceptance criteria

- [ ] Impact nhìn là fine powder reaction, không debris explosion.
- [ ] Slide effect đi theo surface tangent.
- [ ] EdgeLeave kế thừa hướng momentum.
- [ ] Stress pile hitting long obstacle không spawn unbounded particles.
- [ ] VFX disabled → gameplay parity exact.
- [ ] All runtime VFX pooled/persistent; no Instantiate/Destroy hot path.
- [ ] p95 frame time sau VFX vẫn trong project budget.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| VFX quá mạnh che sand body | player đọc particle hơn material | reduce size/up velocity/rate |
| Event bridge allocations | GC spikes on collisions | fixed buffers/struct events |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- Profiler + particle count.
- Disable-VFX parity.
- Video review obstacle impact/slide/edge.
- Pool lifecycle 10× reload.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 010: Sand interaction VFX: stream, impact, slide, edge-leave

Source of truth:
handoff/story-010-sand-interaction-vfx/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
