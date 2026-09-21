---
name: spec-feature
description: >
  Lên spec đầy đủ cho một feature Unity mobile — gameplay logic, visual, editor setup — đủ để giao
  agent implement mà không phải giải thích thêm. KÍCH HOẠT khi: "làm feature X", "spec cho ...",
  cần mô tả một tính năng trước khi code, hoặc biến yêu cầu GDD thành thứ implement được.
  Prototype/GDD chưa lock thì dùng bản rút gọn ở refs/spec-lite.md.
---

# SKILL: spec-feature

Output là **một spec**, không phải code. Spec mô tả **cái gì**, không mô tả **làm thế nào**.

---

## Bước đầu tiên — đọc context

`Docs/project-context.md` · `Docs/glossary.md` · `standards/system-design.md §1` (layer nào chịu
trách nhiệm phần nào) · `Docs/data-model.md`.

Chỉ hỏi những gì **không** có trong các file đó:

- Feature này thuộc layer nào, chạm layer nào khác?
- Có event / data / prefab / profile nào liên quan đã tồn tại?
- Có ảnh reference, mockup, video gameplay không?

Có video/ảnh ref → mô tả lại bằng English technical term theo mốc thời gian trước khi spec
(xem `skills/game-feel-motion/`).

Thiếu thứ nào không suy ra được → hỏi trắc nghiệm (`workflow/ask-and-visualise.md`).

---

## Bước cuối — assumptions & ready check

```markdown
## Assumptions
- (ghi rõ, để sau này biết chỗ nào là giả định)

## Open questions
- [ ] Q1 — ... → Owner: Dev / GD / Art

## Ready-to-implement checklist
- [ ] Gameplay logic đã confirm
- [ ] Visual reference đủ
- [ ] Editor workflow GD đã duyệt (nếu có)
- [ ] Dependency đã có hoặc có kế hoạch tạo
- [ ] Không còn open question blocker
- [ ] handoff/<story>/implementation-notes.html đã tạo
```

---

## Song song khi implement

Agent **vừa code vừa cập nhật** `implementation-notes.html` — ghi ngay khi phát sinh, không để cuối
mới nhớ lại. Khung: `templates/implementation-notes.html`.

> ⚠️ Quyết định / đánh đổi phát sinh giữa lúc code mà spec chưa định ⇒ **DỪNG, hỏi trắc nghiệm**
> (`enrich-context`) trước khi làm. Sau khi dev chọn → ghi vào notes. Đây là chốt chặn quan trọng
> nhất: tránh agent âm thầm chọn hướng sai rồi sửa đi sửa lại nhiều vòng.

---

## Lưu ý

- Không viết code trong spec.
- Không liệt kê tên class agent phải tạo — chỉ ghi `Required contracts`.
- Dùng đúng tên trong `glossary.md`; không tự đặt tên mới cho khái niệm đã có.
- Dev cung cấp ảnh/video ref → mô tả visual **từ ref đó**, không tự sáng tác thêm chi tiết.

## Detail routes

- `refs/full-spec-template.md`
