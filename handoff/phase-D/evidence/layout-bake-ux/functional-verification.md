# D-A — Functional verification (Layout Bake + contour-hash contract)

Status: **EXECUTED (code) — verification PENDING in Unity.** Nothing below is marked PASS until it is run.

Code is staged in `handoff/phase-D/da-staging/` and is applied with one command **after Codex V1 is finished and Unity is idle**:

```powershell
powershell -ExecutionPolicy Bypass -File handoff\phase-D\da-staging\apply-da.ps1
```

## 1. What the packet changed

| Item | Where | Notes |
|---|---|---|
| Expected geometry identity on the layout | `LayoutDefinition.contourHash` | Baker writes the same hash to definition + mask in one bake |
| Single runtime entry point | `LayoutDefinition.TryBuildMaskSet` | Rejects null mask, empty/mismatched hash, board-size mismatch, grid-size mismatch, stale cellSize, bad bit length, max-cell overflow. Every error says to rebake. No SVG parsing, no rasterize. |
| Runtime callers | `LevelSpawner` (1 line), `LevelDataValidator` (1 line) | Both now call `layout.TryBuildMaskSet` |
| Existing layouts | 4 × `Resources/Layouts/*.asset` | `contourHash` copied from their mask (the value a rebake would write) so levels keep loading |
| Status logic (UI-free, tested) | `LayoutBakeStatus.cs` | Ready / Needs Rebake / Source Missing / Invalid SVG / Bake Failed + recovery text + "levels still load?" |
| Discovery + SVG parse cache | `LayoutBakeLibrary.cs` | `FindAssets` only on open / focus / Refresh / after bake; SVG parse cached by file write time |
| Window | `LayoutBakeWindow*.cs` (4 partials), `LayoutContourPreview.cs`, `USS/LayoutBakeWindow.uss` | SVG Source (auto preview on Browse, Layout ID rules, disabled-reason, inline WHAT/WHERE/HOW), Existing Layouts (search, status badges, per-row Rebake/Show, click to load into SVG Source), Rebake All with in-window confirmation + progress + summary, collapsed Advanced (hashes/paths), always-visible status bar |
| Tests | `Tests/Editor/LayoutBakeProductionTests.cs` | see §3 |
| Docs | `Docs/glossary.md`, `PHASE-D-PLAN.md` D3 marked superseded | |

Known limitation (by data, not by tool): `phase_c_level_01..03_layout` were migrated from legacy JSON and have **no SVG recorded**. They show **Source Missing** (levels still load). They can only be rebaked after someone picks their SVG under SVG Source with the same Layout ID. `test_tool` has an SVG and is rebakeable.

## 2. Run order (you)

1. Wait for Codex V1 = DONE; Unity idle.
2. Run `apply-da.ps1`. Expect "copied" ×11, "patched" ×7, "hashed" ×4.
3. Focus Unity → compile. **Console must be clean.** If there is a compile error, send me the Console text; do not let Codex "fix" D-A files.
4. Test Runner → EditMode → run `LayoutBakeProductionTests`, `LayoutBakeTests`, then the full EditMode suite.
5. Do the UX review walkthrough in `ux-review.md` (pass 1 → 2 → 3) and save screenshots into `screenshots/`.
6. Run `tools/style-gate.ps1` and `git diff --check`.

## 3. Test checklist

| Test | Expected | Result |
|---|---|---|
| `LayoutDefinition_ValidBake_BuildsMasks` | PASS | PENDING |
| `LayoutDefinition_NullMask_Blocks` | PASS | PENDING |
| `LayoutDefinition_MissingExpectedHash_Blocks` | PASS | PENDING |
| `LayoutDefinition_HashMismatch_Blocks` | PASS | PENDING |
| `LayoutDefinition_StaleCellSize_Blocks` | PASS | PENDING |
| `LayoutDefinition_BitLengthOrOverflow_Blocks` | PASS | PENDING |
| `LayoutDefinition_BoardSizeMismatch_Blocks` | PASS | PENDING |
| `ProductionSpawn_UsesBakedMask_ZeroRasterizeCalls` | `RasterizeCallCount == 0` through `LevelManager.BeginLevel` | PENDING |
| `ProductionSpawn_StaleHash_BlocksWithRebakeMessage_NoFallbackRasterize` | throws, message contains "rebake", 0 rasterize | PENDING |
| `ProductionLayouts_AllBuildThroughAuthoritativePath` | 3 Phase C layouts build | PENDING |
| `Status_*` (8 cases) | PASS | PENDING |
| `LayoutId_Rules` (5 cases) | PASS | PENDING |
| `Rebake_PreservesAssetGuids_AndWritesMatchingHashes` | GUIDs unchanged, hash identical across rebakes; fixture assets deleted afterwards | PENDING |
| `Library_ParseCache_ReusesResultForUnchangedFile` | PASS | PENDING |
| Existing `LayoutBakeTests` (5) | still PASS | PENDING |
| Full EditMode suite | PASS or failures proven unrelated (exact test names) | PENDING |

## 4. Static checks

| Check | Result |
|---|---|
| Unity compile clean, no new warnings | PENDING |
| Console clean after opening Layout Bake, Refresh, Bake, Rebake All | PENDING |
| `tools/style-gate.ps1` (touched C#) | PENDING — local line-length / one-statement check run before staging: clean |
| `git diff --check` | PENDING |
