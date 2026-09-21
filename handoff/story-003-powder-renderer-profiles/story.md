# Story 003 — Powder renderer và sand profiles production

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Simulation đúng chưa đủ tạo Powder look | visualizer dùng soft render + jitter + highlight | Improvement chính là visual/feel |
| Legacy-like per-pixel CPU beauty pass dễ tốn CPU | grid mịn ~tens of thousands cells | Cần để shading sang GPU |

## 1. Kết quả mong đợi

> Sau story này, production sand render bằng một field quad + shader/profile, Powder là preset mặc định và có thể đổi profile mà không sửa simulation code.

## 2. Ranh giới

**Làm:**
- Implement state texture upload theo kết luận Story 001.
- Implement shader palette/type/shade và Powder soft sampling.
- Tạo `SandFeelProfile` + Powder preset.
- Tạo material/prefab thật, không runtime Shader.Find/new Material.
- Bind renderer lifecycle với sand field.
- Expose debug toggle PureCA/Sandy/Powder nếu hữu ích nhưng Powder default.

**KHÔNG làm trong story này:**
- Không interaction VFX.
- Không final art/material polish.
- Không gameplay rule.

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
| texture bridge | Visual |
| shader/material | Visual |
| Powder values | Profile |
| palette | Profile |

### 5.2 Số liệu tune được

| Value | Default |
|---|---:|
| colorJitter | 0.35 |
| surfaceHighlight | 0.30 |
| softRender | true |
| sparkle | false |
| visualGrainScale | Powder baseline |

### 5.3 Required contracts

- Visual đọc snapshot/state từ Simulation; không mutate gameplay.
- 1 source-of-truth cho sand color/palette.
- Renderer survives enable/disable/reload without material leak.

## 6. Acceptance criteria

- [ ] Powder default khớp direction của HTML visualizer ở density/softness.
- [ ] Sand body ≤1 main draw call; debug overlay không tính production.
- [ ] 0 material allocation trong gameplay loop.
- [ ] Texture/filter/material tạo/bind ở init, không mỗi frame.
- [ ] Quality downgrade có hook nhưng chưa cần final tier ở Story 003.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Bilinear làm bleed material | salt/pepper màu hòa ở border | type-aware shader/sample strategy |
| Texture upload spike | p95 cao | dùng format/cadence từ Story 001 |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- Frame Debugger.
- Profiler CPU/GPU.
- Screenshot/video comparison với visualizer.
- Reload 10× material count không tăng.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 003: Powder renderer và sand profiles production

Source of truth:
handoff/story-003-powder-renderer-profiles/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
