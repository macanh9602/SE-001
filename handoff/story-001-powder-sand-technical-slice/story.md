# Story 001 — Powder sand technical slice chứng minh feel và budget

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Improvement chính của project là sand feel | `sand-feel-lab.html` preset Powder | Nếu nền sand sai thì clone không có khác biệt |
| Grid mịn + momentum + dynamic obstacle có rủi ro CPU/upload | Powder ≈ grid 180×320 ở reference board | Nhiều story sau phụ thuộc |
| Rigidbody-per-grain không phù hợp target mobile | project perf budget + design direction | Cần prove custom simulation sớm |

## 1. Kết quả mong đợi

> Sau story này, technical harness chứng minh Powder sand trượt trên ramp <45°, tương tác drawn obstacle và có số đo p95/GC/draw call trên device thật.

## 2. Ranh giới

**Làm:**
- Tạo isolated Sand Lab scene/harness.
- Prototype persistent grid + Burst single-writer step.
- Prototype vertical velocity + horizontal momentum + impact-to-horizontal.
- Prototype valid/static/dynamic obstacle masks.
- Prototype 1-quad soft renderer.
- Expose Powder controls để so với `sand-feel-lab.html`.
- Stress 1×/2×/5× tới điểm gãy và ghi số.

**KHÔNG làm trong story này:**
- Không biến slice thành production module.
- Không cup/win/lose/level editor.
- Không interaction ParticleSystem production.

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
| CA + momentum | Simulation |
| state texture render | Visual |
| harness/HUD | Dev/Test |
| Powder values | Data/Profile prototype |

### 5.2 Số liệu tune được

Powder baseline bắt buộc bắt đầu từ: grainScale 1.5px-equivalent; gravity .30; vmax 4; repose 1; momentumRetention .95; impactHorizontal .50; surfaceFlow .14; jitter .35; highlight .30; soft render; rate 16; width 6; substep 1. Tất cả slider/profile, không literal rải trong code.

### 5.3 Required contracts

- Logical grain state tối thiểu: `type`, `shade`, `fallVelocity`, `horizontalMomentum`.
- Masks tách riêng: valid/static/dynamic.
- Collision với line phải giữ count.
- Renderer không dùng GameObject/Rigidbody per grain.
- Technical slice ghi D-xxx chọn format texture/simulation cadence sau đo.

## 6. Acceptance criteria

- [ ] Ramp 30–40°: sand tiếp tục slide; không kẹt kiểu CA 45°.
- [ ] Rời mép obstacle: grain giữ horizontal momentum và rơi tiếp.
- [ ] Draw obstacle trong khi sand đang chạy không xoá/nhân grain.
- [ ] Powder nhìn thành khối bột mịn; không đọc thành bead/pixel lớn; không hoàn toàn liquid.
- [ ] Gameplay loop của harness: 0 B managed allocation/frame sau warm-up.
- [ ] Sand render dùng ≤3 draw calls trong harness.
- [ ] Đo p95 trên target device; nếu chưa có target device thì Story giữ BLOCKED/PENDING, không DONE.
- [ ] Tìm và ghi break point của grid/load thay vì chỉ test mức mặc định.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Upload texture là bottleneck | p95 CPU/GPU tăng theo grid | thử state texture format/dirty update cadence |
| Momentum gây tunneling | grain xuyên thin wall | step collision từng cell tối đa vmax |
| Dynamic wall ăn grain | accounting lệch | overlap-resolution deterministic |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- Profiler device: avg + p95 simulation, upload/render.
- GC Alloc gameplay loop.
- Frame Debugger draw calls.
- Grain accounting trước/sau dynamic line.
- Capture video đối chiếu visualizer.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 001: Powder sand technical slice chứng minh feel và budget

Source of truth:
handoff/story-001-powder-sand-technical-slice/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
