SE-001 — EXECUTE BIG PHASE C
Complete Playable Core
(rev 3 — 2026-09-22: rev 2 gameplay rules + sand feel 'Bột mịn' + juice layer + logical/visual decoupling; sections marked [REV] changed vs rev 1, [REV3] added in rev 3)

C2C STATE: PLAN → EXECUTE
Workspace: SE-001 only.
Expected baseline: main @ <Phase-B-closure commit> (commit made by Product Owner after Phase B fixes), clean worktree.

ROLE
Bạn là implementation worker.
Architecture và gameplay contracts bên dưới đã được planning layer + Product Owner chốt.
KHÔNG tự redesign architecture.
KHÔNG mở Phase D/E.
Tự chạy packet liên tục:
implement → compile → targeted tests → fix → packet tiếp theo.
Chỉ BLOCKED khi gặp semantic/product conflict thật sự.

======================================================================
0. PREFLIGHT + CLOSE PHASE B DOCS  [REV]
======================================================================

1. workspace_info phải xác nhận workspaceName == SE-001. Nếu khác: STOP / BLOCKED.

2. git status phải clean.
   Nếu dirty CHỈ do line-ending noise (git diff --ignore-cr-at-eol trống) → ghi nhận và tiếp tục.
   Dirty thật → STOP / BLOCKED, không tự commit/stash.

3. Đọc:
   - AGENTS.md, SE001-ARCHITECTURE-BLUEPRINT.md
   - Docs/runtime-architecture.md, Docs/data-model.md, Docs/glossary.md
   - handoff/ROADMAP.md
   - handoff/phase-B-layout-simulation-foundation/PHASE.md + implementation-notes.html
   - LevelManager / LevelSpawner / LevelContext
   - SandSimulation / LayoutRasterizer / ExtrudedBevelMeshBuilder / fixture
   - Presentation/BoardCameraFitter.cs
   - 5_Shaders/SE001_LayoutSurface.shader, SE001_SandField.shader
   - standards/code-style.md, standards/system-design.md, standards/performance-budget.md
   - Docs/visualizers/sand-feel-lab.html  (REFERENCE for sand feel — preset "Bột mịn" / powder, §8B)

4. Product Owner has accepted current Phase B visual result.

Before Phase C implementation:
- mark Phase B DONE / CLOSURE PASS in canonical docs.
- replace stale B01–B07 PENDING/PARTIAL text with actual final status where evidence exists.
- manual visual acceptance may be recorded as Product Owner accepted.
- record Phase B post-review fixes (already in code, document them — do not redo):
  * ExtrudedBevelMeshBuilder extrudes toward camera (-Z): board plane z = 0, cap at z = -height faces -Z,
    contour winding normalized, separate cap vertices (hard edge), world-unit UV * uvScale (texture repeat).
  * SandFieldVisual quad faces -Z and is sized in board units (cells * cellSize).
  * BoardCameraFitter: contain fit in BoardRoot space (full width on 9:16 and taller, full height on tablet).
  * LevelContext.BoardSize added.
  * SE001/LayoutSurface (lit-lite, ShadowCaster, _BaseMap tiling) + MAT_Wall / MAT_Obstacle.
  * SE001/SandField (unlit alpha) + MAT_SandField.
  * LevelSpawner warns when level JSON missing.
- record known deferred visual debt:
  * Current ExtrudedBevelMeshBuilder does not implement a true geometric polygon inset/fillet.
    Accepted for Phase B. True top-to-side bevel refinement deferred to Phase E.
    It MUST remain visual-only and may never change gameplay masks.
  * Board floor surface is now IN Phase C scope (§8C) — record as "moved to Phase C", not deferred.

5. Update ROADMAP to Big Phase model only. Remove/retire obsolete old phase B–I gate table.
A Foundation — DONE
B Layout + Simulation Foundation — DONE / PASS
C Complete Playable Core — CURRENT / EXECUTABLE
D GD Production Pipeline — PLANNED
E Feel + Performance + Ship Quality — PLANNED

6. Baseline compile/tests/console before Phase C changes. Record evidence.

======================================================================
1. PHASE C GOAL  [REV]
======================================================================

Core loop (Product Owner):
- Player TAPS a Source (jar) to open its valve → sand pours. Tap again → valve closes, pouring stops.
  Each Source holds a finite amount set by GD.
- Player DRAWS lines that become chutes/barriers guiding sand.
- WIN: every Cup filled 100% with its own material.
- LOSE: any grain of a wrong material enters a Cup (immediate),
        OR all Sources are empty, sand settled, and ≥1 Cup not full.

Level JSON
    ↓
LevelSpawner
    ├── Wall / StaticObstacle
    ├── SandSimulation (static + cup-wall + dynamic masks, multi-material)
    ├── Source(s)   (tap valve)
    └── Cup(s)      (wall mask + sink)
            ↓
GameplayManager
    ├── valve commands (tap)
    ├── open Sources emit finite sand
    ├── Simulation steps
    ├── Cup collects accepted material / detects foreign material
    ├── Player Draw → dynamic obstacle mask (ink budget)
    ├── accounting
    └── end-state
          ↓
      WIN / LOSE → Retry / Next → minimal HUD

Phase C closure means:
- open GameScene, load an authored Phase C level
- tap source → head rotates down → visible sand; tap again → stops, head rotates up
- sand collides with static walls/obstacles/cup walls
- player can draw 3D-looking chutes limited by ink
- sand reaches cup, cup visibly/logically collects, full cup stops accepting
- wrong-color sand in cup → LOSE immediately
- deterministic win / lose, Retry, Next over a bounded authored sequence
- no Level Editor/progression save system yet

======================================================================
2. NON-NEGOTIABLE OWNERSHIP  [REV: input + source valve]
======================================================================

LevelManager
- lifecycle / current level identity / retry/reload request.
- does NOT own gameplay rules, emit sand, collect grains, or process draw/tap.

LevelSpawner
- load/validate canonical JSON.
- create roots/state/simulation/entities; create Source/Cup/DrawStroke via Factory.
- bind GameplayManager + GameplayInputController.
- cleanup reverse-order.

GameplayManager
- one per-level gameplay orchestration owner.
- consumes valve commands and draw commands.
- asks Sources for emission, steps simulation, consumes Cup results.
- owns accounting, ink budget, win/lose.
- handles retry/next semantically through LevelManager.
- does NOT instantiate prefabs directly.

SandSimulation
- authoritative sand state, material IDs per cell.
- static mask (Phase B), cup-wall mask, dynamic obstacle mask (Player Draw).
- semantic collection/removal operations for Cup; movement result for stability.
- no HUD/game-over logic.

SourceDomain
- stableId, materialId, logical amount, remaining, cadence, emission geometry.
- valve state machine (see §8). Produces semantic emit requests only while Open.
- does NOT touch Renderer/mesh/animation.

CupDomain
- accepted material, required, collected, full state, foreign-detected flag.
- geometry from authored data only.

GameplayInputController
- converts pointer/touch into TapSource(stableId) or Draw path intents.
- does NOT modify SandSimulation or domains directly; sends commands to GameplayManager.

Visual
- render state only, replaceable child View, no gameplay authority.
- SourceVisual owns head rotation animation; it reports "animation finished" only as a
  presentation signal — gameplay timing uses the domain's deterministic timer (§8).

======================================================================
3. EXISTING BASELINE TO CLEAN UP
======================================================================

- SourceData and CupData already exist in LevelJson.
- GameplayManager.cs and GameplayInputController.cs are commented legacy CH013 code.
- DemoFactory / DemoView CH013 residue exists.
- Do NOT reuse Cake/sticker semantics.
- Replace with clean SE001 implementations or delete residue.
Do not leave two GameplayManager definitions or commented legacy production code after Phase C.
ToolSelectLevelUI stays as development tool; it must keep working through LevelManager.

======================================================================
4. LEVEL DATA EXTENSION  [REV]
======================================================================

Current:
SourceData { stableId, materialId, position, logicalAmount }
CupData    { stableId, acceptedMaterialId, position, size, requiredAmount }

SourceData additions:
- startsOpen (bool, default false)
- optional emissionRate override (grains/second); default from SourceProfile
- optional streamWidth override (board units); default from SourceProfile
Emission direction is always straight down in Phase C (no field).

CupData:
- position = bottom-center of cup in board space (document it), size = outer width/height.
- sink = cup rect inset by SourceProfile/CupProfile wallThickness on left/right/bottom; top edge open.
- requiredAmount set by GD (grains).
- no foreign tolerance field (rule is immediate lose, §11).

LevelData addition:
- drawInkBudget (board units of total stroke length). 0 = drawing disabled. Default from DrawPathProfile.

Validator rules:
- stable IDs unique across all level entities.
- Source logicalAmount > 0; Source inside valid board.
- Cup size positive, requiredAmount > 0, sink overlaps valid board, sink capacity (cells) ≥ requiredAmount.
- material IDs > 0 and defined in MaterialPalette.
- per material: Σ source logicalAmount ≥ Σ cup requiredAmount (else unwinnable → validation error).
- every material that has a Source has ≥1 Cup (else guaranteed lose → validation error).
- Source rect and Cup rect must not overlap static mask.

Do not introduce save/progression schema in Phase C.
Update Docs/data-model.md accordingly (schemaVersion bump + backward-compatible defaults).

======================================================================
5. PROFILES  [REV]
======================================================================

MaterialPalette (ScriptableObject)
- materialId → sand color (Color32), UI color. Used by SandFieldVisual, Source/Cup visuals, HUD.
- SandFieldVisual must color cells by material via palette lookup (no per-frame allocation).

SourceProfile
- emissionRate (grains/step, default 16 = lab powder "rate")
- streamWidth (cells, default 6 = lab powder "width")
- valveOpenDelay (seconds, default 0.2) = head rotation time before emission starts
- valveCloseRotateTime (seconds)
- hitPadding (board units) for tap hit-test

CupProfile
- wallThickness (board units)
- fill presentation defaults, completion feedback timing

DrawPathProfile
- drawThickness (board units)
- minPointDistance
- maxPointsPerStroke, maxStrokes
- defaultInkBudget
- visual extrude height (visual only)
- drawStartDeadZone (pixels) to separate tap from draw

SandVisualProfile  [REV3]
- jitter, highlight, soft (bilinear) — powder defaults (§8B)

JuiceProfile  [REV3]
- all tween durations/eases/amplitudes of §8C in one asset so feel can be tuned without code

GameplayRuntimeProfile
- aggregates MaterialPalette, SourceProfile, CupProfile, DrawPathProfile, SandSimulationProfile,
  SandVisualProfile, JuiceProfile, PrefabProfile
- grainsPerUnit (logical → grains, §8A)
- stableStepsForLose (consecutive no-movement fixed steps)
- fixedStepHz (default 60, matches lab)

Do not put win rules or level-specific amounts in PrefabProfile.

======================================================================
6. PREFAB + VISUAL ARCHITECTURE  [REV]
======================================================================

Under Assets/_Core/3_Prefabs/Gameplay/

Source/SandSource.prefab
SandSource
├── SandSourceDomain
├── SandSourceVisual           (rotation anim, tint by material, open/closed/empty state)
├── Pivot                      (rotation pivot; closed = head up 0°, open = head down 180°)
│   └── View (MeshFilter + MeshRenderer, quad)
└── Anchors/EmitPoint          (visual alignment only)

Cup/Cup.prefab
Cup
├── CupDomain
├── CupVisual
├── View (MeshFilter + MeshRenderer, quad)
├── FillView (quad, fill ratio)
└── Anchors/Entry, Anchors/Feedback

Draw/DrawStroke.prefab
DrawStroke
├── DrawStrokeVisual
└── View (MeshFilter + MeshRenderer, GENERATED EXTRUDED MESH)

2D sprite visuals for Source/Cup (art later; use rectangle placeholder texture now):
- NOT SpriteRenderer. Use quad MeshRenderer + new shader SE001/SpriteSurface:
  URP lit-lite like SE001/LayoutSurface + alpha clip (_Cutoff) + ShadowCaster pass with clip()
  → casts shadow following sprite silhouette. _BaseMap, _BaseColor (tint by material), SRP Batcher.
- Separate materials: MAT_Source, MAT_Cup, MAT_CupFill.
- Known trade-off (document): hard-edged shadow; alpha test costs early-Z, acceptable for few objects.

Draw path visual MUST look 3D, no art dependency:
- accepted polyline → thick outline polygon (thickness from DrawPathProfile, round/miter joins bounded)
  → ExtrudedBevelMeshBuilder (toward -Z) → MeshRenderer.
- new material MAT_DrawPath on SE001/LayoutSurface (or SE001/DrawSurface variant if a distinct look is needed);
  casts + receives shadow.
- while dragging: preview mesh rebuild throttled (only when a new accepted point is added), reusing lists/mesh.
- visual never feeds gameplay; dynamic mask is rasterized from the same accepted polyline + thickness.

Use simple development materials. Final art belongs Phase E.

======================================================================
7. FACTORIES
======================================================================

SourceCreateParameters / SourceFactory
CupCreateParameters / CupFactory
DrawStrokeCreateParameters / DrawStrokeFactory

Factories: acquire/instantiate prefab, validate components, bind data/profile/context,
return runtime entity/domain, deterministic cleanup/reset.
No gameplay decisions in factories. Direct Instantiate allowed; API/cleanup pool-ready.
No generic pooling framework.

======================================================================
8. SOURCE GAMEPLAY  [REV]
======================================================================

Valve state machine (SourceDomain, deterministic, fixed-step time):

Closed --tap--> Opening(valveOpenDelay) --timer done--> Open
Open   --tap--> Closed          (emission stops THIS step; visual rotates head up)
Opening --tap--> Closed         (cancel; no grains emitted)
any    --remaining == 0--> Empty (tap ignored; visual shows empty/closed)

- startsOpen = true → begins in Open at level start.
- Emission only in Open state.
- Opening timer counts fixed steps, NOT animation callbacks.

Emission per fixed step while Open:
→ accumulator += emissionRate * dt
→ convert authored board position (bottom of source) + streamWidth to a bounded cell row
→ SandSimulation.TryEmit per cell with source materialId, up to floor(accumulator) and remaining
→ only successfully inserted grains decrement remaining and accumulator.

Attempted emission != emitted amount. Blocked cells: remaining preserved, retry next step.
Conservation: initial = remaining + emittedIntoSimulation.
No random emission for authoritative count (deterministic cell order).

======================================================================
8A. LOGICAL vs VISUAL UNITS  [REV3]
======================================================================

Product Owner: visual/animation of sand, Source and Cup MAY differ from the exact numbers in data.
Gameplay stays exact; presentation is free to juice.

- GD authors LOGICAL units: SourceData.logicalAmount, CupData.requiredAmount (e.g. 100 / 80).
- Simulation works in GRAINS: grains = logical * GameplayRuntimeProfile.grainsPerUnit (int, e.g. 12).
  Conversion happens once at spawn (Source budget, Cup required). Authoritative accounting is in grains.
- Win/lose, full-cup, conservation tests are computed in grains — never from visual state.
- HUD / labels show logical units: ceil(remainingGrains / grainsPerUnit), floor(collectedGrains / grainsPerUnit).
- Visuals (source content level, cup fill level, counters) interpolate toward the authoritative ratio with
  their own easing/overshoot; they may lag or overshoot for feel but must converge to the true ratio and
  must never be read back by gameplay.
- Cup visual fill is stylized (FillView), NOT a render of sim cells inside the cup. Grains entering the sink
  are removed from the sim (§9) and "arrive" visually through the fill animation.
- Validator converts to grains before capacity checks.

======================================================================
8B. SAND FEEL — preset "Bột mịn" (powder)  [REV3]
======================================================================

Reference: Docs/visualizers/sand-feel-lab.html, PRESETS.powder. Port its model into SandSimulation
(same authority, internals upgraded — this is NOT a second simulation):

Lab values (board 270 px wide = 10.8 units → 25 px/unit):
  cs 1.5 px      → cellSize 0.06 units  (grid 180 × 320 = 57,600 cells for the 10.8 × 19.2 board)
  g .3, vmax 4 (cells/step), repose 1.0, slide .95, splash .5, flow .14
  jitter .35, shade (highlight) .3, sparkle off, soft render ON (bilinear)
  rate 16 grains/step, width 6 cells, 1 substep, step rate 60 Hz (lab runs 1 step per rAF frame)
Put all of these in SandSimulationProfile / SourceProfile / SandVisualProfile as named fields with the powder
values as defaults (create PhaseCSandPowder profile assets). Keep Phase B "basic" behaviour selectable only if
trivial; otherwise replace.

Model to port (per cell, structure-of-arrays, allocated once):
- material (byte), shade (byte, per-grain random tone assigned at emit), velocity (float), momentum (float),
  step stamp (byte) to avoid double-move.
- bottom-up row scan, alternate direction per row, gravity accumulate up to vmax, multi-cell fall,
  splash converts impact speed into lateral momentum, diagonal roll with repose, momentum slide along lines,
  flow (lateral levelling), momentum clamp — exactly as stepSand() in the lab, adapted to board-space
  (lab y grows downward; board y grows upward — invert consistently).
- occupancy blockers: invalid OR static OR cupWall OR dynamic OR occupied.
- Step returns moved-grain count (for §11 stable detection).

Determinism (required by §11 and tests): replace Math.random with a seeded PRNG (xorshift32/PCG) owned by
SandSimulation, seeded from levelId hash + generation-independent constant, advanced in scan order. Same inputs
(same tap/draw commands on the same steps) → identical grid. Emission jitter (lab random x/y offset, initial
vel/mom, shade) uses the same PRNG.

Rendering (SandFieldVisual, port renderSand()):
- CPU pass writes RGBA32 color per cell = palette[material] + jitter*shade offset + surface highlight
  (lighter when cell above is empty, darker when cell below is empty) — no allocation
  (texture.GetPixelData<Color32>() NativeArray reused, Apply(false)).
- Empty cells alpha 0. filterMode = Bilinear (powder soft look), wrap Clamp.
- SE001/SandField shader stays unlit alpha; optionally a tiny alpha-threshold smoothstep for soft edges.
- One quad, one draw call. Budget: ~230 KB texture upload/frame at 180×320 — record it; dirty-row upload is Phase E.

Performance guard for mobile (record, don't over-engineer):
- skip empty rows (track per-row occupied count / active row range) so cost scales with live sand, not grid.
- managed C# is acceptable in Phase C; if step > 2 ms on Editor profiler at 57.6k cells with a full level,
  record it as Phase E Burst/Jobs item (Burst/Collections packages are NOT installed; do not add them in Phase C).

Acceptance (manual, Product Owner): side by side with the lab "Bột mịn" preset, the Unity stream looks as soft
and flowing, piles flatten, sand slides along drawn ramps instead of sticking on shallow slopes.

======================================================================
8C. JUICE / SATISFY LAYER (presentation only)  [REV3]
======================================================================

"Satisfying to watch" is a Phase C acceptance item, not polish. All juice is View-side, driven by domain
events/snapshot, never affects authority. Use DOTween (already in project) or simple coroutine-free tween
helpers; no allocation per frame; kill tweens on unload/retry.

Board:
- board floor quad (light playfield like the demo, e.g. off-white) on SE001/LayoutSurface, receives wall/
  obstacle/stroke/source/cup shadows. Wall material mid-grey like demo.

Source (jar):
- valve open: head rotates down 180° with ease-out-back (slight overshoot), duration = valveOpenDelay;
  emission starts when the deterministic timer ends (§8) — tune so they coincide.
- valve close: emission stops immediately, head rotates up with ease-out.
- while pouring: gentle squash/stretch pulse + micro-shake; content level (inner fill) drains smoothly
  toward remaining ratio.
- empty: desaturate / small "pop" then idle.
- tap feedback even when Empty: small refuse wiggle.

Cup:
- fill level eases toward collected/required (smooth damp), surface wobble when receiving.
- receive punch: small scale punch throttled (max ~6/s).
- full: pop scale + brightness flash + mouth "lid" or check glyph appears; then stays calm.
- wrong material: red flash + shake, then Lose screen after short beat (gameplay already frozen).

Draw stroke:
- preview mesh follows finger; on commit extrude height animates 0 → full (ease-out-back, ~0.15 s)
  — visual only, mask is already stamped at commit.
- ink/Energy bar eases, turns red when low.

Sand:
- powder look per §8B (soft bilinear, tone jitter, surface highlight).
- stream coherence: emission width 6 cells + per-grain tone keeps stream readable in each material color.

End state:
- Win: cups bounce in sequence, then Win panel. Lose: shake, then Lose panel with reason.

Out of Phase C (still Phase E): particle VFX, sound, haptics, screen-space post effects.

======================================================================
9. CUP COLLECTION  [REV]
======================================================================

Geometry (from CupData only):
- cup-wall cells: left/right/bottom walls of cup rect (wallThickness) → cup-wall mask (blocks movement).
- sink cells: interior of cup rect.
- top edge open.

Each fixed step, iterate ONLY sink cells:
- accepted material grain:
    if cup not full → remove from simulation, collected += 1
    if cup full → grain is NOT removed (see closed mouth below)
- foreign material grain → raise ForeignMaterialEntered(cupId, materialId) → LOSE (§11). Freeze.

Full cup (collected >= required):
- cup marks Full, stops collecting.
- "mouth closes": the sink cells become part of cup-wall mask (blocked), so further sand piles on top
  of the cup or flows elsewhere. Grains already occupying sink cells at that instant: keep them in place
  (they cannot move out because sink is now blocked; count them as inField, not collected).
- Full is not a lose.

Never collect more than required. No Renderer/Collider queries.

CupVisual: fill = clamp01(collected / required), full feedback, foreign-lose feedback.

======================================================================
10. ACCOUNTING MODEL  [REV]
======================================================================

Per material:
emittedFromSources = inField + collectedInCups
lost = 0 (board bottom is closed by valid mask in Phase C — document).

Also per source: initial = remaining + emitted.
Ink: inkUsed + inkRemaining = drawInkBudget.

Required checks:
- Source decrement == successful insertion.
- Cup removal decrements inField, increments collected exactly once.
- Full cup never increments.
- Retry/reload → fresh state.
Expose read-only snapshot (per source remaining/state, per cup collected/required/full, ink, game state).

======================================================================
11. WIN / LOSE SEMANTICS  [REV]
======================================================================

WIN (checked each step after collection):
- every Cup Full.

LOSE — immediate:
- any foreign-material grain enters any Cup sink (reason = WrongCup, cupId, materialId).

LOSE — settled:
- every Source is Empty (remaining == 0)
AND no Opening/Open source with remaining > 0
AND simulation reports no movement for stableStepsForLose consecutive steps
AND ≥1 Cup not Full
(reason = NotFilled).

NOT lose:
- Sources closed while they still have sand (player may reopen) — even if field is stable.
- Sources empty while grains are still moving.

No real-time timeout. SandSimulation.Step must return moved-grain count (or changed flag).

Once game over: freeze authoritative gameplay (no emission, no step, no draw, no tap).
Win check has priority over settled lose in the same step; WrongCup lose has priority over win
(foreign grain detected in the same step as last cup filling → LOSE). Document.

Events: LevelWon, LevelLost(reason), SourceStateChanged, CupChanged, InkChanged, GameStateChanged.

======================================================================
12. PLAYER DRAW  [REV]
======================================================================

Draw allowed any time during Playing (sand may be flowing), limited by ink.

pointer down (not on a Source, not over UI)
→ wait until drag > drawStartDeadZone, then start stroke
→ project to board XY (shared mapping §13)
→ min-distance filtering, reject points outside valid board
→ consume ink by accepted segment length; stop adding points when ink is 0
→ preview visual
→ pointer up commits accepted stroke (≥2 points)
→ GameplayManager validates
→ shared rasterizer stamps polyline+thickness into dynamicObstacleMask
→ DrawStrokeVisual finalizes same accepted path

Stroke becomes a barrier only on commit (pointer up). Preview has no gameplay effect.
Ink is consumed while drawing; a cancelled/invalid stroke (<2 points) refunds its ink.
No erase in Phase C.

Rules:
- use shared Phase-B geometry/rasterization rules; no Physics collider as truth.
- dynamic mask separate; static mask and cup-wall mask never mutated by draw.
- stroke may not be stamped over Source rects or Cup sink cells (skip those cells) — document.
- max points per stroke, max strokes (profile).
- drawn through existing sand: grains stay in their cell that instant; future moves cannot enter blocked cells.
- retry/unload clears dynamic mask, strokes, ink.

SandSimulation occupancy: cannot move into invalid OR static OR cupWall OR dynamic.

======================================================================
13. GAMEPLAY INPUT  [REV]
======================================================================

Replace commented CH013 GameplayInputController. Project uses Input System 1.19 (activeInputHandler = Both);
use Input System pointer/touch, no new packages.

Phase C input:
- pointer down → board point → hit-test Source rects (authored position/size + hitPadding, board space, no Physics)
  → if hit and pointer released within drawStartDeadZone: TapSource(stableId) command.
- otherwise → draw (§12).
- UI blocking check (EventSystem).
- bind/unbind current LevelContext/GameplayManager; readiness + generation guard.
- no camera orbit/x-ray/sticker semantics.

Inactive when: no level ready, loading/unloading, game over.

Screen → board: ray from active gameplay camera to BoardRoot plane (z = 0 local), convert to BoardRoot local XY.
BoardRoot may be under an offset parent — always go through BoardRoot transform. One reusable service.

======================================================================
14. GAMEPLAY MANAGER  [REV]
======================================================================

One deterministic fixed-step owner (own accumulator at fixedStepHz, max N steps per frame).

if Playing:
    1. apply queued valve commands (tap)
    2. commit pending accepted draw strokes → dynamic mask
    3. advance valve timers; open Sources emit accepted grains
    4. SandSimulation.Step() → moved count
    5. scan Cup sinks → collect / detect foreign / close full mouths
    6. update accounting + ink snapshot
    7. raise domain events for visuals/HUD
    8. evaluate win/lose (§11 priority)

Any Phase B dev runner that steps SandSimulation must not double-step when GameplayManager is bound.

======================================================================
15. SPAWNER INTEGRATION  [REV]
======================================================================

load + validate data
→ RuntimeState → roots
→ SandSimulation (static/valid masks + empty dynamic mask)
→ wall/obstacle visuals
→ create Cups (register cup-wall mask + sink regions)
→ create Sources
→ SandField (palette)
→ GameplayManager.Bind(...)
→ GameplayInputController.Bind(...)
→ ready/input open

Masks exist before any emission. BoardCameraFitter keeps working via LevelReady.

Unload: input unbind → stop tick → cancel token → clear strokes → cleanup Source/Cup → dispose simulation → destroy roots.
Old generation callbacks cannot affect new level.

======================================================================
16. MINIMAL HUD  [REV]
======================================================================

Functional, not final art (layout reference: demo — level label top-left, energy bar top-right).
- level id label
- Ink / Energy bar (inkRemaining / budget)
- per cup collected / required (small label near cup or list)
- per source remaining (label near source)
- game state Playing / Win / Lose(+reason)
- Retry always visible; Next enabled after Win; Retry after Lose.
HUD listens to GameplayManager events/snapshot only.
No progression save, no level-select production screen.

======================================================================
17. RETRY / NEXT
======================================================================

Retry: LevelManager.ReloadCurrentLevel(); fully resets sources, valves, cups, accounting, ink,
dynamic masks, strokes, game-over state, simulation.

Next: PhaseCLevelSequence ScriptableObject (phase_c_level_01, phase_c_level_02, phase_c_level_03).
Disable Next on last level. No PlayerPrefs/save/unlocks.
GameScene bootstrap auto-loads first level of the sequence (development flag, default on);
ToolSelectLevelUI still works.

======================================================================
18. AUTHORED PHASE C LEVELS  [REV]
======================================================================

Do NOT mutate phase_b_svg_test.

phase_c_level_01.json
- simple board, 1 Source (material 1), 1 Cup (material 1), ≥1 StaticObstacle
- winnable by tapping only (no draw); ink budget small
- win/retry smoke

phase_c_level_02.json
- 1 Source + 1 Cup where Player Draw is required to redirect sand; ≥1 StaticObstacle

phase_c_level_03.json
- 2 materials (e.g. 1 = yellow, 2 = red), 2 Sources, 2 Cups
- wrong-cup lose reachable if player opens the wrong valve or draws badly
- winnable with draw + valve timing

Readable IDs: source_yellow, cup_yellow, obstacle_...
Amounts: source logicalAmount modestly > cup requiredAmount (e.g. +15%) — GD tunes later.

======================================================================
19. WORKER PACKETS  [REV]
======================================================================

handoff/phase-C-complete-playable-core/
    PHASE.md, implementation-notes.html
    worker/ C01-source-valve.md, C02-cup-accounting.md, C03-player-draw.md,
            C04-gameplay-orchestration.md, C05-end-state-hud.md, C06-retry-next-levels.md, C07-integration.md

Packets are NOT gates. Execute continuously.

C01 Source + Valve: data/profile/palette/prefab/domain/visual(rotation)/factory, SE001/SpriteSurface shader.
    Exit: conservation PASS; valve state machine tests PASS (opening delay, cancel, empty).
C02 Cup + Accounting: cup wall mask, sink, collect, full-mouth close, foreign detection.
    Exit: exactly-once collection; full cup stops; foreign detected.
C03 Player Draw: input intent, ink, dynamic mask, extruded 3D stroke visual, cleanup.
    Exit: barrier changes sand path; static/cup masks unchanged; ink conserved.
C04 Orchestration: GameplayManager, GameplayInputController (tap vs draw), spawner bind/unbind.
    Exit: tap → source → simulation → cup integrated.
C05 Win/Lose + HUD. Exit: deterministic game-over tests PASS (all §11 cases + priorities).
C06 Retry / Next + 3 authored levels. Exit: retry resets; next switches; next disabled on last.
C07 Full integration + 10-cycle + manual Play smoke.

======================================================================
20. TEST MATRIX  [REV additions marked +]
======================================================================

SOURCE: finite amount; blocked emission keeps remaining; decrement == inserted; material preserved; retry resets
 + closed source never emits
 + Opening delay: no grain before valveOpenDelay steps
 + tap during Opening cancels
 + Open→Closed stops same step
 + Empty ignores tap
 + startsOpen honored
CUP: accepted collected; required threshold; no double collection; deterministic sink mapping
 + cup walls block movement (grain beside cup does not enter sideways)
 + full cup: no further collection, mouth blocked, sand piles
 + foreign grain in sink → WrongCup event
PALETTE: + SandField color per material; unknown material rejected by validator
ACCOUNTING: emitted = inField + collected; movement keeps total; reload fresh
 + ink used + remaining = budget; cancelled stroke refunds
DRAW: pointer→board path (incl. offset BoardRoot parent); min-distance; thickness raster;
      static mask unchanged; dynamic mask changed; reload clears; max guards; old generation ignored
 + ink exhausted stops stroke growth
 + stroke skips source rects / cup sink cells
 + tap vs draw dead-zone
SAND: Phase B tests PASS (update expectations where the powder model legitimately changes them); dynamic mask blocks;
 + Step returns moved count
 + seeded determinism: same seed + same commands → identical Cells after N steps
 + grain count conserved by Step (no loss/dup with multi-cell fall + momentum)
 + momentum: grain slides along a shallow (<45°) ramp further than pure CA
UNITS: + logical→grains conversion; HUD shows logical; win uses grains; visual never read by gameplay
GAME STATE:
 + all cups full → WIN
 + foreign grain → LOSE immediately (even if other cups full)
 + sources closed with sand left + stable field → NOT lose
 + sources empty, grains moving → NOT lose
 + sources empty + stable N steps + cup not full → LOSE NotFilled
 + game over blocks tap/draw/emission
 + deterministic repeat (same inputs → same result)
LIFECYCLE: load/reload/unload/Retry/Next/10 cycles; no growing roots/entities/strokes; no stale subscriptions
HUD: semantic state only; Retry/Next wired; ink bar updates.

======================================================================
21. PERFORMANCE RULES
======================================================================

Keep Phase B guards. Additionally:
- no per-frame LINQ, no allocation per grain, no Instantiate per grain.
- cup scan iterates sink cells only; source emission bounded row.
- dynamic mask stamp only affected cells on commit.
- stroke mesh rebuild only on accepted point add (preview) and once on commit; reuse buffers.
- SandField texture upload once per frame max; palette lookup array, no Color allocations.
- no FindObjectOfType in hot path; no duplicate simulation runners.
- sand step skips empty rows; all per-cell arrays allocated once per level.
- juice tweens: no per-frame allocation, pooled/killed on unload.
- alpha-clip sprite shader only on Source/Cup (few renderers).
Record: grid size, source/cup/stroke count, fixed-step CPU if measurable, steady-state GC.

======================================================================
22. OUT OF SCOPE
======================================================================

Level Editor; New/Open/Save authoring; progression save/unlocks/PlayerPrefs; final level sequence system;
final art; VFX polish; sound/haptics; quality tiers; true geometric bevel; ads/analytics/IAP;
stroke erase/undo; material rules beyond "own material only / wrong cup = lose".

======================================================================
23. STOP CONDITIONS
======================================================================

BLOCKED only if:
- a gameplay rule needed is not defined by this contract.
- collection requires changing the authoritative geometry source.
- Source/Cup semantics conflict with current JSON with no safe migration.
- SandSimulation cannot support dynamic/cup-wall masks or material IDs without architectural rewrite.
- gameplay requires another simulation authority.
- baseline does not compile due to an unrelated, non-isolatable issue.

Do NOT stop for: local naming, prefab/material choice, helper placement, test refactor, HUD layout, animation easing.

======================================================================
24. PHASE C CLOSURE GATE  [REV]
======================================================================

[ ] Phase B docs formally closed (incl. post-review fixes + deferred debts).
[ ] CH013 GameplayManager/Input residue removed/replaced.
[ ] Source valve tap on/off with rotation anim; emission only after open delay.
[ ] Source amount finite and conserved.
[ ] Cup walls block; sink collects own material; full cup closes mouth.
[ ] Wrong material in cup → immediate LOSE.
[ ] MaterialPalette colors sand/source/cup.
[ ] Player Draw with ink budget updates dynamic mask; 3D extruded stroke visual with own material.
[ ] static + cup-wall masks immutable by draw.
[ ] GameplayManager owns single deterministic fixed-step flow.
[ ] GameplayInput tap vs draw, binds/unbinds per level.
[ ] Win works; settled Lose waits for empty sources + stable field.
[ ] Retry fully resets; Next loads next authored level; disabled on last.
[ ] minimal HUD incl. ink bar.
[ ] Sand model ported from sand-feel-lab "Bột mịn" (profile defaults = powder values), seeded deterministic.
[ ] SandField soft powder render (palette + jitter + highlight, bilinear).
[ ] Juice layer §8C implemented via JuiceProfile; board floor visible; kills tweens on retry/unload.
[ ] Logical vs grain units per §8A.
[ ] GameScene playable without test runner.
[ ] level_01 completable by tap only; level_02 needs draw; level_03 two materials.
[ ] 10 lifecycle cycles pass.
[ ] compile clean; EditMode tests pass; PlayMode/integration tests pass.
[ ] manual Play walkthrough: PENDING MANUAL (Product Owner) unless actually performed with a tool.
[ ] console has no new Phase C errors/warnings.
[ ] git diff --check PASS.
[ ] no Phase D/E implementation leaked in.

Then Phase C = DONE / CLOSURE PASS (manual walkthrough may remain PENDING MANUAL), update ROADMAP,
do NOT start Phase D.

======================================================================
25. MANUAL PLAY WALKTHROUGH (Product Owner)
======================================================================

Feel check first: open sand-feel-lab.html preset "Bột mịn" beside Unity Play and compare stream/pile/ramp slide.
Level 01: Play → level_01 loads → tap source (head rotates down, sand after delay) → tap again stops
→ sand hits wall/obstacle → cup fills, HUD updates → full cup stops accepting → Win → Retry pristine → Win → Next.
Level 02: tap source → draw chute (ink bar drops) → sand redirected → cup fills → Win; Retry clears strokes/ink.
Level 03: open wrong valve / misroute → red sand into yellow cup → immediate Lose(WrongCup) → Retry → correct play → Win.
Capture hierarchy/state evidence in implementation notes.

======================================================================
26. FINAL REPORT
======================================================================

C2C: EXECUTED
Status: DONE / BLOCKED
Observable result: what can a player actually do now?
Packets: C01..C07 PASS/FAIL
Gameplay contracts: Source valve, Cup, Draw+ink, accounting, win/lose, retry/next
Architecture: ownership, sand authority, static/cup-wall/dynamic masks, View separation
Verification: compile, tests, manual Play (or PENDING MANUAL), levels 01–03, retry, next, 10-cycle, console, git diff --check
Performance: observed values (grid size, active grains, step ms, texture upload KB/frame, GC).
Deferred: true bevel → Phase E; Burst/Jobs sand step + dirty-row upload → Phase E if budget exceeded;
VFX/sound/haptics → Phase E; stroke erase → later.
Do not claim checks that were not run.
