# Story 012 — Level editor authoring, validation và playtest preview

> Execution mode: **AUTONOMOUS_PACKETS**  
> Frontier collaboration: **INHERIT**  
> Size: **L**

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| Editor foundation chưa đủ để GD tự làm level hoàn chỉnh | P4 exit gate | Vẫn phụ thuộc dev |
| Draw area/obstacle/source/cup có spatial validation | gameplay nature | Cần visual authoring + preview |

## 1. Kết quả mong đợi

> Sau story này, GD có thể tạo một level từ file trống, đặt source/cup/barrier, chỉnh draw area, validate, preview sand và playtest trong game không hỏi dev.

## 2. Ranh giới

**Làm:**
- Authoring tools cho source/cup/static obstacle polyline/draw area.
- Snap/grid/selection controls ở mức cần thiết.
- Validation blocking/warning/info với message sửa được.
- Preview generated valid/static mask.
- Preview source/cup routing metadata.
- Playtest current document bằng runtime loader.
- GD quick guide 5–10 bước.

**KHÔNG làm trong story này:**
- Không procedural generation.
- Không difficulty auto-score.
- Không final editor skin.

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
| spatial authoring | Editor |
| validation | Editor/Data |
| derived mask preview | Editor |
| playtest bridge | Editor→Runtime |

### 5.2 Số liệu tune được

Editor-only snap/opacity/preferences không vào level data. Gameplay values vẫn profile/level data.

### 5.3 Required contracts

- Five-surface validation pattern theo `knowledge/editor-ux/validation-surfacing.md`.
- Load old/invalid file báo warning; không silent mutate.
- Preview uses same rasterization/BoardSpace contract as runtime.
- Playtest không save generated cache vào source JSON.

## 6. Acceptance criteria

- [ ] GD flow: New → board → 2 sources → 2 cups → obstacles → validate → save → playtest.
- [ ] Blocking errors chặn save; warnings không tự sửa.
- [ ] Obstacle preview mask parity với runtime test.
- [ ] Undo/Redo hoạt động cho add/delete/move/polyline edit.
- [ ] Level lớn nhất hiện có editor vẫn responsive.
- [ ] Quick guide hoàn thành và một full level được author bằng chính tool.

## 7. Cần hỏi trước khi làm

Không có blocker nếu `Docs/USER-SETUP-GATE.md` đã được chốt.

## 8. Rủi ro

| Rủi ro | Dấu hiệu | Ứng phó |
|---|---|---|
| Story L drift | editor phình convenience features | packet boundaries, out-of-scope strict |
| Preview sim khác runtime | GD tin sai | shared code path, no duplicate algorithm |

## 9. Khi implement

- Story là contract; không redesign gameplay.
- Không hardcode tunable; dùng Profile / prefab field / level data.
- Không copy namespace/project-specific code từ project cũ.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Không claim PASS nếu chưa có evidence thật.

## 10. Verification

- Checklist `skills/level-editor/refs/checklist.md`.
- Runtime parity.
- Manual end-to-end authoring.
- Editor performance capture.

## 11. Harvest

- Pattern generic về sand/grid/input/VFX có reusable không → `knowledge/`.
- Bug/race/allocation lặp lại → `standards/anti-patterns.md`.
- Tunable/motion reusable → đúng knowledge/profile owner.
- Project-level decision → `Docs/decision-log.md`.

## 12. Implementation handoff

```text
Chạy Story 012: Level editor authoring, validation và playtest preview

Source of truth:
handoff/story-012-level-editor-authoring-preview/story.md

- Đọc đúng Context cần đọc; không scan toàn repo nếu không cần.
- Story là contract; local/reversible decision tự quyết và ghi implementation-notes.
- Không dùng hoặc yêu cầu Assets(2).zip.
- Tự verify tới khi acceptance có evidence thật.
- Không đổi SDK/final art.
- Final report: Observable result / Files changed / Verification / Semantic deviations / Risks / DONE|DOING|BLOCKED.
```

## 13. Story closure

DONE chỉ khi tất cả required acceptance PASS/N/A có evidence, semantic deviations = NONE hoặc đã approve, implementation-notes hoàn chỉnh, final diff đúng scope.
