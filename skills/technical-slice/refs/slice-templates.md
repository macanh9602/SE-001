# Slice templates and detailed examples

### Bước 0 — viết Slice Brief (trước khi mở Unity)

```markdown
# Slice: [tên]

## Câu hỏi cần trả lời
(một câu, có thể trả lời được bằng có/không hoặc bằng một con số)

## Vì sao hỏi bây giờ
Story nào bị chặn nếu không biết câu trả lời.

## Ngưỡng chấp nhận
| Chỉ số | Ngưỡng | Đo bằng gì | Thiết bị |
|---|---|---|---|

## Phương án đem thử
| # | Cách làm | Kỳ vọng | Rủi ro |
|---|---|---|---|

## Budget
Thời gian tối đa: ... Quá thì dừng và báo.

## KHÔNG làm trong slice này
- ...
```

Confirm brief với dev **trước khi code**. Đây là chỗ rẻ nhất để phát hiện mình đang trả lời sai câu hỏi.

### Bước 1 — dựng harness đo trước, feature sau

Thứ tự này quan trọng: **có thước đo trước, rồi mới có thứ để đo.**

- Scene test riêng, số lượng object cấu hình được (qua SO/field, không hardcode).
- Hiển thị số đo ngay trên màn hình (ms/frame, draw call, alloc/frame, mem) — dùng prefab TMP có
  script quản lý theo `standards/code-style.md §7`.
- Nút/slider để đổi tham số tại chỗ — mỗi lần build lại để đổi một số là một lần lãng phí.
- Stress mode: nhân số lượng lên 2×/5×/10× để tìm điểm gãy, không chỉ đo ở mức "đủ dùng".

### Bước 2 — chạy phương án, ghi số

Mỗi phương án ghi **cùng một bảng**, cùng thiết bị, cùng số lượng object:

| Phương án | ms/frame | draw call | GC alloc/frame | mem | ghi chú |
|---|---|---|---|---|---|

Không so số giữa hai lần đo khác điều kiện. Khác thiết bị / khác build config / khác số lượng ⇒
**không phải cùng một phép đo**.

### Bước 3 — kết luận

```markdown
## Kết luận slice

**Trả lời:** (đúng câu hỏi ở brief)

**Bằng chứng:** (bảng số + thiết bị + điều kiện đo)

**Điểm gãy:** vượt [ngưỡng] khi [điều kiện] — đây là trần thực tế.

**Chốt:** dùng phương án [X].

**Loại phương án nào, vì sao:** ...

**Đánh đổi đã chấp nhận:** ...

**Nợ kỹ thuật để lại:** (cái gì trong slice KHÔNG được bê thẳng vào production)

**Ràng buộc cho story sau:** (số lượng tối đa, cách tổ chức data, thứ không được làm)
```

Kết luận project-level ⇒ ghi `D-xxx` vào `Docs/decision-log.md` theo `workflow/decisions.md`.
Ràng buộc rút ra ⇒ cập nhật `Docs/project-context.md` (đánh dấu 🔒 nếu là contract toàn project).

---

## Bẫy hay gặp

| Bẫy | Hậu quả | Cách tránh |
|---|---|---|
| đo trong Editor rồi kết luận | số sai 2–10×, quyết định sai | build lên device |
| slice phình thành feature | mất tuần, vẫn không có kết luận | budget thời gian + phần "KHÔNG làm" |
| đo ở mức "đủ dùng" | không biết trần, gặp gãy ở level 40 | stress 2×/5×/10× |
| so hai phương án khác điều kiện | chọn nhầm | cùng scene, cùng device, cùng count |
| chỉ nhìn ms/frame | GC spike gây giật không thấy trong average | đo alloc/frame + frame time percentile |
| bê nguyên code slice vào production | hardcode + shortcut lan ra | story refactor riêng, liệt kê nợ ở kết luận |
| slice trả lời câu hỏi khác câu hỏi cần | tưởng an toàn, gãy sau | confirm brief trước khi code |

---
