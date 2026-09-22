# Phase C C-R1 performance capture

EditMode capture using `GameplayManager.AdvanceSteps` in chunks of 60. GC delta is approximate.

| Level | Grid | Steps | Max grains | Avg ms/step | Max ms/step | GC delta bytes | Result |
|---|---:|---:|---:|---:|---:|---:|---|
| phase_c_level_01 | 180x321 | 780 | 1800 | 0,230 | 0,299 | 0 | Won |
| phase_c_level_02 | 180x321 | 1440 | 1800 | 0,243 | 0,278 | 0 | Lost |
| phase_c_level_03 | 180x321 | 180 | 2086 | 0,146 | 0,254 | 0 | Won |
