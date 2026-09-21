# Canonical detail moved from SKILL.md

## Cấu trúc spec — ba phần, confirm từng phần

### Phần 0 — Vì sao có feature này

Bảng ba cột. Đây là phần quyết định spec có chạy trơn không.

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| cái gì đang thiếu/sai | trích **đúng chỗ** trong code, log, hoặc GDD | người chơi hoặc GD chịu hậu quả gì |

---

### Phần 1 — Gameplay logic

```markdown
## [Feature] — Gameplay Logic

### Mô tả ngắn
1–2 câu, từ góc nhìn người chơi.

### Luồng chính (happy path)
1. Trigger / điều kiện bắt đầu
2. Bước xử lý logic
3. State thay đổi
4. Output: event / callback / data update

### Edge cases
| Ca | Xử lý |
|---|---|

### Data model
- Input:
- Output:
- State cần lưu: (và **thuộc loại nào** — source of truth / generated / runtime state)

### Events
| Tên | Trigger khi | Listener dự kiến | Unsubscribe ở đâu |
|---|---|---|---|

### Layer assignment
| Việc | Layer chịu trách nhiệm |
|---|---|
Đối chiếu `standards/system-design.md §1`. Nếu một việc không rơi gọn vào layer nào → escalate.

### Dependency
- Depends on:
- Provides: (cái mà story sau sẽ dựa vào — đổi là escalate)

### Chưa rõ ở phần này
- [ ] (câu hỏi + phương án đề xuất)
```

**Confirm rồi mới sang Phần 2.**

---

### Phần 2 — Visual

```markdown
## [Feature] — Visual

### Mô tả visual tổng thể
Trông thế nào, cảm giác muốn đạt.

### Prefab structure
Theo contract `standards/folder-structure.md §6`: logic ở root, visual ở child `View/`.

<ElementRoot>
├── Domain / Visual controller
├── Collider (nếu cần picking)
└── View/  ← child duy nhất chứa phần nhìn thấy được

Prefab giữ **số slot tối đa**; runtime bật/tắt theo data.

### Animation / Tween
| Tên | Trigger | Duration | Easing | Preset id | Ghi chú |
|---|---|---|---|---|---|

Duration/easing **đọc từ TimingProfile/MotionProfile**, component chỉ giữ preset id.
Chuyển động phức tạp → dùng vocabulary ở `knowledge/motion/vocabulary.md`.

### Material / Shader
- Shared material từ PrefabProfile; khác biệt per-instance qua MaterialPropertyBlock.
- Property nào ghi qua MPB — liệt kê hết, ghi **cùng một hàm** (xem code-style §8).

### VFX / Audio
| Cue | Trigger | Pooled? |
|---|---|---|

### Text (nếu có)
Qua prefab TMP có script quản lý (`Init` / `Show` / `Hide` / feedback), pooled.

### Trạng thái đọc được
| State | Người chơi nhận ra bằng gì |
|---|---|
Mỗi state phải phân biệt được **bằng mắt**, không chỉ trong code.

### Responsive / mobile
- Safe area, aspect ratio, số phần tử hiển thị đồng thời theo màn hình.

### Polish checklist
- [ ] ...

### Chưa rõ ở phần này
- [ ]
```

**Confirm rồi mới sang Phần 3.**

---

### Phần 3 — Editor setup

Chỉ viết nếu feature cần tooling cho GD/level designer. Không cần thì ghi *"Không cần Editor Setup"*
và bỏ qua.

Feature là **level editor** hoặc tool lớn → dùng `skills/level-editor/` thay cho phần này.

```markdown
## [Feature] — Editor Setup

### Mục đích
GD cần làm được gì với tool này?

### Loại tool
- [ ] EditorWindow (UI Toolkit)  - [ ] Custom Inspector  - [ ] SO config  - [ ] MenuItem  - [ ] Gizmo

### GD workflow
1. ... (thứ tự thao tác thật của GD, không phải thứ tự field trong data)

### Data flow
Document ↔ level JSON ↔ runtime. Ai đọc, ai ghi, convert ở đâu.

### Validation
| Rule | Severity | Xử lý |
|---|---|---|
Blocking chặn save, warning cho save. Message viết bằng ngôn ngữ GD.

### GD workflow target
> GD làm được [mục tiêu cụ thể] trong [X phút] mà không cần hỏi dev.
```

---
