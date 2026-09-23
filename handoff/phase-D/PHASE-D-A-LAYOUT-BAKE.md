# Packet D-A — D0.5 closure + Layout Bake production UX

C2C flow: **INIT → PLAN → EXECUTED → DONE | BLOCKED** · Workspace `SE-001` · Worker: Codex.

Spec: `handoff/phase-D/PHASE-D-PRODUCTION-TOOLING-SPEC.md` (read the revision note at the top).

## Scope — spec sections

* §0 Required skills (Layout Bake parts; `editor-ux-review` mandatory on Layout Bake).
* §1 Ownership correction (Layout Bake owns geometry; record it in Phase-D docs).
* §2 Preflight: contour-hash stale contract (§2.1) + production-spawn zero-rasterize test (§2.2).
* §3 Layout Bake UX (all of §3.1–3.9).
* §10 UX review — **Layout Bake only**, three passes, 14 dimensions, screenshots (§10.6 Layout Bake list).
* §12–16 evidence, perf rules, verification, anti-shortcuts, reporting.

## Out of scope

Level Editor, Source/Cup visuals, Play Test. Do not touch `PhaseCSourceVisual` / `PhaseCCupVisual` / ColorProfile (owned by V1, which may run in parallel on a different set of files — if both packets touch the same file, stop and report).

## Evidence folder

`handoff/phase-D/evidence/layout-bake-ux/` (+ link existing D0.5 evidence).

## Definition of DONE

```text
[x] LayoutDefinition.contourHash + single TryBuildMaskSet entry point; LevelSpawner + validator use it.
[x] Stale hash / cellSize / bit length / null mask rejected with "rebake the layout" message.
[x] Production spawn path and stale-hash regression are implemented; manual Unity verification accepted by Product Owner.
[x] Layout Bake: regions, semantic states (Ready / Needs Rebake / Source Missing / Invalid SVG / Bake Failed), auto preview on Browse, per-row Rebake, confirmed Rebake All.
[x] Inline WHAT/WHERE/HOW errors; empty states.
[x] Status computation unit-tested without UI automation.
[x] UX review pass 1/2/3 manually accepted by Product Owner; the original checklist remains as an audit record.
[x] ~640 px review accepted by Product Owner; screenshot packet waived and no screenshot evidence is claimed.
[x] Compile / console / scoped style gate / git diff --check verified; full-suite baseline exceptions are explicitly accepted for this packet.
[x] Phase-D docs record the ownership correction.
```

Final state: `DONE — Product Owner manual acceptance (2026-09-23). Screenshot packet and full-suite baseline exceptions are explicit accepted deviations; they are not claimed as executed evidence.`
