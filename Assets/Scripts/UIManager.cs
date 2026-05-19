using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Resources")]
    public Text bloodText;
    public Text woodText;

    [Header("Enemy")]
    public Text waveText;
    public Text waveSubText;
    public Text enemyNameText;
    public Text enemyModifierText;
    public Image enemyHPFill;
    public Text enemyHPText;
    public Text bossTimerText;
    public GameObject bossTimerRow;
    public GameObject wavePreviewBanner;
    public Text wavePreviewText;

    [Header("Soldiers")]
    public Text soldierCountText;
    public GameObject soldierHPRow;
    public Image soldierHPFill;
    public Text soldierHPText;
    public Button buyTankButton;
    public Button buyBerserkerButton;
    public Button buyPaladinButton;
    public Button formationButton;
    public Text formationButtonText;
    public Text mixedBonusText;
    public GameObject healSelfPanel;
    public Button healSelfButton;

    [Header("Blood Surge")]
    public GameObject bloodSurgePanel;
    public Text bloodSurgeInfoText;
    public Button bloodSurgeButton;
    public Button upgradeSurgeButton;
    public Text surgeCostText;
    public Button upgradeHealSelfButton;
    public Text healCostText;

    [Header("Blood Storm")]
    public Text bloodStormInfoText;
    public Button bloodStormButton;

    [Header("Workers")]
    public GameObject workersPanel;
    public Text workerInfoText;
    public Button buyWorkerButton;
    public Button bloodPactButton;
    public Text bloodPactText;
    public Text shrineInfoText;
    public Button buyShrineButton;
    public Text clickPowerInfoText;
    public Button clickPowerButton;

    [Header("Equipment")]
    public GameObject equipmentPanel;
    public Text weaponInfoText;
    public Button upgradeWeaponButton;
    public Text weaponCostText;
    public Text armorInfoText;
    public Button upgradeArmorButton;
    public Text armorCostText;
    public Text talismanInfoText;
    public Button upgradeTalismanButton;
    public Text talismanCostText;

    [Header("Fortifications")]
    public Text fortInfoText;
    public Button upgradeFortButton;
    public Text fortCostText;

    [Header("Blood Ritual")]
    public GameObject bloodRitualPanel;
    public Text bloodRitualInfoText;
    public Button buyBloodRitualButton;
    public Text bloodRitualCostText;

    [Header("Prestige")]
    public GameObject prestigePanel;
    public Text prestigeInfoText;
    public Button prestigeButton;

    [Header("Prestige Shop")]
    public GameObject prestigeShopPanel;
    public Text prestigeShopPointsText;
    public Text pSoldierCapInfoText;
    public Button pSoldierCapButton;
    public Text pClickBonusInfoText;
    public Button pClickBonusButton;
    public Text pRitualEffInfoText;
    public Button pRitualEffButton;
    public Text pWeaponHeadStartInfoText;
    public Button pWeaponHeadStartButton;
    public Text pBloodTitheInfoText;
    public Button pBloodTitheButton;
    public Text pIronWallInfoText;
    public Button pIronWallButton;
    public Text pBountyBonusInfoText;
    public Button pBountyBonusButton;

    [Header("Soul Shard Shop")]
    public GameObject soulShardShopPanel;
    public Text soulShardShopPointsText;
    public Text ssBossTimerInfoText;
    public Button ssBossTimerButton;
    public Text ssDoubleChestInfoText;
    public Button ssDoubleChestButton;
    public Text ssRollbackInfoText;
    public Button ssRollbackButton;
    public Text ssBloodTapInfoText;
    public Button ssBloodTapButton;
    public Text ssShardHungerInfoText;
    public Button ssShardHungerButton;

    [Header("Settings")]
    public GameObject settingsPanel;
    public Text soundToggleText;
    public Text notifToggleText;

    [Header("Blood Bank")]
    public GameObject bloodBankPanel;
    public Text bloodBankInfoText;
    public Text bloodBankAccruedText;
    public Button depositBloodButton;
    public Button withdrawBloodButton;

    [Header("Prestige Milestone")]
    public Text prestigeMilestoneText;

    [Header("Stats")]
    public GameObject statsPanel;
    public Text statsText;

    [Header("Achievement Toast")]
    public GameObject achievementToast;
    public Text achievementToastText;

    [Header("Sprites")]
    public Image enemyImage;
    public Sprite[] enemySprites;

    [Header("Barracks")]
    public Text barracksInfoText;
    public Button upgradeBarracksButton;
    public Text barracksUpgradeCostText;

    [Header("Feature Request")]
    public GameObject featureRequestPanel;
    public InputField featureTitleField;
    public InputField featureDescField;
    public Text featureStatusText;
    public Button featureSubmitButton;

    [Header("Feature Vote")]
    public GameObject featureVotePanel;
    public Text voteIssueText;
    public Text voteStatusText;
    public Button votePrevButton;
    public Button voteNextButton;
    public Button voteButton;

    [Header("Offline Earnings")]
    public GameObject offlinePanel;
    public Text offlineText;

    [Header("Talent Selection")]
    public GameObject talentSelectionPanel;
    public Button talentButton0, talentButton1, talentButton2;
    public Text   talentButtonText0, talentButtonText1, talentButtonText2;
    public Text   talentHeaderText;

    [Header("Daily Challenge")]
    public GameObject dailyChallengeRow;
    public Button     dailyChallengeButton;
    public Text       dailyChallengeInfoText;

    [Header("Corruption")]
    public Text   corruptionText;
    public Button purifyButton;
    public Text   purifyButtonText;

    [Header("Soul Sacrifice")]
    public Button soulSacrificeButton;
    public Text   soulSacrificeInfoText;

    [Header("Damage Numbers")]
    public RectTransform damageLayer;

    static readonly (AchievementFlags flag, string title, double bloodReward, int ppReward)[] k_AchievDefs =
    {
        (AchievementFlags.FirstKill,     "First Blood",         50.0,  0),
        (AchievementFlags.Wave10,        "Wave 10 Reached",     200.0, 0),
        (AchievementFlags.Wave25,        "Wave 25 Reached",     500.0, 0),
        (AchievementFlags.Blood1K,       "Blood Hoarder (1K)",  100.0, 0),
        (AchievementFlags.Blood10K,      "Blood Baron (10K)",   500.0, 0),
        (AchievementFlags.FirstSoldier,  "First Recruit",       25.0,  0),
        (AchievementFlags.FullLegion,    "Full Legion",         300.0, 0),
        (AchievementFlags.FirstRitual,   "Blood Ritualist",     100.0, 0),
        (AchievementFlags.FirstPrestige, "Reborn in Blood",     0.0,   1),
        (AchievementFlags.Wave50,        "Wave 50 Reached",       1000.0, 0),
        (AchievementFlags.Blood100K,     "Blood Empire (100K)",   1000.0, 0),
        (AchievementFlags.Untouchable,   "Untouchable (×10)",     500.0,  0),
        (AchievementFlags.Prestige3,     "Reborn Thrice",         0.0,    1),
        (AchievementFlags.Wave100,       "Centurion (Wave 100)",  2000.0, 0),
        (AchievementFlags.BloodMillion,  "Blood Millionaire",     2000.0, 0),
        (AchievementFlags.BossSlayer,    "Boss Slayer (×25)",     0.0,    1),
    };

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[UIManager] GameManager.Instance is null — check scene setup.");
            return;
        }
        GameManager.Instance.OnStateChanged        += Refresh;
        GameManager.Instance.OnDamageDealt         += SpawnDamageNumber;
        GameManager.Instance.OnAchievementUnlocked += ShowAchievementToast;
        GameManager.Instance.OnMilestoneChest      += ShowMilestoneToast;
        Refresh();
        ShowOfflinePanel();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged        -= Refresh;
            GameManager.Instance.OnDamageDealt         -= SpawnDamageNumber;
            GameManager.Instance.OnAchievementUnlocked -= ShowAchievementToast;
            GameManager.Instance.OnMilestoneChest      -= ShowMilestoneToast;
        }
    }

    void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null || bloodText == null)
        {
            Debug.LogWarning("[UIManager] UI references not wired. Run IdleClicker → Setup Scene.");
            return;
        }

        // Header
        string dailyTag = gm.DailyBonusAvailable ? "  ★ DAILY ×10" : "";
        bloodText.text = gm.BloodPerSec > 0
            ? $"Blood: {GameManager.FormatNumber(gm.Blood)}  +{gm.BloodPerSec:F1}/s{dailyTag}"
            : $"Blood: {GameManager.FormatNumber(gm.Blood)}{dailyTag}";

        if (gm.SoulShardShopUnlocked)
            woodText.text = $"Wood: {GameManager.FormatNumber(gm.Wood)}  ⬡{GameManager.FormatNumber(gm.SoulShards)}";
        else
            woodText.text = gm.WoodPerSecond > 0
                ? $"Wood: {GameManager.FormatNumber(gm.Wood)}  +{gm.WoodPerSecond:F1}/s"
                : $"Wood: {GameManager.FormatNumber(gm.Wood)}";

        // Wave preview overlay (covers enemy card when active)
        if (wavePreviewBanner != null)
        {
            wavePreviewBanner.SetActive(gm.WavePreviewActive);
            if (gm.WavePreviewActive && wavePreviewText != null)
            {
                string bossWarn = gm.IsBossWave ? "\n⚠  BOSS INCOMING  ⚠" : "";
                wavePreviewText.text = $"Wave {gm.Wave} incoming...{bossWarn}";
            }
        }

        // Enemy sprite
        if (enemyImage != null && enemySprites != null && enemySprites.Length > 0)
        {
            int idx = Mathf.Min(gm.EnemySpriteIndex, enemySprites.Length - 1);
            var spr = idx >= 0 ? enemySprites[idx] : null;
            enemyImage.sprite = spr;
            enemyImage.color  = spr != null ? Color.white : Color.clear;
        }

        waveText.text = $"Wave {gm.Wave}";
        if (waveSubText != null)
        {
            string streakTag = gm.WaveStreak > 0 ? $"  🔥×{gm.StreakMultiplier:F1}" : "";
            string echoTag   = gm.BloodEchoCount > 0 ? $"  ⚡×{gm.BloodEchoCount}" : "";
            if (gm.IsBossWave)
                waveSubText.text = $"★ BOSS WAVE ★{streakTag}{echoTag}";
            else if (gm.IsBountyWave)
                waveSubText.text = $"★ BOUNTY WAVE ★{streakTag}{echoTag}";
            else if (gm.IsEliteWave)
                waveSubText.text = $"⚔ ELITE WAVE ⚔{streakTag}{echoTag}";
            else
                waveSubText.text = $"Boss in {gm.WavesUntilBoss} wave{(gm.WavesUntilBoss == 1 ? "" : "s")}{streakTag}{echoTag}";
        }

        if (enemyModifierText != null)
        {
            if (gm.IsBossWave && gm.CurrentBossAbility != BossAbility.None)
            {
                string shield = gm.BossShieldActive ? " (shield)" : "";
                enemyModifierText.text  = gm.BossAbilityDisplay + shield;
                enemyModifierText.color = new Color(0.55f, 0.8f, 1f);
                enemyModifierText.gameObject.SetActive(true);
            }
            else if (gm.IsBountyWave)
            {
                enemyModifierText.text  = "2x HP  |  3x Blood";
                enemyModifierText.color = new Color(1f, 0.84f, 0f);
                enemyModifierText.gameObject.SetActive(true);
            }
            else if (gm.IsEliteWave)
            {
                string mod = gm.CurrentEnemyModifier != EnemyModifier.None
                    ? $"  |  {gm.EnemyModifierDisplay}" : "";
                enemyModifierText.text  = $"⚔ 1.75x HP  |  2x Blood{mod}";
                enemyModifierText.color = new Color(1f, 0.55f, 0.1f);
                enemyModifierText.gameObject.SetActive(true);
            }
            else if (gm.IsBloodyWave)
            {
                string mod = gm.CurrentEnemyModifier != EnemyModifier.None
                    ? $"  |  {gm.EnemyModifierDisplay}" : "";
                enemyModifierText.text  = $"☽ Blood Moon  |  +20% Atk{mod}";
                enemyModifierText.color = new Color(0.9f, 0.2f, 0.2f);
                enemyModifierText.gameObject.SetActive(true);
            }
            else if (gm.CurrentEnemyModifier != EnemyModifier.None)
            {
                enemyModifierText.text  = gm.EnemyModifierDisplay;
                enemyModifierText.color = gm.CurrentEnemyModifier == EnemyModifier.Spectral
                    ? new Color(0.2f, 0.9f, 0.9f)
                    : new Color(1f, 0.65f, 0.1f);
                enemyModifierText.gameObject.SetActive(true);
            }
            else
            {
                enemyModifierText.gameObject.SetActive(false);
            }
        }

        if (gm.IsBossWave)
        {
            enemyNameText.text  = $"☠ {gm.EnemyName} ☠";
            enemyNameText.color = new Color(1f, 0.84f, 0f);
        }
        else if (gm.IsBountyWave)
        {
            enemyNameText.text  = $"★ {gm.EnemyName} ★";
            enemyNameText.color = new Color(1f, 0.84f, 0f);
        }
        else if (gm.IsEliteWave)
        {
            enemyNameText.text  = gm.EnemyName;
            enemyNameText.color = new Color(1f, 0.55f, 0.1f);
        }
        else
        {
            enemyNameText.text  = gm.EnemyName;
            enemyNameText.color = Color.white;
        }

        bool showTimer = gm.IsBossWave && gm.SoldierCount > 0 && gm.BossTimeRemaining > 0f;
        if (bossTimerRow != null) bossTimerRow.SetActive(showTimer);
        if (showTimer && bossTimerText != null)
        {
            int secs = Mathf.CeilToInt(gm.BossTimeRemaining);
            bossTimerText.text  = $"⏱ {secs}s  — defeat the boss or face the penalty!";
            bossTimerText.color = secs <= 10 ? new Color(1f, 0.2f, 0.2f) : new Color(1f, 0.6f, 0.1f);
        }
        enemyHPFill.fillAmount = gm.EnemyMaxHP > 0 ? gm.EnemyHP / gm.EnemyMaxHP : 0f;
        enemyHPText.text = $"{GameManager.FormatHP(gm.EnemyHP)} / {GameManager.FormatHP(gm.EnemyMaxHP)}  |  +{GameManager.FormatNumber(gm.WaveBloodPreview)} blood";

        // Army
        bool hasSoldiers = gm.SoldierCount > 0;
        bool atCap       = gm.SoldierCount >= gm.MaxSoldiers;

        string compBonus = gm.IsAllTank      ? "  ♦ Regen"
                         : gm.IsAllBerserker ? "  ⚡ Crit"
                         : gm.IsAllPaladin   ? "  ✚ Heal"
                         : gm.IsMixedArmy    ? "  🛡 −15% dmg"
                         : "";
        string furyTag = gm.IdleFuryStacks > 0 ? $"  | Idle Fury x{gm.IdleFuryStacks}" : "";
        soldierCountText.text = hasSoldiers
            ? $"Soldiers: {gm.SoldierCount}/{gm.MaxSoldiers}  [T:{gm.TankCount} B:{gm.BerserkerCount} P:{gm.PaladinCount}]{compBonus}{furyTag}"
            : $"No soldiers — buy one!  (max {gm.MaxSoldiers})";

        soldierHPRow.SetActive(hasSoldiers);
        if (hasSoldiers)
        {
            soldierHPFill.fillAmount = gm.FrontlineMaxHP > 0 ? gm.SoldierHP / gm.FrontlineMaxHP : 0f;
            string cls = gm.FrontlineIsTank ? "Tank" : gm.FrontlineIsBerserker ? "Berserker" : "Paladin";
            string desperTag   = gm.DesperationActive ? "  💥" : "";
            string lastStandTag = gm.LastStandActive  ? "  ⚔ LAST STAND" : "";
            soldierHPText.text = $"{cls}: {GameManager.FormatHP(gm.SoldierHP)} / {GameManager.FormatHP(gm.FrontlineMaxHP)} HP  |  {gm.EffectiveAttack:F1} DPS{desperTag}{lastStandTag}";
        }

        if (formationButtonText != null)
            formationButtonText.text = gm.BerserkerFront ? "Formation: Berserker Front" : "Formation: Tank Front";

        if (mixedBonusText != null)
        {
            mixedBonusText.gameObject.SetActive(gm.IsMixedArmy);
            if (gm.IsMixedArmy)
                mixedBonusText.text = $"Mixed Formation: −{GameManager.MixedArmyDmgReduction * 100:F0}% incoming damage";
        }

        bool canBuySoldier = gm.Blood >= GameManager.SoldierCost && !atCap;
        if (buyTankButton      != null) buyTankButton.interactable      = canBuySoldier;
        if (buyBerserkerButton != null) buyBerserkerButton.interactable = canBuySoldier;
        if (buyPaladinButton   != null) buyPaladinButton.interactable   = canBuySoldier;

        healSelfPanel.SetActive(gm.HealSelfUnlocked);
        if (gm.HealSelfUnlocked)
            healSelfButton.interactable = gm.Blood >= GameManager.HealSelfCost
                && hasSoldiers
                && gm.SoldierHP < gm.FrontlineMaxHP;

        // Blood Surge
        if (bloodStormInfoText != null && bloodSurgePanel != null && bloodSurgePanel.activeSelf)
        {
            if (gm.BloodStormUnlocked)
            {
                string stormInfo = gm.BloodStormReady
                    ? $"Blood Storm  —  {GameManager.FormatNumber(GameManager.BloodStormBaseDmg + (gm.Wave - 1) * GameManager.BloodStormDmgPerWave)} dmg  ({GameManager.FormatNumber(GameManager.BloodStormCost)} blood)"
                    : $"Blood Storm  —  Cooldown: {Mathf.CeilToInt(gm.BloodStormCooldownLeft)}s";
                bloodStormInfoText.text = stormInfo;
                if (bloodStormButton != null)
                    bloodStormButton.interactable = gm.BloodStormReady && gm.Blood >= GameManager.BloodStormCost && hasSoldiers && gm.EnemyHP > 0;
            }
            else if (bloodStormInfoText != null)
                bloodStormInfoText.text = $"Blood Storm  —  Unlocks at wave {GameManager.BloodStormUnlockWave}";
        }

        if (bloodSurgePanel != null) bloodSurgePanel.SetActive(gm.SurgeUnlocked);
        if (gm.SurgeUnlocked && bloodSurgeInfoText != null)
        {
            string flawlessTag = gm.FlawlessActive ? "  ⚡ FLAWLESS!" : "";
            bloodSurgeInfoText.text = gm.SurgeActive
                ? $"Blood Surge  —  2× attack  {Mathf.CeilToInt(gm.SurgeTimeRemaining)}s remaining{flawlessTag}"
                : $"Blood Surge  —  2× attack for {gm.SurgeDurationEffective:F0}s{flawlessTag}";
            if (bloodSurgeButton != null)
                bloodSurgeButton.interactable = !gm.SurgeActive
                    && gm.Blood >= GameManager.SurgeCost
                    && hasSoldiers;
            if (upgradeSurgeButton != null)
                upgradeSurgeButton.interactable = gm.SurgeUpgradeLevel < GameManager.MaxSpellUpgradeLevel
                    && gm.Blood >= gm.SurgeUpgradeCost;
            if (surgeCostText != null)
                surgeCostText.text = gm.SurgeUpgradeLevel < GameManager.MaxSpellUpgradeLevel
                    ? $"Upgrade Surge\n(Lv.{gm.SurgeUpgradeLevel}/{GameManager.MaxSpellUpgradeLevel}  {GameManager.FormatNumber(gm.SurgeUpgradeCost)} blood)"
                    : "Surge MAX";
        }

        if (gm.HealSelfUnlocked)
        {
            if (upgradeHealSelfButton != null)
                upgradeHealSelfButton.interactable = gm.HealUpgradeLevel < GameManager.MaxSpellUpgradeLevel
                    && gm.Blood >= gm.HealUpgradeCost;
            if (healCostText != null)
                healCostText.text = gm.HealUpgradeLevel < GameManager.MaxSpellUpgradeLevel
                    ? $"Upgrade Heal\n(Lv.{gm.HealUpgradeLevel}/{GameManager.MaxSpellUpgradeLevel}  {GameManager.FormatNumber(gm.HealUpgradeCost)} blood)"
                    : "Heal MAX";
        }

        // Workers + Blood Pact
        if (workersPanel != null) workersPanel.SetActive(gm.WorkersUnlocked);
        workerInfoText.text          = $"Workers: {gm.WorkerCount}";
        buyWorkerButton.interactable = gm.Blood >= GameManager.WorkerCost;
        if (bloodPactButton != null)
        {
            bloodPactButton.interactable = gm.Blood >= GameManager.BloodPactBloodCost;
            if (bloodPactText != null)
                bloodPactText.text = $"Blood Pact\n({GameManager.FormatNumber(GameManager.BloodPactBloodCost)} blood → {GameManager.FormatNumber(GameManager.BloodPactWoodGain)} wood)";
        }
        if (shrineInfoText != null)
        {
            double shrineIncome = gm.ShrineCount * GameManager.ShrineBloodPerSec;
            shrineInfoText.text = $"Blood Shrine  {gm.ShrineCount}/{GameManager.ShrineMaxCount}  (+{shrineIncome:F1}/s blood)";
        }
        if (buyShrineButton != null)
        {
            buyShrineButton.interactable = gm.ShrineUnlocked && gm.Wood >= GameManager.ShrineWoodCost && gm.ShrineCount < GameManager.ShrineMaxCount;
        }
        if (clickPowerInfoText != null)
        {
            clickPowerInfoText.text = $"Click Power  Lv.{gm.ClickPowerLevel}/{GameManager.ClickPowerMaxLevel}  (+{gm.ClickPowerLevel}/tap)";
        }
        if (clickPowerButton != null)
        {
            clickPowerButton.interactable = gm.ClickPowerUnlocked && gm.Wood >= gm.ClickPowerCost && gm.ClickPowerLevel < GameManager.ClickPowerMaxLevel;
        }

        // Equipment
        if (equipmentPanel != null) equipmentPanel.SetActive(gm.WorkersUnlocked);
        if (gm.WorkersUnlocked)
        {
            if (weaponInfoText != null)
                weaponInfoText.text = $"Weapon  Lv.{gm.WeaponLevel}/{GameManager.MaxEquipLevel}  (+{gm.EquipAttackBonus:F0} atk)";
            if (upgradeWeaponButton != null)
                upgradeWeaponButton.interactable = gm.WeaponLevel < GameManager.MaxEquipLevel && gm.Wood >= gm.WeaponUpgradeCost;
            if (weaponCostText != null)
                weaponCostText.text = gm.WeaponLevel < GameManager.MaxEquipLevel
                    ? $"Upgrade\n({GameManager.FormatNumber(gm.WeaponUpgradeCost)} wood)" : "MAX";

            if (armorInfoText != null)
                armorInfoText.text = $"Armor  Lv.{gm.ArmorLevel}/{GameManager.MaxEquipLevel}  (+{gm.EquipArmorBonus:F0} HP)";
            if (upgradeArmorButton != null)
                upgradeArmorButton.interactable = gm.ArmorLevel < GameManager.MaxEquipLevel && gm.Wood >= gm.ArmorUpgradeCost;
            if (armorCostText != null)
                armorCostText.text = gm.ArmorLevel < GameManager.MaxEquipLevel
                    ? $"Upgrade\n({GameManager.FormatNumber(gm.ArmorUpgradeCost)} wood)" : "MAX";

            if (talismanInfoText != null)
                talismanInfoText.text = $"Talisman  Lv.{gm.TalismanLevel}/{GameManager.MaxEquipLevel}  (+{gm.EquipTalismanBonus * 100:F0}% reward)";
            if (upgradeTalismanButton != null)
                upgradeTalismanButton.interactable = gm.TalismanLevel < GameManager.MaxEquipLevel && gm.Wood >= gm.TalismanUpgradeCost;
            if (talismanCostText != null)
                talismanCostText.text = gm.TalismanLevel < GameManager.MaxEquipLevel
                    ? $"Upgrade\n({GameManager.FormatNumber(gm.TalismanUpgradeCost)} wood)" : "MAX";
        }

        // Fortifications (always visible)
        if (fortInfoText != null)
        {
            int pct = Mathf.RoundToInt(gm.FortificationDmgReduction * 100);
            fortInfoText.text = $"Fortifications  Lv.{gm.FortificationLevel}/{GameManager.MaxFortificationLevel}  (−{pct}% enemy HP)";
        }
        if (upgradeFortButton != null)
            upgradeFortButton.interactable = gm.FortificationLevel < GameManager.MaxFortificationLevel
                                          && gm.Wood >= gm.FortificationCost;
        if (fortCostText != null)
            fortCostText.text = gm.FortificationLevel >= GameManager.MaxFortificationLevel
                ? "MAX"
                : $"Fortify\n({GameManager.FormatNumber(gm.FortificationCost)} wood)";

        // Blood Ritual
        if (bloodRitualPanel != null) bloodRitualPanel.SetActive(gm.WorkersUnlocked);
        if (gm.WorkersUnlocked && bloodRitualInfoText != null)
        {
            bloodRitualInfoText.text = gm.BloodRitualCount > 0
                ? $"Blood Ritual: {gm.BloodRitualCount}  +{gm.BloodPerSec:F1} blood/s"
                : "Blood Ritual  —  passive blood income";
            if (buyBloodRitualButton != null)
                buyBloodRitualButton.interactable = gm.Wood >= gm.BloodRitualCost;
            if (bloodRitualCostText != null)
                bloodRitualCostText.text = $"Perform\n({GameManager.FormatNumber(gm.BloodRitualCost)} wood)";
        }

        // Prestige
        bool canPrestige = gm.Wave >= GameManager.PrestigeWaveRequirement;
        if (prestigePanel != null) prestigePanel.SetActive(canPrestige);
        if (canPrestige && prestigeInfoText != null)
        {
            string milestoneTag = gm.PrestigeMilestonesReached > 0
                ? $"  ⭐+{gm.PrestigeMilestoneDmgBonus * 100:F0}% atk"
                : "";
            prestigeInfoText.text = gm.PrestigeCount > 0
                ? $"Prestige Lv.{gm.PrestigeCount}  ×{gm.PrestigeMultiplier:F2} blood{milestoneTag}"
                : $"Prestige  —  reset for ×{gm.PrestigeMultiplier + 0.5:F2} blood bonus";
            if (prestigeButton != null) prestigeButton.interactable = true;
        }
        if (prestigeMilestoneText != null)
        {
            bool showMilestone = gm.PrestigeMilestonesReached > 0 && canPrestige;
            prestigeMilestoneText.gameObject.SetActive(showMilestone);
            if (showMilestone)
                prestigeMilestoneText.text = $"Milestone {gm.PrestigeMilestonesReached}/4  —  next at prestige {NextPrestigeMilestone(gm.PrestigeCount)}";
        }

        // Prestige Shop
        bool showShop = gm.PrestigeCount >= 1;
        if (prestigeShopPanel != null) prestigeShopPanel.SetActive(showShop);
        if (showShop)
        {
            bool canSpend = gm.PrestigePoints >= GameManager.PrestigeShopCost;
            if (prestigeShopPointsText != null)
                prestigeShopPointsText.text = $"Prestige Points: {gm.PrestigePoints}";

            if (pSoldierCapInfoText != null)
                pSoldierCapInfoText.text = $"Soldier Cap +10  (Lv.{gm.PSoldierCapLevel})";
            if (pSoldierCapButton != null)   pSoldierCapButton.interactable   = canSpend;

            if (pClickBonusInfoText != null)
                pClickBonusInfoText.text = $"Click Bonus +0.5  (Lv.{gm.PClickBonusLevel})";
            if (pClickBonusButton != null)   pClickBonusButton.interactable   = canSpend;

            if (pRitualEffInfoText != null)
                pRitualEffInfoText.text = $"Ritual Eff. +0.5/s  (Lv.{gm.PRitualEffLevel})";
            if (pRitualEffButton != null)    pRitualEffButton.interactable    = canSpend;

            if (pWeaponHeadStartInfoText != null)
                pWeaponHeadStartInfoText.text = $"Weapon Head Start  (Lv.{gm.PWeaponHeadStartLevel})";
            if (pWeaponHeadStartButton != null) pWeaponHeadStartButton.interactable = canSpend;

            if (pBloodTitheInfoText != null)
                pBloodTitheInfoText.text = $"Blood Tithe +0.5/s  (Lv.{gm.PBloodTitheLevel})";
            if (pBloodTitheButton != null)   pBloodTitheButton.interactable   = canSpend;

            if (pIronWallInfoText != null)
                pIronWallInfoText.text = $"Iron Wall −{GameManager.IronWallDmgReduction * 100:F0}% dmg  (Lv.{gm.PIronWallLevel})";
            if (pIronWallButton != null)     pIronWallButton.interactable     = canSpend;

            if (pBountyBonusInfoText != null)
                pBountyBonusInfoText.text = $"Bounty Mastery +1x  (Lv.{gm.PBountyBonusLevel})";
            if (pBountyBonusButton != null)  pBountyBonusButton.interactable  = canSpend;
        }

        // Soul Shard Shop
        if (soulShardShopPanel != null) soulShardShopPanel.SetActive(gm.SoulShardShopUnlocked);
        if (gm.SoulShardShopUnlocked)
        {
            bool canBuySS = gm.SoulShards >= GameManager.SSUpgradeCost;
            if (soulShardShopPointsText != null)
                soulShardShopPointsText.text = $"Soul Shards: {GameManager.FormatNumber(gm.SoulShards)}";

            if (ssBossTimerInfoText != null)
                ssBossTimerInfoText.text = $"Boss Timer +15s  (Lv.{gm.SSBossTimerLevel}/{GameManager.SSMaxLevel})";
            if (ssBossTimerButton != null)
                ssBossTimerButton.interactable = canBuySS && gm.SSBossTimerLevel < GameManager.SSMaxLevel;

            if (ssDoubleChestInfoText != null)
                ssDoubleChestInfoText.text = $"Double Chest  (Lv.{gm.SSDoubleChestLevel}/{GameManager.SSMaxLevel})";
            if (ssDoubleChestButton != null)
                ssDoubleChestButton.interactable = canBuySS && gm.SSDoubleChestLevel < GameManager.SSMaxLevel;

            if (ssRollbackInfoText != null)
                ssRollbackInfoText.text = $"Rollback Shield  (Lv.{gm.SSRollbackLevel}/{GameManager.SSMaxLevel})";
            if (ssRollbackButton != null)
                ssRollbackButton.interactable = canBuySS && gm.SSRollbackLevel < GameManager.SSMaxLevel;

            if (ssBloodTapInfoText != null)
                ssBloodTapInfoText.text = $"Blood Tap +1/s  (Lv.{gm.SSBloodTapLevel}/{GameManager.SSMaxLevel})";
            if (ssBloodTapButton != null)
                ssBloodTapButton.interactable = canBuySS && gm.SSBloodTapLevel < GameManager.SSMaxLevel;

            if (ssShardHungerInfoText != null)
                ssShardHungerInfoText.text = $"Shard Hunger +20% boss blood  (Lv.{gm.SSShardHungerLevel}/{GameManager.SSMaxLevel})";
            if (ssShardHungerButton != null)
                ssShardHungerButton.interactable = canBuySS && gm.SSShardHungerLevel < GameManager.SSMaxLevel;
        }

        // Blood Bank
        if (bloodBankPanel != null)
        {
            if (bloodBankInfoText != null)
                bloodBankInfoText.text = $"Blood Bank  {GameManager.FormatNumber(gm.BloodBankDeposit)}/{GameManager.FormatNumber(GameManager.BankMaxDeposit)}  (+{GameManager.BankInterestRatePerHour * 100:F0}%/hr)";
            if (bloodBankAccruedText != null)
                bloodBankAccruedText.text = gm.BloodBankAccrued > 0
                    ? $"Interest accrued: +{GameManager.FormatNumber(gm.BloodBankAccrued)} blood"
                    : "Interest accrued: none yet";
            if (depositBloodButton != null)
                depositBloodButton.interactable = gm.Blood >= 1.0
                    && gm.BloodBankDeposit < GameManager.BankMaxDeposit;
            if (withdrawBloodButton != null)
                withdrawBloodButton.interactable = gm.BloodBankDeposit > 0 || gm.BloodBankAccrued > 0;
        }

        barracksInfoText.text        = $"Barracks  Lv.{gm.BarracksLevel}  —  Max {gm.MaxSoldiers} soldiers";
        barracksUpgradeCostText.text = $"Upgrade\n({GameManager.FormatNumber(gm.BarracksUpgradeCost)} wood)";
        upgradeBarracksButton.interactable = gm.Wood >= gm.BarracksUpgradeCost;

        // Corruption
        if (corruptionText != null)
        {
            corruptionText.gameObject.SetActive(gm.CorruptionLevel > 0);
            if (gm.CorruptionLevel > 0)
            {
                int penaltyHP = Mathf.RoundToInt(gm.CorruptionLevel * GameManager.CorruptionHPPenalty);
                corruptionText.text = $"☠ Corruption Lv.{gm.CorruptionLevel}  —  −{penaltyHP} max HP";
            }
        }
        if (purifyButton != null)
        {
            purifyButton.gameObject.SetActive(gm.CorruptionLevel > 0 && gm.SoulShardShopUnlocked);
            purifyButton.interactable = gm.SoulShards >= GameManager.PurifyCost;
            if (purifyButtonText != null)
                purifyButtonText.text = $"Purify\n({GameManager.PurifyCost:F0} shards)";
        }

        // Daily Challenge
        if (dailyChallengeRow != null)
        {
            bool showChallenge = gm.SoldierCount > 0 && !gm.WavePreviewActive;
            dailyChallengeRow.SetActive(showChallenge);
            if (showChallenge)
            {
                if (gm.DailyChallengeActive)
                {
                    int secs = Mathf.CeilToInt(gm.ChallengeTimeRemaining);
                    if (dailyChallengeInfoText != null)
                        dailyChallengeInfoText.text = $"⚔ CHALLENGE  ×{GameManager.ChallengeBloodMult:F0} reward  —  {secs}s left";
                    if (dailyChallengeButton != null) dailyChallengeButton.interactable = false;
                }
                else if (gm.DailyChallengeAvailable)
                {
                    if (dailyChallengeInfoText != null)
                        dailyChallengeInfoText.text = $"Daily Challenge: {GameManager.ChallengeHPMult:F0}× HP enemy  —  ×{GameManager.ChallengeBloodMult:F0} blood!";
                    if (dailyChallengeButton != null) dailyChallengeButton.interactable = true;
                }
                else
                {
                    if (dailyChallengeInfoText != null)
                        dailyChallengeInfoText.text = "Daily Challenge: completed — come back tomorrow";
                    if (dailyChallengeButton != null) dailyChallengeButton.interactable = false;
                }
            }
        }

        // Soul Sacrifice
        if (soulSacrificeButton != null)
        {
            soulSacrificeButton.gameObject.SetActive(gm.SoulSacrificeUnlocked);
            if (gm.SoulSacrificeUnlocked)
            {
                soulSacrificeButton.interactable = gm.SoldierCount > 0;
                if (soulSacrificeInfoText != null)
                    soulSacrificeInfoText.text = $"Soul Sacrifice  —  lose 1 soldier → ×{GameManager.SoulSacrificeBloodMult:F0} blood";
            }
        }

        // Prestige Talent Selection overlay
        if (talentSelectionPanel != null)
        {
            talentSelectionPanel.SetActive(gm.PendingPrestige);
            if (gm.PendingPrestige)
            {
                if (talentHeaderText != null)
                    talentHeaderText.text = $"Choose a Prestige Talent\n(Prestige {gm.PrestigeCount + 1})";
                var opts  = gm.PendingTalentChoices;
                var btns  = new[] { talentButton0,     talentButton1,     talentButton2     };
                var texts = new[] { talentButtonText0, talentButtonText1, talentButtonText2 };
                for (int i = 0; i < 3; i++)
                {
                    bool hasOpt = i < opts.Length;
                    if (btns[i]  != null) btns[i].gameObject.SetActive(hasOpt);
                    if (texts[i] != null && hasOpt)
                        texts[i].text = TalentDescription(opts[i]);
                }
            }
        }

        // Talent summary in prestige panel
        if (canPrestige && prestigeInfoText != null && gm.Talents != TalentFlags.None)
        {
            string talentLine = TalentSummaryLine(gm.Talents);
            if (!prestigeInfoText.text.Contains(talentLine))
                prestigeInfoText.text += $"\n{talentLine}";
        }
    }

    static int NextPrestigeMilestone(int current)
    {
        int[] ms = { 5, 10, 20, 50 };
        foreach (int m in ms) if (current < m) return m;
        return -1;
    }

    // ── Stats Panel ───────────────────────────────────────────────────────────

    public void ShowStatsPanel()
    {
        if (statsPanel == null) return;
        RefreshStats();
        statsPanel.SetActive(true);
    }

    public void HideStatsPanel()
    {
        if (statsPanel != null) statsPanel.SetActive(false);
    }

    // ── Settings Panel ────────────────────────────────────────────────────────

    public void ShowSettingsPanel()
    {
        if (settingsPanel == null) return;
        RefreshSettings();
        settingsPanel.SetActive(true);
    }

    public void HideSettingsPanel()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // ── Talent Selection Panel ────────────────────────────────────────────────

    public void HideTalentPanel()
    {
        GameManager.Instance?.CancelPrestige();
    }

    public static string TalentDescription(TalentFlags t) => t switch
    {
        TalentFlags.BloodFrenzy  => "Blood Frenzy\n+25% kill blood rewards",
        TalentFlags.Undying      => "Undying\nFrontline revives once per wave at 1 HP",
        TalentFlags.ShardHunter  => "Shard Hunter\nBosses drop 2 soul shards instead of 1",
        TalentFlags.IronSkin     => "Iron Skin\n+15 max HP to frontline soldier",
        TalentFlags.BloodRush    => "Blood Rush\nBoss/challenge kill activates Blood Surge",
        TalentFlags.Glutton      => "Glutton\nBlood Rituals produce 25% more blood/s",
        TalentFlags.EchoMastery  => "Echo Mastery\nBlood Echo lasts 8 waves instead of 5",
        TalentFlags.Bloodlust    => "Bloodlust\nHeal frontline for 5% of each enemy's max HP on kill",
        _                        => "",
    };

    public static string TalentSummaryLine(TalentFlags talents)
    {
        var names = new System.Collections.Generic.List<string>();
        if ((talents & TalentFlags.BloodFrenzy)  != 0) names.Add("Frenzy");
        if ((talents & TalentFlags.Undying)       != 0) names.Add("Undying");
        if ((talents & TalentFlags.ShardHunter)   != 0) names.Add("Shard");
        if ((talents & TalentFlags.IronSkin)       != 0) names.Add("IronSkin");
        if ((talents & TalentFlags.BloodRush)      != 0) names.Add("BloodRush");
        if ((talents & TalentFlags.Glutton)        != 0) names.Add("Glutton");
        if ((talents & TalentFlags.EchoMastery)    != 0) names.Add("EchoMastery");
        if ((talents & TalentFlags.Bloodlust)      != 0) names.Add("Bloodlust");
        return names.Count > 0 ? "Talents: " + string.Join(" | ", names) : "";
    }

    void RefreshSettings()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (soundToggleText != null)
            soundToggleText.text = gm.SoundEnabled ? "Sound: ON" : "Sound: OFF";
        if (notifToggleText != null)
            notifToggleText.text = gm.NotificationsEnabled ? "Notifications: ON" : "Notifications: OFF";
    }

    void RefreshStats()
    {
        var gm = GameManager.Instance;
        if (gm == null || statsText == null) return;

        int h = (int)(gm.TimePlayed / 3600);
        int m = (int)((gm.TimePlayed % 3600) / 60);
        int s = (int)(gm.TimePlayed % 60);

        var sb = new StringBuilder();
        sb.AppendLine($"Enemies Defeated:  {gm.TotalEnemiesKilled}");
        sb.AppendLine($"Bosses Killed:     {gm.TotalBossesKilled}");
        sb.AppendLine($"Soldiers Lost:     {gm.TotalSoldiersLost}");
        sb.AppendLine($"Blood Earned:      {GameManager.FormatNumber(gm.TotalBloodEarned)}");
        sb.AppendLine($"Blood Bank:        {GameManager.FormatNumber(gm.BloodBankDeposit)} (+{GameManager.FormatNumber(gm.BloodBankAccrued)})");
        sb.AppendLine($"Best Wave:         {gm.BestWave}");
        sb.AppendLine($"Best Streak:       {gm.BestStreak}  (current: {gm.WaveStreak})");
        sb.AppendLine($"Veteran Bonus:     +{gm.VeteranAttackBonus}/{GameManager.VeteranAttackCap} atk (from boss kills)");
        sb.AppendLine($"Soul Shards:       {GameManager.FormatNumber(gm.SoulShards)}");
        sb.AppendLine($"Time Played:       {h}h {m}m {s}s");
        sb.AppendLine($"Prestige Level:    {gm.PrestigeCount}  (milestones: {gm.PrestigeMilestonesReached}/4)");
        sb.AppendLine();
        sb.AppendLine("── Achievements ──────────────────");
        foreach (var (flag, title, _, _) in k_AchievDefs)
            sb.AppendLine($"  {((gm.Achievements & flag) != 0 ? "✓" : "○")}  {title}");

        statsText.text = sb.ToString();
    }

    // ── Toasts ────────────────────────────────────────────────────────────────

    void ShowAchievementToast(AchievementFlags flag)
    {
        string title = flag.ToString(), reward = "";
        foreach (var (f, t, blood, pp) in k_AchievDefs)
        {
            if (f != flag) continue;
            title = t;
            if (blood > 0) reward = $" (+{GameManager.FormatNumber(blood)} blood)";
            else if (pp > 0) reward = " (+1 PP)";
            break;
        }
        StartCoroutine(ToastRoutine($"Achievement: {title}{reward}"));
    }

    void ShowMilestoneToast(string message) => StartCoroutine(ToastRoutine(message));

    IEnumerator ToastRoutine(string message)
    {
        if (achievementToast == null) yield break;
        achievementToastText.text = message;
        achievementToast.SetActive(true);
        yield return new WaitForSeconds(3f);
        achievementToast.SetActive(false);
    }

    // ── Offline Earnings ──────────────────────────────────────────────────────

    void ShowOfflinePanel()
    {
        var gm = GameManager.Instance;
        if (offlinePanel == null || gm == null) return;
        bool hasWood  = gm.OfflineWoodEarned  > 0;
        bool hasBlood = gm.OfflineBloodEarned > 0;
        if (!hasWood && !hasBlood) return;
        offlinePanel.SetActive(true);
        var sb = new StringBuilder("While you were away:\n");
        if (hasBlood) sb.AppendLine($"+{GameManager.FormatNumber(gm.OfflineBloodEarned)} blood");
        if (hasWood)  sb.Append($"+{GameManager.FormatNumber(gm.OfflineWoodEarned)} wood");
        offlineText.text = sb.ToString();
    }

    public void DismissOfflinePanel()
    {
        if (offlinePanel != null) offlinePanel.SetActive(false);
        GameManager.Instance?.ClearOfflineEarnings();
    }

    // ── Damage Numbers ────────────────────────────────────────────────────────

    void SpawnDamageNumber(float amount, bool isEnemy)
    {
        if (damageLayer == null || amount < 0.5f) return;
        var sourceRT = isEnemy ? enemyHPFill.rectTransform : soldierHPFill.rectTransform;
        Vector3[] corners = new Vector3[4];
        sourceRT.GetWorldCorners(corners);
        Vector2 screenPos = (corners[0] + corners[2]) * 0.5f;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                damageLayer, screenPos, null, out Vector2 localPos)) return;
        localPos.x += UnityEngine.Random.Range(-80f, 80f);
        Color color = isEnemy ? new Color(1f, 0.25f, 0.25f) : new Color(1f, 0.55f, 0.1f);
        DamageNumber.Spawn(damageLayer, Mathf.CeilToInt(amount).ToString(), color, localPos);
    }

    // ── Feature Request ───────────────────────────────────────────────────────

    const string k_GhApi = "https://api.github.com/repos/willettmeister/bloodidle/issues";

    static string GetToken()
    {
        var asset = Resources.Load<TextAsset>("bloodidle_secrets");
        return asset != null ? asset.text.Trim() : string.Empty;
    }

    const string k_RateLimitKey    = "LastFeatureSubmit";
    const int    k_RateLimitMinutes = 60;
    const int    k_MaxTitleLen      = 120;
    const int    k_MaxBodyLen       = 1000;

    public void ShowFeaturePanel()
    {
        if (featureRequestPanel == null) return;
        featureRequestPanel.SetActive(true);
        featureSubmitButton.interactable = true;
        featureStatusText.text           = RateLimitStatus();
        if (featureStatusText.text.Length > 0)
            featureSubmitButton.interactable = false;
    }

    public void HideFeaturePanel()
    {
        if (featureRequestPanel == null) return;
        featureRequestPanel.SetActive(false);
        featureTitleField.text           = "";
        featureDescField.text            = "";
        featureStatusText.text           = "";
        featureSubmitButton.interactable = true;
    }

    const int k_MinTitleLen = 10;

    // Strip control characters (keep printable ASCII + common Unicode). Collapses runs of whitespace.
    static string SanitizeInput(string raw)
    {
        if (raw == null) return "";
        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (char c in raw)
            if (c >= 32 && c != 127) sb.Append(c);  // drop DEL and all C0 controls
        // collapse multiple spaces/tabs into one
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString().Trim(), @"\s{2,}", " ");
    }

    static bool HasMinLetters(string s, int min = 3)
    {
        int count = 0;
        foreach (char c in s) if (char.IsLetter(c) && ++count >= min) return true;
        return false;
    }

    public void SubmitFeature()
    {
        string title = SanitizeInput(featureTitleField.text);
        if (title.Length == 0)
            { featureStatusText.text = "Please enter a title."; return; }
        if (title.Length < k_MinTitleLen)
            { featureStatusText.text = $"Title too short — at least {k_MinTitleLen} characters."; return; }
        if (title.Length > k_MaxTitleLen)
            { featureStatusText.text = $"Title too long ({title.Length}/{k_MaxTitleLen} chars)."; return; }
        if (!HasMinLetters(title))
            { featureStatusText.text = "Title must contain at least 3 letters."; return; }
        string rateLimitMsg = RateLimitStatus();
        if (rateLimitMsg.Length > 0) { featureStatusText.text = rateLimitMsg; return; }
        StartCoroutine(PostIssue(title, SanitizeInput(featureDescField.text)));
    }

    // Returns a non-empty string when the player must wait before submitting again.
    string RateLimitStatus()
    {
        if (!PlayerPrefs.HasKey(k_RateLimitKey)) return "";
        var styles = DateTimeStyles.RoundtripKind;
        if (!DateTime.TryParse(PlayerPrefs.GetString(k_RateLimitKey), null, styles, out DateTime last))
            return "";
        double minutesSince = (DateTime.UtcNow - last).TotalMinutes;
        if (minutesSince >= k_RateLimitMinutes) return "";
        int remaining = Mathf.CeilToInt((float)(k_RateLimitMinutes - minutesSince));
        return $"Please wait {remaining} min before submitting again.";
    }

    IEnumerator PostIssue(string title, string rawBody)
    {
        string token = GetToken();
        if (string.IsNullOrEmpty(token))
        {
            featureStatusText.text           = "Submissions not configured.";
            featureSubmitButton.interactable = true;
            yield break;
        }

        featureSubmitButton.interactable = false;
        featureStatusText.text           = "Submitting…";

        // Truncate body to avoid hitting GitHub's request size limits
        if (rawBody.Length > k_MaxBodyLen)
            rawBody = rawBody.Substring(0, k_MaxBodyLen) + "… (truncated)";

        // Build issue body with player context so submissions are immediately actionable
        string context = BuildPlayerContext();
        string body = "**Community Request**\n\n" +
                      (rawBody.Length > 0 ? rawBody : "_No description provided._") +
                      context;

        string json  = "{\"title\":"  + JStr(title) +
                       ",\"body\":"   + JStr(body) +
                       ",\"labels\":[\"feature request\"]}";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);

        var req = new UnityWebRequest(k_GhApi, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout         = 15; // seconds — prevents indefinite hang on slow/no connection
        req.SetRequestHeader("Authorization",        "Bearer " + token);
        req.SetRequestHeader("Accept",               "application/vnd.github+json");
        req.SetRequestHeader("Content-Type",         "application/json");
        req.SetRequestHeader("X-GitHub-Api-Version", "2022-11-28");
        req.SetRequestHeader("User-Agent",           "BloodIdle/1.0");

        yield return req.SendWebRequest();

        bool success = req.result == UnityWebRequest.Result.Success;
        try
        {
            if (success)
            {
                PlayerPrefs.SetString(k_RateLimitKey,
                    DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                PlayerPrefs.Save();
                featureStatusText.text = "Submitted" + ParseIssueNumber(req.downloadHandler.text) +
                                         "! Thank you.";
                yield return new WaitForSeconds(2.5f);
                HideFeaturePanel();
            }
            else
            {
                long code = req.responseCode;
                featureStatusText.text = code == 422
                    ? "Label 'feature request' missing — ask the dev to create it on GitHub."
                    : $"Failed ({(code > 0 ? code.ToString() : req.error)}) — check your connection.";
            }
        }
        finally
        {
            if (!success) featureSubmitButton.interactable = true;
            req.Dispose();
        }
    }

    // Appends wave / prestige / time-played so every issue has triage context.
    static string BuildPlayerContext()
    {
        var gm = GameManager.Instance;
        if (gm == null) return "";
        int h = (int)(gm.TimePlayed / 3600);
        int m = (int)((gm.TimePlayed % 3600) / 60);
        return "\n\n---\n**Player context**\n" +
               $"- Wave: {gm.Wave}\n" +
               $"- Prestige: {gm.PrestigeCount}\n" +
               $"- Time played: {h}h {m}m\n" +
               $"- Platform: {Application.platform}";
    }

    // Extracts " #123" from the GitHub API JSON response, or "" on failure.
    static string ParseIssueNumber(string responseText)
    {
        if (string.IsNullOrEmpty(responseText)) return "";
        try
        {
            int idx = responseText.IndexOf("\"number\":", StringComparison.Ordinal);
            if (idx < 0) return "";
            int start = idx + 9;
            while (start < responseText.Length && responseText[start] == ' ') start++;
            int end = start;
            while (end < responseText.Length && char.IsDigit(responseText[end])) end++;
            if (end > start) return " #" + responseText.Substring(start, end - start);
        }
        catch { }
        return "";
    }

#if UNITY_INCLUDE_TESTS
    public static string JStrForTest(string s)               => JStr(s);
    public static string ParseIssueNumberForTest(string raw) => ParseIssueNumber(raw);
#endif

    // ── Feature Vote Panel ────────────────────────────────────────────────────

    struct IssueEntry { public int Number; public string Title; public int Votes; }

    IssueEntry[] _voteIssues;
    int          _voteIndex;
    bool         _voteFetching;

    const string k_VotedKeyPrefix = "VotedIssue_";

    public void ShowVotePanel()
    {
        if (featureVotePanel == null) return;
        HideFeaturePanel();
        featureVotePanel.SetActive(true);
        if (_voteIssues == null && !_voteFetching)
            StartCoroutine(FetchOpenFeatureRequests());
        else
            RefreshVoteDisplay();
    }

    public void HideVotePanel()
    {
        if (featureVotePanel != null) featureVotePanel.SetActive(false);
    }

    public void VotePrev()
    {
        if (_voteIssues == null || _voteIssues.Length == 0) return;
        _voteIndex = (_voteIndex - 1 + _voteIssues.Length) % _voteIssues.Length;
        RefreshVoteDisplay();
    }

    public void VoteNext()
    {
        if (_voteIssues == null || _voteIssues.Length == 0) return;
        _voteIndex = (_voteIndex + 1) % _voteIssues.Length;
        RefreshVoteDisplay();
    }

    public void VoteOnCurrent()
    {
        if (_voteIssues == null || _voteIssues.Length == 0) return;
        var issue = _voteIssues[_voteIndex];
        if (HasVoted(issue.Number))
        {
            if (voteStatusText != null) voteStatusText.text = "Already voted!";
            return;
        }
        StartCoroutine(PostVote(issue.Number));
    }

    public void RefreshVoteList()
    {
        if (_voteFetching) return;
        _voteIssues = null;
        StartCoroutine(FetchOpenFeatureRequests());
    }

    bool HasVoted(int n) => PlayerPrefs.GetInt(k_VotedKeyPrefix + n, 0) == 1;
    void MarkVoted(int n) { PlayerPrefs.SetInt(k_VotedKeyPrefix + n, 1); PlayerPrefs.Save(); }

    void RefreshVoteDisplay()
    {
        if (_voteIssues == null || _voteIssues.Length == 0)
        {
            if (voteIssueText  != null) voteIssueText.text  = _voteFetching ? "Loading..." : "No open feature requests.";
            if (voteStatusText != null) voteStatusText.text = "";
            if (voteButton     != null) voteButton.interactable     = false;
            if (votePrevButton != null) votePrevButton.interactable = false;
            if (voteNextButton != null) voteNextButton.interactable = false;
            return;
        }
        var issue = _voteIssues[_voteIndex];
        bool voted = HasVoted(issue.Number);
        if (voteIssueText  != null)
            voteIssueText.text  = $"#{issue.Number}: {issue.Title}\n+1  {issue.Votes} vote{(issue.Votes != 1 ? "s" : "")}";
        if (voteStatusText != null)
            voteStatusText.text = voted ? "You voted on this" : $"({_voteIndex + 1} of {_voteIssues.Length})";
        if (voteButton     != null) voteButton.interactable     = !voted;
        if (votePrevButton != null) votePrevButton.interactable = _voteIssues.Length > 1;
        if (voteNextButton != null) voteNextButton.interactable = _voteIssues.Length > 1;
    }

    IEnumerator FetchOpenFeatureRequests()
    {
        _voteFetching = true;
        RefreshVoteDisplay();
        string url = k_GhApi + "?labels=feature+request&state=open&per_page=20";
        var req = UnityWebRequest.Get(url);
        req.timeout = 15;
        req.SetRequestHeader("Accept",               "application/vnd.github+json");
        req.SetRequestHeader("X-GitHub-Api-Version", "2022-11-28");
        req.SetRequestHeader("User-Agent",           "BloodIdle/1.0");
        string token = GetToken();
        if (token.Length > 0)
            req.SetRequestHeader("Authorization", "Bearer " + token);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            _voteIssues = ParseIssueList(req.downloadHandler.text);
        else if (voteIssueText != null)
            voteIssueText.text = "Failed to load — check connection.";
        req.Dispose();
        _voteFetching = false;
        RefreshVoteDisplay();
    }

    IEnumerator PostVote(int issueNumber)
    {
        if (voteButton     != null) voteButton.interactable = false;
        if (voteStatusText != null) voteStatusText.text     = "Voting...";
        string token = GetToken();
        if (token.Length == 0)
        {
            if (voteStatusText != null) voteStatusText.text = "Voting not configured.";
            yield break;
        }
        string url   = k_GhApi + "/" + issueNumber + "/reactions";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("{\"content\":\"+1\"}");
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout         = 15;
        req.SetRequestHeader("Authorization",        "Bearer " + token);
        req.SetRequestHeader("Accept",               "application/vnd.github+json");
        req.SetRequestHeader("Content-Type",         "application/json");
        req.SetRequestHeader("X-GitHub-Api-Version", "2022-11-28");
        req.SetRequestHeader("User-Agent",           "BloodIdle/1.0");
        yield return req.SendWebRequest();
        bool ok = req.result == UnityWebRequest.Result.Success ||
                  req.responseCode == 200 || req.responseCode == 201;
        if (ok)
        {
            MarkVoted(issueNumber);
            for (int i = 0; i < _voteIssues.Length; i++)
            {
                if (_voteIssues[i].Number != issueNumber) continue;
                var e = _voteIssues[i]; e.Votes++; _voteIssues[i] = e; break;
            }
        }
        else if (voteStatusText != null)
            voteStatusText.text = $"Vote failed ({req.responseCode}) — try again.";
        req.Dispose();
        RefreshVoteDisplay();
    }

    static IssueEntry[] ParseIssueList(string json)
    {
        var issues = new System.Collections.Generic.List<IssueEntry>();
        if (string.IsNullOrEmpty(json)) return issues.ToArray();
        int pos = 0;
        while (pos < json.Length)
        {
            int ni = json.IndexOf("\"number\":", pos, StringComparison.Ordinal);
            if (ni < 0) break;
            int n = ExtractJsonInt(json, ni + 9);
            if (n <= 0) { pos = ni + 9; continue; }
            int ti = json.IndexOf("\"title\":", ni, StringComparison.Ordinal);
            string title = (ti > 0 && ti - ni < 3000) ? ExtractJsonStr(json, ti + 8) : "";
            int pi = json.IndexOf("\"+1\":", ni, StringComparison.Ordinal);
            int votes = (pi > 0 && pi - ni < 6000) ? ExtractJsonInt(json, pi + 5) : 0;
            if (title.Length > 0)
                issues.Add(new IssueEntry { Number = n, Title = title, Votes = votes });
            pos = ni + 9;
        }
        return issues.ToArray();
    }

    static int ExtractJsonInt(string s, int start)
    {
        while (start < s.Length && s[start] == ' ') start++;
        int end = start;
        while (end < s.Length && char.IsDigit(s[end])) end++;
        return (end > start && int.TryParse(s.Substring(start, end - start), out int v)) ? v : 0;
    }

    static string ExtractJsonStr(string s, int start)
    {
        while (start < s.Length && s[start] != '\"') start++;
        if (start >= s.Length) return "";
        start++;
        var sb = new StringBuilder();
        while (start < s.Length && s[start] != '\"')
        {
            if (s[start] == '\\' && start + 1 < s.Length) { sb.Append(s[start + 1]); start += 2; }
            else sb.Append(s[start++]);
        }
        return sb.ToString();
    }

    static string JStr(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\n", "\\n").Replace("\r", "\\r")
                .Replace("\t", "\\t") + "\"";
}
