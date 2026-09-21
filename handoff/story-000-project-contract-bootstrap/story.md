# Story 000 — Project contract và bootstrap sẵn sàng

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Project docs còn placeholder identity/contracts | `Docs/project-context.md`, `runtime-architecture.md`, `data-model.md`, `ROADMAP.md` | Worker phải đoán namespace, authority, data |
| Nhiều script template cũ đang comment và còn namespace project cũ | `_Core/4_Scripts/*` | Dễ vô tình resurrect code không liên quan |
| Build settings cần ownership scene rõ | `EditorBuildSettings.asset`, `_Core/Scenes/GameScene.unity` | Cần ownership scene rõ trước core loop |

## 1. Kết quả mong đợi

> Sau story này, một agent mới đọc 6 canonical docs là biết chính xác project identity, scene flow, simulation authority và nơi đặt code/data mà không hỏi lại.

## 2. Ranh giới

**Làm:**
- Điền project identity và các 🔒 contract từ USER-SETUP-GATE.
- Chốt `GameScene` là scene duy nhất cho bootstrap và gameplay.
- Chốt `Assets/_Core/4_Scripts` là canonical structure; audit/reuse theo từng file và không resurrect CH013 gameplay semantics.
- Map layer cụ thể vào `Docs/runtime-architecture.md` ở mức owner/module, chưa cần feature code.
- Khởi tạo Level JSON schemaVersion owner và Resources/Levels folder contract.
- Update ROADMAP từ proposed thành canonical sau khi gate pass.

**KHÔNG làm trong story này:**
- Không implement sand simulation.
- Không implement gameplay source/cup/draw.
- Không SDK, không final art.

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

| Việc | Layer | Ghi chú |
|---|---|---|
| Project identity | Docs | canonical |
| Scene flow | Bootstrap | GameScene duy nhất |
| Module boundaries | Architecture | canonical `4_Scripts`; asmdef chỉ thêm tại chỗ khi cần |
| Level root schema owner | Data | schema only |

### 5.2 Số liệu tune được

| Giá trị | Owner |
|---|---|
| namespace / assembly names | project contract |
| frame target / low device | project context |
| scene names | project context |

### 5.3 Required contracts

- Domain/Simulation/Visual boundaries theo `standards/system-design.md`.
- Simulation authority = custom 2D grid, không Rigidbody authority.
- `Assets/_Core/4_Scripts` là canonical structure; generic code là reusable, còn CH013 gameplay-specific code phải replace/adapt.
- `GameScene` là scene duy nhất cho bootstrap/meta entry và gameplay.

## 6. Acceptance criteria

- [ ] `template-lint.ps1` không còn fail vì project identity placeholder.
- [ ] Compile baseline sạch; warning/error pre-existing được ghi rõ nếu có.
- [ ] `Docs/project-context.md` có namespace, asmdef strategy, target device, physics authority/dimension/determinism.
- [ ] `Docs/runtime-architecture.md` có layer map tối thiểu cho Sand, Level, Input, Cup, Source, HUD.
- [ ] `Docs/data-model.md` xác định source-of-truth/generated/runtime-state.
- [ ] `handoff/ROADMAP.md` có Story 001–014.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Port CH013 semantics ngoài scope | diff lớn hoặc old gameplay type sống lại | audit từng file; chỉ port generic contract/flow cần thiết |
| Dirty tree che diff | file pre-existing xuất hiện trong story diff | commit/stash trước story |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- Static validation: template lint + `git diff --check`.
- Unity compile.
- Inspect build settings/scene flow.
- Final diff không chứa cleanup ngoài scope.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 000: Project contract và bootstrap sẵn sàng

Source of truth:
handoff/story-000-project-contract-bootstrap/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
