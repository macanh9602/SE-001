# Canonical detail moved from SKILL.md

## 0. Trước khi hook

Đọc đúng causal slice, không scan toàn project:

`input/trigger → domain decision → reservation/ownership → mutation → presentation/handoff → cleanup`

Tra `standards/anti-patterns.md §B` và skill `presentation-lifecycle/` nếu có pool/async/tween.

Viết nội bộ 2–5 hypothesis; mỗi hypothesis phải có **một dấu hiệu phân biệt được trong audit**. Không
cần hỏi dev duyệt hypothesis.

---

## 1. Instrumentation ownership

Agent tự làm đủ vòng sau:

```
Bug report
  ↓
inspect targeted code
  ↓
identify causal boundaries + hypotheses
  ↓
add targeted audit hooks
  ↓
compile/source-check
  ↓
"Audit ready — repro case lỗi một lần rồi nhắn 'đã repro'."
```

Không dừng ở việc đưa snippet để dev tự chèn. Không bắt dev copy/paste console log.

Nếu project đã có reusable `AgentDebugAudit`, dùng nó. Nếu chưa có, tạo một helper generic nhỏ ở
`Assets/_Core/Scripts/Diagnostics/AgentDebugAudit.cs`; helper sống lại qua bug sau, hook cụ thể thì có
thể tháo sau khi fix.

---
