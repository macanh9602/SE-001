# Hybrid handoff

## Use when

Use for a bounded Hybrid flow where Domain requests a physical phase and receives a semantic result.

## Do NOT use when

Do not use a delay to assume that Physics settled, and do not make the Physics component decide the gameplay result.

## Ownership

Domain command → Simulation → physical state/contact/settle → semantic signal → Domain decides gameplay result → presentation normalization.

## Tunables

Simulation bounds, contact/settle criteria, and presentation timing belong in the declared component/Profile/level data.

## Copy/adapt baseline

Use an explicit readiness/contact/settle signal. Never use await Delay(...) as a Physics-settled assumption.

`Domain command → Physics component enables simulation → body moves/collides → settle/contact criterion → Simulation semantic event → Domain decides result → Visual normalizes final presentation`.

Do not use `await Delay(500)` as a settle assumption. Use explicit readiness/settle state or the agreed source timing data. Pool Release resets velocity/state/callbacks/generation; Acquire/Bind reapplies complete state.

## Failure modes

Magic delays race with timestep and contact state; stale generation or readiness writes can let an old motion affect the new owner.

## Verification

Verify command ownership, explicit semantic signal, generation/cancellation, final normalization, and Release/Acquire lifecycle.
