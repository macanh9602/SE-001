# Agent Audit: phase-c-draw
Generated: 2026-09-22T16:34:08.7363100+07:00
Scope: Phase C draw bug — level phase_c_level_01 gen 1

## 1 · Input.Down
- frame: 174
- time: 3,2894
- details: screen=(838, 1334) screenSize=1080x1920 board=(8.38, 13.34) overUi=True uiObj=BG guiRect=(x:13.96, y:209.45, width:453.82, height:338.62) blocked=False state=Playing

## 2 · Input.DrawStart
- frame: 176
- time: 3,3181
- details: screen=(844, 1314) startBoard=(8.38, 13.34)

## 3 · Input.Up
- frame: 241
- time: 4,0809
- details: drawing=True points=41 lastBoard=(7.27, 10.91) dragPx=268

## 4 · Commit.Request
- frame: 241
- time: 4,0816
- details: bound=True state=Playing points=41 first=(8.38, 13.34) last=(7.27, 10.91) thickness=0,300 ink=6,00 strokes=0/16

## 5 · Commit.Stamped
- frame: 241
- time: 4,0822
- details: accepted=37 radius=0,150 cellsR=2 inkLeft=0,00 dynamicCellsTotal=189 dynBounds=(52,107)-(85,134) grid=108x192 cell=0,100

## 6 · Commit.VisualOk
- frame: 241
- time: 4,0849
- details: <none>

## 7 · Input.Down
- frame: 468
- time: 6,5691
- details: screen=(539, 1778) screenSize=1080x1920 board=(5.39, 17.78) overUi=False uiObj=- guiRect=(x:13.96, y:209.45, width:453.82, height:338.62) blocked=False state=Playing

## 8 · Input.Up
- frame: 475
- time: 6,6457
- details: drawing=False points=0 lastBoard=- dragPx=0

## 9 · Input.Tap
- frame: 475
- time: 6,6470
- details: board=(5.39, 17.78) hit=source_yellow

