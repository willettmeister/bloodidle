# Combat System

## Overview

Combat runs every frame inside `GameManager.RunCombat(float dt)`, called from `Update()` when `SoldierCount > 0 && EnemyHP > 0`.

## Damage Calculation

### Player → Enemy

```
effectiveAttack = TotalAttack
if SurgeActive:        effectiveAttack × SurgeMultiplier (2×)
if Armored modifier:   effectiveAttack × 0.5
if BossShieldActive:   damage absorbed by shield first
EnemyHP -= effectiveAttack × dt
```

`TotalAttack` per soldier class:

| Class | Base Attack | Notes |
|-------|-------------|-------|
| Tank | 5 dmg/s | +Weapon level × 3 |
| Berserker | 12 dmg/s | +Weapon level × 3, 20% crit chance × 2× |
| Paladin | 3 dmg/s | +Weapon level × 3 |

Prestige milestones (every 5/10/20/50 prestiges) add +5% attack per milestone reached.

### Enemy → Soldier (frontline only)

```
incomingAtk = EnemyAttack
if Berserk ability & EnemyHP < 25%:  incomingAtk × 2
if MixedArmy:                        incomingAtk × (1 - 0.15)
if PIronWallLevel > 0:               incomingAtk × (1 - level × 0.10)
if Drain ability:                    incomingAtk += BossDrainPerSec (5/s)
SoldierHP -= incomingAtk × dt
```

Only the frontline soldier takes damage. Formation (Tank Front / Berserker Front) determines which class is frontline:

| Formation | Frontline priority |
|-----------|--------------------|
| Tank Front (default) | Tank → Berserker → Paladin |
| Berserker Front | Berserker → Tank → Paladin |

## Soldier Classes

| Stat | Tank | Berserker | Paladin |
|------|------|-----------|---------|
| Max HP | 50 | 25 | 20 |
| Attack | 5/s | 12/s | 3/s |
| Special | HP regen 2/s (all-tank army) | 20% crit × 2× | Heal 1 HP/s × count |
| Cost | 10 blood | 10 blood | 10 blood |

**IronSkin talent** adds +15 HP to frontline max HP.  
**Blood Corruption** removes 5 HP per corruption level from frontline max HP.

## Boss Abilities

Bosses spawn with a random ability (`BossAbility` enum):

| Ability | Effect |
|---------|--------|
| None | No special mechanic |
| Shield | 20% of EnemyMaxHP as shield; absorbs damage until depleted |
| Berserk | When EnemyHP < 25%, attack doubles |
| Drain | Enemy drains an additional 5 HP/s from frontline |

## Enemy Modifiers (non-boss)

25% chance for a modifier when spawning a normal enemy:

| Modifier | Effect |
|----------|--------|
| None | Standard |
| Armored | Player damage halved |
| Enraged | EnemyAttack × 1.5 |
| Regen | Enemy heals 2% of MaxHP/s |

## Kill Reward Formula

```
baseReward = floor(25 × 1.4^(wave-1) × PrestigeMultiplier × (1 + TalismanBonus))
if boss:           baseReward × 3
if dailyChallenge: baseReward × ChallengeBloodMult (5×)
if BloodFrenzy:    baseReward × 1.25
if flawless:       baseReward × 2
final = floor(baseReward × StreakMultiplier)
```

**Flawless** — wave cleared without soldier dying and `_flawlessTimer ≤ 10s`.  
**Streak** — consecutive kills without soldier death, up to 3× multiplier.

## Boss Wave Rules

- First boss spawns between wave 6–12 (random).
- After each boss kill, next boss is `Wave + Random(5, 11)` waves away.
- Boss has 90s timer (+15s per `SSBossTimerLevel`).
- **Timeout penalty**: all soldiers wiped, 25% blood lost, wave rolled back 3 (reduced by `SSRollbackLevel`).

## Daily Challenge

Activated manually once per calendar day (UTC). Boosts the current enemy:

```
EnemyMaxHP  × 5
EnemyAttack × 2
Timer: 60 seconds
Reward: ×5 blood
```

If the challenge timer runs out before the enemy dies, `DailyChallengeActive` turns off with no reward.

## Undying Talent

When frontline HP hits 0, if the Undying talent is active and hasn't been used this wave, the soldier is revived to 1 HP instead of dying. Resets on each new wave.
