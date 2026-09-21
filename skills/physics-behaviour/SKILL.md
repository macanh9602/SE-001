---
name: physics-behaviour
description: >
  Guidance for Physics behaviours and authority choices: fall, gravity, roll, slide, bounce, impact,
  collision, friction, stack, settle, spring, joint, rope, suction, attraction, granular, sand,
  Rigidbody/Rigidbody2D and related Vietnamese terms (vật lý, rơi, lăn, trượt, va chạm, ma sát).
---

# SKILL: physics-behaviour

1. **CLASSIFY AUTHORITY** — Presentation, Hybrid, or Simulation-driven.
2. **IDENTIFY BEHAVIOUR** — use the [catalogue](../../knowledge/physics/behaviour-catalog.md).
3. **DECOMPOSE PHASES** — release, motion, contact, energy decay, settle.
4. **NAME TERMS** — use [vocabulary](../../knowledge/physics/vocabulary.md).
5. **COMPARE 2–4 IMPLEMENTATION APPROACHES** — Authored / Fake, Real Physics, Hybrid implementation, or Custom simulation; expose CPU/GPU/GC/draw-call/memory trade-offs when relevant.
6. **EXPOSE TUNABLES** — component/Profile/level data; never hardcode gameplay numbers.
7. **SEARCH** — use [keywords](../../knowledge/physics/search-keywords.md).
8. **IMPLEMENT only when requested** — load the matching recipe.
9. **VERIFY** — inspect ownership, pooling, semantic signals, and the chosen authority.

Component owns HOW; caller decides WHAT. Self-contained does not mean gameplay authority. A Physics component emits semantic signals and does not decide Win/Lose unless explicitly assigned by the story. Do not assume Rigidbody is always correct or merge this skill with `game-feel-motion`.

Refs: [authority modes](refs/authority-modes.md), [contacts/materials](refs/contacts-and-materials.md), [stability/performance](refs/stability-and-performance.md), [Unity 2D](refs/unity-2d.md), [Unity 3D](refs/unity-3d.md).

Recipes: [settle detector](recipes/settle-detector.md), [impact probe](recipes/impact-probe.md), [impulse driver](recipes/impulse-driver.md), [hybrid handoff](recipes/hybrid-handoff.md).

Compose with `game-feel-motion` for presentation, `technical-slice` for scale/risk, `debug-audit` for runtime evidence, and `presentation-lifecycle` for pooled async handoff.
