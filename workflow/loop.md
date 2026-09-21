# Workflow — vòng làm việc

> Không sửa file này khi làm game mới.
> Mục đích: giữ nhịp `requirement → decision → story → implement → harvest` để việc không bị gián
> đoạn, và người khác nhảy vào giữa chừng không cần đọc toàn bộ script.

---

## 1. Vòng tám bước

```text
   requirement (dev mô tả, có thể kèm video/ảnh)
        │
        ▼
   [1] CLARIFY    ── skills/enrich-context — tách BIẾT / ĐOÁN / CẦN HỎI
        │             hỏi trắc nghiệm có recommend
        ▼
   [2] SHOW       ── templates/visualiser-base.html · option-picker.html
        │             không gian · chuyển động · timing · nhiều biến → dựng .html TRƯỚC khi hỏi
        ▼
   [3] DECIDE     ── templates/decision-record.md → Docs/decision-log.md (D-xxx)
        │             ghi cả cái đã LOẠI và đánh đổi đã chấp nhận
        ▼
   [4] STORY      ── templates/story.md — goal · boundary · acceptance · evidence
        │             story hoàn chỉnh có embedded Implementation handoff
        ▼
   [5] IMPLEMENT  ── DIRECT hoặc AUTONOMOUS_PACKETS
        │             standards/code-style.md + implementation-notes.html
        │             gặp trade-off mới → quay lại [1], KHÔNG tự quyết project-level
        ▼
   [6] VERIFY     ── workflow/verification.md — compile · test · console · screenshot
        │
        ▼
   [7] HARVEST + CANONICAL UPDATES ── workflow/harvest.md → knowledge/ · skill · anti-patterns.md
        │
        ▼
   [8] CLOSE      ── evaluate gates + final diff/scope
        │
        └─ PASS → update ROADMAP → DONE
```

Implementation finished != Story DONE. Story chỉ DONE sau verification, harvest/canonical updates và closure gate pass; ROADMAP là final action của CLOSE.
Bước tốn thời gian nhất là **[1] và [2]**. Bỏ qua chúng là lý do số một khiến phải sửa đi sửa lại.

Frontier collaboration là sidecar của story, không phải execution mode mới: route theo story/project,
PLAN tối đa một lần trước implementation cho Story L/high-risk, REVIEW sau local verification. Preflight,
fallback và evidence theo `workflow/frontier-collaboration.md`; không gọi lại ở từng packet.

Đang ở một giai đoạn lớn của game (mở project · làm editor · làm difficulty · feel pass) →
`playbooks/` cho biết chuỗi story và gate giữa chúng. Vòng tám bước này chạy **bên trong** mỗi story.

---

## 2. Story sizing — vừa đủ, đừng chặt

Story mô tả **cái gì và đến đâu**, không mô tả **cách code**.

| Size | Định nghĩa | Story viết bao nhiêu | Execution mặc định |
|---|---|---|---|
| **S** | ≤ 1 buổi, không đổi contract | Goal + 3–5 acceptance. Có thể không cần file riêng | `DIRECT` |
| **M** | 1–2 ngày, chạm 1–2 layer | File story đầy đủ. In/Out scope rõ. Không liệt kê class | `DIRECT` |
| **L** | nhiều ngày, đổi contract, hoặc nhiều domain | File story + phases; mỗi phase ship/verify độc lập | `AUTONOMOUS_PACKETS` khi HOW dài hoặc dùng lower-model worker |

S không có story file vẫn phải có story contract + implementation handoff trong chat/handoff note; S/M/L có story.md dùng embedded Implementation handoff.

### Story contract vs Worker packet

```text
story.md
= WHY / GOAL / SCOPE / CONTRACT / ACCEPTANCE / VERIFICATION

worker/*.md
= local HOW / files / exact steps / code skeleton / packet verification
```

Worker packet **không phải human approval gate**.
Packet pass cũng không đồng nghĩa Story DONE; closure gate vẫn bắt buộc.

Trong `AUTONOMOUS_PACKETS`:
1. worker chạy packet;
2. self-verify + lint/check + inspect diff;
3. `Semantic deviations = NONE` ⇒ tự chạy packet tiếp;
4. chỉ dừng khi gặp stop condition trong story/packet.

Dùng packet khi:
- Story L.
- Migration/refactor có nguy cơ semantic drift.
- High-risk implementation.
- Lower-model worker cần instruction locality.
- Exact execution order quan trọng.

Không tạo packet chỉ để chia nhỏ một story đơn giản.

### Dấu hiệu story quá chặt — cắt đi

- Liệt kê tên class/method agent phải tạo → bỏ, chỉ giữ `Required contracts`.
- Mô tả từng thao tác Unity mà agent tự làm được qua MCP → bỏ.
- Lặp lại guardrail đã có trong `AGENTS.md` → bỏ, chỉ ghi guardrail **riêng** của story.
- Acceptance kiểu "code sạch" → không verify được; đổi thành evidence cụ thể.
- Nhét code skeleton dài vào story L → chuyển sang worker packet/recipe.

### Dấu hiệu story quá lỏng — bổ sung

- Không có `Out of scope` → scope creep.
- Acceptance không có `Evidence` → không ai biết lúc nào DONE.
- Goal có ≥ 2 cách hiểu → thêm một câu ví dụ cụ thể.
- Worker phải tự suy ra thứ tự implementation quan trọng → tạo packet.
- Rule quan trọng nằm rất xa step thực thi → đưa local rule vào packet.

### Cách viết phần "vì sao" — bảng ba cột

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| mô tả cái đang sai | trích **đúng chỗ** trong code / log / capture | người chơi hoặc GD chịu hậu quả gì |

Có bảng này thì agent hiểu **tại sao** và tự thiết kế được. Không có nó thì story phải dài gấp năm
lần để bù, và vẫn dễ trượt.

---

## 3. Cách hỏi dev — bắt buộc trắc nghiệm

Chi tiết ở `workflow/ask-and-visualise.md`. Tóm tắt:

```text
Q: <câu hỏi một dòng>

  A) <option>          ← RECOMMEND
     được: ...  mất: ...  ảnh hưởng: CPU / GC / draw call / maintainability
  B) <option>
     được: ...  mất: ...
  C) Other — dev tự nhập
```

- 2–4 option, luôn có "Other", luôn có **một** option recommend + lý do một câu.
- Batch tối đa 2–4 câu. Ưu tiên câu **phân biệt được nhiều hypothesis nhất**.
- Gộp các câu cùng "một lần mở Unity" vào chung batch.
- Câu hỏi không đổi kết luận → bỏ.
- Free-text chỉ khi xin **dữ liệu thô**: log, repro steps, screenshot, video, số đo.

Không bao giờ: tự chọn trade-off project-level rồi báo "tôi đã chọn X vì đơn giản hơn".

---

## 4. implementation-notes.html — nhật ký sống

Copy `templates/implementation-notes.html` vào `handoff/<story>/`. Ghi **ngay khi phát sinh**:

| Loại | Ghi cái gì |
|---|---|
| 🔵 Decision | quyết định không có trong story: chọn gì · vì sao · đã loại gì |
| 🟠 Deviation | làm khác story: khác ở đâu · lý do · ảnh hưởng |
| 🔴 Trade-off | được gì / mất gì / xem lại khi nào |
| 🟣 Open risk | giả định chưa chắc · chỗ dev nên tự verify · owner · next action |
| 🟢 Performance | CPU / GPU / GC / Memory / Measurement |
| ⚪ Verification | `Check · Status · Evidence` — theo `workflow/verification.md` |
| 🕘 Changelog | timestamp + thay đổi lớn + lý do |

Decision **project-level** thì thêm `D-xxx` vào `Docs/decision-log.md`.

---

## 5. Người mới nhảy vào giữa chừng

Một người (hoặc một agent session mới) phải hiểu được project bằng đúng **sáu file**, không đọc code:

```text
Docs/project-context.md
standards/system-design.md
Docs/runtime-architecture.md
Docs/data-model.md
Docs/decision-log.md
handoff/ROADMAP.md
```

(+ `Docs/glossary.md` khi gặp một từ không chắc nghĩa.)

Sáu file này không đủ → đó là **bug của tài liệu**, sửa tài liệu trước khi sửa code.

---

## 6. Khi dev đưa video / ảnh reference

1. Mô tả lại bằng **English technical term** cái mình thấy.
2. Chỉ rõ mốc thời gian.
3. Tách **cái quan sát được** khỏi **cái đang suy đoán**.
4. Dựng visualiser mô phỏng lại → hỏi trước khi code.
5. Không tự sáng tác thêm chi tiết không có trong ref.

Chi tiết: `skills/game-feel-motion/`.

---

## 7. Cuối mỗi story

Story DONE chỉ sau verification + closure + harvest + canonical updates + roadmap. Nếu còn PENDING/FAIL/PARTIAL thì giữ DOING hoặc BLOCKED.

Chạy `workflow/harvest.md`, trả lời bốn câu, ghi vào final report:

- [ ] Pattern nào ở story này **không dính game cụ thể**? → `knowledge/` hoặc `standards/system-design.md`
- [ ] Câu hỏi nào phải hỏi lại lần thứ 2+? → mục trong `Docs/project-context.md`
- [ ] Bug loại nào lặp lại? → một dòng trong `standards/anti-patterns.md`
- [ ] Tween/motion/metric nào dùng lại được? → `knowledge/motion/presets.json` hoặc `knowledge/difficulty/`

Cuối project chạy harvest một lần cho toàn bộ → cập nhật Template/ gốc.
