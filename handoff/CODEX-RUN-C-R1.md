# CODEX RUN C-R1 — SpriteSurface shader + visual materials + lifecycle/perf evidence
Workspace: SE-001 only. Baseline: main @ bd1621b (clean). One run = this packet only.

## Status contract (read first)
- Only two valid end states: **DONE** (every DoD box below ticked with evidence) or **BLOCKED** (exact reason + what you tried).
- `PARTIAL`, "verification slice", "foundation" are NOT valid. Do not stop to report between tasks.
- Do not start anything outside this file. Do not start juice, sand simulation or HUD work.

## DO NOT TOUCH (owned by another worker in parallel)
- `Assets/_Core/4_Scripts/Simulation/**`
- `Assets/_Core/4_Scripts/Elements/Sand/SandFieldVisual.cs`
- `Assets/_Core/4_Scripts/Presentation/PhaseC*Visual.cs`
- `Assets/_Core/4_Scripts/Diagnostics/PhaseCDebugView.cs`
- `Assets/_Core/4_Scripts/System/Management/{GameplayManager,GameplayInputController,PhaseCEntities}.cs`
- `Assets/_Core/4_Scripts/Tests/Editor/PhaseCPlaythroughTests.cs`
If a task seems to require editing these → BLOCKED with the reason, do not edit.

## Tasks

### T1 — Shader `SE001/SpriteSurface` (`Assets/_Core/5_Shaders/SE001_SpriteSurface.shader`)
Copy structure/conventions of `SE001_LayoutSurface.shader` (URP lit-lite, SRP Batcher CBUFFER, instancing) and add:
- `_BaseMap` (tiling/offset), `_BaseColor` (tint, will be set per-renderer via MaterialPropertyBlock), `_Cutoff` (default 0.5).
- ForwardLit: `clip(albedo.a - _Cutoff)`; main light Lambert + SH ambient + received main-light shadows (same keywords as LayoutSurface).
- ShadowCaster pass: samples `_BaseMap` alpha and `clip()` → shadow follows the sprite silhouette.
- DepthOnly pass with the same clip.
- Cull Off (quad may be seen from either side after the valve rotates).
- Keep variant count minimal (same multi_compile set as LayoutSurface, nothing extra).

### T2 — Materials + profile
- Create `Assets/_Core/1_Materials/MAT_Source.mat`, `MAT_Cup.mat`, `MAT_CupBack.mat` on SpriteSurface; `MAT_DrawPath.mat` on LayoutSurface.
- Placeholder textures: generate `Assets/_Core/0_Texture2D/Dev/dev_rounded_rect.png` (256×256, white rounded rect, transparent corners, Wrap=Clamp, alphaIsTransparency) via an editor script; assign to Source/Cup materials.
- New ScriptableObject `SE001.Data.PhaseCVisualMaterials` (`Assets/_Core/4_Scripts/Data/PhaseCVisualMaterials.cs`) with fields: `sourceMaterial`, `cupMaterial`, `cupBackMaterial`, `drawPathMaterial`. Asset at `Assets/_Core/Resources/Profiles/PhaseCVisualMaterials.asset`, all fields assigned.
- Add a reference `public PhaseCVisualMaterials visualMaterials;` to `GameplayRuntimeProfile` and assign it in the existing runtime profile asset.
- All asset creation via an editor menu `SE001/Phase C/Create visual materials` (idempotent), executed through Unity MCP.

### T3 — Lifecycle test (Phase C levels)
New file `Assets/_Core/4_Scripts/Tests/Editor/PhaseCLifecycleTests.cs`:
- `PhaseC_ReloadTenTimes_NoAccumulation`: BeginLevel each of phase_c_level_01..03, ReloadCurrentLevel ×10; assert previous context disposed, owner child count stable, LevelRoot child count stable, no leaked `Mesh`/`Material` instances (count via `Resources.FindObjectsOfTypeAll<Mesh>()` before/after, allow ±0 for runtime-created ones named `DebugStroke|PhaseCDebug*|Generated*`).
- `PhaseC_NextFollowsSequence_DisabledOnLast`: via LevelManager API walk the authored `PhaseCLevelSequence` to the end.
- `PhaseC_VisualMaterialsAssigned`: runtime profile → visualMaterials → all 4 non-null, shaders named `SE001/SpriteSurface` / `SE001/LayoutSurface`.

### T4 — Perf capture (evidence only, no optimisation)
- Editor menu `SE001/Phase C/Perf capture` (or a PlayMode-free EditMode helper) that loads each Phase C level, opens all valves, runs `GameplayManager.AdvanceSteps` in chunks of 60 until game over, and records per level: grid W×H, total steps, max grains in field, avg and max ms per step (Stopwatch), GC alloc delta over the run (`GC.GetTotalMemory` before/after, note it is approximate).
- Write results to `handoff/phase-C-complete-playable-core/perf-C-R1.md` (table). Run it via MCP and commit the file.

### T5 — Style gate script
- `tools/style-gate.ps1` (and `.sh` equivalent if trivial): fail if any `.cs` under `Assets/_Core/4_Scripts` changed vs `bd1621b` has a line > 160 chars, or a line with ≥2 statements (`;` count ≥ 2 outside `for (`), or `ScriptableObject.CreateInstance` outside `Tests/` and `Editor/`.
- Run it; report its output. Your own new/edited files must pass. Pre-existing violations in files you did not touch: list them, do not fix (outside scope).

## DoD (all required for DONE)
- [ ] Project compiles, console 0 errors / 0 new warnings (evidence: MCP console read).
- [ ] EditMode: all existing tests + 3 new lifecycle tests PASS (list every test name with result; the 8 `PhaseCPlaythroughTests` must still PASS).
- [ ] `SE001/SpriteSurface` exists; screenshot via MCP of a test quad with `dev_rounded_rect` casting a rounded shadow onto the board floor (`handoff/phase-C-complete-playable-core/evidence/spritesurface-shadow.png`).
- [ ] `PhaseCVisualMaterials.asset` exists with 4 materials assigned; referenced from the runtime profile.
- [ ] `perf-C-R1.md` committed with numbers for all 3 levels.
- [ ] Style gate output pasted; own files clean.
- [ ] `git diff --check` clean; single commit `C-R1: SpriteSurface, visual materials, lifecycle+perf evidence`.

## Report format
| DoD item | Status | Evidence (test name / file / screenshot path / command output) |
Anything not run → `NOT RUN` (never PASS). Final line: `STATUS: DONE` or `STATUS: BLOCKED — <reason>`.
