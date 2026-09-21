# Impact probe recipe

## Use when

Use when a collision callback must provide impact evidence to a semantic owner.

## Do NOT use when

Do not use it to decide gameplay outcome or equate relative velocity magnitude with normal impact.

## Ownership

The component owns contact sampling and emits an impact signal. Domain or Simulation interprets the signal.

## Tunables

Collision filtering and any impact thresholds belong in the component/Profile/level data; do not hardcode gameplay decisions.

## Copy/adapt baseline

The following Unity 3D callback is a baseline and must be verified against the installed Unity API.

Callback emits normal impact speed; it does not decide gameplay. `relativeVelocity.magnitude` is not normal impact speed.

```csharp
using System;
using UnityEngine;
public sealed class RigidbodyImpactProbe : MonoBehaviour
{
    public event Action<float> Impacted;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount == 0) return;
        var contact = collision.GetContact(0);
        var normalImpactSpeed = Mathf.Max(0f, -Vector3.Dot(collision.relativeVelocity, contact.normal));
        Impacted?.Invoke(normalImpactSpeed);
    }
}
```

## Failure modes

Using relativeVelocity magnitude produces incorrect impact evidence; gameplay logic in the callback couples Physics to Domain semantics.

## Verification

Verify normal impact speed with a known contact normal, callback lifecycle, filtering, and semantic consumer ownership.
