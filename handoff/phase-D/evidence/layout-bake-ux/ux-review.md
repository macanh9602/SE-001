# D-A — Layout Bake UX review (editor-ux-review, 3 passes)

Reviewer: Ducan (real Unity Editor). Author self-review is **not** a substitute — every row stays `PENDING` until done in the Editor.
Rule: **record findings first, fix after.** Keep this file as the "before" list; fixes go in a new section at the bottom with re-run results.

Screenshots → `screenshots/` with window width in the file name, e.g. `layout-bake_640px_empty.png`.

Finding format:

```text
Severity (Blocker/Major/Minor) | Task | Friction | Expected | Evidence | Fix direction
```

---

## Pass 1 — First use (no guide)

Close the window, then open `SE001 > Phase D > Layout Bake` fresh. Do not read the code or this packet while doing it.

| # | Task | Expected | Result | Screenshot |
|---|---|---|---|---|
| 1.1 | Say out loud what the tool is for | Header + subtitle make it obvious | PENDING | `layout-bake_wide_empty.png` |
| 1.2 | Find how to choose an SVG | Empty state + "Choose SVG…" | PENDING | |
| 1.3 | Pick `TrashStuff/test_tool.svg` | Preview + metrics appear without another click; Layout ID pre-filled `test_tool` | PENDING | `layout-bake_wide_svg-preview.png` |
| 1.4 | Understand whether it parsed | Status row says Parsed OK + relation to baked layout | PENDING | |
| 1.5 | Understand Layout ID | Hint under the field: new vs updates existing | PENDING | |
| 1.6 | Find the primary action | "Bake Layout" is the strongest button | PENDING | |
| 1.7 | Find existing layouts | List with badges | PENDING | `layout-bake_wide_list.png` |
| 1.8 | Understand why `phase_c_level_0x_layout` is Source Missing and whether levels are broken | Row text says no SVG linked + "Levels using it still load" + how to fix | PENDING | |
| 1.9 | Rebake an existing layout | Row "Rebake" on `test_tool` | PENDING | |

Write every place you hesitated, mis-clicked or needed Console.

## Pass 2 — Core workflow

| # | Scenario | Steps | Expected | Result | Screenshot |
|---|---|---|---|---|---|
| 2.1 | New SVG → bake | Copy `test_tool.svg` → `TrashStuff/ux_new.svg`; choose it; ID `ux_new`; Bake | Success message; row `ux_new` = Ready; selected | PENDING | `layout-bake_wide_ready.png` |
| 2.2 | Existing → rebake | Click row `test_tool` → Rebake | Status bar "Rebaked… Ready"; GUID of `test_tool.asset` unchanged (Inspector › Debug or .meta) | PENDING | `layout-bake_wide_rebake-result.png` |
| 2.3 | SVG edited after bake | Edit `ux_new.svg` (move a wall), save, focus Unity (or press Refresh) | Row → **Needs Rebake**, "SVG changed… Levels using it still load"; Rebake → Ready | PENDING | `layout-bake_wide_stale.png` |
| 2.4 | Invalid SVG | Replace `ux_new.svg` content with `<svg>` garbage; press Refresh | Row → **Invalid SVG** with fix text; picking it in SVG Source shows red WHAT/WHERE/HOW box; Bake disabled with reason | PENDING | `layout-bake_wide_invalid.png` |
| 2.5 | Missing source | Rename `ux_new.svg` away; Refresh | Row → **Source Missing**, "SVG file not found…" | PENDING | |
| 2.6 | Invalid Layout ID | Type `Stage 7` | Red hint; Bake disabled; reason next to the button | PENDING | `layout-bake_wide_disabled-reason.png` |
| 2.7 | Rebake All | Rebake All… → read confirmation → confirm | Confirmation states counts + skipped; progress bar; summary in status bar | PENDING | `layout-bake_wide_rebake-all.png` |
| 2.8 | Recovery without Console / Project window | For 2.3–2.6 | Everything needed is in the window | PENDING | |

Clean up after: delete `ux_new` layout assets (definition, mask, prefab) and `TrashStuff/ux_new.svg`.

## Pass 3 — Stress + recovery

| # | Case | Expected | Result | Screenshot |
|---|---|---|---|---|
| 3.1 | Resize to ~640 px wide | No clipped primary action; preview + metrics wrap; row buttons visible | PENDING | `layout-bake_640px_*.png` |
| 3.2 | Many layouts / long names | List scrolls with the page; search filters | PENDING | |
| 3.3 | Domain reload (edit any script) with SVG selected | Window restores SVG, Layout ID, search; no duplicate callbacks (one click = one bake) | PENDING | |
| 3.4 | Close + reopen window | Same as 3.3 | PENDING | |
| 3.5 | Enter Play Mode with window open | Bake / Rebake / Rebake All disabled with "Exit Play Mode to bake layouts." | PENDING | |
| 3.6 | Click Rebake repeatedly / switch rows fast | No errors, status consistent | PENDING | |
| 3.7 | Keyboard: Tab through fields, Enter in SVG field | Focus visible; typed path is accepted | PENDING | |

## 14 dimensions (fill after the passes)

| Dimension | PASS / FAIL / PENDING | Note |
|---|---|---|
| 1 Task completion | PENDING | |
| 2 Discoverability | PENDING | |
| 3 Information architecture | PENDING | |
| 4 Interaction quality | PENDING | |
| 5 Error recovery | PENDING | |
| 6 Visual hierarchy | PENDING | |
| 7 Control sizing/density | PENDING | |
| 8 Semantic color | PENDING | |
| 9 Icon usage | PENDING | Badges use ✔ ↻ ? ✖ + text |
| 10 Frequency-based emphasis | PENDING | |
| 11 Responsive layout | PENDING | |
| 12 Accessibility/readability | PENDING | |
| 13 GD terminology | PENDING | |
| 14 First use without documentation | PENDING | |

## Findings (before fixes)

| Severity | Task | Friction | Expected | Evidence | Fix direction |
|---|---|---|---|---|---|
| | | | | | |

## Fixes + re-run

(Send the findings to Claude; fixes are recorded here with the re-run result of each affected case.)
