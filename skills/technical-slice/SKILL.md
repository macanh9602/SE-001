---
name: technical-slice
description: >
  Dựng lát cắt kỹ thuật mỏng nhất để chứng minh một rủi ro lớn là làm được (hoặc không) TRƯỚC khi
  xây feature đầy đủ. KÍCH HOẠT khi: procedural mesh, runtime generation, custom rendering, save/load
  architecture, editor foundation, hoặc bất cứ thứ gì "chưa biết có chạy nổi trên mobile không".
  Output là kết luận đo được + quyết định go/no-go, không phải feature hoàn chỉnh.
---

# SKILL: technical-slice

Một technical slice trả lời **đúng một câu hỏi**: *"Cách làm X có đáp ứng được ràng buộc không?"*

Không phải prototype gameplay. Không phải feature. Là **thí nghiệm có kết luận đo được**.

---

## Bước đầu tiên — đọc context

`Docs/project-context.md` · `standards/system-design.md` (slice sẽ nằm ở layer nào) ·
`standards/performance-budget.md` (ngưỡng phải đạt là bao nhiêu).

---

## Khi nào cần slice

| Dấu hiệu | Ví dụ |
|---|---|
| chưa ai trong team làm bao giờ | procedural mesh deform theo curve |
| chi phí sửa sai rất cao | format save/load, cấu trúc level data |
| nghi ngờ performance mobile | vài trăm object động, custom shader, mesh rebuild |
| là nền cho nhiều story sau | editor foundation, runtime state indexing |
| có >1 cách làm, chưa biết cách nào trụ được | mesh vs sprite vs particle |

**Không cần slice** khi: đã có tiền lệ trong project cũ chạy ổn, hoặc chi phí thử = chi phí làm thật.

---

## Luật của một slice

1. **Một câu hỏi, một slice.** Hai câu hỏi ⇒ hai slice, chạy nối tiếp.
2. **Có ngưỡng số trước khi code.** "Nhanh" không phải ngưỡng. `≤ 2ms/frame trên [thiết bị chuẩn]`
   mới là ngưỡng. Lấy từ `standards/performance-budget.md`.
3. **Đo trên thiết bị thật**, không phải Editor. Editor profiler chỉ để tìm hot spot, không để kết luận.
4. **Slice được phép xấu** — placeholder art, hardcode input, không UI. Nhưng **vẫn không được
   hardcode số liệu tune được** và **vẫn không `CreatePrimitive` / `new Material`**: nếu slice đi vào
   production thì đúng chỗ đó là nợ. Prefab placeholder vẫn là **prefab thật**.
5. **Slice có ngày hết hạn.** Quá budget thời gian mà chưa kết luận ⇒ đó cũng là một kết luận
   ("cách này không rẻ") ⇒ báo dev, hỏi trắc nghiệm hướng tiếp.
6. **Slice không tự thành feature.** Chuyển sang production ⇒ story riêng, có refactor pass.

---

## Core flow

1. Brief — one question, one smallest slice, numeric acceptance threshold before implementation.
2. Harness — build the smallest measurement harness, not production feature code.
3. Measure — run on the target device/profile and record evidence.
4. Conclude — compare against threshold and make go/no-go decision.

Hard rules:
- Measure CPU/GPU/memory/GC/draw calls relevant to the risk; do not guess.
- A slice is not a production feature and must not silently become one.
- If evidence is insufficient, report `PENDING` and the missing measurement.

## Output

- Slice brief and question.
- Harness and measurement evidence.
- Numeric result against threshold.
- Conclusion and go/no-go decision.

## Detail routes

- `refs/slice-templates.md` — detailed brief/conclusion templates and examples.
