# Story [XXX] — [Tên nói được KẾT QUẢ, không phải chủ đề]

> Execution mode: **DIRECT / AUTONOMOUS_PACKETS / SINGLE_PACKET**
> Frontier collaboration: **INHERIT**

> Size: **S / M / L** — theo `workflow/loop.md`.
> Copy file này vào `handoff/story-XXX-<slug>/story.md`.
> Xoá mọi phần không dùng. Story để lại phần rỗng là story chưa viết xong.
> Story hoàn chỉnh phải **executable**: phần cuối luôn có `Implementation handoff`.
> Story size L / migration / high-risk / lower-model worker có thể kèm `worker/` packets.

---

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| | trích **đúng chỗ**: file:line, log, GDD, hoặc số đo | người chơi / GD chịu gì |

Không điền được cột **Bằng chứng** ⇒ chưa đủ dữ liệu để làm. Quay lại `enrich-context`.

## 1. Kết quả mong đợi

Một câu, quan sát được từ bên ngoài:

> Sau story này, [ai] có thể [làm gì] mà trước đó không làm được.

## 2. Ranh giới

**Làm:**
-

**KHÔNG làm trong story này:**
-

Phần "KHÔNG làm" là bắt buộc. Thiếu nó thì scope sẽ trôi và không ai biết lúc nào là xong.

## 3. Context cần đọc

- `Docs/project-context.md` · `standards/system-design.md` · `standards/code-style.md`
- Skill: `skills/<...>/SKILL.md`
- Story liên quan: `handoff/story-XXX/`
- Decision liên quan: `D-XXX`

Chỉ liệt kê context task này thật sự cần. Không yêu cầu worker load toàn bộ `skills/` hoặc toàn repo.

## 4. Đầu vào đã có

| Cái gì | Ở đâu | Trạng thái |
|---|---|---|

## 5. Việc cần làm

S không có story file: story contract + implementation handoff có thể nằm trực tiếp trong chat/handoff note.
S/M/L có story.md: embedded Implementation handoff nằm trong story.

> Sizing (`workflow/loop.md`):
> **S** — 3–6 gạch đầu dòng, agent tự quyết cách làm.
> **M** — có layer/contract rõ; thường implement trực tiếp từ story.
> **L / migration / high-risk / lower-model worker** — chia phase; nếu execution cần nhiều local instruction thì tạo `worker/` packets thay vì nhét HOW dài vào story.

### 5.1 Layer assignment

| Việc | Layer | Ghi chú |
|---|---|---|
| | Domain / Simulation / Visual / Data / Editor / HUD | |

Việc nào không rơi gọn vào một layer ⇒ **escalate**, đừng tự đặt bừa.

### 5.2 Số liệu tune được

| Giá trị | Nằm ở đâu | Default | Ai chỉnh |
|---|---|---|---|

Mọi số phải có chỗ ở: **prefab field · Profile SO · level data**. Không hardcode (`AGENTS.md §5`).

### 5.3 Contract mới (nếu có)

| Contract | Ai dùng | Đổi sau này tốn gì |
|---|---|---|

## 6. Acceptance criteria

Viết dạng **kiểm chứng được**, không phải "hoạt động tốt".

- [ ]
- [ ] **Performance:** [chỉ số] ≤ [ngưỡng] trên [thiết bị chuẩn], đo ở [cấu hình nặng nhất]
- [ ] Load → unload → load lại 10 lần: không rò object, không rò event
- [ ] Không hardcode số; không `CreatePrimitive`; không `new Material` / `Shader.Find`

Chỉ giữ acceptance phù hợp story; xoá dòng generic không liên quan.

## 7. Cần hỏi trước khi làm

- [ ] Q1 — ... → phương án đề xuất: ... (hỏi bằng **trắc nghiệm**, có visualiser nếu liên quan
      không gian / chuyển động / timing / phân bố số)

Story còn open question dạng **blocker** ⇒ chưa được bắt đầu code.

## 8. Rủi ro

| Rủi ro | Dấu hiệu sẽ thấy | Ứng phó |
|---|---|---|

---

## 9. Khi implement

- Vừa code vừa cập nhật `implementation-notes.html` — ghi ngay, đừng để cuối mới nhớ lại.
- Gặp quyết định/trade-off spec chưa định ⇒ **DỪNG, hỏi trắc nghiệm**, rồi ghi vào notes.
- Decision local, reversible, không đổi contract/gameplay/data/scope ⇒ worker tự quyết, không hỏi.
- Quyết định project-level ⇒ `D-xxx` trong `Docs/decision-log.md`.
- Từ mới / đổi nghĩa ⇒ cập nhật `Docs/glossary.md` **trong cùng story**.
- Không re-plan story nếu contract đã rõ.
- Không mở rộng scope chỉ để "làm sạch" code xung quanh.

## 10. Verification

Điền theo `workflow/verification.md`. Trạng thái: PASS / FAIL / PENDING / PENDING RERUN /
PARTIAL / KNOWN BASELINE FAILURES / N/A.

| Hạng mục | Trạng thái | Bằng chứng (log / số đo / ảnh / bước đã chạy) |
|---|---|---|

**Không ghi PASS cho thứ chưa chạy thật.**

## 11. Harvest

Theo `workflow/harvest.md`:

- Bug/anti-pattern mới ⇒ `standards/anti-patterns.md`
- Pattern generic ⇒ `knowledge/`
- Bài học quy trình ⇒ `workflow/`
- Story quá chặt / quá lỏng ở đâu ⇒ ghi lại, dùng để chỉnh sizing

---

## 12. Implementation handoff

> Section này làm cho story **chạy được ngay** mà không cần user đi tìm/copy một prompt khác.
> `handoff/RUN-STORY-PROMPT.md` vẫn là generic launcher/fallback.

### Execution mode

Chọn **một**:

- `DIRECT` — default cho S/M khi story đủ rõ.
- `AUTONOMOUS_PACKETS` — dùng cho L, migration, high-risk, hoặc lower-model worker khi cần instruction locality.
- `SINGLE_PACKET` — chỉ khi debug/risk cao và dev/frontier model yêu cầu.

### Frontier collaboration

- `INHERIT` — default; dùng project mode trong `Docs/project-context.md`.
- `AUTO` — story override, router tự quyết theo risk/size.
- `OFF` — story tuyệt đối local.
- `REQUIRED` — không chạy frontier-less; provider fail sau retry thì BLOCKED.

### Prompt implement

```text
Chạy story hiện tại.

Source of truth:
`handoff/story-XXX-<slug>/story.md`

WORKER MODE:
- Story là contract; không redesign hoặc re-plan nếu contract đã rõ.
- Đọc đúng Context cần đọc + skill được story chỉ định. Không scan toàn repo.
- Không invent requirement.
- Decision local/reversible, không đổi contract/gameplay/data/scope → tự quyết và ghi ngắn vào implementation-notes.
- Chỉ escalate khi có blocker/mâu thuẫn contract hoặc decision chạm project architecture, gameplay semantics, serialization/data compatibility, hard performance budget, hoặc story scope.
- Mọi tunable → prefab field / Profile SO / level data. Không hardcode.
- Vừa implement vừa cập nhật `implementation-notes.html`.
- Tự verify tới khi acceptance có evidence thật.
- Không claim PASS/DONE nếu chưa chạy verification tương ứng.

FRONTIER COLLABORATION:
- Đọc story override `INHERIT / AUTO / OFF / REQUIRED`; `INHERIT` hoặc field thiếu dùng project default trong `Docs/project-context.md`.
- Follow `workflow/frontier-collaboration.md`.
- Không nhập hoặc lưu connector name trong story/project; provider tự map current workspace.
- `AUTO` fail preflight sau một repair/retry → local fallback và ghi reason.
- `REQUIRED` fail preflight sau một repair/retry → BLOCKED; chỉ hỏi user nếu cần auth/consent.

EXECUTION:
- Nếu `Execution mode = DIRECT`:
  implement toàn story trực tiếp.

- Nếu `Execution mode = AUTONOMOUS_PACKETS` và folder `worker/` tồn tại:
  chạy packet theo thứ tự tên/file.
  Sau mỗi packet:
  1. chạy packet verification;
  2. chạy lint/check liên quan;
  3. inspect scoped diff;
  4. update implementation-notes;
  5. nếu PASS và `Semantic deviations = NONE` → tự chuyển packet tiếp.
  Không chờ human approval giữa packet.

- Nếu `Execution mode = SINGLE_PACKET`:
  chỉ chạy packet được chỉ định rồi dừng sau verification.

CHỈ DỪNG nếu:
- verification FAIL và không thể tự sửa local;
- semantic deviation != NONE;
- story/standard/contract mâu thuẫn;
- cần đổi project architecture/gameplay/data/scope;
- cần drop semantics đã có;
- xuất hiện unexpected runtime/gameplay change ngoài scope.

FINAL REPORT:
- Observable result
- Files changed
- Verification evidence
- Semantic deviations
- Open risks / debt
- Final status: DONE / DOING / BLOCKED
```

### Worker packets — chỉ khi cần

Nếu dùng `AUTONOMOUS_PACKETS`:

- Tạo từ `templates/worker-packet.md`.
- Packet chứa **HOW/local execution**, không copy lại toàn story.
- Instruction quan trọng phải nằm gần step liên quan.
- Story vẫn là source of truth.
- Packet chỉ là self-verification boundary, **không phải human approval gate**.

## 13. Story closure

Story chỉ được DONE khi:

- [ ] Required Acceptance Criteria đều PASS hoặc N/A có lý do.
- [ ] Không còn required gate ở trạng thái FAIL / PENDING / PENDING RERUN / PARTIAL.
- [ ] Mọi PASS có evidence thật.
- [ ] implementation-notes.html có final verification evidence.
- [ ] Semantic deviations = NONE, hoặc deviation đã được dev approve và ghi canonical.
- [ ] Harvest đã chạy đủ 4 câu.
- [ ] Project-level decision đã vào Docs/decision-log.md.
- [ ] Open debt/risk còn lại đã được ghi vào canonical owner.
- [ ] Story Closure gates đã pass; ROADMAP update là final action sau PASS.
- [ ] Final diff không chứa unexpected scope/runtime change.

Nếu bất kỳ required gate chưa đủ:

Story = DOING hoặc BLOCKED; không được DONE.

Không cho phép chỉ ghi Final status: DONE trong implementation notes rồi coi là đóng story.

### CLOSE STORY

Sau implementation:

1. Run verification.
2. Resolve required acceptance statuses.
3. Update implementation-notes with actual evidence.
4. Run Harvest and apply canonical updates.
5. Record project-level decisions/debt/risk to canonical owners.
6. Run final diff/scope check.
7. Evaluate Story Closure gates.
8. Only after PASS, update handoff/ROADMAP.md and mark story DONE.

Only if every required closure gate passes: mark story DONE.
Otherwise keep story DOING/BLOCKED and report the exact remaining gate.
