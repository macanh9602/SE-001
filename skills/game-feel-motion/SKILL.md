---
name: game-feel-motion
description: >
  Biến mô tả cảm tính về chuyển động ("chưa đã tay", "cứng quá", "giống video này") thành spec
  chuyển động implement được — bằng English technical term + visualiser để hai bên hình dung cùng
  một thứ. KÍCH HOẠT khi: bàn về anim/tween/movement của element, có video/gif reference, juice pass,
  "animation này chưa ổn", hoặc cần chọn easing/timing/curve. Dùng kèm knowledge/motion/.
---

# SKILL: game-feel-motion

Vấn đề cốt lõi: **"chưa đã tay" không implement được.** Skill này tồn tại để dịch cảm giác sang
từ vựng chung, rồi sang số.

Ba luật:

1. **Luôn trả lời bằng English technical term** (`anticipation`, `overshoot`, `settle`, `ease-out
   back`, `squash & stretch`), kèm giải thích ngắn tiếng Việt. Term lấy từ `knowledge/motion/vocabulary.md`.
2. **Luôn dựng visualiser trước khi hỏi** — chuyển động không mô tả được bằng lời.
   Nền: `knowledge/motion/playground.html`.
3. **Mọi số cuối cùng nằm ở `MotionProfile`/`TimingProfile`**, component chỉ giữ `preset id`.
   Không hardcode duration/easing trong code. (`AGENTS.md §5`)

---

## Bước đầu tiên — đọc context

`knowledge/motion/vocabulary.md` (từ vựng) · `knowledge/motion/presets.json` (preset đã có) ·
`Docs/project-context.md` (tween lib đang dùng, profile nào giữ số) · `knowledge/feel/checklist.md`.

**Trước khi đề xuất chuyển động mới → tra `presets.json`.** Có preset gần đúng thì tinh chỉnh preset,
đừng đẻ preset thứ 12 gần giống preset thứ 3. Preset trùng nhau là cách nhanh nhất để game mất
tính nhất quán.

---

## Flow

### Bước 0 — phân loại yêu cầu

| Loại | Dấu hiệu | Đi tiếp |
|---|---|---|
| **A. Có video/gif ref** | dev gửi link/file | → `refs/video-ref-analysis.md` |
| **B. Mô tả cảm tính** | "chưa đã", "cứng", "nặng nề" | → Core workflow 1 (dịch cảm giác) |
| **C. Có sẵn anim, cần tinh chỉnh** | "nhanh quá / chậm quá" | → Core workflow 3 (visualiser so sánh) |
| **D. Element mới, chưa có gì** | "làm anim cho X" | → Core workflow 2 (chọn từ vocabulary) |

## Core workflow

1. Translate feeling → English technical term with a short Vietnamese explanation.
2. Describe the 5-phase motion: anticipation, action, overshoot, settle, follow-through.
3. Compare 2–4 options in the HTML visualiser; include current values, replay, and slow motion.
4. Commit values to `MotionProfile`/`TimingProfile` and reusable preset; component keeps preset ID.

Hard rules:
- Do not hardcode duration/easing in code.
- Declare interrupt policy before implementation.
- Immediate cue must appear in the first frame; Domain does not wait for Visual tween.
- Pooled visual Release kills/cancels and invalidates generation; Acquire/Bind fully rebinds state.

## Detail routes

- `refs/video-ref-analysis.md` — only when a video/gif reference exists.
- `refs/tuning-reference.md` — interrupt policy, immediate cue, duration guidance, pitfalls.
- `presentation-lifecycle/` — async/pooling ownership.

## Đầu ra

1. Bảng **Motion spec** 5 pha + số + preset id.
2. `.html` visualiser (lưu ở `handoff/<story>/`).
3. Preset mới đẩy vào `knowledge/motion/presets.json` (nếu dùng lại được).
4. Số chốt nằm trong `MotionProfile` / `TimingProfile`, **không** trong code.

Trước khi đóng story feel: chạy `knowledge/feel/checklist.md`.
