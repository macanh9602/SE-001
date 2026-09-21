# Roadmap — SE-001 Salt/Pepper Sand Clone

> Scope: clone core loop của `Salt & Pepper, Don't mix em up`; improvement duy nhất có chủ đích là **sand feel**.
> Excluded: SDK/ads/analytics/IAP và final art polish.

## Giai đoạn

- P1 Bootstrap: Story 000
- P2 Technical slice: Story 001
- P3 Core loop: Story 002–009
- P6 Feel pass (sand-specific, không final art): Story 010
- P4 Level editor: Story 011–012
- Meta/progression: Story 013
- Ship-prep performance gate: Story 014

## Story

| # | Tên | Size | Trạng thái | Phụ thuộc | Decision |
|---|---|---|---|---|---|
| 000 | Project contract + bootstrap ready | M | TODO | User Setup Gate | Identity / architecture |
| 001 | Powder sand technical slice proves feel + budget | M | TODO | 000 | Sand feasibility |
| 002 | Production sand simulation core | M | TODO | 001 | Simulation contract |
| 003 | Powder renderer + profiles | M | TODO | 002 | Visual/data |
| 004 | Level data + board runtime | M | TODO | 000,002 | Data/runtime |
| 005 | Player-drawn paths become dynamic obstacles | M | TODO | 004 | Input/simulation |
| 006 | Salt/pepper sources emit finite material streams | M | TODO | 003,004 | Spawner/simulation |
| 007 | Cups collect, separate and audit materials | M | TODO | 004,006 | Domain/accounting |
| 008 | Gameplay rules reach deterministic win/lose | M | TODO | 005,007 | Domain |
| 009 | Full playable loop: load → play → result → retry/next | M | TODO | 008 | Core loop/HUD |
| 010 | Sand interaction VFX: stream, impact, slide, edge-leave | M | TODO | 009 | Feel/presentation |
| 011 | Level editor can open/save core level data | M | TODO | 009 | Editor foundation |
| 012 | Level editor authoring + validation + preview | L | TODO | 011 | Editor UX |
| 013 | Progression/save + ordered level sequence | M | TODO | 009,011 | Meta without SDK |
| 014 | Performance + quality scaling + device gate | M | TODO | 010,012,013 | Ship prep |

## Mốc

| Mốc | Quan sát được | Story |
|---|---|---|
| Foundation ready | Project identity/contracts rõ, compile baseline sạch | 000 |
| Technical feasibility | Powder sand chạy với drawn obstacle và có số đo trên device | 001 |
| Sand runtime production | Simulation + render production, không legacy dependency | 002–003 |
| Gameplay slice | Vẽ path, source đổ, cup collect, win/lose đúng | 004–008 |
| Playable loop | Load → play → result → retry/next trên device | 009 |
| Sand feel pass | Stream/impact/slide/edge feedback bounded và pooled | 010 |
| Editor ready | GD tự tạo/sửa/validate/playtest level | 011–012 |
| Progression ready | Save level progress + next/retry | 013 |
| Non-SDK ship prep | Quality tier + stress/device gate đạt budget | 014 |

## Không nằm trong roadmap này

- SDK / ads / analytics / IAP / attribution.
- Final art polish, production environment art, final UI skin.
- Gameplay gimmick không có trong source-of-truth/reference đã xác nhận.

---

## Rủi ro đang theo dõi

| Rủi ro | Dấu hiệu sẽ thấy | Ứng phó | Trạng thái |
|---|---|---|---|

---

## Câu hỏi còn mở ở mức dự án

| Câu hỏi | Owner | Chặn cái gì | Hạn |
|---|---|---|---|

Câu hỏi trong phạm vi một story ⇒ `handoff/story-XXX/open-questions.html`, không để ở đây.

---

## Nợ kỹ thuật

| Nợ | Từ story | Vì sao chấp nhận | Phải trả khi |
|---|---|---|---|

Nợ từ technical slice ghi ở đây ngay khi slice kết thúc, đừng đợi tới lúc ship.
