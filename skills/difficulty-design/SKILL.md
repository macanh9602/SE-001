---
name: difficulty-design
description: >
  Thiết kế và kiểm soát độ khó — từ đo lường độ khó thật, tới sinh level, tới đường cong khó theo
  tiến trình. KÍCH HOẠT khi: "level dễ quá / khó quá", cần level generation, cần difficulty curve,
  DDA, booster trigger, cân bằng, hoặc cần trả lời "level này khó bao nhiêu". Nguyên tắc lõi:
  Instrument → Evaluate → Generate. Không đoán độ khó.
---

# SKILL: difficulty-design

Luật lõi, theo đúng thứ tự, **không được đảo**:

```
1. INSTRUMENT  — đo được độ khó của một level đã có
2. EVALUATE    — chấm điểm & phân loại level, đối chiếu với dữ liệu thật
3. GENERATE    — sinh/mutate level nhắm vào điểm số mục tiêu
```

Nhảy thẳng sang GENERATE khi chưa có thước đo là lỗi đắt nhất trong mảng này: sinh ra hàng nghìn
level mà **không biết chúng khó bao nhiêu**, rồi tune bằng cảm giác.

---

## Bước đầu tiên — đọc context

`knowledge/difficulty/metric-vocabulary.md` (từ vựng đo lường) ·
`knowledge/difficulty/pipeline-patterns.md` (khung Producer/Mutator/Validator) ·
`Docs/data-model.md` (level data hiện tại) · `Docs/glossary.md`.

Skill liên quan: `gd-communication` (GD sẽ đọc số này — dễ đọc quá đà), `game-design-advisor` và
`ship-level-design` nếu project có.

---

## Core flow

INSTRUMENT → EVALUATE → GENERATE. Không được đảo thứ tự; không đoán độ khó.
- Instrument level đã có bằng bot/simulator versioned, deterministic seed và golden test.
- Evaluate bằng metric có điều kiện đo, weights giải thích được và calibration với dữ liệu thật.
- Generate chỉ sau khi có target score; validate solvability và progression phase.

## Output
1. Difficulty definition — 2–3 trục và glossary.
2. Versioned bot/simulator — botVersion, policy, seed và golden test.
3. Measurement report — passRate/avgMoves/nearMiss và điều kiện đo.
4. Weights/bands decision — công thức, band và lý do.
5. Visualiser — so sánh curve/option khi cần.
6. Generic harvest — pattern reusable → knowledge/difficulty hoặc skill.

## Detail routes

- `refs/bot-and-metrics.md` — bot, human model, golden test, batch metrics.
- `refs/evaluation-and-calibration.md` — evaluation, score, bands, calibration.
- `refs/generation-and-dda.md` — generation, phase windows, curves, DDA, booster.
