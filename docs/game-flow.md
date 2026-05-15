# Game Flow

## Main Update Loop

Every frame, `GameManager.Update()` runs a set of independent subsystems. Each subsystem sets `changed = true` if it mutated state, and `OnStateChanged` fires at the end if anything changed.

```mermaid
flowchart TD
    U([Update dt]) --> SURGE{SurgeActive?}
    SURGE -->|yes| DT1[Decrement SurgeTimeRemaining]
    SURGE --> REGEN{All-Tank army\n& HP < max?}
    DT1 --> REGEN
    REGEN -->|yes| DT2[Regen HP at TankRegenRate]
    REGEN --> PAL{PaladinCount > 0\n& HP < max?}
    DT2 --> PAL
    PAL -->|yes| DT3[Heal at PaladinHealRate × count]
    PAL --> TAP{SSBloodTapLevel > 0?}
    DT3 --> TAP
    TAP -->|yes| DT4[AddBlood BloodTapPerSec × dt]
    TAP --> FLAWLESS{EnemyHP > 0\n& !WavePreview?}
    DT4 --> FLAWLESS
    FLAWLESS -->|yes| DT5[Increment flawless timer]
    FLAWLESS --> WORKERS{WorkerCount > 0?}
    DT5 --> WORKERS
    WORKERS -->|yes| DT6[Wood += WoodPerSecond × dt]
    WORKERS --> RITUALS{BloodPerSec > 0?}
    DT6 --> RITUALS
    RITUALS -->|yes| DT7[AddBlood BloodPerSec × dt]
    RITUALS --> BANK{BloodBankDeposit > 0?}
    DT7 --> BANK
    BANK -->|yes| DT8[Accrue bank interest]
    BANK --> REGEN2{Enemy Regen modifier?}
    DT8 --> REGEN2
    REGEN2 -->|yes| DT9[EnemyHP += EnemyMaxHP × 2% × dt]
    REGEN2 --> PREVIEW{WavePreviewActive?}
    DT9 --> PREVIEW
    PREVIEW -->|timer expired| SP[SpawnEnemy wave]
    PREVIEW --> COMBAT{Soldiers > 0\n& EnemyHP > 0?}
    SP --> COMBAT
    COMBAT -->|yes| RC[RunCombat dt]
    COMBAT --> CHALLENGE{DailyChallengeActive\n& timer > 0?}
    RC --> CHALLENGE
    CHALLENGE -->|timer out| CE[End challenge]
    CHALLENGE --> BOSS{IsBossWave\n& !Challenge?}
    CE --> BOSS
    BOSS -->|timer out| BTE[BossTimerExpired]
    BOSS --> FIRE{changed?}
    BTE --> FIRE
    FIRE -->|yes| EVT[OnStateChanged.Invoke]
```

## Combat Round

```mermaid
flowchart TD
    RC([RunCombat dt]) --> EFF[eff = TotalAttack\n× SurgeMultiplier if active]
    EFF --> ARM{Armored modifier?}
    ARM -->|yes| HALF[eff × 0.5]
    ARM --> SHIELD{BossShieldActive?}
    HALF --> SHIELD
    SHIELD -->|yes| SHD[shieldHP -= eff × dt\nif depleted: BossShieldActive = false]
    SHIELD -->|no| DMG[EnemyHP -= eff × dt]
    SHD --> DEAD{EnemyHP ≤ 0?}
    DMG --> DEAD
    DEAD -->|no| INCOMING[incomingAtk = EnemyAttack]
    DEAD -->|yes| KILL[Enemy Kill sequence]

    KILL --> BOSSCHECK{wasBoss?}
    BOSSCHECK -->|yes| SHARDS[SoulShards++\nreward × 3]
    BOSSCHECK --> CHALCHECK{wasChallenge?}
    SHARDS --> CHALCHECK
    CHALCHECK -->|yes| CHALREW[reward × ChallengeBloodMult]
    CHALCHECK --> FRENZY{Talent: BloodFrenzy?}
    CHALREW --> FRENZY
    FRENZY -->|yes| FREW[reward × 1.25]
    FRENZY --> FLAWLESSCHECK{_flawlessTimer\n0 < t ≤ 10s?}
    FREW --> FLAWLESSCHECK
    FLAWLESSCHECK -->|yes| FLAW[reward × 2]
    FLAWLESSCHECK --> STREAK[reward × StreakMultiplier\nWaveStreak++]
    FLAW --> STREAK
    STREAK --> ADDBLOOD[AddBlood reward]
    ADDBLOOD --> BLOODRUSH{Talent: BloodRush\n& boss/challenge?}
    BLOODRUSH -->|yes| SURGE2[Activate Blood Surge]
    BLOODRUSH --> WAVE[Wave++\nWavePreviewActive = true]

    INCOMING --> BERSERK{Berserk ability\n& HP < 25%?}
    BERSERK -->|yes| DOUBLE[incomingAtk × 2]
    BERSERK --> MIXED{MixedArmy?}
    DOUBLE --> MIXED
    MIXED -->|yes| REDUCE[incomingAtk × 0.85]
    MIXED --> IRONWALL{PIronWallLevel > 0?}
    REDUCE --> IRONWALL
    IRONWALL -->|yes| IWRED[incomingAtk × 1 - level×10%]
    IRONWALL --> DRAIN{Drain ability?}
    IWRED --> DRAIN
    DRAIN -->|yes| TOTAL[totalIncoming += BossDrainPerSec]
    DRAIN --> SOLDIERDOWN[SoldierHP -= totalIncoming × dt]
    TOTAL --> SOLDIERDOWN
    SOLDIERDOWN --> SOLDIERDIE{SoldierHP ≤ 0?}
    SOLDIERDIE -->|Undying talent\nnot used yet| REVIVE[SoldierHP = 1\n_undyingUsed = true]
    SOLDIERDIE -->|yes| NEXTFRONT[Remove frontline soldier\nSoldierHP = next FrontlineMaxHP\nWaveStreak = 0]
    SOLDIERDIE -->|no| DMGTICK{dmgTimer ≥ 0.4s?}
    REVIVE --> DMGTICK
    NEXTFRONT --> DMGTICK
    DMGTICK -->|yes| EMIT[OnDamageDealt events → DamageNumber]
```

## Player Actions

```mermaid
flowchart LR
    subgraph Blood["Blood Actions"]
        FB[Farm Blood\n+EffectiveBloodPerClick]
        HealSelf[Heal Self\n-25 blood +20 HP]
        Surge[Blood Surge\n-50 blood 2× atk 10s]
        Pact[Blood Pact\n-200 blood +100 wood]
        SoulSac[Soul Sacrifice\n-1 soldier ×10 blood reward]
        Deposit[Deposit to Bank\n10% of blood]
    end

    subgraph Wood["Wood Actions"]
        Worker[Buy Worker\n-50 blood +0.5 wood/s]
        Ritual[Buy Blood Ritual\n-30 wood +1 blood/s]
        Barracks[Upgrade Barracks\n-20 wood +5 soldier cap]
        Fort[Upgrade Fort\n-50 wood −2% enemy HP]
        Equip[Upgrade Equipment\nWeapon/Armor/Talisman]
    end

    subgraph Prestige["Prestige Actions"]
        Prestige[Request Prestige\nwave ≥ 20]
        Talent[Choose Talent\n3 random options]
        PresShop[Prestige Shop\n1 PP per upgrade]
        Purify[Purify\n-3 soul shards −1 corruption]
    end

    subgraph SoulShard["Soul Shard Shop\nunlocked on first boss kill"]
        SSBoss[Boss Timer +15s]
        SSDouble[Double Chest chance]
        SSRollback[Rollback Shield −1 wave]
        SSBloodTap[Blood Tap +1 blood/s]
    end
```

## Wave Progression

```mermaid
stateDiagram-v2
    [*] --> WavePreview : SpawnEnemy()
    WavePreview --> Combat : Preview timer 3s expires
    Combat --> EnemyDead : EnemyHP ≤ 0
    Combat --> AllSoldiersDead : SoldierHP ≤ 0 and no soldiers left
    EnemyDead --> WavePreview : Wave++ SpawnEnemy()
    AllSoldiersDead --> Combat : Enemy HP persists — combat pauses until soldier bought
    Combat --> BossTimeout : Boss timer expires
    BossTimeout --> WavePreview : BossTimerExpired() — rollback 3 waves, wipe soldiers
```
