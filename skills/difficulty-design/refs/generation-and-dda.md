# Generation and DDA

## Giai đoạn 3 — GENERATE

### 3.1 Ba tầng — Producer / Mutator / Validator

Chi tiết ở `knowledge/difficulty/pipeline-patterns.md`.

```
Producer  → sinh ứng viên (có seed)
Mutator   → biến đổi ứng viên để đẩy score về mục tiêu
Validator → loại ứng viên vi phạm rule cứng
```

Luật:

- **Validator không được sửa**, chỉ loại. Sửa trong validator ⇒ không ai biết level cuối khác gì bản gốc.
- **Producer phải có seed**, và seed ghi vào level data. Không tái tạo được level đã sinh là hỏng.
- **Retry có giới hạn**, và khi hết retry thì **báo lý do fail cuối cùng**, không trả về im lặng.
- **Giữ near-miss.** Ứng viên trượt sát ngưỡng là dữ liệu quý — lưu lại kèm lý do trượt để GD xem.
  Vứt thẳng ⇒ mất thông tin về việc ngưỡng đang quá chặt.

### 3.2 Phase windows — theo tiến trình, không theo index tuyệt đối

Rule dạng *"chướng ngại loại A chỉ xuất hiện ở nửa sau"* phải viết theo **tiến trình chuẩn hoá**
(`placementProgress ∈ [0,1]`), không theo số thứ tự tuyệt đối. Level dài ngắn khác nhau ⇒ index
tuyệt đối cho ra bố cục khác hẳn nhau.

### 3.3 Hai đường cong — **không bao giờ vẽ chồng lên nhau**

| Đường cong | Trục X | Nói về |
|---|---|---|
| **Pacing curve** | `turnProgress` — tiến trình *trong một lượt chơi* | nhịp trong một level |
| **Phase curve** | `placementProgress` — tiến trình *khi sinh level* | bố cục vật thể trong level |

Hai trục **khác nhau về bản chất**. Vẽ chung một biểu đồ ⇒ GD kết luận sai. Đây là lỗi đã xảy ra
thật. Nếu phải trình bày cùng trang ⇒ hai biểu đồ tách biệt, mỗi cái ghi rõ trục X là gì.

### 3.4 Difficulty curve theo tiến trình người chơi

| Yếu tố | Nguyên tắc |
|---|---|
| xu hướng chung | tăng dần, **không đơn điệu** |
| nhịp nghỉ | cứ 3–5 level khó thì một level dễ rõ rệt |
| spike | có chủ đích, đặt ở mốc (level tròn chục), không rơi ngẫu nhiên |
| level đầu | dạy cơ chế, không được fail — mỗi level dạy **một** thứ |
| cơ chế mới | ra mắt ở level dễ, khó lên sau 2–3 level |

### 3.5 DDA (nếu có) — luật an toàn

- DDA điều chỉnh **đầu vào của việc sinh level**, không sửa level đang chơi giữa chừng.
- Mọi điều chỉnh phải **log được**: người chơi này đang ở band nào, vì sao.
- Có **trần và sàn**. DDA không giới hạn ⇒ trôi về một trong hai cực.
- Có **chế độ tắt** để test và để so sánh A/B.
- Người chơi **không được cảm thấy** bị điều chỉnh — thay đổi nhỏ, chậm, không đảo chiều liên tục.

---

## Booster / trợ giúp — trigger

| Trigger | Chỉ số |
|---|---|
| fail liên tiếp | `consecutiveFails ≥ N` |
| gần thắng mà thua | `nearMissRate` cao ở level đó |
| đứng yên lâu | thời gian không thao tác |

Luật: booster xuất hiện **sau khi** thua, không phải trước — gợi ý trước khi thua làm người chơi
thấy bị coi thường và làm hỏng số liệu độ khó.

---

## Bẫy hay gặp

| Bẫy | Hậu quả |
|---|---|
| generate trước khi có thước đo | hàng nghìn level không biết khó bao nhiêu |
| bot không có version | so số đo giữa hai đời bot → kết luận sai |
| dùng solver để đo độ khó cảm nhận | mọi level đều "dễ" |
| nói "không giải được" khi chỉ là policy thua | loại nhầm level tốt |
| normalize theo tập khác nhau rồi so | xếp hạng sai |
| coi difficultyScore là thang tỉ lệ | "khó gấp 1.7 lần" — vô nghĩa |
| vẽ pacing curve chồng phase curve | GD đọc sai hoàn toàn |
| validator vừa loại vừa sửa | không ai biết level cuối khác gì bản sinh |
| vứt near-miss | mất dữ liệu về ngưỡng quá chặt |
| phase window theo index tuyệt đối | level dài ngắn khác nhau ra bố cục khác nhau |
| không lưu seed | không tái tạo được level bị báo lỗi |
| đưa số cho GD không kèm điều kiện đo | GD tin số sai |
| DDA không trần/sàn | trôi cực đoan |

---
