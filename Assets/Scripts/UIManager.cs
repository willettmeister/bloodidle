using System;
using System.Collections;
using System.Globalization;
using System.Text;
#if DOTWEEN
using DG.Tweening;
#endif
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Resources")]
    public Text bloodText;
    public Text woodText;
    public Text farmBloodInfoText;

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
    public Button truceButton;
    public Text   truceButtonText;

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
    public Button autoSurgeButton;
    public Text   autoSurgeButtonText;
    public Button autoHealButton;
    public Text   autoHealButtonText;
    public Button autoStormButton;
    public Text   autoStormButtonText;

    [Header("Blood Storm")]
    public Text bloodStormInfoText;
    public Button bloodStormButton;
    public Button upgradeBloodStormButton;
    public Text   stormCostText;

    [Header("Blood Oath")]
    public Text   bloodOathInfoText;
    public Button bloodOathButton;
    public Button autoBloodOathButton;
    public Text   autoBloodOathButtonText;
    public Button upgradeBloodOathButton;
    public Text   bloodOathUpgradeCostText;

    [Header("War Cry")]
    public Text   warCryInfoText;
    public Button warCryButton;
    public Button autoWarCryButton;
    public Text   autoWarCryButtonText;
    public Button upgradeWarCryButton;
    public Text   warCryUpgradeCostText;

    [Header("Hex Curse")]
    public Text   hexCurseInfoText;
    public Button hexCurseButton;
    public Button autoHexCurseButton;
    public Text   autoHexCurseButtonText;
    public Button upgradeHexCurseButton;
    public Text   hexCurseUpgradeCostText;

    [Header("Blood Shield")]
    public Text   bloodShieldInfoText;
    public Button bloodShieldButton;
    public Button autoBloodShieldButton;
    public Text   autoBloodShieldButtonText;

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

    [Header("Blood Well")]
    public Text   bloodWellInfoText;
    public Button buyBloodWellButton;

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
    public Text bannerInfoText;
    public Button upgradeBannerButton;
    public Text bannerCostText;

    [Header("Fortifications")]
    public Text fortInfoText;
    public Button upgradeFortButton;
    public Text fortCostText;

    [Header("Blood Ritual")]
    public GameObject bloodRitualPanel;
    public Text bloodRitualInfoText;
    public Button buyBloodRitualButton;
    public Text bloodRitualCostText;
    public Button autoRitualButton;
    public Text   autoRitualButtonText;

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
    public Text pBloodRitualStartInfoText;
    public Button pBloodRitualStartButton;
    public Text pBloodMasteryInfoText;
    public Button pBloodMasteryButton;
    public Text pSacredGroundInfoText;
    public Button pSacredGroundButton;
    public Text pEternalFlameInfoText;
    public Button pEternalFlameButton;
    public Text pWarMachineInfoText;
    public Button pWarMachineButton;
    public Text pCrimsonLegacyInfoText;
    public Button pCrimsonLegacyButton;
    public Text pBloodlineInfoText;
    public Button pBloodlineButton;
    public Text pIronBastionInfoText;
    public Button pIronBastionButton;
    public Text pBloodPriceInfoText;
    public Button pBloodPriceButton;
    public Text pVoidPactInfoText;
    public Button pVoidPactButton;

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
    public Text ssSoulHarvestInfoText;
    public Button ssSoulHarvestButton;
    public Text ssCrimsonPulseInfoText;
    public Button ssCrimsonPulseButton;
    public Text ssCrimsonBrandInfoText;
    public Button ssCrimsonBrandButton;
    public Text ssWarSpoilsInfoText;
    public Button ssWarSpoilsButton;
    public Text ssGhostStrikeInfoText;
    public Button ssGhostStrikeButton;
    public Text ssDeathsBountyInfoText;
    public Button ssDeathsBountyButton;
    public Text ssRuneSealInfoText;
    public Button ssRuneSealButton;
    public Text ssWarCrestInfoText;
    public Button ssWarCrestButton;
    public Text ssVitalSurgeInfoText;
    public Button ssVitalSurgeButton;
    public Text ssVoidConduitInfoText;
    public Button ssVoidConduitButton;
    public Text ssBloodEchoInfoText;
    public Button ssBloodEchoButton;
    public Text ssIronMarrowInfoText;
    public Button ssIronMarrowButton;
    public Text ssWrathBloomInfoText;
    public Button ssWrathBloomButton;
    public Text ssBloodNovaInfoText;
    public Button ssBloodNovaButton;
    public Text ssEchoSurgeInfoText;
    public Button ssEchoSurgeButton;
    public Text ssEntropyAmpInfoText;
    public Button ssEntropyAmpButton;
    public Text ssBoneWardInfoText;
    public Button ssBoneWardButton;
    public Text ssCrimsonStormInfoText;
    public Button ssCrimsonStormButton;

    [Header("Settings")]
    public GameObject settingsPanel;
    public Text soundToggleText;
    public Text notifToggleText;
    public Text speedToggleText;

    [Header("Blood Bank")]
    public GameObject bloodBankPanel;
    public Text bloodBankInfoText;
    public Text bloodBankAccruedText;
    public Button depositBloodButton;
    public Button withdrawBloodButton;
    public Button autoBankButton;
    public Text   autoBankButtonText;
    public Button bankInterestUpgradeButton;
    public Text   bankInterestUpgradeCostText;

    [Header("Cursed Blood")]
    public GameObject cursedBloodPanel;
    public Button cursedBloodButton;
    public Text   cursedBloodButtonText;
    public GameObject killIncomePanel;
    public Button killIncomeUpgradeButton;
    public Text   killIncomeUpgradeCostText;

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
    public Button autoBuyButton;
    public Text   autoBuyButtonText;

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
    public Button desecrateButton;
    public Text   desecrateButtonText;
    public Button autoDesecrateButton;
    public Text   autoDesecrateButtonText;
    public Button upgradeDesecrateButton;
    public Text   desecrateUpgradeCostText;

    [Header("Entropy")]
    public Text   entropyInfoText;
    public Button entropyButton;
    public Button upgradeEntropyButton;
    public Text   entropyUpgradeCostText;

    [Header("Soul Sacrifice")]
    public Button soulSacrificeButton;
    public Text   soulSacrificeInfoText;

    [Header("Soldier Sacrifice")]
    public Button soldierSacrificeButton;
    public Text   soldierSacrificeInfoText;

    [Header("Daily Quests")]
    public GameObject questsPanel;
    public Text       questInfoText0, questInfoText1, questInfoText2;
    public Button     questClaimButton0, questClaimButton1, questClaimButton2;
    public Text       questClaimButtonText0, questClaimButtonText1, questClaimButtonText2;
    public Text       questStreakText;

    [Header("Ads & IAP")]
    public GameObject adBoostRow;
    public Button     watchAdButton;
    public Text       adBoostButtonText;
    public GameObject iapShopPanel;
    public Button     removeAdsButton;
    public Text       removeAdsButtonText;
    public Button     starterPackButton;
    public Text       starterPackButtonText;
    public Button     bloodBoostSmallButton;
    public Button     bloodBoostLargeButton;

    [Header("Damage Numbers")]
    public RectTransform damageLayer;

    [Header("Tutorial")]
    public GameObject tutorialPanel;
    public Text       tutorialTitleText;
    public Text       tutorialBodyText;

    [Header("Tab System")]
    public GameObject battleTabPanel;
    public GameObject buildTabPanel;
    public GameObject progressTabPanel;
    public GameObject settingsTabPanel;

    double _lastBloodDisplay;
    bool   _tutorialWasActive;

    // Achievement definitions live in GameManager.AchievementDefs — no duplicate table here.

    void Start()
    {
#if DOTWEEN
        DOTween.Init(recycleAllByDefault: true, useSafeMode: true).SetCapacity(200, 10);
#endif
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
        string comboTag = gm.ComboStacks > 0 ? $"  🔥×{gm.ComboMult:F1}" : "";
        bloodText.text = gm.BloodPerSec > 0
            ? $"Blood: {GameManager.FormatNumber(gm.Blood)}  +{gm.BloodPerSec:F1}/s{dailyTag}{comboTag}"
            : $"Blood: {GameManager.FormatNumber(gm.Blood)}{dailyTag}{comboTag}";
#if DOTWEEN
        if (gm.Blood > _lastBloodDisplay && bloodText != null)
        {
            bloodText.transform.DOKill();
            bloodText.transform.DOPunchScale(Vector3.one * 0.10f, 0.18f, 1, 0.5f);
        }
#endif
        _lastBloodDisplay = gm.Blood;

        if (farmBloodInfoText != null)
        {
            string echoHint = gm.NextTapIsEcho ? "  ★ NEXT: 2×!" : "";
            farmBloodInfoText.text = $"+{GameManager.FormatNumber(gm.EffectiveBloodPerClick)}/tap{echoHint}";
        }

        string speedTag = gm.GameSpeedMult >= GameManager.GameSpeedFast ? "  2×⚡" : "";
        if (gm.SoulShardShopUnlocked)
            woodText.text = $"Wood: {GameManager.FormatNumber(gm.Wood)}  ⬡{GameManager.FormatNumber(gm.SoulShards)}{speedTag}";
        else
            woodText.text = gm.WoodPerSecond > 0
                ? $"Wood: {GameManager.FormatNumber(gm.Wood)}  +{gm.WoodPerSecond:F1}/s{speedTag}"
                : $"Wood: {GameManager.FormatNumber(gm.Wood)}{speedTag}";

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

#if DOTWEEN
        if (waveText.text != $"Wave {gm.Wave}")
        {
            waveText.transform.DOKill();
            waveText.transform.DOPunchScale(Vector3.one * 0.18f, 0.25f, 1, 0.5f);
        }
#endif
        waveText.text = $"Wave {gm.Wave}";
        if (waveSubText != null)
        {
            string moraleBonus = gm.WaveStreak >= 3 ? "+15%⚔" : "";
            string streakTag = gm.WaveStreak > 0 ? $"  🔥×{gm.StreakMultiplier:F1}{moraleBonus}" : "";
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
                string counter = "";
                if (gm.CurrentEnemyModifier == EnemyModifier.Armored && gm.IsAllBerserker)
                    counter = "  ⚡ Berserker bypasses armor";
                else if (gm.CurrentEnemyModifier == EnemyModifier.Cursed && gm.PaladinCount > 0)
                    counter = "  ✚ Paladin +25% vs cursed";
                enemyModifierText.text  = gm.EnemyModifierDisplay + counter;
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
#if DOTWEEN
        enemyHPFill.DOFillAmount(gm.EnemyMaxHP > 0 ? gm.EnemyHP / gm.EnemyMaxHP : 0f, 0.12f).SetEase(Ease.OutSine);
#else
        enemyHPFill.fillAmount = gm.EnemyMaxHP > 0 ? gm.EnemyHP / gm.EnemyMaxHP : 0f;
#endif
        string ttkTag = "";
        if (gm.SoldierCount > 0 && gm.EnemyHP > 0 && gm.EffectiveAttack > 0f)
        {
            float regenDps = gm.CurrentEnemyModifier == EnemyModifier.Regen ? gm.EnemyMaxHP * GameManager.EnemyRegenPct : 0f;
            float netDps = gm.EffectiveAttack - regenDps;
            if (netDps > 0f)
            {
                float ttk = gm.EnemyHP / netDps;
                ttkTag = ttk < 60f ? $"  ~{Mathf.CeilToInt(ttk)}s" : $"  ~{Mathf.CeilToInt(ttk / 60f)}m";
            }
        }
        enemyHPText.text = $"{GameManager.FormatHP(gm.EnemyHP)} / {GameManager.FormatHP(gm.EnemyMaxHP)}{ttkTag}  |  +{GameManager.FormatNumber(gm.WaveBloodPreview)} blood";
        if (truceButton != null)
        {
            bool canTruce = !gm.WavePreviewActive && gm.EnemyHP > 0 && gm.Blood >= GameManager.TruceCost;
            truceButton.interactable = canTruce;
            if (truceButtonText != null)
                truceButtonText.text = $"Skip Wave\n({GameManager.FormatNumber(GameManager.TruceCost)} blood)";
        }

        // Army
        bool hasSoldiers = gm.SoldierCount > 0;
        bool atCap       = gm.SoldierCount >= gm.MaxSoldiers;

        string compBonus = gm.PackTacticsActive ? "  ⚔ Pack Tactics +15%"
                         : gm.IsAllTank        ? $"  ♦ Regen +{GameManager.TankRegenRate:F0}/s  −{GameManager.TankShieldWallReduction * 100:F0}% dmg"
                         : gm.IsAllBerserker   ? $"  ⚡ Crit {GameManager.BerserkerCritChance * 100:F0}% ×{GameManager.BerserkerCritMult:F0}"
                         : gm.IsAllPaladin     ? $"  ✚ Heal +{gm.PaladinCount * GameManager.PaladinHealRate:F0}/s"
                         : gm.IsMixedArmy      ? $"  🛡 −{GameManager.MixedArmyDmgReduction * 100:F0}% dmg"
                         : "";
        string furyTag = gm.IdleFuryStacks > 0 ? $"  | Idle Fury x{gm.IdleFuryStacks}" : "";
        soldierCountText.text = hasSoldiers
            ? $"Soldiers: {gm.SoldierCount}/{gm.MaxSoldiers}  [T:{gm.TankCount} B:{gm.BerserkerCount} P:{gm.PaladinCount}]{compBonus}{furyTag}"
            : $"No soldiers — buy one!  (max {gm.MaxSoldiers})";

        soldierHPRow.SetActive(hasSoldiers);
        if (hasSoldiers)
        {
#if DOTWEEN
            soldierHPFill.DOFillAmount(gm.FrontlineMaxHP > 0 ? gm.SoldierHP / gm.FrontlineMaxHP : 0f, 0.15f).SetEase(Ease.OutCubic);
#else
            soldierHPFill.fillAmount = gm.FrontlineMaxHP > 0 ? gm.SoldierHP / gm.FrontlineMaxHP : 0f;
#endif
            string cls = gm.FrontlineIsTank ? "Tank" : gm.FrontlineIsBerserker ? "Berserker" : "Paladin";
            string desperTag      = gm.DesperationActive    ? "  💥 DESPERATION" : "";
            string lastStandTag   = gm.DeathsDoorActive      ? "  💀 DEATH'S DOOR" : gm.LastStandActive ? "  ⚔ LAST STAND" : "";
            string rageTag        = gm.BerserkerRageActive  ? "  🔴 RAGE" : "";
            string meditTag       = gm.MeditationReady      ? "  ⚡ FOCUS" : "";
            string adrenalTag     = gm.AdrenalineStacks > 0
                ? $"  ⬆×{gm.AdrenalineStacks}({Mathf.CeilToInt(gm.AdrenalineTimeLeft)}s)" : "";
            string pactTag        = gm.CrimsonPactCharged   ? "  🔺 PACT" : "";
            string undyingTag     = gm.UndyingAvailable     ? "  ✝ UNDYING" : "";
            soldierHPText.text = $"{cls}: {GameManager.FormatHP(gm.SoldierHP)} / {GameManager.FormatHP(gm.FrontlineMaxHP)} HP  |  {gm.EffectiveAttack:F1} DPS{desperTag}{rageTag}{lastStandTag}{meditTag}{adrenalTag}{pactTag}{undyingTag}";
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
                if (upgradeBloodStormButton != null)
                    upgradeBloodStormButton.interactable = gm.BloodStormUpgradeLevel < GameManager.MaxSpellUpgradeLevel
                        && gm.Blood >= gm.BloodStormUpgradeCost;
                if (stormCostText != null)
                {
                    if (gm.BloodStormUpgradeLevel < GameManager.MaxSpellUpgradeLevel)
                    {
                        float nextCD = GameManager.BloodStormCooldown - (gm.BloodStormUpgradeLevel + 1) * GameManager.BloodStormCooldownReduction;
                        stormCostText.text = $"Upgrade Storm\nLv.{gm.BloodStormUpgradeLevel}→{gm.BloodStormUpgradeLevel + 1}  {gm.BloodStormCooldownEffective:F0}s→{nextCD:F0}s CD\n({GameManager.FormatNumber(gm.BloodStormUpgradeCost)} blood)";
                    }
                    else
                        stormCostText.text = $"Storm Upgrade\n(Max Level)";
                }
                if (autoStormButtonText != null)
                    autoStormButtonText.text = gm.AutoStorm ? "Auto-Storm: ON" : "Auto-Storm: OFF";
                if (autoStormButton != null) autoStormButton.gameObject.SetActive(true);
            }
            else
            {
                if (autoStormButton != null) autoStormButton.gameObject.SetActive(false);
                if (upgradeBloodStormButton != null) upgradeBloodStormButton.gameObject.SetActive(false);
                if (bloodStormInfoText != null)
                    bloodStormInfoText.text = $"Blood Storm  —  Unlocks at wave {GameManager.BloodStormUnlockWave}";
            }
        }
        if (bloodOathInfoText != null)
        {
            if (gm.BloodOathUnlocked)
            {
                bloodOathInfoText.text = gm.BloodOathActive
                    ? $"Blood Oath  —  ×4 atk + reflect  {Mathf.CeilToInt(gm.BloodOathTimeRemaining)}s"
                    : gm.BloodOathReady
                        ? $"Blood Oath  —  ×4 atk, 50% reflect for {gm.BloodOathDurationEffective:F0}s  ({GameManager.FormatNumber(GameManager.BloodOathCost)} blood)"
                        : $"Blood Oath  —  Cooldown: {Mathf.CeilToInt(gm.BloodOathCooldownLeft)}s";
                if (bloodOathButton != null)
                    bloodOathButton.interactable = gm.BloodOathCanCast;
                if (upgradeBloodOathButton != null)
                    upgradeBloodOathButton.interactable = gm.BloodOathUpgradeLevel < GameManager.MaxSpellUpgradeLevel
                        && gm.Blood >= gm.BloodOathUpgradeCost;
                if (bloodOathUpgradeCostText != null)
                {
                    if (gm.BloodOathUpgradeLevel < GameManager.MaxSpellUpgradeLevel)
                    {
                        float nextDur = GameManager.BloodOathDuration + (gm.BloodOathUpgradeLevel + 1) * 5f;
                        bloodOathUpgradeCostText.text = $"Upgrade Blood Oath\nLv.{gm.BloodOathUpgradeLevel}→{gm.BloodOathUpgradeLevel + 1}  {gm.BloodOathDurationEffective:F0}s→{nextDur:F0}s\n({GameManager.FormatNumber(gm.BloodOathUpgradeCost)} blood)";
                    }
                    else
                        bloodOathUpgradeCostText.text = "Blood Oath MAX";
                }
            }
            else
            {
                bloodOathInfoText.text = $"Blood Oath  —  Unlocks at wave {GameManager.BloodOathUnlockWave}";
                if (bloodOathButton != null) bloodOathButton.interactable = false;
                if (upgradeBloodOathButton != null) upgradeBloodOathButton.gameObject.SetActive(false);
            }
            if (autoBloodOathButton != null)
            {
                autoBloodOathButton.gameObject.SetActive(gm.BloodOathUnlocked);
                if (gm.BloodOathUnlocked && autoBloodOathButtonText != null)
                    autoBloodOathButtonText.text = gm.AutoBloodOath ? "Auto-Oath: ON" : "Auto-Oath: OFF";
            }
        }

        if (warCryInfoText != null)
        {
            if (gm.WarCryUnlocked)
            {
                warCryInfoText.text = gm.WarCryActive
                    ? $"War Cry  —  ×2 attack  {Mathf.CeilToInt(gm.WarCryTimeLeft)}s"
                    : $"War Cry  —  ×2 attack for {gm.WarCryDurationEffective:F0}s  ({GameManager.FormatNumber(GameManager.WarCryCost)} blood)";
                if (warCryButton != null)
                    warCryButton.interactable = !gm.WarCryActive && gm.Blood >= GameManager.WarCryCost;
                if (upgradeWarCryButton != null)
                    upgradeWarCryButton.interactable = gm.WarCryUpgradeLevel < GameManager.MaxSpellUpgradeLevel
                        && gm.Blood >= gm.WarCryUpgradeCost;
                if (warCryUpgradeCostText != null)
                {
                    if (gm.WarCryUpgradeLevel < GameManager.MaxSpellUpgradeLevel)
                    {
                        float nextDur = GameManager.WarCryDuration + (gm.WarCryUpgradeLevel + 1) * 5f;
                        warCryUpgradeCostText.text = $"Upgrade War Cry\nLv.{gm.WarCryUpgradeLevel}→{gm.WarCryUpgradeLevel + 1}  {gm.WarCryDurationEffective:F0}s→{nextDur:F0}s\n({GameManager.FormatNumber(gm.WarCryUpgradeCost)} blood)";
                    }
                    else
                        warCryUpgradeCostText.text = "War Cry MAX";
                }
            }
            else
            {
                warCryInfoText.text = $"War Cry  —  Unlocks at wave {GameManager.WarCryUnlockWave}";
                if (warCryButton != null) warCryButton.interactable = false;
                if (upgradeWarCryButton != null) upgradeWarCryButton.gameObject.SetActive(false);
            }
            if (autoWarCryButton != null)
            {
                autoWarCryButton.gameObject.SetActive(gm.WarCryUnlocked);
                if (gm.WarCryUnlocked && autoWarCryButtonText != null)
                    autoWarCryButtonText.text = gm.AutoWarCry ? "Auto-Cry: ON" : "Auto-Cry: OFF";
            }
        }
        if (hexCurseInfoText != null)
        {
            if (gm.HexCurseUnlocked)
            {
                hexCurseInfoText.text = gm.HexCurseActive
                    ? $"Hex Curse  —  −{GameManager.HexCurseAtkReduction * 100:F0}% enemy atk  {Mathf.CeilToInt(gm.HexCurseTimeLeft)}s"
                    : $"Hex Curse  —  −{GameManager.HexCurseAtkReduction * 100:F0}% enemy atk for {gm.HexCurseDurationEffective:F0}s  ({GameManager.FormatNumber(GameManager.HexCurseCost)} blood)";
                if (hexCurseButton != null)
                    hexCurseButton.interactable = !gm.HexCurseActive && gm.Blood >= GameManager.HexCurseCost && gm.EnemyHP > 0;
                if (upgradeHexCurseButton != null)
                    upgradeHexCurseButton.interactable = gm.HexCurseUpgradeLevel < GameManager.MaxSpellUpgradeLevel
                        && gm.Blood >= gm.HexCurseUpgradeCost;
                if (hexCurseUpgradeCostText != null)
                {
                    if (gm.HexCurseUpgradeLevel < GameManager.MaxSpellUpgradeLevel)
                    {
                        float nextDur = GameManager.HexCurseDuration + (gm.HexCurseUpgradeLevel + 1) * 5f;
                        hexCurseUpgradeCostText.text = $"Upgrade Hex Curse\nLv.{gm.HexCurseUpgradeLevel}→{gm.HexCurseUpgradeLevel + 1}  {gm.HexCurseDurationEffective:F0}s→{nextDur:F0}s\n({GameManager.FormatNumber(gm.HexCurseUpgradeCost)} blood)";
                    }
                    else
                        hexCurseUpgradeCostText.text = "Hex Curse MAX";
                }
            }
            else
            {
                hexCurseInfoText.text = $"Hex Curse  —  Unlocks at wave {GameManager.HexCurseUnlockWave}";
                if (hexCurseButton != null) hexCurseButton.interactable = false;
                if (upgradeHexCurseButton != null) upgradeHexCurseButton.gameObject.SetActive(false);
            }
            if (autoHexCurseButton != null)
            {
                autoHexCurseButton.gameObject.SetActive(gm.HexCurseUnlocked);
                if (gm.HexCurseUnlocked && autoHexCurseButtonText != null)
                    autoHexCurseButtonText.text = gm.AutoHexCurse ? "Auto-Hex: ON" : "Auto-Hex: OFF";
            }
        }
        if (bloodShieldInfoText != null)
        {
            if (gm.BloodShieldUnlocked)
            {
                bloodShieldInfoText.text = gm.BloodShieldHP > 0f
                    ? $"Blood Shield  —  {gm.BloodShieldHP:F0} / {GameManager.BloodShieldAmount:F0} HP remaining"
                    : $"Blood Shield  —  {GameManager.BloodShieldAmount:F0} HP absorb  ({GameManager.FormatNumber(GameManager.BloodShieldCost)} blood)";
                if (bloodShieldButton != null)
                    bloodShieldButton.interactable = gm.BloodShieldHP <= 0f && gm.Blood >= GameManager.BloodShieldCost && hasSoldiers;
            }
            else
            {
                bloodShieldInfoText.text = $"Blood Shield  —  Unlocks at {GameManager.FormatNumber(GameManager.BloodShieldUnlockThreshold)} total blood";
                if (bloodShieldButton != null) bloodShieldButton.interactable = false;
            }
            if (autoBloodShieldButton != null)
            {
                autoBloodShieldButton.gameObject.SetActive(gm.BloodShieldUnlocked);
                if (gm.BloodShieldUnlocked && autoBloodShieldButtonText != null)
                    autoBloodShieldButtonText.text = gm.AutoBloodShield
                        ? $"Auto-Shield: ON (<{GameManager.AutoBloodShieldThreshold * 100:F0}%)"
                        : "Auto-Shield: OFF";
            }
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
            {
                if (gm.SurgeUpgradeLevel < GameManager.MaxSpellUpgradeLevel)
                {
                    float nextDur = GameManager.SurgeDuration + (gm.SurgeUpgradeLevel + 1) * 5f;
                    surgeCostText.text = $"Upgrade Surge\nLv.{gm.SurgeUpgradeLevel}→{gm.SurgeUpgradeLevel + 1}  {gm.SurgeDurationEffective:F0}s→{nextDur:F0}s\n({GameManager.FormatNumber(gm.SurgeUpgradeCost)} blood)";
                }
                else
                    surgeCostText.text = "Surge MAX";
            }
            if (autoSurgeButtonText != null)
                autoSurgeButtonText.text = gm.AutoSurge ? "Auto-Surge: ON" : "Auto-Surge: OFF";
        }

        if (gm.HealSelfUnlocked)
        {
            if (upgradeHealSelfButton != null)
                upgradeHealSelfButton.interactable = gm.HealUpgradeLevel < GameManager.MaxSpellUpgradeLevel
                    && gm.Blood >= gm.HealUpgradeCost;
            if (healCostText != null)
            {
                if (gm.HealUpgradeLevel < GameManager.MaxSpellUpgradeLevel)
                {
                    float nextHeal = GameManager.HealSelfAmount + (gm.HealUpgradeLevel + 1) * 10f;
                    healCostText.text = $"Upgrade Heal\nLv.{gm.HealUpgradeLevel}→{gm.HealUpgradeLevel + 1}  +{gm.HealSelfAmountEffective:F0}→+{nextHeal:F0} HP\n({GameManager.FormatNumber(gm.HealUpgradeCost)} blood)";
                }
                else
                    healCostText.text = "Heal MAX";
            }
            if (autoHealButtonText != null)
                autoHealButtonText.text = gm.AutoHeal
                    ? $"Auto-Heal: ON (<{GameManager.AutoHealThreshold * 100:F0}% HP)"
                    : "Auto-Heal: OFF";
        }

        // Workers + Blood Pact
        if (workersPanel != null) workersPanel.SetActive(gm.WorkersUnlocked);
        string effBonus = gm.WorkerEfficiencyMult > 1.0
            ? $"  (+{(gm.WorkerEfficiencyMult - 1.0) * 100:F0}% eff)"
            : "";
        workerInfoText.text = $"Workers: {gm.WorkerCount}{effBonus}";
        buyWorkerButton.interactable = gm.Blood >= GameManager.WorkerCost;
        if (bloodPactButton != null)
        {
            bloodPactButton.interactable = gm.Blood >= GameManager.BloodPactBloodCost;
            if (bloodPactText != null)
                bloodPactText.text = $"Blood Pact\n({GameManager.FormatNumber(GameManager.BloodPactBloodCost)} blood → {GameManager.FormatNumber(GameManager.BloodPactWoodGain)} wood)";
        }
        if (shrineInfoText != null)
        {
            double shrineIncome = gm.ShrineCount * GameManager.ShrineBloodPerSec * gm.AchievementBloodIncomeMult * gm.AdBoostMult;
            shrineInfoText.text = $"Blood Shrine  {gm.ShrineCount}/{GameManager.ShrineMaxCount}  (+{GameManager.FormatNumber(shrineIncome)}/s blood)";
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
        if (bloodWellInfoText != null)
        {
            if (gm.BloodWellUnlocked && gm.BloodWellCount > 0)
            {
                double woodConsumed = gm.BloodWellCount * GameManager.BloodWellWoodPerSec;
                bloodWellInfoText.text = $"Blood Well  {gm.BloodWellCount}/{GameManager.BloodWellMaxCount}  (+{gm.BloodWellBloodPerSec:F1}/s blood, −{woodConsumed:F1}/s wood)";
            }
            else
                bloodWellInfoText.text = gm.BloodWellUnlocked
                    ? $"Blood Well  0/{GameManager.BloodWellMaxCount}  (consumes {GameManager.BloodWellWoodPerSec:F1} wood/s each)"
                    : $"Blood Well  (need 3 workers + wave 8)";
        }
        if (buyBloodWellButton != null)
        {
            buyBloodWellButton.interactable = gm.BloodWellUnlocked && gm.Wood >= gm.BloodWellCost && gm.BloodWellCount < GameManager.BloodWellMaxCount;
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
            if (bannerInfoText != null)
                bannerInfoText.text = $"War Banner  Lv.{gm.BannerLevel}/{GameManager.MaxEquipLevel}  (streak cap ×{gm.StreakMultiplierCap:F1})";
            if (upgradeBannerButton != null)
                upgradeBannerButton.interactable = gm.BannerLevel < GameManager.MaxEquipLevel && gm.Wood >= gm.BannerUpgradeCost;
            if (bannerCostText != null)
                bannerCostText.text = gm.BannerLevel < GameManager.MaxEquipLevel
                    ? $"Upgrade\n({GameManager.FormatNumber(gm.BannerUpgradeCost)} wood)" : "MAX";
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
            double ritualIncome = gm.BloodRitualCount * (GameManager.BloodRitualBloodPerSec + gm.PRitualEffLevel * 0.5)
                * gm.PrestigeMultiplier * gm.AchievementBloodIncomeMult * gm.AdBoostMult;
            bloodRitualInfoText.text = gm.BloodRitualCount > 0
                ? $"Blood Ritual: {gm.BloodRitualCount}  +{GameManager.FormatNumber(ritualIncome)}/s blood"
                : "Blood Ritual  —  passive blood income";
            if (buyBloodRitualButton != null)
                buyBloodRitualButton.interactable = gm.Wood >= gm.BloodRitualCost;
            if (bloodRitualCostText != null)
                bloodRitualCostText.text = $"Perform\n({GameManager.FormatNumber(gm.BloodRitualCost)} wood)";
            if (autoRitualButton != null)
            {
                autoRitualButton.interactable = true;
                if (autoRitualButtonText != null)
                    autoRitualButtonText.text = gm.AutoBuyRituals ? "Auto-Ritual: ON" : "Auto-Ritual: OFF";
            }
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

            if (pBloodRitualStartInfoText != null)
                pBloodRitualStartInfoText.text = $"Crimson Rite +1 Ritual  (Lv.{gm.PBloodRitualStartLevel})";
            if (pBloodRitualStartButton != null) pBloodRitualStartButton.interactable = canSpend;
            if (pBloodMasteryInfoText != null)
                pBloodMasteryInfoText.text = $"Blood Mastery +{GameManager.PBloodMasteryBonus} Vet cap  (Lv.{gm.PBloodMasteryLevel}, cap {gm.VeteranAttackCap})";
            if (pBloodMasteryButton != null) pBloodMasteryButton.interactable = canSpend;
            if (pSacredGroundInfoText != null)
                pSacredGroundInfoText.text = $"Sacred Ground +{GameManager.PSacredGroundBonus * 100:F0}% shrine income  (Lv.{gm.PSacredGroundLevel}/3)";
            if (pSacredGroundButton != null) pSacredGroundButton.interactable = canSpend && gm.PSacredGroundLevel < 3;
            if (pEternalFlameInfoText != null)
                pEternalFlameInfoText.text = $"Eternal Flame +{GameManager.PEternalFlameBonus * 100:F0}% Blood Well yield  (Lv.{gm.PEternalFlameLevel}/3)";
            if (pEternalFlameButton != null) pEternalFlameButton.interactable = canSpend && gm.PEternalFlameLevel < 3;
            if (pWarMachineInfoText != null)
                pWarMachineInfoText.text = $"War Machine +{GameManager.PWarMachineBonus * 100:F0}% soldier attack  (Lv.{gm.PWarMachineLevel}/3)";
            if (pWarMachineButton != null) pWarMachineButton.interactable = canSpend && gm.PWarMachineLevel < 3;
            if (pCrimsonLegacyInfoText != null)
                pCrimsonLegacyInfoText.text = $"Crimson Legacy +{GameManager.PCrimsonLegacyBonus * 100:F0}% blood income per prestige  (Lv.{gm.PCrimsonLegacyLevel}/3)";
            if (pCrimsonLegacyButton != null) pCrimsonLegacyButton.interactable = canSpend && gm.PCrimsonLegacyLevel < 3;
            if (pBloodlineInfoText != null)
                pBloodlineInfoText.text = $"Bloodline +{GameManager.PBloodlineStartBonus:F0} starting blood per run  (Lv.{gm.PBloodlineLevel}/3)";
            if (pBloodlineButton != null) pBloodlineButton.interactable = canSpend && gm.PBloodlineLevel < 3;
            if (pIronBastionInfoText != null)
                pIronBastionInfoText.text = $"Iron Bastion +{GameManager.PIronBastionHPBonus:F0} max HP to all soldier types  (Lv.{gm.PIronBastionLevel}/3)";
            if (pIronBastionButton != null) pIronBastionButton.interactable = canSpend && gm.PIronBastionLevel < 3;
            if (pBloodPriceInfoText != null)
                pBloodPriceInfoText.text = $"Blood Price +{GameManager.PBloodPriceBonus * 100:F0}% kill income rate  (Lv.{gm.PBloodPriceLevel}/3)";
            if (pBloodPriceButton != null) pBloodPriceButton.interactable = canSpend && gm.PBloodPriceLevel < 3;
            if (pVoidPactInfoText != null)
                pVoidPactInfoText.text = $"Void Pact +1 shard per boss kill  (Lv.{gm.PVoidPactLevel}/3)";
            if (pVoidPactButton != null) pVoidPactButton.interactable = canSpend && gm.PVoidPactLevel < 3;
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
            if (ssSoulHarvestInfoText != null)
                ssSoulHarvestInfoText.text = $"Soul Harvest {gm.EffectiveSoulHarvestPct * 100:F2}% enemy HP → blood  (Lv.{gm.SSSoulHarvestLevel}/{GameManager.SSMaxLevel})";
            if (ssSoulHarvestButton != null)
                ssSoulHarvestButton.interactable = canBuySS && gm.SSSoulHarvestLevel < GameManager.SSMaxLevel;
            if (ssCrimsonPulseInfoText != null)
                ssCrimsonPulseInfoText.text = $"Crimson Pulse +{GameManager.SSCrimsonPulseBonus * 100:F0}% ritual income  (Lv.{gm.SSCrimsonPulseLevel}/{GameManager.SSMaxLevel})";
            if (ssCrimsonPulseButton != null)
                ssCrimsonPulseButton.interactable = canBuySS && gm.SSCrimsonPulseLevel < GameManager.SSMaxLevel;
            if (ssCrimsonBrandInfoText != null)
                ssCrimsonBrandInfoText.text = $"Crimson Brand +{GameManager.SSCrimsonBrandBonus * 100:F0}% boss dmg  (Lv.{gm.SSCrimsonBrandLevel}/{GameManager.SSMaxLevel})";
            if (ssCrimsonBrandButton != null)
                ssCrimsonBrandButton.interactable = canBuySS && gm.SSCrimsonBrandLevel < GameManager.SSMaxLevel;
            if (ssWarSpoilsInfoText != null)
                ssWarSpoilsInfoText.text = $"War Spoils +{GameManager.SSWarSpoilsBonus * 100:F0}% all wave rewards  (Lv.{gm.SSWarSpoilsLevel}/{GameManager.SSMaxLevel})";
            if (ssWarSpoilsButton != null)
                ssWarSpoilsButton.interactable = canBuySS && gm.SSWarSpoilsLevel < GameManager.SSMaxLevel;
            if (ssGhostStrikeInfoText != null)
                ssGhostStrikeInfoText.text = $"Ghost Strike +{GameManager.SSGhostStrikeBonus * 100:F0}% Blood Storm damage  (Lv.{gm.SSGhostStrikeLevel}/{GameManager.SSMaxLevel})";
            if (ssGhostStrikeButton != null)
                ssGhostStrikeButton.interactable = canBuySS && gm.SSGhostStrikeLevel < GameManager.SSMaxLevel;
            if (ssDeathsBountyInfoText != null)
                ssDeathsBountyInfoText.text = $"Death's Bounty +{GameManager.SSDeathsBountyBonus * 100:F0}% Soul Sacrifice blood  (Lv.{gm.SSDeathsBountyLevel}/{GameManager.SSMaxLevel})";
            if (ssDeathsBountyButton != null)
                ssDeathsBountyButton.interactable = canBuySS && gm.SSDeathsBountyLevel < GameManager.SSMaxLevel;
            if (ssRuneSealInfoText != null)
                ssRuneSealInfoText.text = $"Rune Seal +{GameManager.SSRuneSealStackBonus} max combo stacks  (Lv.{gm.SSRuneSealLevel}/{GameManager.SSMaxLevel})";
            if (ssRuneSealButton != null)
                ssRuneSealButton.interactable = canBuySS && gm.SSRuneSealLevel < GameManager.SSMaxLevel;
            if (ssWarCrestInfoText != null)
                ssWarCrestInfoText.text = $"War Crest +{GameManager.SSWarCrestBonus:F1} streak multiplier cap  (Lv.{gm.SSWarCrestLevel}/{GameManager.SSMaxLevel})";
            if (ssWarCrestButton != null)
                ssWarCrestButton.interactable = canBuySS && gm.SSWarCrestLevel < GameManager.SSMaxLevel;
            if (ssVitalSurgeInfoText != null)
                ssVitalSurgeInfoText.text = $"Vital Surge Heal Self restores +{GameManager.SSVitalSurgeHPBonus:F0} extra HP  (Lv.{gm.SSVitalSurgeLevel}/{GameManager.SSMaxLevel})";
            if (ssVitalSurgeButton != null)
                ssVitalSurgeButton.interactable = canBuySS && gm.SSVitalSurgeLevel < GameManager.SSMaxLevel;
            bool canBuyT2 = gm.SoulShards >= GameManager.SSTier2Cost;
            if (ssVoidConduitInfoText != null)
                ssVoidConduitInfoText.text = $"Void Conduit +{GameManager.SSVoidConduitBonus * 100:F0}% all income  (Lv.{gm.SSVoidConduitLevel}/{GameManager.SSTier2MaxLevel})";
            if (ssVoidConduitButton != null)
                ssVoidConduitButton.interactable = canBuyT2 && gm.SSVoidConduitLevel < GameManager.SSTier2MaxLevel;
            if (ssBloodEchoInfoText != null)
                ssBloodEchoInfoText.text = $"Blood Echo +{gm.BloodEchoPerSec:F1}/s from {gm.TotalBossesKilled} bosses  (Lv.{gm.SSBloodEchoLevel}/{GameManager.SSTier2MaxLevel})";
            if (ssBloodEchoButton != null)
                ssBloodEchoButton.interactable = canBuyT2 && gm.SSBloodEchoLevel < GameManager.SSTier2MaxLevel;
            if (ssIronMarrowInfoText != null)
                ssIronMarrowInfoText.text = $"Iron Marrow +{GameManager.SSIronMarrowBonus:F0} atk all soldiers  (Lv.{gm.SSIronMarrowLevel}/{GameManager.SSTier2MaxLevel})";
            if (ssIronMarrowButton != null)
                ssIronMarrowButton.interactable = canBuyT2 && gm.SSIronMarrowLevel < GameManager.SSTier2MaxLevel;
            if (ssWrathBloomInfoText != null)
                ssWrathBloomInfoText.text = $"Wrath Bloom boss kill extends Surge +{GameManager.SSWrathBloomSurgeSecs:F0}s  (Lv.{gm.SSWrathBloomLevel}/{GameManager.SSTier2MaxLevel})";
            if (ssWrathBloomButton != null)
                ssWrathBloomButton.interactable = canBuyT2 && gm.SSWrathBloomLevel < GameManager.SSTier2MaxLevel;
            if (ssBloodNovaInfoText != null)
                ssBloodNovaInfoText.text = $"Blood Nova Storm hits +{GameManager.SSBloodNovaPct * 100:F0}% enemy max HP  (Lv.{gm.SSBloodNovaLevel}/{GameManager.SSTier2MaxLevel})";
            if (ssBloodNovaButton != null)
                ssBloodNovaButton.interactable = canBuyT2 && gm.SSBloodNovaLevel < GameManager.SSTier2MaxLevel;
            if (ssEchoSurgeInfoText != null)
                ssEchoSurgeInfoText.text = $"Echo Surge +{GameManager.SSEchoSurgeSecs:F0}s Surge duration  (Lv.{gm.SSEchoSurgeLevel}/{GameManager.SSTier2MaxLevel})";
            if (ssEchoSurgeButton != null)
                ssEchoSurgeButton.interactable = canBuyT2 && gm.SSEchoSurgeLevel < GameManager.SSTier2MaxLevel;
            if (ssEntropyAmpInfoText != null)
                ssEntropyAmpInfoText.text = $"Entropy Amp +{GameManager.SSEntropyAmpBonus * 100:F0}% Entropy damage  (Lv.{gm.SSEntropyAmpLevel}/{GameManager.SSTier2MaxLevel})";
            if (ssEntropyAmpButton != null)
                ssEntropyAmpButton.interactable = canBuyT2 && gm.SSEntropyAmpLevel < GameManager.SSTier2MaxLevel;
            if (ssBoneWardInfoText != null)
                ssBoneWardInfoText.text = $"Bone Ward +{GameManager.SSBoneWardBonus * 100:F0}% Blood Shield capacity  (Lv.{gm.SSBoneWardLevel}/{GameManager.SSTier2MaxLevel})";
            if (ssBoneWardButton != null)
                ssBoneWardButton.interactable = canBuyT2 && gm.SSBoneWardLevel < GameManager.SSTier2MaxLevel;
            if (ssCrimsonStormInfoText != null)
                ssCrimsonStormInfoText.text = $"Crimson Storm Blood Storm fires {(gm.SSCrimsonStormLevel > 0 ? gm.SSCrimsonStormLevel + 1 : 2)}× per cast  (Lv.{gm.SSCrimsonStormLevel}/{GameManager.SSTier2MaxLevel})";
            if (ssCrimsonStormButton != null)
                ssCrimsonStormButton.interactable = canBuyT2 && gm.SSCrimsonStormLevel < GameManager.SSTier2MaxLevel;
        }

        // Blood Bank
        if (bloodBankPanel != null)
        {
            if (bloodBankInfoText != null)
                bloodBankInfoText.text = $"Blood Bank  {GameManager.FormatNumber(gm.BloodBankDeposit)}/{GameManager.FormatNumber(gm.BankMaxDeposit)}  (+{gm.EffectiveBankInterestRate * 100:F1}%/hr)";
            if (bloodBankAccruedText != null)
                bloodBankAccruedText.text = gm.BloodBankAccrued > 0
                    ? $"Interest accrued: +{GameManager.FormatNumber(gm.BloodBankAccrued)} blood"
                    : "Interest accrued: none yet";
            if (depositBloodButton != null)
                depositBloodButton.interactable = gm.Blood >= 1.0
                    && gm.BloodBankDeposit < gm.BankMaxDeposit;
            if (withdrawBloodButton != null)
                withdrawBloodButton.interactable = gm.BloodBankDeposit > 0 || gm.BloodBankAccrued > 0;
            if (autoBankButton != null)
            {
                autoBankButton.interactable = true;
                if (autoBankButtonText != null)
                    autoBankButtonText.text = gm.AutoBankDeposit ? "Auto: ON" : "Auto: OFF";
            }
            if (bankInterestUpgradeButton != null)
            {
                bool maxed = gm.BankInterestLevel >= GameManager.BankInterestMaxLevel;
                bankInterestUpgradeButton.gameObject.SetActive(true);
                bankInterestUpgradeButton.interactable = !maxed && gm.Blood >= gm.BankInterestUpgradeCost;
                if (bankInterestUpgradeCostText != null)
                {
                    if (maxed)
                        bankInterestUpgradeCostText.text = $"Interest: {gm.EffectiveBankInterestRate * 100:F1}%/hr  (MAX)";
                    else
                    {
                        double nextRate = (gm.EffectiveBankInterestRate + GameManager.BankInterestRatePerLevel) * 100;
                        bankInterestUpgradeCostText.text = $"Interest Lv.{gm.BankInterestLevel}/{GameManager.BankInterestMaxLevel}  {gm.EffectiveBankInterestRate * 100:F1}%→{nextRate:F1}%  ({GameManager.FormatNumber(gm.BankInterestUpgradeCost)} blood)";
                    }
                }
            }
        }

        if (cursedBloodPanel != null)
        {
            cursedBloodPanel.SetActive(gm.CursedBloodUnlocked);
            if (cursedBloodButtonText != null)
                cursedBloodButtonText.text = gm.CursedBloodEnabled ? "Cursed Blood: ON" : "Cursed Blood: OFF";
        }

        if (killIncomePanel != null)
        {
            killIncomePanel.SetActive(gm.SoulHarvestUnlocked);
            if (gm.SoulHarvestUnlocked)
            {
                bool maxed = gm.KillIncomeUpgradeLevel >= GameManager.KillIncomeMaxLevel;
                if (killIncomeUpgradeButton != null)
                    killIncomeUpgradeButton.interactable = !maxed && gm.Blood >= gm.KillIncomeUpgradeCost;
                if (killIncomeUpgradeCostText != null)
                {
                    double rate = gm.EffectiveKillIncomeRate;
                    if (maxed)
                        killIncomeUpgradeCostText.text = $"Kill Income: {rate:F2}/kill  (MAX)";
                    else
                    {
                        double nextRate = rate + GameManager.KillIncomeRatePerLevel;
                        killIncomeUpgradeCostText.text = $"Kill Income Lv.{gm.KillIncomeUpgradeLevel}/{GameManager.KillIncomeMaxLevel}  {rate:F2}→{nextRate:F2}/kill  ({GameManager.FormatNumber(gm.KillIncomeUpgradeCost)} blood)";
                    }
                }
            }
        }

        barracksInfoText.text        = $"Barracks  Lv.{gm.BarracksLevel}  —  Max {gm.MaxSoldiers} soldiers";
        barracksUpgradeCostText.text = $"Upgrade\n({GameManager.FormatNumber(gm.BarracksUpgradeCost)} wood)";
        upgradeBarracksButton.interactable = gm.Wood >= gm.BarracksUpgradeCost;
        if (autoBuyButtonText != null)
            autoBuyButtonText.text = gm.AutoBuySoldiers ? "Auto-Buy: ON" : "Auto-Buy: OFF";

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
        if (desecrateButton != null)
        {
            bool showDesecrate = gm.DesecrateUnlocked && gm.CorruptionLevel > 0;
            desecrateButton.gameObject.SetActive(showDesecrate);
            if (showDesecrate)
            {
                desecrateButton.interactable = gm.DesecrateCanCast;
                if (desecrateButtonText != null)
                {
                    string cdStr = !gm.DesecrateReady ? $"  ({Mathf.CeilToInt(gm.DesecrateCooldownLeft)}s)" : "";
                    desecrateButtonText.text = $"Desecrate\n(-1 corrupt +50% burst{cdStr})";
                }
            }
        }
        if (autoDesecrateButton != null)
        {
            bool showAutoDesecrate = gm.DesecrateUnlocked && gm.CorruptionLevel > 0;
            autoDesecrateButton.gameObject.SetActive(showAutoDesecrate);
            if (showAutoDesecrate && autoDesecrateButtonText != null)
                autoDesecrateButtonText.text = gm.AutoDesecrate ? "Auto-Desecrate: ON" : "Auto-Desecrate: OFF";
        }
        if (upgradeDesecrateButton != null)
        {
            upgradeDesecrateButton.gameObject.SetActive(gm.DesecrateUnlocked);
            if (gm.DesecrateUnlocked)
            {
                upgradeDesecrateButton.interactable = gm.DesecrateUpgradeLevel < GameManager.MaxSpellUpgradeLevel
                    && gm.Blood >= gm.DesecrateUpgradeCost;
                if (desecrateUpgradeCostText != null)
                {
                    if (gm.DesecrateUpgradeLevel < GameManager.MaxSpellUpgradeLevel)
                    {
                        float nextCD = GameManager.DesecrateCooldown - (gm.DesecrateUpgradeLevel + 1) * GameManager.DesecrateCooldownReduction;
                        desecrateUpgradeCostText.text = $"Upgrade Desecrate\nLv.{gm.DesecrateUpgradeLevel}→{gm.DesecrateUpgradeLevel + 1}  {gm.DesecrateCooldownEffective:F0}s→{nextCD:F0}s CD\n({GameManager.FormatNumber(gm.DesecrateUpgradeCost)} blood)";
                    }
                    else
                        desecrateUpgradeCostText.text = "Desecrate MAX";
                }
            }
        }

        if (entropyInfoText != null)
        {
            if (gm.EntropyUnlocked)
            {
                string cdStr = !gm.EntropyReady ? $"  ({Mathf.CeilToInt(gm.EntropyCooldownLeft)}s)" : "";
                entropyInfoText.text = $"Entropy  —  Deal {gm.EntropyEffectivePct * 100:F0}% of enemy HP{cdStr}  (300 blood)";
                if (entropyButton != null) entropyButton.interactable = gm.EntropyCanCast;
            }
            else
            {
                entropyInfoText.text = $"Entropy  —  Unlocks at wave {GameManager.EntropyUnlockWave}";
                if (entropyButton != null) entropyButton.interactable = false;
            }
        }
        if (upgradeEntropyButton != null)
        {
            upgradeEntropyButton.gameObject.SetActive(gm.EntropyUnlocked);
            if (gm.EntropyUnlocked)
            {
                upgradeEntropyButton.interactable = gm.EntropyUpgradeLevel < GameManager.MaxSpellUpgradeLevel
                    && gm.Blood >= gm.EntropyUpgradeCost;
                if (entropyUpgradeCostText != null)
                {
                    if (gm.EntropyUpgradeLevel < GameManager.MaxSpellUpgradeLevel)
                    {
                        float nextPct = (GameManager.EntropyDamagePct + (gm.EntropyUpgradeLevel + 1) * GameManager.EntropyUpgradeDamagePct) * 100f;
                        entropyUpgradeCostText.text = $"Upgrade Entropy\nLv.{gm.EntropyUpgradeLevel}→{gm.EntropyUpgradeLevel + 1}  {gm.EntropyEffectivePct * 100:F0}%→{nextPct:F0}%\n({GameManager.FormatNumber(gm.EntropyUpgradeCost)} blood)";
                    }
                    else
                        entropyUpgradeCostText.text = "Entropy MAX";
                }
            }
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

        // Soldier Sacrifice
        if (soldierSacrificeButton != null)
        {
            bool sacUnlocked = gm.SacrificeUnlocked;
            soldierSacrificeButton.interactable = sacUnlocked && gm.EnemyHP > 0;
            if (soldierSacrificeInfoText != null)
                soldierSacrificeInfoText.text = sacUnlocked
                    ? $"Soldier Sacrifice  —  lose 1 soldier → {GameManager.SacrificeDmgMult:F0}× HP burst"
                    : "Soldier Sacrifice  —  Unlocks at wave 3 (2+ soldiers)";
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

        // Daily Quests panel (refresh if open)
        if (questsPanel != null && questsPanel.activeSelf)
            RefreshQuestsPanel();

        // Ad Boost row
        if (adBoostRow != null)
        {
            bool showAds = !gm.AdsRemoved;
            adBoostRow.SetActive(showAds);
            if (showAds && adBoostButtonText != null)
            {
                if (gm.AdBoostActive)
                {
                    int secs = Mathf.CeilToInt(gm.AdBoostTimeRemaining);
                    adBoostButtonText.text     = $"2× Blood Active  {secs / 60}:{secs % 60:D2}";
                    if (watchAdButton != null) watchAdButton.interactable = false;
                }
                else
                {
                    adBoostButtonText.text     = "Watch Ad  →  2× Blood (5 min)";
                    if (watchAdButton != null) watchAdButton.interactable = true;
                }
            }
        }

        // Tutorial panel — fade in on first show, instant hide on dismiss
        if (tutorialPanel != null)
        {
            if (gm.TutorialActive)
            {
                if (tutorialTitleText != null) tutorialTitleText.text = gm.TutorialTitle;
                if (tutorialBodyText  != null) tutorialBodyText.text  = gm.TutorialBody;
                if (!_tutorialWasActive)
                {
#if DOTWEEN
                    var cg = tutorialPanel.GetComponent<CanvasGroup>() ?? tutorialPanel.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    tutorialPanel.SetActive(true);
                    cg.DOFade(1f, 0.4f).SetEase(Ease.OutQuad);
#else
                    tutorialPanel.SetActive(true);
#endif
                }
            }
            else
            {
                tutorialPanel.SetActive(false);
            }
            _tutorialWasActive = gm.TutorialActive;
        }
    }

    static int NextPrestigeMilestone(int current)
    {
        int[] ms = { 5, 10, 20, 50 };
        foreach (int m in ms) if (current < m) return m;
        return -1;
    }

    // ── Tab System ────────────────────────────────────────────────────────────

    public void ShowBattleTab()   => ShowTab(0);
    public void ShowBuildTab()    => ShowTab(1);
    public void ShowProgressTab() => ShowTab(2);
    public void ShowSettingsTab() => ShowTab(3);

    void ShowTab(int i)
    {
        if (battleTabPanel   != null) battleTabPanel.SetActive(i == 0);
        if (buildTabPanel    != null) buildTabPanel.SetActive(i == 1);
        if (progressTabPanel != null) progressTabPanel.SetActive(i == 2);
        if (settingsTabPanel != null) settingsTabPanel.SetActive(i == 3);
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

    // ── IAP Shop Panel ────────────────────────────────────────────────────────

    public void ShowIAPPanel()
    {
        if (iapShopPanel == null) return;
        var gm = GameManager.Instance;
        if (removeAdsButton    != null) removeAdsButton.gameObject.SetActive(!gm.AdsRemoved);
        if (starterPackButton  != null) starterPackButton.gameObject.SetActive(!gm.StarterPackOwned);
        iapShopPanel.SetActive(true);
    }

    public void HideIAPPanel()
    {
        if (iapShopPanel != null) iapShopPanel.SetActive(false);
    }

    // ── Daily Quests Panel ────────────────────────────────────────────────────

    public void ShowQuestsPanel()
    {
        if (questsPanel == null) return;
        RefreshQuestsPanel();
        questsPanel.SetActive(true);
    }

    public void HideQuestsPanel()
    {
        if (questsPanel != null) questsPanel.SetActive(false);
    }

    void RefreshQuestsPanel()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.DailyQuestsReady) return;
        var infoTexts  = new[] { questInfoText0,  questInfoText1,  questInfoText2  };
        var claimBtns  = new[] { questClaimButton0, questClaimButton1, questClaimButton2 };
        var claimTexts = new[] { questClaimButtonText0, questClaimButtonText1, questClaimButtonText2 };
        for (int i = 0; i < GameManager.DailyQuestCount; i++)
        {
            var def      = GameManager.QuestPool[gm.DailyQuestIndices[i]];
            int progress = gm.DailyQuestProgress[i];
            bool claimed = gm.DailyQuestClaimed[i];
            bool complete = progress >= def.Target;

            string reward = def.ShardReward > 0
                ? $"+{GameManager.FormatNumber(def.BloodReward)} blood & {def.ShardReward} ⬡"
                : $"+{GameManager.FormatNumber(def.BloodReward)} blood";

            if (infoTexts[i] != null)
                infoTexts[i].text = claimed
                    ? $"✓ {def.Desc}"
                    : $"{def.Desc}  [{progress}/{def.Target}]\n{reward}";

            if (claimBtns[i] != null)
            {
                claimBtns[i].interactable = complete && !claimed;
                if (claimTexts[i] != null)
                    claimTexts[i].text = claimed ? "Claimed" : complete ? "Claim!" : "Locked";
            }
        }
        if (questStreakText != null)
        {
            int streak = gm.DailyQuestStreak;
            int bonus  = GameManager.QuestStreakBonusShards(streak + 1);
            string streakLine = streak > 0
                ? $"Streak: {streak} day{(streak == 1 ? "" : "s")}  (best: {gm.BestQuestStreak})"
                : $"No active streak";
            string bonusLine  = gm.AllQuestsClaimed
                ? $"All quests done!"
                : $"Complete all 3 today: +{bonus} bonus ⬡";
            questStreakText.text = $"{streakLine}\n{bonusLine}";
        }
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
        TalentFlags.Hemomancer   => "Hemomancer\n+0.2 blood/click per ritual owned",
        TalentFlags.WarDrum      => "War Drum\n+5 attack per soldier while streak ≥ 5",
        TalentFlags.Warlord      => "Warlord\n+0.1 blood/click per veteran attack bonus point",
        TalentFlags.SoulDrain         => "Soul Drain\nSoldier death deals 15% of enemy current HP",
        TalentFlags.FrenziedHarvest   => "Frenzied Harvest\n+0.5 blood/sec per ritual owned",
        TalentFlags.RiftStrike        => "Rift Strike\nEntropy cooldown reduced by 10 seconds",
        TalentFlags.CrimsonTide       => "Crimson Tide\n+0.1 blood/click per boss killed",
        TalentFlags.StormCaller       => "Storm Caller\nBlood Storm cooldown reduced by 15 seconds",
        TalentFlags.BloodPact         => "Blood Pact\nWorkers produce +0.2 blood/sec each",
        TalentFlags.IronPhalanx       => "Iron Phalanx\n+10 max HP to all frontline soldier types",
        TalentFlags.Bloodlord         => "Bloodlord\nBerserker Rage activates at 40% HP instead of 30%",
        TalentFlags.PhoenixRise       => "Phoenix Rise\nNext soldier after a death enters at 150% max HP",
        TalentFlags.TitansWill        => "Titan's Will\nTank HP regen rate doubled in all-tank army",
        TalentFlags.SurgeMastery      => "Surge Mastery\nBlood Surge multiplier +0.5×",
        TalentFlags.Vanguard          => "Vanguard\nAll-tank army takes 20% less incoming damage",
        TalentFlags.Bloodbound        => "Bloodbound\n+25% kill blood reward while Berserker Rage is active",
        TalentFlags.CrimsonVeil       => "Crimson Veil\nBelow 30% HP: damage halved for 5s (60s cooldown)",
        TalentFlags.Hemorrhage        => "Hemorrhage\nEach kill stacks a bleed: 1% enemy max HP/s for 3s (max 5 stacks)",
        TalentFlags.SoulBind          => "Soul Bind\nSoldier death doubles the next wave's blood reward",
        TalentFlags.SiegeBreaker      => "Siege Breaker\n+20% soldier damage vs bosses and daily challenges",
        _                             => "",
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
        if ((talents & TalentFlags.Hemomancer)     != 0) names.Add("Hemomancer");
        if ((talents & TalentFlags.WarDrum)        != 0) names.Add("WarDrum");
        if ((talents & TalentFlags.Warlord)        != 0) names.Add("Warlord");
        if ((talents & TalentFlags.SoulDrain)        != 0) names.Add("SoulDrain");
        if ((talents & TalentFlags.FrenziedHarvest)  != 0) names.Add("FrzHarvest");
        if ((talents & TalentFlags.RiftStrike)       != 0) names.Add("RiftStrike");
        if ((talents & TalentFlags.CrimsonTide)      != 0) names.Add("CrimsonTide");
        if ((talents & TalentFlags.StormCaller)      != 0) names.Add("StormCaller");
        if ((talents & TalentFlags.BloodPact)        != 0) names.Add("BloodPact");
        if ((talents & TalentFlags.IronPhalanx)      != 0) names.Add("IronPhalanx");
        if ((talents & TalentFlags.Bloodlord)        != 0) names.Add("Bloodlord");
        if ((talents & TalentFlags.PhoenixRise)      != 0) names.Add("PhoenixRise");
        if ((talents & TalentFlags.TitansWill)       != 0) names.Add("TitansWill");
        if ((talents & TalentFlags.SurgeMastery)     != 0) names.Add("SurgeMastery");
        if ((talents & TalentFlags.Vanguard)         != 0) names.Add("Vanguard");
        if ((talents & TalentFlags.Bloodbound)       != 0) names.Add("Bloodbound");
        if ((talents & TalentFlags.CrimsonVeil)      != 0) names.Add("CrimsonVeil");
        if ((talents & TalentFlags.Hemorrhage)       != 0) names.Add("Hemorrhage");
        if ((talents & TalentFlags.SoulBind)         != 0) names.Add("SoulBind");
        if ((talents & TalentFlags.SiegeBreaker)     != 0) names.Add("SiegeBrkr");
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
        if (speedToggleText != null)
            speedToggleText.text = gm.GameSpeedMult >= GameManager.GameSpeedFast ? "Speed: 2×" : "Speed: 1×";
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
        sb.AppendLine($"Spells Cast:       {gm.TotalSpellsCast}");
        sb.AppendLine($"Soldiers Lost:     {gm.TotalSoldiersLost}");
        sb.AppendLine($"Blood Earned:      {GameManager.FormatNumber(gm.TotalBloodEarned)}");
        sb.AppendLine($"Blood Bank:        {GameManager.FormatNumber(gm.BloodBankDeposit)} (+{GameManager.FormatNumber(gm.BloodBankAccrued)})");
        sb.AppendLine($"Best Wave:         {gm.BestWave}");
        sb.AppendLine($"Best Streak:       {gm.BestStreak}  (current: {gm.WaveStreak})");
        sb.AppendLine($"Quest Streak:      {gm.DailyQuestStreak}  (best: {gm.BestQuestStreak})");
        sb.AppendLine($"Veteran Bonus:     +{gm.VeteranAttackBonus}/{gm.VeteranAttackCap} atk (from boss kills)");
        sb.AppendLine($"Soul Shards:       {GameManager.FormatNumber(gm.SoulShards)}");
        sb.AppendLine($"Time Played:       {h}h {m}m {s}s");
        sb.AppendLine($"Prestige Level:    {gm.PrestigeCount}  (milestones: {gm.PrestigeMilestonesReached}/4)");
        if (gm.BloodPerSec > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Income / sec ───────────────────");
            double mult = gm.AchievementBloodIncomeMult * gm.AdBoostMult;
            if (gm.BloodRitualCount > 0)
            {
                double ritualBase = gm.BloodRitualCount * (GameManager.BloodRitualBloodPerSec + gm.PRitualEffLevel * 0.5) * gm.PrestigeMultiplier;
                sb.AppendLine($"  Rituals ×{gm.BloodRitualCount}:      +{GameManager.FormatNumber(ritualBase * mult)}/s");
            }
            if (gm.ShrineCount > 0)
                sb.AppendLine($"  Shrines ×{gm.ShrineCount}:       +{GameManager.FormatNumber(gm.ShrineCount * GameManager.ShrineBloodPerSec * mult)}/s");
            if (gm.KillIncomePerSec > 0)
                sb.AppendLine($"  Kill income:        +{GameManager.FormatNumber(gm.KillIncomePerSec * mult)}/s");
            if (gm.BloodTithePerSec > 0)
                sb.AppendLine($"  Blood Tithe:        +{GameManager.FormatNumber(gm.BloodTithePerSec * mult)}/s");
            if (gm.BloodTapPerSec > 0)
                sb.AppendLine($"  Blood Tap:          +{GameManager.FormatNumber(gm.BloodTapPerSec * mult)}/s");
            sb.AppendLine($"  Total:              +{GameManager.FormatNumber(gm.BloodPerSec)}/s");
        }
        sb.AppendLine();
        sb.AppendLine("── Passive Unlocks ────────────────");
        if (gm.SiphonUnlocked)       sb.AppendLine($"  ⚕ Siphon (wave {GameManager.SiphonUnlockWave}+):       {GameManager.SiphonRate * 100:F0}% dmg dealt → HP");
        if (gm.SoulHarvestUnlocked)  sb.AppendLine($"  🌾 Soul Harvest (10 kills):   +{gm.EffectiveSoulHarvestPct * 100:F2}% enemy HP → reward");
        if (gm.CursedBloodUnlocked)  sb.AppendLine($"  🩸 Cursed Blood (wave {GameManager.CursedBloodUnlockWave}+):  {(gm.CursedBloodEnabled ? "ON" : "OFF")}  ({GameManager.CursedBloodConversionRate * 100:F0}% dmg taken → blood)");
        sb.AppendLine();
        sb.AppendLine("── Achievement Bonuses ────────────");
        if (gm.AchievementBloodIncomeMult > 1.0)
            sb.AppendLine($"  Passive income:  ×{gm.AchievementBloodIncomeMult:F2}");
        if (gm.AchievementClickBonus > 0)
            sb.AppendLine($"  Click bonus:     +{gm.AchievementClickBonus:F1}/tap");
        if (gm.AchievementAttackBonus > 0)
            sb.AppendLine($"  Attack bonus:    +{gm.AchievementAttackBonus:F0}/soldier");
        int completedAchieves = 0;
        foreach (var d in GameManager.AchievementDefs) if ((gm.Achievements & d.Flag) != 0) completedAchieves++;
        sb.AppendLine($"  Completed:       {completedAchieves}/{GameManager.AchievementDefs.Length}");
        sb.AppendLine();
        sb.AppendLine("── Achievements ──────────────────");
        foreach (var d in GameManager.AchievementDefs)
        {
            bool done = (gm.Achievements & d.Flag) != 0;
            string prog = done ? "" : AchievementProgress(gm, d.Flag);
            sb.AppendLine($"  {(done ? "✓" : "○")}  {d.Title}{prog}");
        }

        statsText.text = sb.ToString();
    }

    static string AchievementProgress(GameManager gm, AchievementFlags f)
    {
        switch (f)
        {
            case AchievementFlags.Wave10:        return $"  [{gm.BestWave}/10]";
            case AchievementFlags.Wave25:        return $"  [{gm.BestWave}/25]";
            case AchievementFlags.Wave50:        return $"  [{gm.BestWave}/50]";
            case AchievementFlags.Wave100:       return $"  [{gm.BestWave}/100]";
            case AchievementFlags.Wave200:       return $"  [{gm.BestWave}/200]";
            case AchievementFlags.Wave500:       return $"  [{gm.BestWave}/500]";
            case AchievementFlags.Wave1000:      return $"  [{gm.BestWave}/1000]";
            case AchievementFlags.Blood1K:       return $"  [{GameManager.FormatNumber(gm.TotalBloodEarned)}/1K]";
            case AchievementFlags.Blood10K:      return $"  [{GameManager.FormatNumber(gm.TotalBloodEarned)}/10K]";
            case AchievementFlags.Blood100K:     return $"  [{GameManager.FormatNumber(gm.TotalBloodEarned)}/100K]";
            case AchievementFlags.BloodMillion:  return $"  [{GameManager.FormatNumber(gm.TotalBloodEarned)}/1M]";
            case AchievementFlags.BloodBillion:  return $"  [{GameManager.FormatNumber(gm.TotalBloodEarned)}/1B]";
            case AchievementFlags.BloodLegend:    return $"  [{GameManager.FormatNumber(gm.TotalBloodEarned)}/10B]";
            case AchievementFlags.BloodTrillion:  return $"  [{GameManager.FormatNumber(gm.TotalBloodEarned)}/1T]";
            case AchievementFlags.Untouchable:    return $"  [{gm.BestStreak}/10]";
            case AchievementFlags.StreakMaster:   return $"  [{gm.BestStreak}/10]";
            case AchievementFlags.StreakLegend:   return $"  [{gm.BestStreak}/25]";
            case AchievementFlags.BossSlayer:     return $"  [{gm.TotalBossesKilled}/25]";
            case AchievementFlags.BossHunter100:  return $"  [{gm.TotalBossesKilled}/100]";
            case AchievementFlags.SpellCaster:    return $"  [{gm.TotalSpellsCast}/50]";
            case AchievementFlags.GrandWizard:    return $"  [{gm.TotalSpellsCast}/500]";
            case AchievementFlags.SpellLord:      return $"  [{gm.TotalSpellsCast}/5000]";
            case AchievementFlags.Prestige3:      return $"  [{gm.PrestigeCount}/3]";
            case AchievementFlags.Prestige5:      return $"  [{gm.PrestigeCount}/5]";
            case AchievementFlags.Prestige10:     return $"  [{gm.PrestigeCount}/10]";
            case AchievementFlags.Prestige20:     return $"  [{gm.PrestigeCount}/20]";
            default: return "";
        }
    }

    // ── Toasts ────────────────────────────────────────────────────────────────

    void ShowAchievementToast(AchievementFlags flag)
    {
        string title = flag.ToString(), reward = "";
        foreach (var d in GameManager.AchievementDefs)
        {
            if (d.Flag != flag) continue;
            title = d.Title;
            if (d.BloodReward > 0) reward = $" (+{GameManager.FormatNumber(d.BloodReward)} blood)";
            else if (d.PPReward > 0) reward = " (+1 PP)";
            break;
        }
        StartCoroutine(ToastRoutine($"Achievement: {title}{reward}"));
    }

    void ShowMilestoneToast(string message) => StartCoroutine(ToastRoutine(message));

    IEnumerator ToastRoutine(string message)
    {
        if (achievementToast == null) yield break;
        achievementToastText.text = message;
#if DOTWEEN
        var rt = achievementToast.GetComponent<RectTransform>();
        rt.DOKill();
        rt.anchoredPosition = new Vector2(0f, -72f);
        achievementToast.SetActive(true);
        rt.DOAnchorPosY(12f, 0.38f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(3.2f);
        rt.DOAnchorPosY(-72f, 0.28f).SetEase(Ease.InQuad);
        yield return new WaitForSeconds(0.3f);
#else
        achievementToast.SetActive(true);
        yield return new WaitForSeconds(3f);
#endif
        achievementToast.SetActive(false);
    }

    // ── Offline Earnings ──────────────────────────────────────────────────────

    void ShowOfflinePanel()
    {
        var gm = GameManager.Instance;
        if (offlinePanel == null || gm == null) return;
        bool hasWood  = gm.OfflineWoodEarned    > 0;
        bool hasBlood = gm.OfflineBloodEarned   > 0;
        bool hasBank  = gm.OfflineBankInterest  > 0;
        if (!hasWood && !hasBlood && !hasBank) return;
        offlinePanel.SetActive(true);
        var sb = new StringBuilder("While you were away:\n");
        if (hasBlood) sb.AppendLine($"+{GameManager.FormatNumber(gm.OfflineBloodEarned)} blood");
        if (hasWood)  sb.AppendLine($"+{GameManager.FormatNumber(gm.OfflineWoodEarned)} wood");
        if (hasBank)  sb.Append($"+{GameManager.FormatNumber(gm.OfflineBankInterest)} bank interest");
        offlineText.text = sb.ToString();
    }

    public void DismissOfflinePanel()
    {
        if (offlinePanel != null) offlinePanel.SetActive(false);
        GameManager.Instance?.ClearOfflineEarnings();
    }

    public void DismissTutorial()
    {
        GameManager.Instance?.DismissTutorial();
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
            featureStatusText.text           = "Submissions unavailable — copy bloodidle_secrets.txt.sample → bloodidle_secrets.txt and add your GitHub PAT.";
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
                featureStatusText.text = code == 401 || code == 403
                    ? "Submission failed — PAT missing or expired. See setup instructions."
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
            if (voteIssueText  != null) voteIssueText.text  = _voteFetching ? "Loading..." : "No feature requests yet — submit one using the Suggest button!";
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
