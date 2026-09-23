---
name: editor-ux-review
description: Review an authoring tool for GD usability after implementation.
---

# SKILL: editor-ux-review

## Khi nào dùng

Dùng sau khi một EditorWindow/authoring tool đã có workflow chạy được. Đây là skill review,
không thay cho `level-editor` implementation guidance.

## Nguyên tắc

Review bằng thao tác thật trong Editor, không suy đoán từ code. Mỗi finding phải mô tả task,
friction quan sát được, hậu quả và hướng sửa. Không ghi "xấu" nếu không chỉ ra impact tới task.

## Quy trình 3 pass

1. **First use**: mở tool không đọc tài liệu, tạo một document tối thiểu và tìm các action chính.
2. **Core workflow**: hoàn thành workflow authoring từ blank tới save/play-test, gồm sửa validation.
3. **Stress and recovery**: resize khoảng 640px, pane scroll, drag/undo, malformed file,
   domain reload, reopen, dirty close và recover sau lỗi.

Mỗi pass ghi màn hình/kích thước, task, expected, observed, impact và evidence. Không chuyển
`PENDING` thành `PASS` nếu chưa thao tác được.

## 14 chiều bắt buộc

1. Task completion
2. Discoverability
3. Information architecture
4. Interaction quality
5. Error recovery
6. Visual hierarchy
7. Control sizing and density
8. Semantic color
9. Icon usage
10. Frequency-based emphasis
11. Responsive layout
12. Accessibility/readability
13. GD terminology
14. First-use without documentation

## Routing

- Dùng `refs/usability-review.md` cho quy trình và format finding.
- Dùng `refs/visual-hierarchy.md` cho ưu tiên thông tin.
- Dùng `refs/editor-visual-system.md` cho role, màu, emphasis và icon.
- Dùng `refs/first-use-review.md` cho first-use task.
- Dùng `refs/review-checklist.md` để chốt từng mục PASS/FAIL/N/A/PENDING.

## Output

```text
Severity | Task | Friction | Expected | Evidence | Fix direction
```

Kết thúc bằng số issue theo severity, các mục PENDING và known limitations. Không tự sửa tool
trong lúc review trừ khi task yêu cầu một fix riêng.
