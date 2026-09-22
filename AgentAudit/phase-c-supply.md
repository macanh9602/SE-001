# Phase C C2B supply audit

Audit executed through Unity Editor on the authored `phase_c_level_02`.

| Run | State | inCorrectCup | inWrongCup | restingOnObstacle | onFloor | stillMoving | otherResting | neverEmitted | Cup result |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| Before draw, both sources opened | Lost / NotFilled | 474 | 0 | 21 | 196 | 0 | 1709 | 0 | coral 0/474, blue 474/474 |
| After two committed ramps, both sources opened | Won | 949 | 0 | 33 | 185 | 0 | 499 | 734 | coral 474/474, blue 474/474 |

## Conclusion

The pre-fix failure was authoring/layout, not a source valve or simulation defect. The
no-draw run exhausted both sources but left the coral cup empty while the blue cup filled.
The two committed ramps route coral into its cup and win without changing simulation
parameters or lowering `requiredAmount`. The remaining `neverEmitted=734` after the win is
expected because the win condition closes the gameplay loop as soon as both cups reach the
required physical fill; it is not a supply deficit.

The authored source `logicalAmount` was raised from 8 to 40 after the first probe because
cup geometry requires 474 grains while the runtime scale emits 240 grains at the original
authoring value. `requiredAmount` remains 1 for each cup.
