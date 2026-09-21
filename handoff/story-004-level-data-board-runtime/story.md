# Story 004 — Level data và board runtime

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Template LevelJson hiện là commented data của game cũ | `_Core/4_Scripts/Data/LevelJson.cs` | Không thể làm clone trên schema cũ |
| Core clone cần source/cup/fixed barrier/draw area | store description/screenshots | Gameplay phải data-driven trước editor |

## 1. Kết quả mong đợi

> Sau story này, runtime load được một level JSON tự chứa board, source, cup, fixed obstacle và draw area rồi dựng board đúng coordinate contract.

## 2. Ranh giới

**Làm:**
- Định nghĩa schemaVersion + stable IDs.
- Data cho board bounds, sand sources, cups, obstacle polylines/thickness, draw area.
- Optional level overrides chỉ cho value đã có owner profile.
- Validator runtime.
- Loader/spawner tạo per-level RuntimeState và roots theo blueprint.
- Seed valid/static obstacle masks trước source activation.
- Tạo 2–3 sample JSON technical levels.

**KHÔNG làm trong story này:**
- Không level editor UI.
- Không win/lose.
- Không source emission behavior hoàn chỉnh.

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
| JSON DTO | Level Data |
| validation | Data |
| load/spawn lifecycle | Spawner |
| board coordinate | Runtime Architecture |
| masks seed | Simulation bridge |

### 5.2 Số liệu tune được

Board/world mapping, obstacle thickness, source/cup dimensions có global profile + level override qua một resolver duy nhất.

### 5.3 Required contracts

Suggested schema:
- levelId/schemaVersion
- board { bounds/drawArea }
- sources[] { stableId, materialId, position, grainCount, streamWidth override? }
- cups[] { stableId, acceptMaterialId, sinkShape/position, requiredCount, contaminationTolerance? }
- obstacles[] { stableId, points[], thickness }
- sandOverride? / timingOverride?
No asset refs inside JSON.

## 6. Acceptance criteria

- [ ] Round-trip JSON test giữ nguyên mọi authored field.
- [ ] Duplicate/dangling IDs bị validator chặn.
- [ ] Level load → unload → load 10× không giữ RuntimeState cũ.
- [ ] Static obstacles được seed trước sand source.
- [ ] Không serialize scene hierarchy/generated grid vào JSON.
- [ ] Sample level chạy dựng board mà không code special-case levelId.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| JsonUtility optional override shadow | object rỗng override profile | presence check/resolver theo data-model rule |
| coordinate drift | editor/runtime khác vị trí | một BoardSpace mapper + parity test |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- EditMode round-trip.
- Runtime sample load.
- 10× load/unload.
- Validation error cases.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 004: Level data và board runtime

Source of truth:
handoff/story-004-level-data-board-runtime/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
