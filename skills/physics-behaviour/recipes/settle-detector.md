# Settle detector recipe

## Use when

Use for a Physics-owned 3D body where a stable-duration threshold is a presentation or simulation signal.

## Do NOT use when

Do not use velocity alone as gameplay truth when the design also requires contact/support or another settled condition. Do not let the detector decide Win/Lose.

## Ownership

The component owns sampling and emits Settled. Domain consumes the semantic event; it does not poll raw Rigidbody state.

## Tunables

Linear threshold, angular threshold, and stable duration are tunables on the component/Profile. Pool Acquire calls ResetState.

## Copy/adapt baseline

The following is a Unity 6 3D baseline and must be verified against the installed Unity API.

Unity 6 3D baseline; recipe only, not runtime framework. Thresholds are tunables and the Domain consumes `Settled` rather than polling Rigidbody. Pool Acquire calls `ResetState()`.

```csharp
using System;
using UnityEngine;

public sealed class RigidbodySettleDetector : MonoBehaviour
{
    [SerializeField] private Rigidbody body;
    [SerializeField, Min(0f)] private float linearSpeedThreshold = 0.05f;
    [SerializeField, Min(0f)] private float angularSpeedThreshold = 0.05f;
    [SerializeField, Min(0f)] private float stableDuration = 0.15f;
    private float _stableTime;
    private bool _isSettled;
    public event Action Settled;
    private void FixedUpdate()
    {
        if (body == null || _isSettled) return;
        var linearStable = body.linearVelocity.sqrMagnitude <= linearSpeedThreshold * linearSpeedThreshold;
        var angularStable = body.angularVelocity.sqrMagnitude <= angularSpeedThreshold * angularSpeedThreshold;
        if (!linearStable || !angularStable) { _stableTime = 0f; return; }
        _stableTime += Time.fixedDeltaTime;
        if (_stableTime < stableDuration) return;
        _isSettled = true;
        Settled?.Invoke();
    }
    public void ResetState() { _stableTime = 0f; _isSettled = false; }
}
```

## Failure modes

Low velocity can occur while a body is unsupported, sliding, or otherwise not gameplay-settled. Pool Release resets state; Acquire rebinds the full expected state.

## Verification

Verify threshold/stable-duration tuning, contact/support semantics when required, event ownership, and Acquire reset in the target Unity version.
