# Generation owner baseline

This is a baseline to adapt, not a universal ownership model.

```csharp
private int _operationGeneration;

public async UniTask RunAsync(CancellationToken cancellationToken)
{
    var generation = ++_operationGeneration;
    SetBlocked(true);
    try
    {
        await PlayAsync(cancellationToken);
    }
    finally
    {
        if (generation != _operationGeneration)
            return;
        SetBlocked(false);
        NormalizeOwnedState();
    }
}
```

Adapt cancellation, owner handoff, and normalization to the component's actual ownership contract. Release invalidates the generation; Acquire/Bind reapplies complete current state.
