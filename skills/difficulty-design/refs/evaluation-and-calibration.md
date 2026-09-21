# Evaluation and calibration

## Giai đoạn 2 — EVALUATE

### 2.1 Điểm khó = tổ hợp có trọng số, và **trọng số phải giải thích được**

```
difficultyScore = Σ wᵢ · normalize(metricᵢ)
```

- `normalize` phải nêu rõ dựa trên **tập level nào** — normalize theo tập A rồi so với số normalize
  theo tập B là lỗi kinh điển.
- Trọng số `wᵢ` là **quyết định thiết kế**, phải ghi `D-xxx` + lý do, không phải kết quả fit số.
- Score chỉ dùng để **xếp hạng và phân band**, không dùng để nói "level này khó gấp 1.7 lần".
  Thang này không phải thang tỉ lệ.

### 2.2 Phân band, đừng dùng số lẻ

GD làm việc bằng band (`Easy / Normal / Hard / Spike`), không bằng `0.63`. Chốt ngưỡng band một lần,
ghi vào decision-log, và **hiển thị band ở mọi nơi GD nhìn thấy**.

### 2.3 Hiệu chuẩn với dữ liệu thật

Có analytics ⇒ so `predicted passRate` với `actual passRate`:

| Kết quả | Nghĩa | Làm gì |
|---|---|---|
| tương quan tốt | thước dùng được | dùng để sinh level |
| lệch hệ thống (luôn dự đoán dễ hơn) | thiếu một trục khó | tìm trục còn thiếu, đừng chỉ chỉnh trọng số |
| lệch ngẫu nhiên | metric nhiễu / N quá nhỏ | tăng N, xem lại metric |

Chưa có dữ liệu thật ⇒ ghi rõ trong báo cáo: *"chưa hiệu chuẩn"*. Không im lặng để GD tưởng đã chuẩn.

---
