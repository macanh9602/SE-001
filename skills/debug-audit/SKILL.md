---
name: debug-audit
description: >
  Runtime bug investigation for Unity bằng targeted file-based instrumentation. KÍCH HOẠT ngay khi
  user báo bug intermittent, sai state/order/ownership, visual-runtime desync, pooling/async race,
  reload bug, rapid-input bug, hoặc nguyên nhân chưa được chứng minh rõ bằng static inspection.
  Agent tự inspect causal path, tự hook audit vào code, compile, rồi chỉ nhờ dev manual repro. Sau
  khi dev nói "đã repro", agent tự đọc file audit .md và tìm first incorrect transition trước khi fix.
---

# SKILL: debug-audit v2 — agent-owned instrumentation

## Contract lõi

1. **Agent instruments; dev không thiết kế log.**
2. **Một repro = một file `.md`**, overwrite/clear ở đầu level-load/repro boundary.
3. **File only; không spam Console.** Không `Debug.Log` chỉ để báo audit path.
4. **Log transition/decision, không log mỗi frame.**
5. Dev chỉ manual test và nói **`đã repro`**.
6. **Không fix khi chưa có evidence.** Evidence chưa đủ → agent tự thêm hook hẹp hơn rồi nhờ repro lại.

Bug hiển nhiên bằng static code inspection (null guard thiếu, off-by-one rõ, condition đảo) có thể fix
thẳng. Bug state/timing/ordering mà chưa chứng minh được thì instrument **ngay từ vòng đầu**, không đợi
fix sai một lần.

---

## 6. Sau khi dev nói `đã repro`

Agent tự tìm channel đang active và đọc **toàn bộ file audit**. Không hỏi dev gửi lại log nếu file
nằm trong workspace/project mà agent truy cập được.

Phân tích theo thứ tự:

1. dựng timeline từ đầu repro;
2. tìm **first divergence** — event đầu tiên khác expected contract;
3. lùi một boundary để tìm writer/decision gây sai;
4. đối chiếu code ở đúng boundary;
5. phân loại evidence:
   - `PROVEN` — log chứng minh root cause;
   - `STRONG` — chỉ còn một hypothesis hợp lý nhưng thiếu một transition;
   - `INSUFFICIENT` — chưa phân biệt được hypothesis.

`INSUFFICIENT` ⇒ tự hook thêm đúng 1–3 boundary còn thiếu rồi nói ngắn: *"Đã thêm audit hẹp hơn ở
reservation→consume. Repro lại một lần."* Không đưa speculative fix.

---

## 7. Khi evidence đủ

Trả lời ngắn theo:

**Root cause → Evidence → Fix → Regression risk → Manual verify**.

Trích vài event đủ chứng minh, không dump cả file. Nếu fix nằm trong autonomy của story thì implement
luôn; chỉ escalate nếu chạm gameplay semantics / project contract / serialization / scope.

Sau fix, giữ audit bật cho **một lần verify lại** nếu bug intermittent. Khi verify ổn:

- disable/remove temporary hooks;
- giữ reusable helper;
- harvest `triệu chứng → nguyên nhân → cách tránh` vào `standards/anti-patterns.md` nếu generic.

---

## Quick patterns

| Triệu chứng | Hook trước |
|---|---|
| object sai sau respawn/reload | release → acquire → bind generation → async completion |
| nhanh tay mới lỗi | command accepted → reservation → mutation identities |
| visual đúng lúc đầu rồi bật ngược | visual write + async owner/generation completion |
| planner chọn đúng nhưng consume sai | candidate → selected stableId → actual consumed stableId |
| slot logic rảnh nhưng visual overlap | logical assignment + physical readiness + handoff |
| lỗi biến mất khi thêm nhiều log | giảm hook, chỉ transition; nghi timing/race |



## Detail routes

- `refs/instrumentation-contract.md`
- `recipes/targeted-audit-hook.md`

## Đầu ra vòng 1

Không cần báo hypothesis dài. Chỉ báo:

- đã hook vùng nào;
- audit channel/file;
- flag đang bật;
- dev cần repro thao tác gì.

Sau đó chờ đúng một việc từ dev: **manual repro**.
