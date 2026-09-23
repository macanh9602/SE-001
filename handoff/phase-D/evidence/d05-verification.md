# D0.5 verification

Environment: Unity `6000.0.70f1`, active instance `SE-001@49dd5fcfcb6952e3`.

| Check | Result | Evidence |
|---|---|---|
| Compile/reimport | PASS | Unity refresh completed; final console error/warning query returned 0 entries. |
| D0.5 targeted tests | PASS, 8/8 | Unity test job `a205c21071754f1ea89b0e625f5c7ce9`. |
| Full EditMode suite | 62/64 | Unity test job `bf6677665e1a4d24ac748ad2410baa4a`; only the two pre-existing Phase C Level 02 gameplay assertions failed. |
| Scoped style gate | PASS | `SCOPED STYLE-GATE: PASS` over all D0/D0.5-created or modified C# files. |
| Repository style gate | BLOCKED by pre-existing dirty files | `tools/style-gate.ps1` still reports only SandSimulation/SandFieldVisual/PhaseCEntities violations outside this packet. |
| Diff whitespace | PASS | `git diff --check` returned no violations. |

The two gameplay failures were not fixed or attributed to the layout pipeline:

- `Playthrough_Level02_TwoColors_Wins`: Lost / NotFilled, coral `177/474`, blue `474/474`.
- `GameOver_SimKeepsSettling`: expected Won, received Lost.
