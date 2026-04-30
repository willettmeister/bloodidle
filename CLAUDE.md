# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 2022.3 LTS progressive idle game targeting Android. Bundle ID: `com.idleclicker.game`.
Dark blood/combat theme. Portrait-only (1080×1920 reference resolution).

## First-time setup

1. Open Unity Hub → **Add project from disk** → select this folder.
2. Unity resolves packages automatically on first open.
3. **IdleClicker → Generate Assets** — creates `rounded_rect.png` and all enemy sprites.
4. **IdleClicker → Setup Scene** — generates `Assets/Scenes/MainScene.unity` and wires all component references. Run Generate Assets first so panels/buttons pick up the rounded sprite.
5. Replace `k_GhToken` in `UIManager.cs` with a GitHub Fine-grained PAT (Issues: Write, bloodidle repo only). Generate at `github.com/settings/tokens → Fine-grained tokens`.
6. Switch platform: **File → Build Settings → Android → Switch Platform**.
7. Build: **File → Build Settings → Build**.

## CLI build (headless)

```bash
/path/to/Unity -batchmode -quit -projectPath . \
  -buildTarget Android -executeMethod BuildScript.Build -logFile build.log

# Run EditMode tests
/path/to/Unity -batchmode -quit -projectPath . \
  -runTests -testPlatform EditMode -logFile test.log
```

## Architecture

### Core loop
Blood is the only currency. The player farms it by tapping **Farm Blood** (+1/tap). Blood funds soldiers and spells.

Soldiers auto-attack the current wave's enemy every frame. The enemy retaliates against the frontline soldier only. When a soldier's HP hits zero it dies and the next soldier steps up at full HP. If all soldiers die, combat pauses (enemy HP persists — no regen). When the enemy dies the wave counter advances, a new (scaled) enemy spawns, and a blood reward is granted.

### Scripts (`Assets/Scripts/`)

| File | Responsibility |
|---|---|
| `GameManager.cs` | Singleton (`DontDestroyOnLoad`). Owns all state. Fires `OnStateChanged` whenever anything changes. Contains `RunCombat(dt)` called from `Update`. |
| `UIManager.cs` | Subscribes to `OnStateChanged`, calls `Refresh()` to update all UI. Holds serialised references to every `Text`, `Image` (HP bar fill), `Button`, and panel `GameObject`. |
| `ClickManager.cs` | Thin button bridge — three one-liner methods that forward to `GameManager`. |

### Scene generation (`Assets/Editor/SceneBuilder.cs`)
Editor-only. `[MenuItem("IdleClicker/Setup Scene")]` builds the full scene programmatically and saves it. Re-run to rebuild from scratch. Uses `GOExt` static extension methods (`CreateChild`, `SetRT`, `Stretch`) to reduce boilerplate.

### Progression milestones
| Threshold | Event |
|---|---|
| 10 blood | Can buy first soldier |
| 50 blood | Can buy first worker (farms wood from forest) |
| 50 total blood earned | **Heal Self** spell unlocks |
| 20 wood | Can upgrade Barracks (Lv.1→2, cap 10→15) |
| Each enemy kill | Wave advances; blood reward × 1.4 per wave |

### Balance constants (all in `GameManager.cs`)
| Constant | Value | Notes |
|---|---|---|
| `BloodPerClick` | 1 | |
| `SoldierCost` | 10 blood | Capped at `MaxSoldiers` |
| `SoldierAttack` | 5 dmg/sec | Per soldier |
| `SoldierMaxHP` | 50 | |
| `MaxSoldiers` (initial) | 10 | Raised by Barracks upgrades |
| `WorkerCost` | 50 blood | No cap |
| `WorkerWoodPerSec` | 0.5 | Per worker |
| `BarracksSoldierBonus` | +5 soldiers | Per upgrade |
| `BarracksCostMultiplier` | × 2.0 | Cost doubles each upgrade |
| `HealSelfCost` | 25 blood | |
| `HealSelfAmount` | +20 HP | Frontline soldier only |
| Wave 1 enemy | 100 HP, 3 atk/sec, 25 blood reward | |
| Enemy HP scale | × 1.5 per wave | |
| Enemy attack scale | × 1.3 per wave | |
| Reward scale | × 1.4 per wave | |

### Android settings
- Min SDK: 22 (Android 5.1), Target SDK: 33
- Scripting backend: IL2CPP, Architecture: ARM64
- Orientation: Portrait only

## Adding content

**New spell**: Add cost/unlock constants + a `UseX()` method in `GameManager` following `UseHealSelf`. Add a panel + button to `UIManager`. Wire in `SceneBuilder.BuildScene()` and `ClickManager`.

**New resource**: Add a `double MyResource` property and a passive accumulation source (like workers → wood). Update `Update()` in `GameManager` to accumulate it. Add a display label in `UIManager` and `SceneBuilder`.

**New upgrade / building**: Follow the Barracks pattern — cost field that scales with level, an `UpgradeX()` method, a Text + Button pair wired through `UIManager` and `SceneBuilder`.

**New enemy type**: Extend `EnemyNames` and optionally add per-index stat overrides in `SpawnEnemy()`.
