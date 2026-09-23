# Agent Audit: sand-lose-stability
Generated: 2026-09-24T04:16:35.2345248+07:00
Scope: Level start -> Win/Lose. Expect: all sources Empty + (stable 30 steps OR no receiver progress 480 steps) -> Lose(NotFilled).

## 1 · LevelStart
- frame: 62718
- time: 164.3406
- details: sources: source_99309e8f141d Closed 1140/1140 source_1d58e419567c Closed 1140/1140 | cups: cup_638cfc7f4c59 0/285 cap 311 cup_c69006beebce 0/285 cap 305

## 2 · SourceState
- frame: 63870
- time: 168.0284
- details: step 215 source_1d58e419567c Opening->Open remaining 1126

## 3 · SourceState
- frame: 64282
- time: 174.9393
- details: step 627 source_1d58e419567c Open->Empty remaining 0

## 4 · SourceState
- frame: 64416
- time: 177.1892
- details: step 761 source_99309e8f141d Opening->Open remaining 1126

## 5 · SourceState
- frame: 64704
- time: 182.0183
- details: step 1049 source_99309e8f141d Open->Empty remaining 0

## 6 · AllSourcesEmpty
- frame: 64704
- time: 182.0190
- details: step 1049 | cups: cup_638cfc7f4c59 112/285 cap 311 cup_c69006beebce 285/285 cap 305 FULL

## 7 · StabilitySample
- frame: 64704
- time: 182.0191
- details: step 1049 stable 0/30 noProgress 0/480 moved 550 pushed 10 collectionActivity 4 | fall 375 roll 64 slide 57 disperse 54 creep 0 | last rule 2 (48,255)->(49,254) | cups: cup_638cfc7f4c59 112/285 cap 311 cup_c69006beebce 285/285 cap 305 FULL

## 8 · StabilitySample
- frame: 64764
- time: 183.0179
- details: step 1109 stable 0/30 noProgress 0/480 moved 558 pushed 9 collectionActivity 2 | fall 387 roll 65 slide 56 disperse 50 creep 0 | last rule 1 (51,243)->(51,242) | cups: cup_638cfc7f4c59 224/285 cap 311 cup_c69006beebce 285/285 cap 305 FULL

## 9 · Win
- frame: 64815
- time: 183.9108
- details: step 1160 | cups: cup_638cfc7f4c59 285/285 cap 311 FULL cup_c69006beebce 285/285 cap 305 FULL

