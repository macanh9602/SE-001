# Usability review procedure

## Chuẩn bị

- Dùng một project state có thể reset.
- Ghi window size, Unity version và document fixture.
- Chuẩn bị một file hợp lệ, một file malformed và một document dirty.
- Bật recording/screenshot để từng finding có evidence.
- Không đọc hướng dẫn sản phẩm trước pass First use.

## Pass 1: First use

Task tối thiểu: mở window, tạo document, tìm Save, thêm một entity, chọn entity, sửa một field,
nhận ra validation và thoát an toàn.

Quan sát:

- Có biết tool này dùng để làm gì trong 10 giây đầu không?
- New/Open/Save có vị trí và label rõ không?
- Empty state nói bước tiếp theo hay chỉ để trống?
- Primary action có nổi bật hơn action phụ không?
- Terminology có cần biết code hoặc schema không?

## Pass 2: Core workflow

Task: blank → cấu hình nền → thêm phần tử → move/edit → tạo issue → click issue để focus →
sửa issue → Save → reopen → Play Test nếu có.

Đo friction bằng số click, đổi pane, modal thừa, field phải tìm, và lần user bị mất context.
Kiểm tra một gesture kéo chỉ tạo một undo entry và local edit không làm mất selection/zoom.

## Pass 3: Stress and recovery

Task: resize 640px, kéo splitter min/max, scroll panel dài, domain reload, malformed open,
dirty close, undo/redo sau structural edit, hide/ghost selection và recovery sau blocking issue.

Ghi rõ tool có giữ được document, selection, mode, pane width, scroll và dirty state hay không.

## Finding format

```text
Severity | Task | Friction | Expected | Evidence | Fix direction
```

Severity:

- Blocking: không hoàn thành task hoặc có nguy cơ làm hỏng data.
- High: hoàn thành được nhưng user dễ mắc sai hoặc mất context.
- Medium: friction lặp lại, tăng chi phí thao tác.
- Low: cải thiện clarity, không chặn workflow.

## Exit criteria

- Cả ba pass đã thao tác thật.
- 14 chiều bắt buộc đã có verdict.
- Finding có evidence, không dựa trên phỏng đoán.
- PENDING và limitation được ghi rõ.
