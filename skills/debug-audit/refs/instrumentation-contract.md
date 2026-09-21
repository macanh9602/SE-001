# Canonical detail moved from SKILL.md

## 2. File contract

Trong Unity Editor, audit nằm ở project root để coding agent đọc trực tiếp:

```text
AgentAudit/<channel>.md
```

Development Build có thể fallback sang `Application.persistentDataPath/AgentAudit/`.

Mỗi investigation có **một channel** ngắn, ví dụ:

```text
queue-drain-priority
strip-reload-visual
tray-delivery-order
```

`Begin(...)` phải overwrite file cũ ở đầu repro boundary. Với level-based game, hook mặc định là
**ngay khi LevelManager nhận request load**, trước cleanup level cũ, để cùng file capture được:

`old cleanup → pool release → new spawn/bind → stale async completion → gameplay`.

Không append nhiều lần chơi vào cùng file rồi bắt AI đoán phiên nào là phiên lỗi.

---

## 3. Toggle / build guard / budget

- Có toggle/flag rõ: `Enabled` hoặc per-channel enable.
- Chỉ hoạt động trong `UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Default budget: khoảng **500 event/repro**; agent được đổi khi case cần.
- Khi chạm budget phải append `AUDIT_TRUNCATED`, **không silently stop**.
- Sau khi fix: disable channel hoặc remove temporary hook. Giữ core helper reusable.

Instrumentation không được làm thay đổi gameplay timing đáng kể. Ưu tiên 5–15 hook chiến lược hơn
100 `Write()` rải khắp class.

---

## 4. Log **decision + reason**, không dump state vô nghĩa

Log yếu:

```text
slot=2; pieceCount=3
```

Log tốt:

```text
QueueDrain.SourceSelected
cake=Blue; sequence=17; slot=3; pieces=3
candidates=[seq15/slot2/p4, seq17/slot3/p3, seq20/slot4/p3]
decision=sequence17
reason=min pieces; tie-break higher slot index
```

Mỗi event nên có khi relevant:

- stable/domain id
- Unity instance id nếu pool/reuse quan trọng
- sequence/order id
- generation / operation id / ownership id cho async
- slot/index **chỉ như vị trí**, không thay stable id
- state before → state after
- decision + reason

Planner/mutator flow phải log **planned identity và actual mutated identity**. Async/pool flow phải log
**started generation/owner và current generation/owner** ở completion/finally.

---

## 5. Chỉ log transition / boundary

### Nên hook

- command accepted/rejected + reason
- candidate set → selected candidate
- reserve/release
- mutation request → mutation result
- ownership handoff
- pool acquire/release/rebind
- async start/replaced/cancelled/completed
- state transition `Ready ↔ Blocked`
- load begin / cleanup / spawn ready

### Cấm mặc định

```csharp
void Update() => Audit(...);
```

Không frame-by-frame position/progress dump. Nếu cần biết threshold crossing, log **một lần khi state
đổi** (`HoldBegin`, `HoldEnd`), không log `progress=.01/.02/...`.

---
