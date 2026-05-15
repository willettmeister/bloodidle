# DamageNumber

Floating damage text that rises and fades over ~1.1 seconds. Spawned by `UIManager.SpawnDamageNumber()` in response to `GameManager.OnDamageDealt`. Not pooled — each instance creates and destroys its own GameObject.

Assembly: `GameAssembly`  
File: `Assets/Scripts/DamageNumber.cs`

---

## Constants

| Constant | Value | Notes |
|----------|-------|-------|
| `Duration` | `1.1f` | Seconds until the GameObject is destroyed |
| `Rise` | `90f` | Pixels risen over the full duration |

---

## Static API

```csharp
public static void Spawn(RectTransform layer, string label, Color color, Vector2 pos)
```

Creates a new GameObject under `layer`, adds a `Text` component (bold, 40pt, `LegacyRuntime.ttf`), anchors it at `pos`, then adds a `DamageNumber` component to start the animation.

| Parameter | Notes |
|-----------|-------|
| `layer` | `UIManager.damageLayer` — the dedicated RectTransform overlay |
| `label` | String to display, e.g. `"42"` |
| `color` | Red (`1, 0.25, 0.25`) for player damage dealt; orange (`1, 0.55, 0.1`) for soldier taking damage |
| `pos` | Local position within `layer` — centered on the relevant HP bar, ±80 px horizontal jitter applied by caller |

---

## Animation (Update loop)

Each frame, `_elapsed` advances by `Time.deltaTime`. The text rises linearly (`Rise × t` pixels) and fades out (alpha goes from 1 → 0, accelerated by `1.4×`). At `_elapsed >= Duration` the GameObject self-destructs.

---

## Integration

`UIManager` calls `DamageNumber.Spawn()` from `SpawnDamageNumber(float amount, bool isEnemy)`, which is subscribed to `GameManager.OnDamageDealt`. Spawning is suppressed when `amount < 0.5f` to avoid visual noise at low damage ticks.
