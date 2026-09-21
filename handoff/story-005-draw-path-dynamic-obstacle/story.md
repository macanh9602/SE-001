# Story 005 — Player path trở thành dynamic sand obstacle

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Core reference là touch-to-draw path | official store description | Đây là input chính của game |
| Custom sand grid không thể dựa vào CircleCollider/Rigidbody | Story 002 | Cần rasterize path vào obstacle mask |

## 1. Kết quả mong đợi

> Sau story này, người chơi vẽ stroke trực tiếp trên board; line hiển thị ngay và trở thành obstacle cho sand cùng frame/step an toàn.

## 2. Ranh giới

**Làm:**
- Single input controller thu pointer và sinh draw commands.
- Screen→board conversion qua một coordinate owner.
- Stroke sampling theo minimum spacing.
- Rasterize mỗi segment dạng capsule/thickness vào dynamic mask.
- Visual line prefab/mesh phản hồi frame đầu.
- Cho phép vẽ khi sand đang chạy.
- Resolve grain overlap khi line mới đi qua occupied cells, bảo toàn count.
- Clear/retry API xóa dynamic mask + visual line sạch.

**KHÔNG làm trong story này:**
- Không cup/win/lose.
- Không ParticleSystem collision feedback.
- Không final line art.

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
| pointer/command | Input |
| stroke data | RuntimeState |
| rasterize | Simulation bridge |
| line drawing | Visual |

### 5.2 Số liệu tune được

strokeThickness, sampleSpacing, maxPointsPerStroke, allowedDrawArea đọc profile/level data. Nếu clone không giới hạn ink thì không tự thêm ink economy.

### 5.3 Required contracts

- Input callback không trực tiếp đổi Domain outcome; command được xử lý theo thứ tự.
- Dynamic obstacle mask là simulation data riêng, không encode vào grain `Types`.
- Drawing over sand không delete grain; resolution deterministic/tolerance-tested.
- UI pointer không vẽ lên board.

## 6. Acceptance criteria

- [ ] Pointer drag tạo continuous line không gap ở tốc độ swipe cao.
- [ ] Sand va/trượt trên line vừa vẽ.
- [ ] Vẽ xuyên vùng đang có sand không làm count lệch.
- [ ] Clear/Retry xóa toàn bộ line state/mask.
- [ ] 0 managed allocation/frame trong sustained drawing sau warm-up hoặc allocation được đo/giải trình theo project budget.
- [ ] Multi-touch policy deterministic: chỉ pointer owner đầu tiên vẽ; pointer khác ignore cho đến release.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| High-speed stroke có lỗ | sand xuyên line | capsule rasterization theo segment, không point-only |
| Line update alloc | GC spike khi drag | preallocated buffers/mesh strategy |
| Overlap resolve nổ CPU | vẽ xuyên pile lớn | bounded local search + measure |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- PlayMode draw tests.
- High-speed automated stroke.
- Grain conservation audit.
- Profiler frame có drag/không drag.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 005: Player path trở thành dynamic sand obstacle

Source of truth:
handoff/story-005-draw-path-dynamic-obstacle/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
