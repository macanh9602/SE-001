# Win/Lose panel invisible — root cause and fix (2026-09-23)

## Symptom
Level reached `State: Won` (cup FULL) but no result panel appeared. No Console error.

## Evidence chain
1. Runtime audit (`AgentAudit/phase-c-result-ui.md`, hooks since removed):
   - `bind`: 2 HUDSystem found; [0] scene instance, active, `hasWin=True hasLose=True`, `rootUI=PanelRoot`; [1] prefab asset (filtered out by `scene.IsValid()`).
   - `state`: `Won | hud=HUDSystem` → the controller received the event.
   - `show-win`: `panel=GameWin_Canvas(Clone) activeSelf=True activeInHierarchy=True parent=PanelRoot canvas=enabled order=0 ScreenSpaceOverlay` **but `scale=(0,0,0)`**.
2. Inspector of the clone: Width 0, Height 0, Pivot 0, Scale 0, Scale field marked as driven.
3. Prefab file `GameWin_Canvas.prefab`, root RectTransform: `m_LocalScale: {x: 0, y: 0, z: 0}`, `m_SizeDelta: {x: 0, y: 0}`, `anchorMin = anchorMax = 0`.

## Root cause
`GameWin_Canvas` / `GameLose_Canvas` are **root-Canvas prefabs**. A root Canvas drives its own RectTransform, so the serialized values are zeros. `HUDSystem` instantiates them **as children of `PanelRoot`**, where the Canvas no longer drives the RectTransform — the panel keeps the zeros: size 0 and scale 0. It exists and is active, but is invisible.

## Fix
`UIPanels.NormalizePanelRect(Panel)` runs right after `Instantiate`: scale 1, identity rotation, anchors stretched 0→1, offsets 0, pivot centre, z = 0, and the nested `CanvasScaler` is disabled (it does nothing on a child canvas and fights the parent canvas scaling).

Applied at the shared panel-creation path, so it covers Win, Lose and any future canvas-style panel prefab.

## Guard
`PhaseCResultHudController` now logs an explicit error when no usable HUDSystem is found, instead of failing silently.
