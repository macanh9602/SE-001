# Tuning reference

## Interrupt policy — chỗ hay sinh bug nhất

Mọi tween phải khai báo trước: **bị gọi lại khi đang chạy thì làm gì?**

| Policy | Nghĩa | Dùng khi |
|---|---|---|
| `restart` | kill tween cũ, chạy lại từ đầu | feedback ngắn, lặp nhanh (nút bấm) |
| `ignore` | đang chạy thì bỏ qua lệnh mới | anim quan trọng không được cắt |
| `queue` | xếp hàng chạy sau | chuỗi có thứ tự |
| `blend` | tiếp tục từ giá trị hiện tại | di chuyển liên tục, đổi đích giữa đường |

Luật cứng:

- Tween **phải bị kill khi element về pool / level unload** — nếu không, tween tiếp tục ghi vào
  transform của object đã tái sử dụng. Đây là bug "object nhảy loạn sau khi respawn".
- Pooled visual dùng contract hai đầu:
  **Release** = kill/cancel + invalidate owner/generation + normalize state an toàn;
  **Acquire/Bind** = ghi lại đầy đủ scale/rotation/color/visibility/outline/count từ data hiện tại.
  Acquire không được tin rằng Release trước đã chạy hoàn hảo.
- Tween/task completion cũ chỉ được settle/reset nếu `operationId/generation` vẫn current; motion A bị thay bởi B không được clear state của B.
- Domain **không đợi tween**. Domain giữ nhịp bằng scheduler của nó (`standards/system-design.md §6`);
  Visual chạy tween song song. Nếu gameplay phải đợi anim xong ⇒ đó là **duration trong TimingProfile**,
  không phải callback của tween.

---

## Feedback tức thì vs anim chính

Luật cảm giác quan trọng nhất trên mobile: **phản hồi phải xuất hiện trong frame đầu tiên.**

Tách hai thứ:

| Lớp | Độ trễ | Ví dụ |
|---|---|---|
| `immediate cue` | 0 frame | đổi màu, scale punch nhỏ, audio click, haptic |
| `main motion` | 0.15–0.5s | di chuyển, xoay, bay tới đích |

Chỉ có `main motion` mà không có `immediate cue` ⇒ dev sẽ nói *"chậm phản hồi"* kể cả khi duration
đã ngắn.

---

## Ngân sách thời lượng (điểm khởi đầu, không phải luật)

| Loại | Duration gợi ý |
|---|---|
| micro feedback (nút, highlight) | 0.08 – 0.15s |
| element di chuyển ngắn | 0.2 – 0.35s |
| element bay xa / theo curve | 0.35 – 0.6s |
| chuyển state UI/panel | 0.2 – 0.3s |
| celebration / win sequence | 0.8 – 2.0s (có skip) |
| stagger giữa các phần tử | 0.03 – 0.08s/phần tử |

Trên mobile, người chơi lặp lại hành động hàng trăm lần ⇒ **thà ngắn hơn 20% còn hơn dài hơn 20%**.
Mọi sequence dài > 1s phải **skip được bằng tap**.

---

## Bẫy hay gặp

| Bẫy | Hậu quả |
|---|---|
| hardcode duration trong script | GD không tune được, mỗi chỗ một số |
| mỗi element một preset riêng | game mất nhất quán, không nhận ra "ngôn ngữ chuyển động" |
| tween không kill khi pool/unload | object nhảy loạn, ghi đè transform |
| domain `await` tween của visual | gameplay khựng khi tween bị cắt; test không chạy được headless |
| chỉ chỉnh easing mà không chỉnh duration | vẫn "chưa đã" — hai biến này phải chỉnh cùng nhau |
| thêm juice khắp nơi cùng lúc | rối mắt, không biết cái nào tạo hiệu quả |
| so sánh phương án bằng lời | mỗi bên hình dung một kiểu, chốt xong vẫn sai |
| anim dài trong vòng lặp chính | phá nhịp, người chơi sốt ruột ở lần thứ 50 |

---
