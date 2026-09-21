---
name: gd-communication
description: >
  Giải thích cơ chế, số liệu và trade-off cho GD/PM/GĐ — người không đọc code. KÍCH HOẠT khi: cần
  giải thích một cơ chế cho GD, viết tooltip, chốt open question với GD, GD đọc số liệu sai, hoặc
  chuẩn bị brief/báo cáo cho người ngoài team code. Nguyên tắc: nói cái họ quyết được, và nói rõ
  mỗi chỉ số KHÔNG nói lên điều gì.
---

# SKILL: gd-communication

Sai lầm mặc định của agent: giải thích **cách hệ thống hoạt động**. GD cần biết **họ quyết được gì
và hệ quả là gì**.

---

## Bốn luật

1. **Bắt đầu bằng quyết định, không bằng cơ chế.**
   ✗ *"Hệ thống dùng weighted score normalize theo tập level..."*
   ✓ *"Anh cần chốt: level 15 nên nằm band Normal hay Hard? Chọn Hard thì ~35% người chơi sẽ fail
   lần đầu."*

2. **Mỗi chỉ số đưa ra phải kèm cái nó KHÔNG nói lên.** Đây là chỗ GD đọc quá đà và ra quyết định sai.
   Ví dụ: *"passRate 62% — nói lên độ khó cảm nhận, KHÔNG nói lên level có vui hay không, và KHÔNG
   nói lên vì sao khó (xem cột lý do thua)."*

3. **Dùng đúng tên trong `glossary.md`.** Một khái niệm một tên, trong code, trong tooltip, và trong
   lời nói. Đặt tên mới giữa cuộc họp là cách chắc chắn tạo hiểu nhầm về sau.

4. **Có yếu tố không gian / chuyển động / phân bố số → dựng visualiser, đừng viết dài.**
   (`workflow/ask-and-visualise.md`)

---



## Detail routes

- `refs/output-templates.md`

## Đầu ra

- Brief/tài liệu cơ chế (khung ở trên) → `handoff/<story>/gd-brief.html` hoặc `.md`.
- Bảng trắc nghiệm chốt quyết định (+ visualiser nếu cần).
- Tooltip/text đã viết, đặt đúng chỗ.
- Từ mới hoặc từ đổi nghĩa → cập nhật `Docs/glossary.md` **trong cùng story**.
