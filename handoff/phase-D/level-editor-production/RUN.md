# SE-001 — Phase D-B Level Editor Runbook

This kit is a hybrid bootstrap for `handoff/phase-D/PHASE-D-B-LEVEL-EDITOR.md`.

## Source of truth

The bootstrap is not the acceptance authority. Codex must read, in this order:

1. `handoff/phase-D/PHASE-D-B-LEVEL-EDITOR.md`
2. `handoff/phase-D/PHASE-D-PRODUCTION-TOOLING-SPEC.md`
3. `handoff/phase-D/PHASE-D-PLAN.md`
4. `skills/level-editor/SKILL.md` + required recipes/refs
5. `skills/gd-communication/SKILL.md`
6. after implementation: `skills/editor-ux-review/SKILL.md`

## Gate

D-B still requires:
- D-A = DONE
- V1 = DONE

Do not pass `-GateApproved` merely to bypass this rule.

## Dry run

From any PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\Apply-PhaseD-LevelEditor.ps1 `
  -ProjectRoot "D:\UnityProject\SE-001" `
  -DryRun
```

The dry run checks workspace markers, git state, prerequisites and all target paths. It writes nothing.

## Apply after C2C INIT verifies D-A + V1

```powershell
powershell -ExecutionPolicy Bypass -File .\Apply-PhaseD-LevelEditor.ps1 `
  -ProjectRoot "D:\UnityProject\SE-001" `
  -GateApproved
```

The script:
- refuses unrelated dirty worktree by default;
- never changes runtime/gameplay files;
- creates only allowlisted Level Editor bootstrap files and evidence templates;
- refuses to overwrite an existing target unless `-Force`;
- backs up replaced targets when `-Force`;
- writes a manifest;
- runs `git diff --check`;
- runs `tools/style-gate.ps1` when available.

## Prompt to give Codex

```text
C2C INIT. Execute handoff/phase-D/level-editor-production/EXECUTE.md.

First verify the D-B entry gate from PHASE-D-B-LEVEL-EDITOR.md: D-A = DONE and V1 = DONE.
If either is not genuinely DONE, report BLOCKED — entry gate and do not pass -GateApproved.

If the gate passes, run Apply-PhaseD-LevelEditor.ps1 -DryRun, inspect the planned allowlist,
then run it with -GateApproved as deterministic bootstrap. Inspect every generated diff.
The script is only a scaffold accelerator; continue implementation, integration, compile/test/fix,
end-to-end walkthrough, and editor-ux-review exactly as the D-B packet and shared spec require.

Do not implement D-C unsaved Play Test in this packet. End with EXECUTED and evidence.
```

## Rollback

When the script applies, it prints the generated manifest path.

To remove files that were added by the bootstrap and restore files that were force-replaced:

```powershell
powershell -ExecutionPolicy Bypass -File .\Apply-PhaseD-LevelEditor.ps1 `
  -ProjectRoot "D:\UnityProject\SE-001" `
  -RollbackManifest "handoff\phase-D\level-editor-production\.bootstrap\manifest-YYYYMMDD-HHMMSS.json"
```

Rollback refuses to delete a file if it has changed since the bootstrap unless `-Force` is supplied.