# UIManager

Subscribes to `GameManager` events in `Start()`. Calls `Refresh()` on every `OnStateChanged` to redraw all UI elements. Also handles toasts, offline panel, damage numbers, and feature-request submissions.

Assembly: `GameAssembly`  
File: `Assets/Scripts/UIManager.cs`

---

## Lifecycle

| Method | When |
|--------|------|
| `Start()` | Subscribes to all four `GameManager` events; calls `Refresh()` and `ShowOfflinePanel()` |
| `OnDestroy()` | Unsubscribes all event handlers |

---

## Serialized Fields (by Inspector header)

All fields are assigned by `SceneBuilder.BuildScene()`. They are `public` so `SceneBuilder` can set them directly via component references.

### Resources
| Field | Type |
|-------|------|
| `bloodText` | `Text` |
| `woodText` | `Text` |

### Enemy
| Field | Type |
|-------|------|
| `waveText` | `Text` |
| `waveSubText` | `Text` — shows boss countdown or streak multiplier |
| `enemyNameText` | `Text` |
| `enemyModifierText` | `Text` — hidden when no modifier/ability |
| `enemyHPFill` | `Image` — fillAmount |
| `enemyHPText` | `Text` |
| `bossTimerText` | `Text` |
| `bossTimerRow` | `GameObject` — hidden when not boss wave |
| `wavePreviewBanner` | `GameObject` — overlay during 3-s preview |
| `wavePreviewText` | `Text` |

### Soldiers
| Field | Type |
|-------|------|
| `soldierCountText` | `Text` — shows T/B/P counts + composition bonus |
| `soldierHPRow` | `GameObject` — hidden when no soldiers |
| `soldierHPFill` | `Image` |
| `soldierHPText` | `Text` |
| `buyTankButton` | `Button` |
| `buyBerserkerButton` | `Button` |
| `buyPaladinButton` | `Button` |
| `formationButton` | `Button` |
| `formationButtonText` | `Text` |
| `mixedBonusText` | `Text` — shown only when army is mixed |
| `healSelfPanel` | `GameObject` |
| `healSelfButton` | `Button` |

### Blood Surge
| Field | Type |
|-------|------|
| `bloodSurgePanel` | `GameObject` |
| `bloodSurgeInfoText` | `Text` |
| `bloodSurgeButton` | `Button` |
| `upgradeSurgeButton` | `Button` |
| `surgeCostText` | `Text` |
| `upgradeHealSelfButton` | `Button` |
| `healCostText` | `Text` |

### Workers
| Field | Type |
|-------|------|
| `workersPanel` | `GameObject` |
| `workerInfoText` | `Text` |
| `buyWorkerButton` | `Button` |
| `bloodPactButton` | `Button` |
| `bloodPactText` | `Text` |

### Equipment
| Field | Type |
|-------|------|
| `equipmentPanel` | `GameObject` |
| `weaponInfoText` | `Text` |
| `upgradeWeaponButton` | `Button` |
| `weaponCostText` | `Text` |
| `armorInfoText` | `Text` |
| `upgradeArmorButton` | `Button` |
| `armorCostText` | `Text` |
| `talismanInfoText` | `Text` |
| `upgradeTalismanButton` | `Button` |
| `talismanCostText` | `Text` |

### Fortifications
| Field | Type |
|-------|------|
| `fortInfoText` | `Text` |
| `upgradeFortButton` | `Button` |
| `fortCostText` | `Text` |

### Blood Ritual
| Field | Type |
|-------|------|
| `bloodRitualPanel` | `GameObject` |
| `bloodRitualInfoText` | `Text` |
| `buyBloodRitualButton` | `Button` |
| `bloodRitualCostText` | `Text` |

### Prestige
| Field | Type |
|-------|------|
| `prestigePanel` | `GameObject` |
| `prestigeInfoText` | `Text` |
| `prestigeButton` | `Button` |

### Prestige Shop
| Field | Type |
|-------|------|
| `prestigeShopPanel` | `GameObject` |
| `prestigeShopPointsText` | `Text` |
| `pSoldierCapInfoText` | `Text` |
| `pSoldierCapButton` | `Button` |
| `pClickBonusInfoText` | `Text` |
| `pClickBonusButton` | `Button` |
| `pRitualEffInfoText` | `Text` |
| `pRitualEffButton` | `Button` |
| `pWeaponHeadStartInfoText` | `Text` |
| `pWeaponHeadStartButton` | `Button` |
| `pBloodTitheInfoText` | `Text` |
| `pBloodTitheButton` | `Button` |
| `pIronWallInfoText` | `Text` |
| `pIronWallButton` | `Button` |

### Soul Shard Shop
| Field | Type |
|-------|------|
| `soulShardShopPanel` | `GameObject` |
| `soulShardShopPointsText` | `Text` |
| `ssBossTimerInfoText` | `Text` |
| `ssBossTimerButton` | `Button` |
| `ssDoubleChestInfoText` | `Text` |
| `ssDoubleChestButton` | `Button` |
| `ssRollbackInfoText` | `Text` |
| `ssRollbackButton` | `Button` |
| `ssBloodTapInfoText` | `Text` |
| `ssBloodTapButton` | `Button` |

### Settings
| Field | Type |
|-------|------|
| `settingsPanel` | `GameObject` |
| `soundToggleText` | `Text` |
| `notifToggleText` | `Text` |

### Blood Bank
| Field | Type |
|-------|------|
| `bloodBankPanel` | `GameObject` |
| `bloodBankInfoText` | `Text` |
| `bloodBankAccruedText` | `Text` |
| `depositBloodButton` | `Button` |
| `withdrawBloodButton` | `Button` |

### Prestige Milestone
| Field | Type |
|-------|------|
| `prestigeMilestoneText` | `Text` |

### Stats
| Field | Type |
|-------|------|
| `statsPanel` | `GameObject` |
| `statsText` | `Text` |

### Achievement Toast
| Field | Type |
|-------|------|
| `achievementToast` | `GameObject` |
| `achievementToastText` | `Text` |

### Sprites
| Field | Type |
|-------|------|
| `enemyImage` | `Image` |
| `enemySprites` | `Sprite[]` — 7 sprites, index matches `EnemySpriteIndex` |

### Barracks
| Field | Type |
|-------|------|
| `barracksInfoText` | `Text` |
| `upgradeBarracksButton` | `Button` |
| `barracksUpgradeCostText` | `Text` |

### Feature Request
| Field | Type |
|-------|------|
| `featureRequestPanel` | `GameObject` |
| `featureTitleField` | `InputField` — max 120 chars |
| `featureDescField` | `InputField` — max 1000 chars |
| `featureStatusText` | `Text` |
| `featureSubmitButton` | `Button` |

### Offline Earnings
| Field | Type |
|-------|------|
| `offlinePanel` | `GameObject` |
| `offlineText` | `Text` |

### Talent Selection
| Field | Type |
|-------|------|
| `talentSelectionPanel` | `GameObject` |
| `talentButton0/1/2` | `Button` |
| `talentButtonText0/1/2` | `Text` |
| `talentHeaderText` | `Text` |

### Daily Challenge
| Field | Type |
|-------|------|
| `dailyChallengeRow` | `GameObject` |
| `dailyChallengeButton` | `Button` |
| `dailyChallengeInfoText` | `Text` |

### Corruption
| Field | Type |
|-------|------|
| `corruptionText` | `Text` |
| `purifyButton` | `Button` |
| `purifyButtonText` | `Text` |

### Soul Sacrifice
| Field | Type |
|-------|------|
| `soulSacrificeButton` | `Button` |
| `soulSacrificeInfoText` | `Text` |

### Damage Numbers
| Field | Type |
|-------|------|
| `damageLayer` | `RectTransform` — parent for spawned `DamageNumber` GOs |

---

## Public Methods

| Method | Notes |
|--------|-------|
| `ShowStatsPanel()` | Activates `statsPanel`, calls `RefreshStats()` |
| `ShowSettingsPanel()` | Activates `settingsPanel` |
| `ShowFeaturePanel()` | Activates `featureRequestPanel`, checks rate limit |
| `HideFeaturePanel()` | Hides and clears feature request panel |
| `SubmitFeature()` | Validates input then calls `PostIssue()` coroutine |
| `DismissOfflinePanel()` | Hides `offlinePanel`, calls `GameManager.ClearOfflineEarnings()` |

---

## Internal Methods

| Method | Notes |
|--------|-------|
| `Refresh()` | Full UI redraw — called on every `OnStateChanged` |
| `RefreshSettings()` | Updates sound/notification toggle texts |
| `RefreshStats()` | Rebuilds stats text block with time played, achievements, etc. |
| `SpawnDamageNumber(float amount, bool isEnemy)` | Called by `OnDamageDealt`; positions and spawns `DamageNumber` |
| `ShowAchievementToast(AchievementFlags)` | Triggered by `OnAchievementUnlocked`; shows 3-s toast |
| `ShowMilestoneToast(string)` | Triggered by `OnMilestoneChest` |
| `ShowOfflinePanel()` | Called once in `Start()` if offline earnings are non-zero |

---

## Feature Request Flow

1. Player opens the panel via **Suggest** button.
2. `ShowFeaturePanel()` checks a 60-minute rate limit stored in `PlayerPrefs["LastFeatureSubmit"]`.
3. `SubmitFeature()` validates title (non-empty, ≤ 120 chars), then POSTs a GitHub issue via `UnityWebRequest` to `repos/willettmeister/bloodidle/issues` using the PAT from `Assets/Resources/bloodidle_secrets.txt`.
4. Issue body includes player context (wave, prestige, army composition, talents, etc.).
5. On success the rate limit timestamp is written and the button is disabled.
