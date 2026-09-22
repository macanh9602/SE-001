# C2B supply audit evidence

Unity Editor execute-code probe, current authored Level02:

```text
before|state=Lost|reason=NotFilled|inCorrectCup=474|inWrongCup=0|restingOnObstacle=21|onFloor=196|stillMoving=0|otherResting=1709|neverEmitted=0|coral=0/474|blue=474/474
after|state=Won|reason=None|inCorrectCup=949|inWrongCup=0|restingOnObstacle=33|onFloor=185|stillMoving=0|otherResting=499|neverEmitted=734|coral=474/474|blue=474/474
```

The fix is data-only: Level02 has a coral gate and source supply `logicalAmount=40` for
both colors; `requiredAmount` was not lowered and `SandSimulation` was not modified.
