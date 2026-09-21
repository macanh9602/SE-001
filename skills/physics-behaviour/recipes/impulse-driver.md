# Impulse driver recipe

## Use when

Use when a caller needs an intent API to apply a force or torque impulse to an owned Rigidbody.

## Do NOT use when

Do not use it as a gameplay authority or hardcode impulse magnitude/direction inside the component.

## Ownership

The caller decides WHAT and supplies magnitude/direction. The component owns HOW and the Rigidbody ownership must be explicit.

## Tunables

Impulse/torque values come from the caller command, component/Profile, or level data. Verify mass and ForceMode assumptions.

## Copy/adapt baseline

The following Unity 3D baseline must be verified against the installed Unity API.

Caller supplies impulse magnitude and direction; do not hardcode gameplay magnitude inside the component.

```csharp
using UnityEngine;
public sealed class RigidbodyImpulseDriver : MonoBehaviour
{
    [SerializeField] private Rigidbody body;
    public void ApplyImpulse(Vector3 impulse) => body.AddForce(impulse, ForceMode.Impulse);
    public void ApplyTorqueImpulse(Vector3 torque) => body.AddTorque(torque, ForceMode.Impulse);
}
```

## Failure modes

Unexpected mass, ForceMode, Rigidbody ownership, or stale pooled state can change the result. Release/Acquire must restore the intended baseline when pooled.

## Verification

Verify mass/ForceMode assumptions, caller-provided values, Rigidbody ownership, and pooled rebind behavior.
