# D0 mapping: architecture requirement → reusable guidance

| Blueprint / project requirement | Recipe or review rule | Evidence |
|---|---|---|
| Document is the serialized source of truth | `document-view-derived-state.md` | Document/ViewState/DerivedState split |
| ViewState is not serialized | `document-view-derived-state.md` | ViewState skeleton and checks |
| ApplyEdit differs from ReloadDocument | `apply-edit-vs-reload.md` | Explicit API and undo boundary |
| Stable identity survives structural edits | `stable-selection-remap.md` | ID index/remap skeleton |
| EditorWindow responsibility stays cohesive | `editor-window-partials.md` | Partial ownership and CreateGUI order |
| UI Toolkit workspace is resizable | `split-workspace-uitk.md` | TwoPaneSplitView, min width, geometry callback |
| Input commit does not rebuild per keystroke | `delayed-fields-and-callbacks.md` | Delayed field and callback lifecycle |
| Validation is actionable and visible | `validation-focus-flow.md` | WHAT/WHERE/HOW and click-to-focus |
| Unsaved Play Test uses normal runtime path | `unsaved-play-test.md` | One-shot override and cleanup |
| UX review is evidence-based | `editor-ux-review/SKILL.md` | Three-pass review and no speculative PASS |
| Fourteen UX dimensions are covered | `editor-ux-review/SKILL.md` | Required dimension list |
| Visual roles and semantic states are consistent | `editor-ux-review/refs/editor-visual-system.md` | Roles, tiers, 13 rules |
| First use needs no documentation | `editor-ux-review/refs/first-use-review.md` | First-use task script |
| Review verdicts remain auditable | `editor-ux-review/refs/review-checklist.md` | PASS/FAIL/N/A/PENDING table |

## Scope check

All recipes and review references are generic. They contain no project entity, level ID, sand,
cup, source, material, or simulation rule. SE-001-specific implementation remains under
`Assets/_Core` and is not imported into the reusable skill guidance.
