# Agent Audit: phase-c-draw
Generated: 2026-09-22T17:03:36.9201075+07:00
Scope: Phase C draw bug — level phase_c_level_01 gen 2

## 1 · Input.Down
- frame: 965
- time: 12,0647
- details: screen=(569, 1540) screenSize=1080x1920 board=(5.69, 15.40) overUi=True uiObj=BG guiRect=(x:13.96, y:209.45, width:453.82, height:338.62) blocked=False state=Playing

## 2 · Input.DrawStart
- frame: 970
- time: 12,1198
- details: screen=(569, 1495) startBoard=(5.69, 15.40)

## 3 · Input.Up
- frame: 994
- time: 12,3852
- details: drawing=True points=20 lastBoard=(3.17, 11.13) dragPx=497

## 4 · Commit.Request
- frame: 994
- time: 12,3860
- details: bound=True state=Playing points=20 first=(5.69, 15.40) last=(3.17, 11.13) thickness=0,300 ink=6,00 strokes=0/16

## 5 · Commit.Stamped
- frame: 994
- time: 12,3876
- details: accepted=20 radius=0,150 cellsR=2 inkLeft=0,25 dynamicCellsTotal=177 dynBounds=(30,110)-(57,154) grid=108x192 cell=0,100

## 6 · Commit.VisualOk
- frame: 994
- time: 12,3909
- details: <none>

## 7 · Input.Down
- frame: 1122
- time: 13,8413
- details: screen=(520, 1778) screenSize=1080x1920 board=(5.20, 17.78) overUi=False uiObj=- guiRect=(x:13.96, y:209.45, width:453.82, height:338.62) blocked=False state=Playing

## 8 · Input.Up
- frame: 1129
- time: 13,9193
- details: drawing=False points=0 lastBoard=- dragPx=0

## 9 · Input.Tap
- frame: 1129
- time: 13,9203
- details: board=(5.20, 17.78) hit=source_yellow

