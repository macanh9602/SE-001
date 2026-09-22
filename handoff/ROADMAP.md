# Roadmap — SE-001 Architecture-Gated Implementation

> Architecture source-of-truth: `SE001-ARCHITECTURE-BLUEPRINT.md`.
> Chỉ story có status `EXECUTABLE` mới được chạy. Phase description không phải execution spec.

## Materialization rule

- Chỉ materialize story kế tiếp sau khi story/gate trước PASS và closure hoàn tất.
- Không tạo trước full spec cho phase B–I; khi materialize phải đọc lại blueprint và evidence mới nhất.
- Story 000 là historical bootstrap evidence. Story 000A đã archive/supersede.
- Story 001–014 của roadmap cũ đã bị D-005 retire; Git history giữ historical record.

## Active phase

| Phase | Name | Status | Scope |
|---|---|---|---|
| A | Foundation | DONE / PASS | Architecture alignment and lifecycle foundation |
| B | Layout + Simulation Foundation | DONE / PASS | Canonical JSON, SVG import, shared geometry/masks, 3D layout, SandSimulation, SandField, integration |
| C | Complete Playable Core | DONE / CLOSURE PASS | Three authored levels, production visuals, progression, HUD result flow, playthroughs, reload, and performance evidence |
| D | GD Production Pipeline | PLANNED | Progression/save/sequence, production Level Editor, SVG workflow, validation, preview, unsaved Play Test |
| E | Feel + Performance + Ship Quality | PLANNED | Sand feel, VFX/material/texture/shadow/quality tiers, device performance |

Phase B worker packets are implementation packets, not architecture gates.

## Active story

| Story | Tên | Size | Status | Dependency | Source |
|---|---|---|---|---|---|
| 000 | Project contract + bootstrap | M | HISTORICAL / DONE | — | `story-000-project-contract-bootstrap/` |
| 001 | Architecture Foundation Alignment | M | **DONE — CLOSURE PASS** | 000, D-005 | `story-001-architecture-foundation-alignment/story.md` |

Không có story implementation nào executable tại thời điểm này. Phase B đã mở gate để materialize
story kế tiếp, nhưng chưa phải execution spec và chưa được triển khai.

## Gated implementation sequence

| Phase | Capability giữ lại | Gate để materialize story |
|---|---|---|
| A | Foundation Alignment | **PASS — Story 001 closed** |
| B | StaticObstacle vertical slice: Data → Factory → prefab/View → simulation mask → editor preview | A PASS; ownership/root lifecycle đã verify |
| C | SandSimulation + SandField visual | B PASS; shared board mapper/rasterizer parity đã verify |
| D | Source vertical slice | C PASS; simulation accepts deterministic semantic emit commands |
| E | Cup vertical slice | D PASS; accounting inputs và sink geometry contract ổn định |
| F | Player Draw vertical slice | E PASS; dynamic mask dùng shared geometry và pooled DrawStroke View |
| G | GameplayManager, end-state và playable load/play/result/retry loop | F PASS; full command/accounting flow testable |
| G2 | Progression/save và ordered level sequence | G PASS; playable loop/level identity ổn định |
| H | Level Editor production flow | G2 PASS; runtime loader/spawner và level identity/progression contract ổn định |
| I | Sand feel/VFX, quality scaling và device performance | G2 + H PASS; production flow feature-complete |

## Product scope preserved

- Powder sand feel và simulation-driven 2D authority.
- Static obstacles và player-drawn dynamic paths.
- Finite Source streams, Cup collection/accounting, deterministic win/lose.
- Full playable loop, progression/save và ordered levels.
- Level Editor: New/Open/Edit/Validate/Save/unsaved Play Test.
- Mobile performance: Redmi 9A low-end target, 60 fps mid / 30 fps low.

## Excluded

- SDK, ads, analytics, IAP và attribution.
- Final art polish hoặc gameplay gimmick chưa được Product Owner chốt.

## Risks tracked

| Risk | Guard |
|---|---|
| Codex chạy phase description như story | Chỉ status `EXECUTABLE` + concrete `story.md` mới được chạy |
| Root ownership quay lại `LevelContext`/`LevelManager` | Story 001 verify `LevelSpawner` ownership trước feature work |
| Editor/runtime obstacle drift | Một shared mapper/rasterizer, parity test trước Sand/Source expansion |
| Mobile cost tăng âm thầm | Không GameObject-per-grain; profile/buffer/pool contract và phase I device gate |
