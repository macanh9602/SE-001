# Provider adapter — codex-with-chatgpt

Use this adapter when `Frontier provider = codex-with-chatgpt`.

## Roles

- Codex remains executor and owns local implementation, verification, notes and closure.
- ChatGPT frontier provides `PLAN` before high-risk/architecture implementation and `REVIEW` after local verification.
- The external installed C2C capability owns workspace → connector mapping and connector read-only access.
- Never auto-switch the current Codex model/session.

## Provider operations

Use the installed C2C CLI from its provider package:

1. `c2c status -w <workspace> --json` for workspace-scoped bridge/tunnel status;
2. provider-managed mapping from current Codex workspace to candidate connector;
3. `workspace_info`, read-only sentinel/source/diff/log access on that connector;
4. `c2c doctor -w <workspace> --json` for provider-native diagnosis/repair, followed by one retry;
5. `c2c record -w <workspace> ...` to release execution metadata/evidence for REVIEW.

Use the checkout-backed CLI path documented by the installed C2C package when `c2c` is not globally linked. Do not expose public endpoint URLs or credentials in notes/reports. If the capability or an operation is unavailable, report that exact runtime fact and apply the generic `AUTO`/`REQUIRED` failure policy.

## PLAN / REVIEW payload

- `PLAN`: send objective, relevant contracts and references by connector paths; request assumptions, risks and recommended plan. Do not paste a repository bundle.
- `REVIEW`: expose verified diff/log/evidence through connector read-only access; request `PASS`, actionable findings, or `PLAN` for one bounded follow-up iteration.
- Reject results produced before workspace + identity preflight passes.
