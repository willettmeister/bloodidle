using System.Collections;
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

    [Header("Soldiers")]
    public Text soldierCountText;
    public GameObject soldierHPRow;
    public Image soldierHPFill;
    public Text soldierHPText;
    public Button buyTankButton;
    public Button buyBerserkerButton;
    public Button formationButton;
    public Text formationButtonText;
    public GameObject healSelfPanel;
    public Button healSelfButton;

    [Header("Blood Surge")]
    public GameObject bloodSurgePanel;
    public Text bloodSurgeInfoText;
    public Button bloodSurgeButton;

    [Header("Workers")]
    public GameObject workersPanel;
    public Text workerInfoText;
    public Button buyWorkerButton;
    public Button bloodPactButton;
    public Text bloodPactText;

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

    [Header("Offline Earnings")]
    public GameObject offlinePanel;
    public Text offlineText;

    [Header("Damage Numbers")]
    public RectTransform damageLayer;

    static readonly (AchievementFlags flag, string title)[] k_AchievDefs =
    {
        (AchievementFlags.FirstKill,     "First Blood"),
        (AchievementFlags.Wave10,        "Wave 10 Reached"),
        (AchievementFlags.Wave25,        "Wave 25 Reached"),
        (AchievementFlags.Blood1K,       "Blood Hoarder (1K)"),
        (AchievementFlags.Blood10K,      "Blood Baron (10K)"),
        (AchievementFlags.FirstSoldier,  "First Recruit"),
        (AchievementFlags.FullLegion,    "Full Legion"),
        (AchievementFlags.FirstRitual,   "Blood Ritualist"),
        (AchievementFlags.FirstPrestige, "Reborn in Blood"),
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
        woodText.text = gm.WoodPerSecond > 0
            ? $"Wood: {GameManager.FormatNumber(gm.Wood)}  +{gm.WoodPerSecond:F1}/s"
            : $"Wood: {GameManager.FormatNumber(gm.Wood)}";

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
            waveSubText.text = gm.IsBossWave
                ? "★ BOSS WAVE ★"
                : $"Boss in {gm.WavesUntilBoss} wave{(gm.WavesUntilBoss == 1 ? "" : "s")}";

        if (enemyModifierText != null)
        {
            enemyModifierText.text = gm.EnemyModifierDisplay;
            enemyModifierText.gameObject.SetActive(gm.CurrentEnemyModifier != EnemyModifier.None);
        }

        if (gm.IsBossWave)
        {
            enemyNameText.text  = $"☠ {gm.EnemyName} ☠";
            enemyNameText.color = new Color(1f, 0.84f, 0f);
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
        enemyHPText.text = $"{GameManager.FormatHP(gm.EnemyHP)} / {GameManager.FormatHP(gm.EnemyMaxHP)}";

        // Army
        bool hasSoldiers = gm.SoldierCount > 0;
        bool atCap       = gm.SoldierCount >= gm.MaxSoldiers;

        string compBonus = gm.IsAllTank ? "  ♦ Regen" : gm.IsAllBerserker ? "  ⚡ Crit" : "";
        soldierCountText.text = hasSoldiers
            ? $"Soldiers: {gm.SoldierCount}/{gm.MaxSoldiers}  [T:{gm.TankCount} B:{gm.BerserkerCount}]{compBonus}"
            : $"No soldiers — buy one!  (max {gm.MaxSoldiers})";

        soldierHPRow.SetActive(hasSoldiers);
        if (hasSoldiers)
        {
            soldierHPFill.fillAmount = gm.FrontlineMaxHP > 0 ? gm.SoldierHP / gm.FrontlineMaxHP : 0f;
            string cls = gm.FrontlineIsTank ? "Tank" : "Berserker";
            soldierHPText.text = $"{cls}: {GameManager.FormatHP(gm.SoldierHP)} / {GameManager.FormatHP(gm.FrontlineMaxHP)} HP";
        }

        if (formationButtonText != null)
            formationButtonText.text = gm.BerserkerFront ? "Formation: Berserker Front" : "Formation: Tank Front";

        bool canBuySoldier = gm.Blood >= GameManager.SoldierCost && !atCap;
        if (buyTankButton      != null) buyTankButton.interactable      = canBuySoldier;
        if (buyBerserkerButton != null) buyBerserkerButton.interactable = canBuySoldier;

        healSelfPanel.SetActive(gm.HealSelfUnlocked);
        if (gm.HealSelfUnlocked)
            healSelfButton.interactable = gm.Blood >= GameManager.HealSelfCost
                && hasSoldiers
                && gm.SoldierHP < gm.FrontlineMaxHP;

        // Blood Surge
        if (bloodSurgePanel != null) bloodSurgePanel.SetActive(gm.SurgeUnlocked);
        if (gm.SurgeUnlocked && bloodSurgeInfoText != null)
        {
            bloodSurgeInfoText.text = gm.SurgeActive
                ? $"Blood Surge  —  2× attack  {Mathf.CeilToInt(gm.SurgeTimeRemaining)}s remaining"
                : "Blood Surge  —  2× attack for 10s";
            if (bloodSurgeButton != null)
                bloodSurgeButton.interactable = !gm.SurgeActive
                    && gm.Blood >= GameManager.SurgeCost
                    && hasSoldiers;
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
                    ? $"Upgrade\n({GameManager.FormatNumber(gm.WeaponUpgradeCost)} wood)"
                    : "MAX";

            if (armorInfoText != null)
                armorInfoText.text = $"Armor  Lv.{gm.ArmorLevel}/{GameManager.MaxEquipLevel}  (+{gm.EquipArmorBonus:F0} HP)";
            if (upgradeArmorButton != null)
                upgradeArmorButton.interactable = gm.ArmorLevel < GameManager.MaxEquipLevel && gm.Wood >= gm.ArmorUpgradeCost;
            if (armorCostText != null)
                armorCostText.text = gm.ArmorLevel < GameManager.MaxEquipLevel
                    ? $"Upgrade\n({GameManager.FormatNumber(gm.ArmorUpgradeCost)} wood)"
                    : "MAX";

            if (talismanInfoText != null)
                talismanInfoText.text = $"Talisman  Lv.{gm.TalismanLevel}/{GameManager.MaxEquipLevel}  (+{gm.EquipTalismanBonus * 100:F0}% reward)";
            if (upgradeTalismanButton != null)
                upgradeTalismanButton.interactable = gm.TalismanLevel < GameManager.MaxEquipLevel && gm.Wood >= gm.TalismanUpgradeCost;
            if (talismanCostText != null)
                talismanCostText.text = gm.TalismanLevel < GameManager.MaxEquipLevel
                    ? $"Upgrade\n({GameManager.FormatNumber(gm.TalismanUpgradeCost)} wood)"
                    : "MAX";
        }

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
            prestigeInfoText.text = gm.PrestigeCount > 0
                ? $"Prestige Lv.{gm.PrestigeCount}  —  all blood ×{gm.PrestigeMultiplier:F2}"
                : $"Prestige  —  reset for ×{gm.PrestigeMultiplier + 0.5:F2} blood bonus";
            if (prestigeButton != null) prestigeButton.interactable = true;
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
            if (pSoldierCapButton != null)  pSoldierCapButton.interactable  = canSpend;
            if (pClickBonusInfoText != null)
                pClickBonusInfoText.text = $"Click Bonus +0.5  (Lv.{gm.PClickBonusLevel})";
            if (pClickBonusButton != null)  pClickBonusButton.interactable  = canSpend;
            if (pRitualEffInfoText != null)
                pRitualEffInfoText.text = $"Ritual Eff. +0.5/s  (Lv.{gm.PRitualEffLevel})";
            if (pRitualEffButton != null)   pRitualEffButton.interactable   = canSpend;
        }

        barracksInfoText.text        = $"Barracks  Lv.{gm.BarracksLevel}  —  Max {gm.MaxSoldiers} soldiers";
        barracksUpgradeCostText.text = $"Upgrade\n({GameManager.FormatNumber(gm.BarracksUpgradeCost)} wood)";
        upgradeBarracksButton.interactable = gm.Wood >= gm.BarracksUpgradeCost;
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

    void RefreshStats()
    {
        var gm = GameManager.Instance;
        if (gm == null || statsText == null) return;

        int h = (int)(gm.TimePlayed / 3600);
        int m = (int)((gm.TimePlayed % 3600) / 60);
        int s = (int)(gm.TimePlayed % 60);

        var sb = new StringBuilder();
        sb.AppendLine($"Enemies Defeated:  {gm.TotalEnemiesKilled}");
        sb.AppendLine($"Blood Earned:      {GameManager.FormatNumber(gm.TotalBloodEarned)}");
        sb.AppendLine($"Time Played:       {h}h {m}m {s}s");
        sb.AppendLine($"Prestige Level:    {gm.PrestigeCount}");
        sb.AppendLine();
        sb.AppendLine("── Achievements ──────────────────");
        foreach (var (flag, title) in k_AchievDefs)
            sb.AppendLine($"  {((gm.Achievements & flag) != 0 ? "✓" : "○")}  {title}");

        statsText.text = sb.ToString();
    }

    // ── Toasts ────────────────────────────────────────────────────────────────

    void ShowAchievementToast(AchievementFlags flag)
    {
        string title = flag.ToString();
        foreach (var (f, t) in k_AchievDefs)
            if (f == flag) { title = t; break; }
        StartCoroutine(ToastRoutine($"Achievement: {title}"));
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

    public void ShowFeaturePanel()
    {
        featureRequestPanel.SetActive(true);
        featureStatusText.text           = "";
        featureSubmitButton.interactable = true;
    }

    public void HideFeaturePanel()
    {
        featureRequestPanel.SetActive(false);
        featureTitleField.text           = "";
        featureDescField.text            = "";
        featureStatusText.text           = "";
        featureSubmitButton.interactable = true;
    }

    public void SubmitFeature()
    {
        string title = featureTitleField.text.Trim();
        if (title.Length == 0) { featureStatusText.text = "Please enter a title."; return; }
        StartCoroutine(PostIssue(title, featureDescField.text.Trim()));
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

        string body  = "**Community Request**\n\n" +
                       (rawBody.Length > 0 ? rawBody : "_No description provided._");
        string json  = "{\"title\":" + JStr(title) + ",\"body\":" + JStr(body) + "}";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);

        var req = new UnityWebRequest(k_GhApi, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization",        "Bearer " + token);
        req.SetRequestHeader("Accept",               "application/vnd.github+json");
        req.SetRequestHeader("Content-Type",         "application/json");
        req.SetRequestHeader("X-GitHub-Api-Version", "2022-11-28");
        req.SetRequestHeader("User-Agent",           "BloodIdle/1.0");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            featureStatusText.text = "Submitted! Thank you.";
            yield return new WaitForSeconds(2f);
            HideFeaturePanel();
        }
        else
        {
            featureStatusText.text           = "Failed — check your connection.";
            featureSubmitButton.interactable = true;
        }
    }

#if UNITY_INCLUDE_TESTS
    public static string JStrForTest(string s) => JStr(s);
#endif

    static string JStr(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\n", "\\n").Replace("\r", "\\r")
                .Replace("\t", "\\t") + "\"";
}
