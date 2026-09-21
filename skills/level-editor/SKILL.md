---
name: level-editor
description: >
  Dựng tool authoring cho GD/level designer bằng UI Toolkit — EditorWindow, canvas, inspector,
  validation, save/load. KÍCH HOẠT khi: "làm level editor", tool cho GD, authoring window, cần GD
  tự tạo/sửa level mà không nhờ dev. Nguyên tắc lõi: Document / ViewState / DerivedState tách bạch,
  và ApplyEdit ≠ ReloadDocument.
---

# SKILL: level-editor

Thước đo duy nhất của một level editor: **GD tự làm được một level hoàn chỉnh mà không hỏi dev.**
Không phải "tool có đủ field".

---

## Bước đầu tiên — đọc context

`Docs/data-model.md` (level data hiện tại) · `knowledge/editor-ux/update-model.md` ·
`knowledge/editor-ux/validation-surfacing.md` · `knowledge/editor-ux/responsive-workspace.md` ·
`refs/checklist.md` · `refs/anti-patterns.md` · `Docs/glossary.md` (tên GD dùng ≠ tên trong code).

---

## 9. Thứ tự làm — đừng đảo

```
1. Data model + save/load + version        ← chưa lưu được thì tool vô nghĩa
2. Hiển thị read-only (mở file, thấy level)
3. Sửa một loại phần tử, có undo
4. Validation
5. Các loại phần tử còn lại
6. Tiện lợi: copy/paste, multi-select, shortcut, template
7. Preview / mô phỏng trong editor
```

Làm canvas đẹp trước khi save/load chạy ⇒ luôn phải làm lại.

---

## 10. Đầu ra

1. Spec editor (Phần 3 của `spec-feature`, hoặc spec riêng nếu tool lớn).
2. Tool trong project + level file mẫu.
3. **Hướng dẫn cho GD** — ngắn, có ảnh, theo `skills/gd-communication/`.
4. Chạy `refs/checklist.md` trước khi giao GD.

Trước khi tuyên bố xong: **tự làm một level hoàn chỉnh bằng chính tool đó**, từ file trống tới file
chạy được trong game. Không làm bước này thì mọi lời "đã xong" đều là phỏng đoán —
`workflow/verification.md` yêu cầu UI walkthrough thật.

## Detail routes

- `refs/workflow-details.md`
