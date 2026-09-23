# Phase V1 — Jar Visual (Source + Cup official art, 7-color ColorProfile)

C2C flow: **INIT → PLAN → EXECUTED → DONE | BLOCKED**

Workspace: `SE-001` only. Planning owner: Claude (reviewed by Ducan). Implementation + verification owner: Codex.

Packet order: **V1 → D-A → D-B → D-C**. D-B (Level Editor) consumes the preview utility built here, so V1 must be DONE first.

Do not commit or push unless explicitly requested. Preserve unrelated dirty files.

References in this folder:

* `reference/jar-reference.png` — target composition (screenshot from original game).
* `reference/CakeBasic.shader` — tint math reference (`ApplyHsl`, `ApplyCakeTint`, sRGB tuning space). **Reference only — do not copy the file into Assets.**
* `reference/jar-tint-lab.html` — browser mirror of the tint math with the approved per-color tuning. Open locally to compare.
* `jar-tint-tuning.json` — approved default values for the 7 colors (source of truth for the material generator).

---

# 1. Locked decisions (Ducan, 2026-09-23)

| # | Decision | Chosen |
|---|---|---|
| V1-1 | Sand inside Source | **Fill layer shader** behind the glass, silhouette mask, level = remaining / amount, sand settles world-down, sparkle noise. Not simulation particles. |
| V1-2 | Cup size mapping | **9-slice caps**: cap thickness fixed, body stretches in height, width scales uniformly. **Taper = 0**, Cup gameplay geometry becomes a rectangle matching the art. |
| V1-3 | Shadow | **Fake drop shadow**: silhouette sprite, multiply ×0.80, offset ≈ (-6, -24) px at native art scale. No shadowmap for Source/Cup. |
| V1-4 | Materials / colorId | **14 material assets** (7 `MAT_CupCap_<Color>` + 7 `MAT_SourceMouth_<Color>`), referenced from ColorProfile. Ids: 1 Red (was Coral), 2 Cobalt (was Blue), 3 Yellow, 4 Purple, 5 Pink, 6 Green, 7 White. Existing levels keep working. |
| V1-5 | Source pose | **Keep Phase C 180° rotation.** Idle = mouth up; open = pivot rotates to 180° so mouth points at the emit point. Art is authored mouth-down, so the idle View is the art rotated 180°. |
| V1-6 | Source size | **Uniform scale from `size.y`**; visual width derived from art aspect. Hit-test and `BodyOffset` use the real visual bounds. |
| V1-7 | Cup FillLine | **Removed.** Set `CupProfile.fillLine = 1.0` **only if** it is visual-only (see §6.3). |
| V1-8 | Editor visual | Level Editor canvas must draw real Source/Cup art per colorId (utility delivered here, consumed in D-B). |

Do not reopen these. If implementation proves one impossible, report `BLOCKED` with evidence.

---

# 2. Art facts (measured, do not re-derive by eye)

All four textures in `Assets/_Core/0_Texture2D/Env` share PPU 100 and were drawn at the same scale — compose them 1:1.

| Texture | Size | Content |
|---|---|---|
| `T_Cup_Body` | 156×130 | Glass, semi-transparent (center alpha ≈ 38), content x 6–148. |
| `T_Cup_Head` | 182×217 | **Two caps in one image**: top cap rows 6–41 (x 10–170), bottom cap rows 172–203 (x 12–168). Middle fully transparent. |
| `T_Source_Body` | 156×192 | Glass outline, **interior alpha 0**, row-convex. Wide top, neck bottom rows ~185–191 (x ~46–108). |
| `T_Source_Mounth` | 71×28 | Opaque cap. Centered on the neck, overlaps body bottom by ~8 px. |

Composition (top-left origin, pixels):

* Cup: body at x = 13, y = 41 inside the head frame; caps drawn over body.
* Source: mouth at x = 42.5, y = 184 (body height − 8). Composite height 212.

All textures are near-white (L ≈ 0.82–0.94) → tinting by multiply works; no per-color art.

Import settings to fix: set `T_Cup_Head` to **Sprite Mode Multiple** with two rects `CupCap_Top` (rows 0–44) and `CupCap_Bottom` (rows 168–217). This is both the 9-slice solution and removes the transparent middle overdraw. Keep the file name (typo `Mounth` included) to avoid breaking GUID references; display names may be corrected.

---

# 3. Rendering architecture

## 3.1 Renderer type

Keep the Phase C prefab contract style: **MeshRenderer + shared quad meshes** (one mesh asset per sprite part with UVs fitted to the sprite rect, created once by the editor setup tool, never per instance). Reason: stable normals/UVs, consistent with existing prefab tests, SRP Batcher compatible.

Do not create per-instance `Material` instances (`renderer.material`). Only `sharedMaterial` + MPB where §3.4 allows.

## 3.2 Shaders (new, `Assets/_Core/5_Shaders/`)

Put the tint math in one include, e.g. `SE001_JarTint.hlsl`, ported from CakeBasic `RgbToHsl / HslToRgb / ApplyHsl / ApplyCakeTint` including the Linear↔sRGB tuning-space macros. The CPU mirror (§5) must match this include exactly.

| Shader | Use | Key properties | Blend |
|---|---|---|---|
| `SE001/JarTint` | Cup caps, Source mouth, both glass bodies (glass = white, identity params) | `_BaseMap`, `_Color`, `_Hue`, `_Saturation`, `_Brightness`, `_Colorize` | Transparent, `SrcAlpha OneMinusSrcAlpha`, ZWrite Off, unlit |
| `SE001/JarSandFill` | Sand inside Source | `_MaskTex` (baked inner silhouette), `_SandColor`, `_FillLevel` (0–1, area-correct), `_FillDirOS` (world-down in object space), `_NoiseTex`, `_Sparkle` | Transparent, alpha from mask |
| `SE001/JarShadow` | Drop shadow | `_MaskTex` (silhouette), `_ShadowMul` (0.80) | **Multiply** (`DstColor Zero`), so it is correct over board and walls alike |

Unlit is intentional: the art is pre-shaded. No `_MAIN_LIGHT_SHADOWS` keywords → fewer variants. Target `#pragma target 2.0` like CakeBasic unless a feature needs more.

## 3.3 Draw order (back → front)

Board/layout → sand field + draw path → **JarShadow** → Source sand fill → glass bodies → caps/mouth.

Falling sand entering a Cup must appear **behind** the Cup glass and caps. Resolve with render queue offsets on the materials (not per-frame sorting code). Verify with a Play Mode screenshot where sand is inside a Cup.

## 3.4 Per-instance data

* Cap/mouth color: material swap by colorId (shared materials, SRP Batcher friendly).
* Source `_FillLevel` / `_FillDirOS`: MPB on the fill renderer only (≤ ~8 Sources per level; acceptable). Update only when value changes.
* Empty Source: `_FillLevel = 0`; mouth keeps its color (remove the old "lerp to gray" body tint).

## 3.5 Source fill level (V1-1, V1-5)

* Bake per-row inner span table from `T_Source_Body` alpha (row-convex, inset 7 px, skip top 10 / bottom 6 rows) → cumulative-area LUT.
* `_FillLevel` must be area-correct, not height-linear (the jar is a trapezoid).
* Settles toward **world-down**: idle (mouth up) → sand at the wide end; after 180° → sand at the mouth end. During the rotation (≤ `valveOpenDuration`) interpolating between the two end states is acceptable.
* Fill ratio source of truth: Source domain remaining / authored amount. No new gameplay state.

## 3.6 Shadow

One shadow quad per Source and per Cup using a baked silhouette (Source = body + mouth, Cup = caps ∪ body). It rotates with the Source pivot. Offset is in world space (does not rotate).

---

# 4. Data changes

## 4.1 ColorProfile

Extend `ColorProfileEntry` (keep existing fields; `sandColor` still drives the simulation):

```csharp
public Material cupCapMaterial;
public Material sourceMouthMaterial;
```

Tint parameters live **only in the materials** (single source of truth). Update `PhaseCColorProfile.asset` to the 7 entries of `jar-tint-tuning.json` (`sandColor`, `uiColor = sandColor`, `displayName`).

Validation (edit + runtime load): every colorId used by a level exists in the profile and has both materials assigned; otherwise a clear GD-readable error.

## 4.2 Generator tool

Editor menu `SE001/Visual/Rebuild Jar Materials`:

* reads `jar-tint-tuning.json` (copy it to an editor-accessible path, e.g. `Assets/_Core/Editor Default Resources/` or keep path constant in the tool),
* creates/updates the 14 materials **in place** (preserve GUIDs on rerun),
* assigns them into ColorProfile,
* idempotent, deterministic, no duplicate assets on rerun.

Also bakes (same menu or `SE001/Visual/Bake Jar Masks`): Source inner fill mask, Source silhouette, Cup silhouette, Source fill LUT → stored in a `JarVisualProfile` ScriptableObject together with the composition constants of §2. Presentation, editor preview and gameplay geometry read dimensions from **this one profile** — no duplicated magic numbers.

## 4.3 Geometry

* Cup: `taper = 0` in `CupProfile`; Cup domain walls/mouth/sink become a rectangle aligned to the art (inner glass width). Wall thickness derived from art (glass edge ≈ 6–8 px at native scale) via `JarVisualProfile`, not guessed.
* Source: `BodyOffset` and tap hit-test use the uniform-scaled visual bounds (V1-6). Emit point stays `source.Position` (mouth when rotated 180°).
* Cup Size: width → uniform scale of caps + body width; height → body stretch; caps keep native thickness × width scale.

---

# 5. Editor preview utility (for D-B)

`JarPreviewUtility` (Editor assembly):

* Produces a composed `Texture2D` for (Source | Cup, colorId, size, fill state) by mirroring `SE001_JarTint.hlsl` on CPU (same sRGB tuning space).
* Cached by key; invalidated when ColorProfile, any of the 14 materials, or `JarVisualProfile` change (content hash, not every repaint).
* No allocation per repaint; textures `HideAndDontSave`, destroyed on cache clear / domain reload.
* Includes the drop shadow so the canvas matches the game.

Parity test: render each of the 7 cap materials to a RenderTexture, compare with CPU mirror; mean ΔRGB ≤ 2/255, max ≤ 6/255 on opaque texels.

---

# 6. Prefab + presentation work

## 6.1 Source (`SandSource.prefab`, `PhaseCSourceVisual`)

`Pivot/View/{Shadow, SandFill, Body, Mouth}`. Rename `Nozzle` → `Mouth` only if tests/references are updated in the same change; otherwise keep the name `Nozzle` and document it. Keep `Anchors/EmitPoint`.

## 6.2 Cup (`Cup.prefab`, `PhaseCCupVisual`)

Replace procedural `WallL/WallR/WallB/Back/FillLine/Rim` visuals with `View/{Shadow, Body, CapTop, CapBottom}`. Remove generated mesh code that is no longer needed (and its per-bind mesh allocations). Keep the ForeignDetected feedback (currently red pulse on FillView) — apply it to the caps via material/MPB and report what was chosen.

## 6.3 FillLine (V1-7)

Before changing `CupProfile.fillLine`, grep whether it affects accounting / win condition / capacity. If visual-only → set 1.0 and delete the FillLine visual. If it affects gameplay → do **not** change the value; only remove the visual; report in EXECUTED.

## 6.4 Old materials

`MAT_Source`, `MAT_Cup`, `MAT_CupBack` become unused → leave the files, mark in report as candidates for deletion (do not delete without approval). Update `PhaseCVisualMaterials` accordingly.

---

# 7. Tests

Update existing tests broken by the new hierarchy (`C1ProductionVisualTests`, `PhaseCFinishTests`, `PhaseCPlaythroughTests`) — change assertions to the new contract, never delete coverage silently.

Add:

* `ColorProfile_HasSevenEntries_WithBothMaterials`
* `ColorProfile_LegacyIds1And2_StillResolve`
* `JarMaterials_Rebuild_IsIdempotent_PreservesGuids`
* `SourceFill_LevelIsAreaCorrect` (LUT: 50 % area ≠ 50 % height)
* `SourceFill_SettlesWorldDown_AtIdleAndPouring`
* `Cup_Taper0_GeometryMatchesArtRect`
* `Cup_NineSlice_CapThicknessConstantAcrossHeights`
* `Source_UniformScale_HitTestMatchesVisualBounds`
* `JarPreview_CpuMirror_MatchesGpu` (§5 tolerance)
* `JarPreview_Cache_NoRebuildWithoutChange`

---

# 8. Evidence

`handoff/phase-V1/evidence/`:

* Play Mode screenshots, all 7 colors on one board (Source idle, Source pouring, Cup empty, Cup with sand inside).
* Side-by-side with `reference/jar-reference.png` at the same zoom.
* Frame Debugger / draw-call count for a level with 3 Source + 3 Cup: before vs after.
* Editor screenshot of `JarPreviewUtility` output grid (7 colors × Source/Cup).

Performance guard (mobile-first): no GC alloc per frame from Source/Cup visuals (Profiler GC Alloc = 0 in steady state), no new per-frame material/texture/mesh creation.

---

# 9. Verification + reporting

Same rules as Phase D spec §14–16: compile clean, console clean, full EditMode suite, scoped style gate, `git diff --check`, no invented PASS.

## Definition of DONE

```text
[x] T_Cup_Head sliced into CupCap_Top / CupCap_Bottom.
[x] SE001_JarTint.hlsl ported from CakeBasic; JarTint / JarSandFill / JarShadow shaders compile.
[x] 14 materials generated from jar-tint-tuning.json; rerun idempotent.
[x] ColorProfile = 7 entries, ids 1–7, legacy levels load.
[x] JarVisualProfile holds all composition constants + baked masks/LUT.
[x] Source prefab: shadow, sand fill (area-correct, world-down), glass, mouth; 180° rotation kept.
[x] Cup prefab: shadow, glass, 9-slice caps; taper 0 geometry matches art.
[x] FillLine handled per §6.3 with the visual-only boundary preserved.
[x] Draw order verified (sand inside Cup behind glass).
[x] JarPreviewUtility + CPU/GPU parity test.
[x] Targeted tests updated/added and PASS.
[x] Screenshot + draw-call + GC evidence packet waived and accepted by Product Owner; no numeric PASS is claimed.
[x] Compile / console / scoped style / diff-check clean; full-suite baseline exceptions are recorded and accepted.
```

Final state: `DONE — Product Owner manual acceptance (2026-09-23)`. Full-suite baseline failures and missing numeric profiler evidence remain documented deviations, not blockers for this visual-only packet.
