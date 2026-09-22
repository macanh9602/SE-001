# Phase C C4 performance capture

EditMode capture using `GameplayManager.AdvanceSteps` in chunks of 60. GC delta is approximate.

| Level | Grid | Steps | Max grains | Sand surface renderers | Avg ms/step | Max ms/step | GC delta bytes | Result |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| phase_c_level_01 | 180x321 | 600 | 543 | 1 | 0.087 | 0.255 | 40960 | Won |
| phase_c_level_02 | 180x321 | 900 | 1666 | 1 | 0.161 | 0.261 | 126976 | Won |
| phase_c_level_03 | 180x321 | 600 | 1092 | 1 | 0.116 | 0.187 | 126976 | Won |
