# D-B Level Editor - UX review

Status: EXECUTED (2026-09-23; partial closure rows remain explicit)

## Pass 1 - First use

| Task | Observed | Result | Evidence |
|---|---|---|---|
| Open/New | `SE001/Phase D/Level Editor` opened a clean `Unsaved level` document. | PASS | Unity Editor window; live UI inspection. |
| Choose layout | `test_tool` row showed `Ready`; selecting it cleared the layout blocker. | PASS | Live UI; `ux-pass1-overlap.png`. |
| Add Source | Source button added and selected a Source with official preview art. | PASS | `ux-pass1-overlap.png`. |
| Add Cup | Cup button added and selected a Cup with official preview art. | PASS | `ux-pass1-overlap.png`. |
| Save | Save/Save As became enabled after the document became valid; dirty status remained visible. | PASS (surface only) | Live UI inspection. |
| Validation | Deliberate Source/Cup overlap surfaced as a red `BLOCK` row with WHAT/WHERE/HOW guidance. | PASS | `ux-pass1-overlap.png`. |

## Pass 2 - Core workflow

| Scenario | Result | Evidence |
|---|---|---|
| End-to-end authoring | PASS (targeted) | Layout -> Source -> Cup -> live Inspector edit executed. |
| Validation repair | PASS | Cup Position X changed from the overlapping placement to 5.0; live status became `Validation: ready` / `No issues found.` |
| Add placement regression | PASS | Fresh document produced Source `(1.08, 1.92)`, Cup `(5.40, 9.60)`, and `issues=0`. |
| Save/reopen | PENDING | Not claimed; no hand-edited JSON. |

## Pass 3 - Stress / recovery

| Case | Result | Evidence |
|---|---|---|
| ~640 px | PASS | Live measurement: `640x552`, Levels pane `DisplayStyle.None`. |
| Wide workspace recovery | PASS | Live measurement: `1100x720`, Levels pane `Flex`. |
| long inspector/list | PARTIAL | ScrollView and inspector pane are present; long-content traversal not completed. |
| domain reload | PASS (technical) | Recompile completed with 0 console errors/warnings. |
| close/reopen | PENDING | Not claimed. |
| malformed file | PENDING | Not claimed. |
| dirty close | PENDING | Not claimed. |
| drag/undo | PENDING | Drag callbacks are implemented; pointer walkthrough was not completed in this session. |
| keyboard focus | PARTIAL | Editor window focus confirmed by Unity MCP; keyboard navigation not walked. |
| Play Test | N/A - D-C | Button is disabled and D-C remains out of scope. |

## 14 dimensions

| Dimension | Verdict | Evidence |
|---|---|---|
| Task completion | PASS (targeted) | Basic authoring + validation repair completed. |
| Discoverability | PASS | Toolbar, Levels, Add Source/Cup, Inspector labels visible. |
| Information architecture | PASS | Levels / Canvas / Inspector split is visible. |
| Interaction quality | PARTIAL | Field workflow verified; pointer drag not verified. |
| Error recovery | PASS | Blocking row gives actionable repair and clears after Inspector edit. |
| Visual hierarchy | PASS | Canvas center, Inspector right, validation below Inspector. |
| Control sizing/density | PASS | Buttons and fields usable in 1100 px window. |
| Semantic color | PASS | Red blocking state visible and paired with text. |
| Icon usage | N/A | Text-first production tool; no icon-only action was introduced. |
| Frequency-based emphasis | PASS | Add/Save are primary; Layout Bake and delete are secondary/danger styles. |
| Responsive layout | PASS | 640 px collapse and 1100 px restore measured live. |
| Accessibility/readability | PASS (visual) | Labels and issue text remained readable in live window. |
| GD terminology | PASS | Source, Cup, Layout, Amount, Required amount, Validation. |
| First-use without documentation | PARTIAL | Basic flow is discoverable; save/reopen and recovery edge cases still need walkthrough. |

## Findings - before fixes

| Severity | Task | Friction | Expected | Evidence | Fix |
|---|---|---|---|---|---|
| High | Add Source + Add Cup | Default placement could overlap and immediately block the document. | New entities should prefer separate valid footprints. | `ux-pass1-overlap.png` | `FindOpenCenter` now checks the actual footprint and existing Source/Cup entities. |

## Fixes + re-run

The placement fix was recompiled and retested. Fresh-document authoring produced one Source, one Cup, and zero validation issues. Remaining PENDING rows are intentionally not converted to PASS.

## Follow-up review - Level Browser density

Review source: Product Owner screenshot, approximately `1000x888`, Level Browser tab with 14 saved levels.

| Severity | Task | Friction | Expected | Evidence | Fix direction |
|---|---|---|---|---|---|
| Medium | Browse many saved levels | Browser content stretched across the full window; each level used a tall two-row layout and Delete expanded to roughly half the screen width. This increased scroll cost and weakened the level-name hierarchy. | A bounded readable list with compact rows; status and destructive action remain visible without dominating the row. | Product Owner screenshot supplied in chat. | Added a max-width browser content column, compact one-row entries on wide windows, and narrow-window two-row fallback. Manual re-review remains pending. |

### Follow-up re-run status

Code fix applied and Unity reimport/compile completed with 0 console errors and 0 warnings. The visual re-run at wide and narrow window sizes remains `PENDING` because this session does not inject EditorWindow resize/click gestures.
