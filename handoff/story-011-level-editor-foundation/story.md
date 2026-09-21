# Story 011 — Level editor mở/sửa/lưu core level data

> Execution mode: **DIRECT**  
> Frontier collaboration: **INHERIT**  
> Size: **M**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Core loop playable nhưng level JSON viết tay không scale | P4 entry condition | GD cần author level |
| Editor skeleton folder tồn tại nhưng chưa có project-specific contract | `_Core/4_Scripts/Editor/Level/` | Cần Document/View/Derived tách rõ |

## 1. Kết quả mong đợi

> Sau story này, GD mở một level JSON, thấy board/source/cup/obstacle, sửa field/transform cơ bản, undo và save round-trip mà runtime đọc đúng.

## 2. Ranh giới

**Làm:**
- Editor Document/ViewState/DerivedState separation.
- Open/new/save JSON v1.
- Read-only board canvas trước, sau đó select/move core entities.
- Undo/Redo document edits.
- Stable ID generation/selection.
- Round-trip parity với runtime schema.
- Basic blocking validation trước save.

**KHÔNG làm trong story này:**
- Không full convenience tooling.
- Không difficulty generator.
- Không final editor styling.

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
| document | Editor/Data |
| canvas/view state | Editor |
| edit commands/undo | Editor |
| runtime parity | Data |

### 5.2 Số liệu tune được

Editor preference/view state vào SessionState/EditorPrefs; không serialize level JSON.

### 5.3 Required contracts

- Runtime JSON schema là source-of-truth chung; editor không có DTO song song.
- Selection dùng stable ID.
- ApplyEdit không reload document.
- Editor assembly không trở thành runtime dependency.

## 6. Acceptance criteria

- [ ] New → save → reopen giữ đủ field.
- [ ] Runtime load file editor vừa save không cần sửa tay.
- [ ] Undo/Redo move/add/remove entity đúng.
- [ ] View state không xuất hiện trong JSON.
- [ ] Blocking validation chặn duplicate ID/out-of-board required entity.
- [ ] Editor interaction thường <100ms.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Editor/runtime drift | level chạy khác preview | shared resolver/mapper + parity tests |
| Reload mỗi edit | mất selection/giật | command update model |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- EditMode round-trip/parity tests.
- Manual author 1 sample level.
- Editor stopwatch on common edits.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 011: Level editor mở/sửa/lưu core level data

Source of truth:
handoff/story-011-level-editor-foundation/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
