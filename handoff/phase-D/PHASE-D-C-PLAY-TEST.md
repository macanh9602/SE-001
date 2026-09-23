# Packet D-C — Unsaved Play Test (D6)

C2C flow: **INIT → PLAN → EXECUTED → DONE | BLOCKED** · Workspace `SE-001` · Worker: Codex.

Spec: `handoff/phase-D/PHASE-D-PRODUCTION-TOOLING-SPEC.md` §9 (+ §12–16).

## Entry gate

1. D-B = DONE.
2. `handoff/phase-C-complete-playable-core/PHASE.md` shows all four re-runs genuinely complete with evidence:
   EditMode full regression · reload probe ×10 on all three levels · performance capture refresh on all three levels · `git diff --check` + console clean.

If (2) is still pending: this packet may **run those four re-runs first** (they are verification, not new features), record evidence in the Phase C folder, then continue. If any re-run fails → `BLOCKED — Phase C gate: <item + evidence>`. Never mark PASS without the evidence file.

## Scope

§9.1 architecture (one-shot override → normal LevelManager / LevelSpawner, consumed once, cleared), §9.2 UX (visible, disabled with exact blocker), §9.3 tests + manual evidence, Play Test rows of the §10 UX review, GD guide Play Test chapter.

## Definition of DONE

```text
[ ] Phase C gate PASS with evidence links.
[ ] Unsaved Play Test uses normal runtime path; no second loader; nothing written to Resources.
[ ] Override consumed exactly once; later normal load unaffected.
[ ] Editor context + dirty state restored after Play Mode.
[ ] Blocking validation prevents Play with exact reason.
[ ] §9.3 tests PASS; manual evidence (unsaved change visible at runtime) with screenshots.
[ ] UX review Play Test cases done; GD guide updated.
[ ] Compile / console / tests / style gate / diff-check clean.
[ ] Roadmap + Phase-D docs reflect actual state.
```

Final state: `DONE` → **Phase D Level Editor + Layout Bake production tooling = DONE**, or `BLOCKED — <specific blocker + evidence>`.
