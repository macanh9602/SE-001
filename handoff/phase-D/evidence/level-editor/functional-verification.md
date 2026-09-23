# D-B Level Editor - Functional verification

Status: EXECUTED (2026-09-23)

| Check | Result | Evidence |
|---|---|---|
| Compile | PASS | Unity 6000.0.70f1 refresh completed; editor state reported `is_compiling=false`. |
| Console | PASS | Unity MCP `read_console(types=error,warning)` returned 0 entries after final compile. |
| Document tests | PASS | Final EditMode job `4ef8022635a4415dbbcd6ddba65669af`: 4/4 completed, no failures. |
| Interaction tests | PASS (targeted) | Real Level Editor UI flow: Ready layout -> Source/Cup -> blocking overlap -> Inspector fix -> `Validation: ready`. Add placement retest produced 1 Source + 1 Cup, `issues=0`. |
| Layout/D0.5 regression | PASS (targeted) | Job `b827d900d7d543f4a71d2fc94c4c3587`: 3/3 passed, including baked-mask zero-rasterize coverage. |
| Wider Layout regression | BASELINE FAILURES | Job `b5e879a7d7e342bdb9b1cbf38ff77acb`: 3 passes and 2 existing fixture failures (`phase_c_level_01_layout` / stale-hash lookup). |
| Full EditMode suite | BASELINE FAILURES | Job `617dceba49d44d099752a36ec1779d15`: 112 tests completed; 11 failure entries, including existing Phase C playthrough/HUD failures and layout fixture/culture mismatches. |
| Scoped style gate | PASS | `tools/style-gate.ps1 -Baseline c6961b4`. |
| git diff --check | SCOPED PASS / WORKTREE BASELINE WARNING | Level Editor scoped files pass; whole-worktree check reports trailing whitespace in pre-existing user changes `Cup.prefab:285-286`. Those prefab changes were preserved. |

The full-suite failures are recorded as baseline/project fixture issues; no D-C implementation was added.
