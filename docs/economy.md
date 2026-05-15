# Economy

## Resources

| Resource | Storage | Primary Source | Sink |
|----------|---------|----------------|------|
| Blood | `double Blood` | Clicks, rituals, kills | Soldiers, spells, bank deposits |
| Wood | `double Wood` | Workers (0.5/s each) | Barracks, rituals, equipment, fortifications |
| Soul Shards | `double SoulShards` | Boss kills | Soul Shard Shop upgrades, Purify |
| Prestige Points | `int PrestigePoints` | One per prestige | Prestige Shop purchases |

## Blood Flow

```mermaid
flowchart LR
    subgraph Sources["Blood Sources"]
        Click["Farm Blood\n+EffectiveBloodPerClick\n(+0.5 per PClickBonus level)"]
        Ritual["Blood Rituals\n+1 blood/s each\n×PrestigeMultiplier"]
        Tithe["Blood Tithe\n+0.5/s per PP level\n×PrestigeMultiplier"]
        Tap["Blood Tap\n+1/s per SS level\n×PrestigeMultiplier"]
        Kill["Enemy Kill\nbase 25 × 1.4^wave\n×various multipliers"]
        Bank["Blood Bank\n+2%/hr on deposit\nmax 10,000 deposit"]
        Glutton["Glutton Talent\n+25% to ritual income"]
    end

    subgraph Sinks["Blood Sinks"]
        Soldier["Buy Soldier\n10 blood each"]
        Heal["Heal Self\n25 blood"]
        Surge["Blood Surge\n50 blood"]
        Sacrifice["Soul Sacrifice\n-1 soldier, +10× kill reward"]
        Deposit["Bank Deposit\n10% of current blood"]
    end

    Click --> Blood[(Blood)]
    Ritual --> Blood
    Tithe --> Blood
    Tap --> Blood
    Kill --> Blood
    Bank --> Blood
    Glutton -.->|"multiplier"| Ritual
    Blood --> Soldier
    Blood --> Heal
    Blood --> Surge
    Blood --> Sacrifice
    Blood --> Deposit
```

## Wood Flow

```mermaid
flowchart LR
    subgraph Sources["Wood Sources"]
        Worker["Workers\n0.5 wood/s each"]
        BloodPact["Blood Pact\n200 blood → 100 wood"]
        Chest["Milestone Chest\n+wood at wave 5/10/15/20..."]
    end

    subgraph Sinks["Wood Sinks"]
        BloodRitual["Buy Blood Ritual\n30 wood → doubles"]
        Barracks["Upgrade Barracks\n20 wood → ×2 each upgrade"]
        Fort["Upgrade Fortifications\n50 wood → ×2 per level"]
        Weapon["Upgrade Weapon\n20 wood × 2^level"]
        Armor["Upgrade Armor\n15 wood × 2^level"]
        Talisman["Upgrade Talisman\n25 wood × 2^level"]
    end

    Worker --> Wood[(Wood)]
    BloodPact --> Wood
    Chest --> Wood
    Wood --> BloodRitual
    Wood --> Barracks
    Wood --> Fort
    Wood --> Weapon
    Wood --> Armor
    Wood --> Talisman
```

## Soul Shard Flow

```mermaid
flowchart LR
    BossKill["Boss Kill\n+1 shard\n(+2 with ShardHunter talent)"] --> SS[(Soul Shards)]
    SS -->|"1 shard each"| BossTimer["Boss Timer +15s\n(max Lv.3)"]
    SS -->|"1 shard each"| DoubleChest["Double Chest odds\n(max Lv.3)"]
    SS -->|"1 shard each"| Rollback["Rollback Shield −1 wave\n(max Lv.3)"]
    SS -->|"1 shard each"| BloodTap["Blood Tap +1 blood/s\n(max Lv.3)"]
    SS -->|"3 shards"| Purify["Purify\n−1 Corruption Level"]
```

## Unlock Thresholds

Unlocks trigger inside `AddBlood()` by checking `TotalBloodEarned`:

| Threshold | Unlock |
|-----------|--------|
| 10 blood | Buy first soldier (UI implicit — cost check) |
| 50 blood | Buy worker (cost check) |
| 200 blood earned | Workers panel visible |
| 250 blood earned | Heal Self spell |
| 500 blood earned | Blood Surge spell |

## Prestige Multiplier

```
PrestigeMultiplier = 1.0 + 0.5 × PrestigeCount
```

Affects: kill rewards, blood ritual income, blood tithe, blood tap, effective blood per click. Does NOT affect wood income or wood costs.

## Blood Bank

- Deposit: 10% of current blood per press, capped at 10,000 total deposit.
- Interest: 2%/hr of deposit, continuously accrued (not compounded).
- Withdraw: returns deposit + all accrued interest instantly.
- Deposit survives prestige; soldiers and blood do not.

## Milestone Chests

Every 5th wave (wave 5, 10, 15, 20 …) a chest fires `OnMilestoneChest` and grants one of:
- Blood bonus: `floor(100 × wave × PrestigeMultiplier × mult)`
- Free soldier (Tank or Berserker, random) — or blood if at cap
- Wood bonus: `floor(25 × wave × mult)` where mult = 2× if `SSDoubleChestLevel > 0`
