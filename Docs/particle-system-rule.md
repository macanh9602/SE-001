# Particle System / VFX Rule

## Decision

Runtime Particle System dùng flow chuẩn:

```text
Caller
  -> EffectsProfile.SpawnEffect(effectId, ...)
  -> VTLTools.ObjectPool.Spawn(prefab)
  -> Effect.Init(...)
  -> Effect runtime API
  -> MainParticleSystem.OnParticleSystemStopped()
  -> Effect release/recycle về ObjectPool
```

## Ownership

| Component | Responsibility |
|---|---|
| Particle prefab | Authoring source cho ParticleSystem modules, renderer, material và hierarchy VFX |
| `EffectsProfile` | Map `EffectId` tới prefab, prewarm pool và cung cấp API spawn |
| `Effect` | Runtime lifecycle và presentation API: play, stop, pause, color, shape, burst |
| `MainParticleSystem` | Forward callback khi main ParticleSystem stopped về `Effect` |
| `ObjectPool` | Acquire/release instance; không để caller tự `Instantiate`/`Destroy` |
| Caller | Chọn `EffectId` và truyền context cần thiết; không điều khiển nội bộ ParticleSystem |

## Required rules

1. Effect lặp trong gameplay phải là prefab thật và phải spawn qua `EffectsProfile`.
2. `EffectsProfile` là source-of-truth cho mapping `EffectId -> Effect prefab`.
3. Caller không gọi trực tiếp `ParticleSystem.Play`, `Stop`, `Emit`, không tự `Instantiate` hoặc `Destroy` effect.
4. Các giá trị artist/GD cần tune nằm trên prefab/component hoặc Profile; không hardcode trong gameplay/domain.
5. Effect pooled phải có lifecycle đầy đủ:
   - Acquire: reset/rebind toàn bộ state runtime cần thiết rồi mới play.
   - Release: cancel task/tween, stop và clear state cần thiết, sau đó `ObjectPool.Recycle`.
6. Khi ParticleSystem kết thúc, `MainParticleSystem` báo về `Effect`; `Effect` là component duy nhất quyết định release.
7. Không dùng đồng thời `Destroy` và `Recycle` cho cùng một instance.
8. Khi reparent effect pooled, dùng `SetParent(parent, false)` và khôi phục transform state từ prefab nếu cần.
9. Particle animation authoring nằm trong prefab. Runtime code chỉ override những tham số thật sự phụ thuộc gameplay/presentation context, ví dụ color, shape hoặc burst count.

## Standard usage

```csharp
EffectsProfile.SpawnEffect(
    EffectsProfile.EffectId.Starts_Sparks,
    position,
    color,
    parent);
```

Caller chỉ chọn effect và truyền input. `EffectsProfile`, `Effect`, `MainParticleSystem` và `ObjectPool` chịu trách nhiệm phần còn lại.

## Exception

Particle System persistent/looping gắn cố định trong scene có thể không auto-recycle. Trường hợp này vẫn phải dùng prefab/component ownership và lifecycle explicit của component sở hữu nó.

## Current implementation note

`Effect.OnParticleSystemStoppedListener()` hiện có cả nhánh `Destroy` và `Recycle`. Prefab dùng pool phải bảo đảm không bật đồng thời hai mode; implementation nên chọn một lifecycle duy nhất, ưu tiên `Recycle` cho effect runtime.
