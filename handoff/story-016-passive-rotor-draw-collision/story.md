# Story 016 — Passive RotatingObstacle + Collision-Safe DrawStroke

Status: EXECUTABLE
Size: L
Execution mode: DIRECT
Frontier collaboration: INHERIT

Planning / independent review: ChatGPT
Implementation / verification: Codex

C2C:
INIT → PLAN → EXECUTED → DONE | PLAN | BLOCKED

Reference:
https://www.youtube.com/shorts/bmOCRbT8AGM

## 1. Locked product semantics

RotatingObstacle is a passive rotor.

- Initial angle = authored `initialAngle`.
- Initial angular velocity = 0.
- No physical sand contact => rotor does not start rotating.
- Nearby sand without contact => no torque.
- Real sand contact produces signed torque.
- Lever arm matters: equivalent contact farther from pivot produces more torque.
- Rotor has inertia, angular damping, max angular speed and a rest threshold.
- When impacts stop it may coast, then must damp to rest.

`degreesPerSecond` is legacy serialized data only. It must NOT drive startup
or autonomous rotation.

Schema remains 5.

## 2. Deterministic shell already applied by setup script

The V2 setup script intentionally performed only low-risk mechanical changes:

- `RotatingObstacleProfile` now exposes passive-rotation tunables:
  - sandTorqueScale
  - momentOfInertiaScale
  - angularDamping
  - maxAngularSpeed
  - restAngularSpeed
  - collisionSweepStepDegrees
- the profile asset has explicit defaults;
- runtime and Editor validation no longer require legacy degreesPerSecond != 0;
- the normal Level Editor inspector no longer edits `Rotation °/s`;
- legacy degreesPerSecond remains serialized for compatibility.

Do not undo these changes unless compile evidence proves the shell itself is
wrong. If so, report BLOCKED/PLAN with evidence instead of silently reverting.

## 3. Preserve current schema-5 project work

Current schema-5 tuning already owns:

- sourceScale
- bowlScale
- sourceEmissionRate
- sourceStreamWidth

Source and receiver collision must use resolved geometry, not stale profile-only
geometry.

Do not regress current Story-015 Bowl behaviour, including:

- bowlCreepChance
- bowlCreepReach
- bowlLevelingCellsPerStep
- bowlLevelingExtraPasses
- bowlSettlingHeadroomCells
- existing rim/catch behaviour
- Required Amount recalculation on Bowl Scale edit

Do not retune Bowl physics as collateral work.

## 4. Core algorithm A — sand-driven passive rotor

Codex owns the implementation and must first PLAN the exact contact model.

Required results:

- authoritative `AngularVelocity` in rotor state;
- torque accumulation from real sand/rotor contacts;
- signed torque from contact direction;
- radial lever arm contribution;
- inertia scaling from rotor dimensions/profile;
- angular speed cap;
- damping;
- deterministic rest snap.

No Rigidbody/Rigidbody2D grains.
No per-grain GameObjects.
No visual-only authority.
No proximity-triggered fake spin.

The integration order relative to `SandSimulation.Step()` must be explicit in PLAN
so contact information is well-defined and deterministic.

## 5. Core algorithm B — rotor versus committed DrawStroke

LOCKED BEHAVIOUR:

When predicted rotor motion would hit existing committed DrawStroke:

1. Do not penetrate.
2. Clamp to the last valid non-overlapping rotor angle.
3. Set AngularVelocity = 0.
4. Do not alter the DrawStroke.
5. Do not allow momentum to tunnel through on the next simulation step.

Later sand torque may rotate the rotor AWAY if that new angular path is clear.
It may never force the rotor THROUGH the stroke.

Use swept/subdivided angular collision using the profile's
`collisionSweepStepDegrees` as the maximum angular increment, or propose an
equivalent bounded method in PLAN.

Do NOT reserve the rotor's full 360-degree sweep envelope for player drawing.
Only current rotor geometry blocks a newly drawn stroke.

## 6. Core algorithm C — safe grain displacement

Current rotor push logic can find a valid target without proving the path to
that target is clear.

Fix this.

A displaced grain must never teleport through:

- invalid board cells;
- StaticMask;
- CupWallMask;
- DynamicMask;
- rotating geometry.

Preserve:

- OccupiedCount;
- EmittedCount;
- material ID;
- shade/velocity/momentum feel data;
- deterministic accounting.

Use bounded, allocation-free search.

If no safe displacement exists, use a deterministic conservative fallback;
do not delete the grain.

## 7. Core algorithm D — DrawStroke First Contact Stop

A drawing gesture stops permanently at FIRST contact with solid gameplay geometry:

- board boundary / invalid area;
- wall/static obstacle;
- current rotor geometry;
- resolved Source solid body;
- Cup/Bowl wall/rim;
- existing committed DrawStroke.

Bowl mouth remains open.

Do not block the Bowl using a full rectangular renderer bound.

For schema 5:
- Source geometry must respect sourceScale via current resolver/domain geometry.
- Bowl geometry must respect bowlScale and current receiver wall geometry.

## 8. Preview / commit single truth

Current raw preview and post-release commit must not disagree.

Implement one shared authoritative stroke acceptance path so:

- preview contains accepted points only;
- commit uses exactly those accepted points;
- effective thickness/radius is identical;
- only accepted path length consumes ink;
- rejected length consumes zero ink;
- starting inside solid geometry creates nothing;
- fully rejected gesture does not increment stroke count;
- fully rejected gesture creates no committed visual;
- once contact stops a gesture, holding the pointer cannot resume it on the far side.

Prevent high-speed pointer tunneling.

Do not false-collide the current stroke against its own immediately accepted
prefix, while still treating older committed strokes as solid.

## 9. Geometry authority

Do not infer gameplay collision from SpriteRenderer bounds.

Prefer existing domain/simulation geometry:

- Layout masks for walls/static obstacles;
- RotatingObstacle geometry from the authoritative rotor state/profile;
- SourceDomain resolved size/body offset;
- CupDomain / CupWallMask for receiver solids;
- DynamicMask / committed stroke data for DrawStroke.

If a shared `DrawStrokeCollisionResolver` or equivalent is introduced, it should
own HOW while GameplayManager/Input decide WHAT.

Preview and commit must use the same service/contract.

## 10. Performance

No per-frame/per-contact allocation in simulation hot paths.

No per-grain List/HashSet allocation.

Reuse/preallocate scratch buffers.

Do not increase sand-grid resolution to solve collision precision.

Report:
- extra memory;
- per-step work;
- expected GC;
- likely low-end impact for Redmi 9A.

## 11. Required baseline before core implementation

Before editing core algorithm files, run and record:

- Unity compile / Console state;
- current RotatingObstacleTests;
- relevant SandSimulationTests;
- LevelTuningTests;
- relevant Phase-C / Draw tests.

Separate PRE-EXISTING failures from Story-016 failures.

## 12. Mandatory tests

Passive rotor:
- stationary with no sand;
- nearby non-contact sand => no rotation;
- physical contact => torque;
- contact direction controls torque sign;
- farther lever arm => stronger response;
- sustained contact accelerates;
- max speed respected;
- damping after contact ends;
- eventually reaches rest;
- grain/accounting conservation.

Rotor / DrawStroke:
- no overlap;
- clamps to last valid angle;
- AngularVelocity = 0 on contact;
- stroke remains unchanged;
- no high-speed angular tunneling;
- later opposite torque may move away if clear;
- later torque cannot cross the blocking stroke.

Draw:
- first-contact stop on wall/static obstacle;
- first-contact stop on current rotor;
- schema-5 scaled Source blocks correctly;
- schema-5 scaled Bowl wall/rim blocks correctly;
- Bowl mouth remains open;
- existing stroke blocks new stroke;
- high-speed pointer cannot tunnel;
- start in blocked geometry rejected;
- accepted ink only;
- rejected stroke no count/no visual;
- preview equals committed accepted path;
- DynamicMask never overwrites immutable solid cells.

Regression:
- RotatingObstacle tests;
- SandSimulation tests touched by shared sim work;
- LevelTuning tests;
- Level Editor validation/geometry tests;
- Phase-C Draw lifecycle/visual tests.

## 13. C2C PLAN gate

FIRST RESPONSE:

C2C PLAN

Before changing core algorithm files, PLAN must state:

1. pre-change compile/test baseline;
2. exact sand-contact evidence used for torque;
3. rotor integration order;
4. torque/inertia formula and units;
5. angular sweep collision method;
6. safe grain relocation/path method;
7. shared Draw first-contact architecture;
8. schema-5 Source/Bowl geometry reuse;
9. exact files to change;
10. allocation/performance impact;
11. test matrix;
12. Semantic deviations = NONE, or BLOCKED.

Do not re-plan the already locked gameplay semantics.

## 14. Execute

After PLAN approval:

- implement core algorithms;
- compile;
- run tests;
- inspect scoped diff;
- update implementation-notes.html;
- report C2C EXECUTED.

Do not claim DONE.

ChatGPT performs independent review after EXECUTED.