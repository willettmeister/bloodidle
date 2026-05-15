# GameManager

Singleton MonoBehaviour. Owns every piece of game state. Drives the combat loop, resource accumulation, timers, and save/load via `Update()`. Fires `OnStateChanged` whenever state mutates so `UIManager` can refresh.

Assembly: `GameAssembly`  
File: `Assets/Scripts/GameManager.cs`

---

## Enums

```csharp
[Flags] enum AchievementFlags   // bitmask — None, FirstKill, Wave10, Wave25, Blood1K, Blood10K,
                                 //           FirstSoldier, FullLegion, FirstRitual, FirstPrestige
         enum EnemyModifier      // None, Armored, Enraged, Regen
         enum BossAbility        // None, Shield, Berserk, Drain
[Flags] enum TalentFlags        // None, BloodFrenzy, Undying, ShardHunter, IronSkin, BloodRush, Glutton
```

---

## Events

| Event | Signature | When fired |
|-------|-----------|-----------|
| `OnStateChanged` | `Action` | Any state mutation |
| `OnDamageDealt` | `Action<float amount, bool isEnemy>` | Every 0.4 s during combat |
| `OnMilestoneChest` | `Action<string message>` | Wave-5 milestone chest or prestige milestone |
| `OnAchievementUnlocked` | `Action<AchievementFlags>` | Achievement first earned |

---

## Singleton

```csharp
public static GameManager Instance { get; private set; }
```

---

## Blood

| Member | Type | Notes |
|--------|------|-------|
| `Blood` | `double` | Current blood |
| `TotalBloodEarned` | `double` | Lifetime total (used for unlock checks) |
| `BloodPerClick` | `const double = 1.0` | Base click value |
| `EffectiveBloodPerClick` | `double` | `(BloodPerClick + PClickBonusLevel×0.5) × PrestigeMultiplier` |
| `BloodPerSec` | `double` | Sum of ritual, tithe, and tap passive income |
| `BloodTithePerSec` | `double` | `PBloodTitheLevel × 0.5 × PrestigeMultiplier` |
| `BloodTapPerSec` | `double` | `SSBloodTapLevel × 1.0 × PrestigeMultiplier` |
| `DailyBonusAvailable` | `bool` | True once per UTC day; first click ×10 |
| `DailyBonusMultiplier` | `const float = 10f` | |

---

## Wood & Workers

| Member | Type | Notes |
|--------|------|-------|
| `Wood` | `double` | |
| `WorkerCount` | `int` | |
| `WoodPerSecond` | `double` | `WorkerCount × 0.5` |
| `WorkerCost` | `const double = 50.0` | |
| `WorkerWoodPerSec` | `const double = 0.5` | |
| `WorkersUnlocked` | `bool` | Unlocks at 200 total blood earned |
| `WorkersUnlockThreshold` | `const double = 200.0` | |
| `BloodPactUnlocked` | `bool` | Same gate as `WorkersUnlocked` |
| `BloodPactBloodCost` | `const double = 200.0` | |
| `BloodPactWoodGain` | `const double = 100.0` | |

---

## Soldiers

| Member | Type | Notes |
|--------|------|-------|
| `TankCount` | `int` | |
| `BerserkerCount` | `int` | |
| `PaladinCount` | `int` | |
| `SoldierCount` | `int` | Sum of all three |
| `MaxSoldiers` | `int` | Initial 10; raised by Barracks/Prestige Shop |
| `SoldierHP` | `float` | Current frontline HP |
| `FrontlineMaxHP` | `float` | Class max HP ± armor/talent/corruption |
| `TotalAttack` | `float` | Combined DPS of all soldiers |
| `BerserkerFront` | `bool` | Formation toggle |
| `FrontlineIsTank/IsBerserker/IsPaladin` | `bool` | Derived from counts and formation |
| `IsAllTank/IsAllBerserker/IsAllPaladin/IsMixedArmy` | `bool` | Army composition flags |

### Constants

| Constant | Value |
|----------|-------|
| `SoldierMaxHP` | 50 |
| `SoldierCost` | 10 blood |
| `SoldierAttack` | 5 dmg/s |
| `BerserkerMaxHP` | 25 |
| `BerserkerAttack` | 12 dmg/s |
| `BerserkerCritChance` | 0.2 (20%) |
| `BerserkerCritMult` | 2× |
| `PaladinMaxHP` | 20 |
| `PaladinAttack` | 3 dmg/s |
| `PaladinHealRate` | 1 HP/s per paladin |
| `TankRegenRate` | 2 HP/s (all-tank only) |
| `MixedArmyDmgReduction` | 0.15 (15%) |

---

## Equipment

| Member | Type | Notes |
|--------|------|-------|
| `WeaponLevel` | `int` | Max 5 |
| `ArmorLevel` | `int` | Max 5 |
| `TalismanLevel` | `int` | Max 5 |
| `EquipAttackBonus` | `float` | `WeaponLevel × 3` dmg/s per soldier |
| `EquipArmorBonus` | `float` | `ArmorLevel × 10` to frontline max HP |
| `EquipTalismanBonus` | `double` | `TalismanLevel × 0.15` kill reward multiplier |
| `WeaponUpgradeCost` | `double` | `floor(20 × 2^WeaponLevel)` |
| `ArmorUpgradeCost` | `double` | `floor(15 × 2^ArmorLevel)` |
| `TalismanUpgradeCost` | `double` | `floor(25 × 2^TalismanLevel)` |
| `MaxEquipLevel` | `const int = 5` | |

---

## Fortifications

| Member | Type | Notes |
|--------|------|-------|
| `FortificationLevel` | `int` | Max 10 |
| `FortificationCost` | `double` | Starts at 50, ×2 per level |
| `FortificationDmgReduction` | `float` | `level × 0.02` — applied to enemy HP on spawn |
| `FortBaseCost` | `const double = 50.0` | |
| `FortCostMultiplier` | `const double = 2.0` | |
| `FortHPReductionPerLevel` | `const float = 0.02` | |
| `MaxFortificationLevel` | `const int = 10` | |

---

## Barracks

| Member | Type | Notes |
|--------|------|-------|
| `BarracksLevel` | `int` | Starts at 1 |
| `BarracksUpgradeCost` | `double` | Starts at 20, ×2 per upgrade |
| `BarracksSoldierBonus` | `const int = 5` | +5 `MaxSoldiers` per upgrade |
| `BarracksCostMultiplier` | `const double = 2.0` | |

---

## Blood Ritual

| Member | Type | Notes |
|--------|------|-------|
| `BloodRitualCount` | `int` | |
| `BloodRitualCost` | `double` | Starts at 30 wood, ×2 each purchase |
| `BloodRitualBaseCost` | `const double = 30.0` | |
| `BloodRitualBloodPerSec` | `const double = 1.0` | Base per ritual |
| `BloodRitualCostMultiplier` | `const double = 2.0` | |

---

## Spells

### Heal Self

| Member | Value |
|--------|-------|
| `HealSelfUnlocked` | Unlocks at 250 total blood earned |
| `HealSelfUnlockThreshold` | `const double = 250.0` |
| `HealSelfCost` | `const double = 25.0` |
| `HealSelfAmount` | `const float = 20f` base HP restored |
| `HealUpgradeLevel` | `int` — max 3 |
| `HealSelfAmountEffective` | `HealSelfAmount + HealUpgradeLevel × 10` |
| `HealUpgradeCost` | `floor(40 × 2^HealUpgradeLevel)` |

### Blood Surge

| Member | Value |
|--------|-------|
| `SurgeUnlocked` | Unlocks at 500 total blood earned |
| `SurgeUnlockThreshold` | `const double = 500.0` |
| `SurgeCost` | `const double = 50.0` |
| `SurgeDuration` | `const float = 10f` seconds |
| `SurgeMultiplier` | `const float = 2f` (2× attack) |
| `SurgeActive` | `bool` |
| `SurgeTimeRemaining` | `float` |
| `SurgeUpgradeLevel` | `int` — max 3 |
| `SurgeDurationEffective` | `SurgeDuration + SurgeUpgradeLevel × 5` |
| `SurgeUpgradeCost` | `floor(60 × 2^SurgeUpgradeLevel)` |
| `MaxSpellUpgradeLevel` | `const int = 3` |

---

## Enemy / Wave

| Member | Type | Notes |
|--------|------|-------|
| `Wave` | `int` | Current wave number |
| `EnemyHP` | `float` | |
| `EnemyMaxHP` | `float` | |
| `EnemyName` | `string` | |
| `EnemyAttack` | `float` | dmg/s |
| `EnemySpriteIndex` | `int` | Index into `UIManager.enemySprites` |
| `CurrentEnemyModifier` | `EnemyModifier` | None / Armored / Enraged / Regen |
| `EnemyModifierDisplay` | `string` | Human-readable label with emoji |
| `WavePreviewActive` | `bool` | 3-second pre-spawn countdown |
| `FlawlessActive` | `bool` | True if `_flawlessTimer ≤ 10 s` and enemy alive |

### Wave scaling formulas

```
EnemyMaxHP  = 100 × 1.5^(wave−1) × typeMult × (1 − FortificationDmgReduction)
EnemyAttack = 3   × 1.3^(wave−1) × typeMult
KillReward  = floor(25 × 1.4^(wave−1) × PrestigeMultiplier × (1 + TalismanBonus))
```

Boss: HP ×5, attack ×2.

---

## Boss Waves

| Member | Type | Notes |
|--------|------|-------|
| `NextBossWave` | `int` | Wave number of next boss |
| `IsBossWave` | `bool` | `Wave == NextBossWave` |
| `WavesUntilBoss` | `int` | `NextBossWave − Wave` |
| `BossTimeRemaining` | `float` | Countdown seconds |
| `CurrentBossAbility` | `BossAbility` | None / Shield / Berserk / Drain |
| `BossShieldActive` | `bool` | True while boss shield HP > 0 |
| `BossAbilityDisplay` | `string` | Human-readable with emoji |
| `BossTimeLimit` | `const float = 90f` | Base timer (+ 15 s per `SSBossTimerLevel`) |
| `BossWaveRollback` | `const int = 3` | Waves rolled back on timeout |
| `BossFailBloodPenaltyPct` | `const float = 0.25` | 25% blood lost on timeout |
| `BossShieldFraction` | `const float = 0.20` | Shield = 20% of EnemyMaxHP |
| `BossDrainPerSec` | `const float = 5f` | Drain ability HP/s |

---

## Prestige

| Member | Type | Notes |
|--------|------|-------|
| `PrestigeCount` | `int` | Total prestiges |
| `PrestigeMultiplier` | `double` | `1.0 + 0.5 × PrestigeCount` |
| `PrestigePoints` | `int` | Spendable in Prestige Shop |
| `PendingPrestige` | `bool` | True while talent selection modal is open |
| `PendingTalentChoices` | `TalentFlags[]` | 3 random unowned options |
| `PrestigeWaveRequirement` | `const int = 20` | |

### Prestige milestones

Reached at 5 / 10 / 20 / 50 prestiges. Each adds `+5%` attack (stacks additively).

| Member | Type | Notes |
|--------|------|-------|
| `PrestigeMilestonesReached` | `int` | 0–4 |
| `PrestigeMilestoneDmgBonus` | `float` | `PrestigeMilestonesReached × 0.05` |
| `MilestoneDmgBonusPerLevel` | `const float = 0.05` | |

---

## Prestige Shop

All cost 1 PP (`PrestigeShopCost = 1`). No level cap.

| Property | Effect |
|----------|--------|
| `PSoldierCapLevel` | +10 `MaxSoldiers` per level |
| `PClickBonusLevel` | +0.5 blood/click per level |
| `PRitualEffLevel` | +0.5 blood/s per ritual per level |
| `PWeaponHeadStartLevel` | Unlocks weapon level 1 after prestige |
| `PBloodTitheLevel` | +0.5 blood/s passive ×PrestigeMultiplier |
| `PIronWallLevel` | −10% enemy damage per level |
| `IronWallDmgReduction` | `const float = 0.10` |

---

## Soul Shards

| Member | Type | Notes |
|--------|------|-------|
| `SoulShards` | `double` | |
| `SoulShardShopUnlocked` | `bool` | Unlocks on first boss kill |
| `SSBossTimerLevel` | `int` | +15 s per level, max 3 |
| `SSDoubleChestLevel` | `int` | Double chest odds, max 3 |
| `SSRollbackLevel` | `int` | −1 rollback wave per level, max 3 |
| `SSBloodTapLevel` | `int` | +1 blood/s ×PrestigeMultiplier, max 3 |
| `SSMaxLevel` | `const int = 3` | |
| `SSUpgradeCost` | `const double = 1.0` | Per upgrade |

---

## Blood Bank

| Member | Type | Notes |
|--------|------|-------|
| `BloodBankDeposit` | `double` | Principal on deposit |
| `BloodBankAccrued` | `double` | Interest accrued so far |
| `BankInterestRatePerHour` | `const double = 0.02` | 2%/hr simple interest |
| `BankMaxDeposit` | `const double = 10_000` | Hard deposit cap |

---

## Wave Streak

| Member | Type | Notes |
|--------|------|-------|
| `WaveStreak` | `int` | Consecutive kills without a soldier dying |
| `StreakMultiplier` | `float` | `min(1 + WaveStreak×0.1, 3)` |
| `MaxStreakMultiplier` | `const float = 3f` | |

---

## Talents

| Member | Type | Notes |
|--------|------|-------|
| `Talents` | `TalentFlags` | Bitmask of owned talents |
| `HasTalent(TalentFlags t)` | `bool` | Inline bit check |
| `TalentIronSkinHP` | `const float = 15f` | +15 HP to frontline |
| `TalentBloodFrenzyBonus` | `const double = 0.25` | +25% kill rewards |
| `TalentGluttonMult` | `const float = 1.25f` | +25% ritual blood/s |

---

## Soul Sacrifice

| Member | Type | Notes |
|--------|------|-------|
| `SoulSacrificeUnlocked` | `bool` | Requires `PrestigeCount >= 1` |
| `SoulSacrificeBloodMult` | `const double = 10.0` | Reward = normal kill × 10 |

---

## Daily Challenge

| Member | Type | Notes |
|--------|------|-------|
| `DailyChallengeAvailable` | `bool` | Once per UTC day |
| `DailyChallengeActive` | `bool` | |
| `ChallengeTimeRemaining` | `float` | |
| `ChallengeDuration` | `const float = 60f` | Seconds |
| `ChallengeHPMult` | `const float = 5f` | Enemy HP ×5 |
| `ChallengeAtkMult` | `const float = 2f` | Enemy attack ×2 |
| `ChallengeBloodMult` | `const double = 5.0` | Kill reward ×5 |

---

## Blood Corruption

| Member | Type | Notes |
|--------|------|-------|
| `CorruptionLevel` | `int` | Increments each prestige, max 5 |
| `MaxCorruptionLevel` | `const int = 5` | |
| `CorruptionHPPenalty` | `const float = 5f` | −5 HP per level from frontline max HP |
| `PurifyCost` | `const double = 3.0` | Soul shards to remove 1 corruption level |

---

## Statistics / Settings

| Member | Type | Notes |
|--------|------|-------|
| `TotalEnemiesKilled` | `int` | |
| `TimePlayed` | `double` | Seconds |
| `Achievements` | `AchievementFlags` | Bitmask |
| `OfflineWoodEarned` | `double` | Set on load, cleared by `ClearOfflineEarnings()` |
| `OfflineBloodEarned` | `double` | |
| `SoundEnabled` | `bool` | |
| `NotificationsEnabled` | `bool` | |

---

## Public Methods

### Farming / Spells

| Method | Returns | Effect |
|--------|---------|--------|
| `FarmBlood()` | `void` | Adds `EffectiveBloodPerClick` (×10 if daily bonus) |
| `UseHealSelf()` | `bool` | −25 blood, +`HealSelfAmountEffective` HP to frontline |
| `UseSurge()` | `bool` | −50 blood, activates Blood Surge for `SurgeDurationEffective` s |
| `UseBloodPact()` | `bool` | −200 blood, +100 wood |
| `UseSoulSacrifice()` | `bool` | −1 frontline soldier, adds ×10 kill-reward blood |
| `StartDailyChallenge()` | `bool` | Activates daily challenge on current enemy |
| `UpgradeSurge()` | `bool` | −`SurgeUpgradeCost` blood, increments `SurgeUpgradeLevel` |
| `UpgradeHealSelf()` | `bool` | −`HealUpgradeCost` blood, increments `HealUpgradeLevel` |

### Soldiers

| Method | Returns | Effect |
|--------|---------|--------|
| `BuyTank()` | `bool` | −10 blood, +1 Tank |
| `BuyBerserker()` | `bool` | −10 blood, +1 Berserker |
| `BuyPaladin()` | `bool` | −10 blood, +1 Paladin |
| `BuySoldier()` | `bool` | Alias for `BuyTank()` |
| `ToggleFormation()` | `void` | Flips `BerserkerFront` |

### Workers / Upgrades

| Method | Returns | Effect |
|--------|---------|--------|
| `BuyWorker()` | `bool` | −50 blood, +1 worker |
| `BuyBloodRitual()` | `bool` | −`BloodRitualCost` wood, +1 ritual |
| `UpgradeBarracks()` | `bool` | −`BarracksUpgradeCost` wood, +5 `MaxSoldiers` |
| `UpgradeFortification()` | `bool` | −`FortificationCost` wood, +1 fort level |
| `UpgradeWeapon()` | `bool` | −`WeaponUpgradeCost` wood |
| `UpgradeArmor()` | `bool` | −`ArmorUpgradeCost` wood, also heals +10 HP |
| `UpgradeTalisman()` | `bool` | −`TalismanUpgradeCost` wood |

### Blood Bank

| Method | Returns | Effect |
|--------|---------|--------|
| `DepositToBank(double amount)` | `bool` | Moves up to `amount` blood to bank (capped at 10,000) |
| `WithdrawFromBank()` | `bool` | Returns deposit + accrued interest |
| `ClearOfflineEarnings()` | `void` | Zeros `OfflineWoodEarned` and `OfflineBloodEarned` |

### Prestige Shop

| Method | Effect |
|--------|--------|
| `BuyPSoldierCap()` | −1 PP, +10 `MaxSoldiers` |
| `BuyPClickBonus()` | −1 PP |
| `BuyPRitualEff()` | −1 PP |
| `BuyPWeaponHeadStart()` | −1 PP |
| `BuyPBloodTithe()` | −1 PP |
| `BuyPIronWall()` | −1 PP |

### Soul Shard Shop

| Method | Effect |
|--------|--------|
| `BuySSBossTimer()` | −1 shard, +15 s boss timer |
| `BuySSDoubleChest()` | −1 shard |
| `BuySSRollback()` | −1 shard, −1 rollback wave |
| `BuySSBloodTap()` | −1 shard |

### Prestige

| Method | Returns | Effect |
|--------|---------|--------|
| `RequestPrestige()` | `void` | Sets `PendingPrestige = true`, generates `PendingTalentChoices` |
| `ConfirmPrestige(int choiceIdx)` | `bool` | Applies talent choice then calls `Prestige()` |
| `CancelPrestige()` | `void` | Clears pending state |
| `Prestige()` | `bool` | Resets combat/resources, increments `PrestigeCount` |
| `Purify()` | `bool` | −3 soul shards, −1 `CorruptionLevel` |

### Settings / Admin

| Method | Effect |
|--------|--------|
| `ToggleSound()` | Flips `SoundEnabled` |
| `ToggleNotifications()` | Flips `NotificationsEnabled` |
| `ResetAllData()` | Wipes all `PlayerPrefs` and resets all state to defaults |

---

## Static Helpers

| Method | Notes |
|--------|-------|
| `FormatNumber(double value)` | Converts to `K/M/B` suffix strings |
| `FormatHP(float value)` | One decimal for values ≥ 100 |
| `MixedArmyDmgReduction` | `const float = 0.15` (static helper for UI) |

---

## Save / Load

`Save()` writes to `PlayerPrefs` every 30 s and on `OnApplicationPause`/`OnApplicationQuit`. `Load()` runs once in `Awake()`. Full key list matches field names exactly (e.g., `"Blood"`, `"TankCount"`, `"NextBossWave"`, etc.). `"SaveTime"` (ISO 8601) enables offline earnings on next load, capped at 8 hours.

---

## Test Helpers (UNITY_INCLUDE_TESTS only)

All `ForTest` methods expose private state for EditMode tests without modifying production code paths. Notable:

| Method | Purpose |
|--------|---------|
| `ResetForTest()` | Nulls `Instance` |
| `TickCombatForTest(float dt)` | Runs one `RunCombat(dt)` frame |
| `SkipWavePreviewForTest()` | Ends preview and spawns enemy |
| `CalculateOfflineWood(workers, seconds)` | Static: same formula as Load() |
| `CalculateOfflineBlood(rituals, ritualEffLevel, prestigeMult, seconds, titheLevel)` | Static |
