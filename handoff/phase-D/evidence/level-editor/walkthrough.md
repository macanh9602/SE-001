# D-B Level Editor - End-to-end walkthrough

Status: EXECUTED (targeted; save/reopen tail remains PENDING)

Executed sequence:

```text
Open Level Editor
-> choose Ready layout
-> Add Source
-> Add Cup
-> observe deliberate blocking overlap
-> select Cup and edit Position X in Inspector
-> verify Validation: ready / No issues found
-> resize to 640 px and restore wide layout
```

Evidence:

| Step | Result | Screenshot / note |
|---|---|---|
| Open + clean document | PASS | Live Unity Editor interaction. |
| Ready layout | PASS | `ux-pass1-overlap.png`. |
| Source + Cup art | PASS | `ux-pass1-overlap.png`; previews use `JarPreviewUtility`. |
| Blocking validation | PASS | `ux-pass1-overlap.png`. |
| Inspector repair | PASS | Live UI: Cup Position X changed to `5.0`, validation cleared. |
| Responsive check | PASS | Live measurement at `640x552` and `1100x720`. |
| Save/reopen | PENDING | Not claimed; no hand-edited JSON. |
