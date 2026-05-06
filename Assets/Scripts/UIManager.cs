using System;
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
    public Image enemyHPFill;
    public Text enemyHPText;

    [Header("Soldiers")]
    public Text soldierCountText;
    public GameObject soldierHPRow;
    public Image soldierHPFill;
    public Text soldierHPText;
    public Button buySoldierButton;
    public GameObject healSelfPanel;
    public Button healSelfButton;

    [Header("Workers")]
    public GameObject workersPanel;
    public Text workerInfoText;
    public Button buyWorkerButton;

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

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[UIManager] GameManager.Instance is null in Start() — subscription failed. Check scene setup.");
            return;
        }
        GameManager.Instance.OnStateChanged += Refresh;
        GameManager.Instance.OnDamageDealt  += SpawnDamageNumber;
        Refresh();
        ShowOfflinePanel();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= Refresh;
            GameManager.Instance.OnDamageDealt  -= SpawnDamageNumber;
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

        bloodText.text = $"Blood: {GameManager.FormatNumber(gm.Blood)}";
        woodText.text  = gm.WoodPerSecond > 0
            ? $"Wood: {GameManager.FormatNumber(gm.Wood)}  +{gm.WoodPerSecond:F1}/s"
            : $"Wood: {GameManager.FormatNumber(gm.Wood)}";

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

        if (gm.IsBossWave)
        {
            enemyNameText.text  = $"☠ {gm.EnemyName} ☠";
            enemyNameText.color = new Color(1f, 0.84f, 0f);   // gold
        }
        else
        {
            enemyNameText.text  = gm.EnemyName;
            enemyNameText.color = Color.white;
        }
        enemyHPFill.fillAmount = gm.EnemyMaxHP > 0 ? gm.EnemyHP / gm.EnemyMaxHP : 0f;
        enemyHPText.text   = $"{GameManager.FormatHP(gm.EnemyHP)} / {GameManager.FormatHP(gm.EnemyMaxHP)}";

        bool hasSoldiers = gm.SoldierCount > 0;
        bool atCap       = gm.SoldierCount >= gm.MaxSoldiers;

        soldierCountText.text = hasSoldiers
            ? $"Soldiers: {gm.SoldierCount} / {gm.MaxSoldiers}"
            : $"No soldiers — buy one!  (max {gm.MaxSoldiers})";

        soldierHPRow.SetActive(hasSoldiers);
        if (hasSoldiers)
        {
            soldierHPFill.fillAmount = gm.SoldierHP / GameManager.SoldierMaxHP;
            soldierHPText.text = $"Frontline: {GameManager.FormatHP(gm.SoldierHP)} / {GameManager.FormatHP(GameManager.SoldierMaxHP)} HP";
        }

        buySoldierButton.interactable = gm.Blood >= GameManager.SoldierCost && !atCap;

        healSelfPanel.SetActive(gm.HealSelfUnlocked);
        if (gm.HealSelfUnlocked)
            healSelfButton.interactable = gm.Blood >= GameManager.HealSelfCost
                && hasSoldiers
                && gm.SoldierHP < GameManager.SoldierMaxHP;

        if (workersPanel != null) workersPanel.SetActive(gm.WorkersUnlocked);
        workerInfoText.text          = $"Workers: {gm.WorkerCount}";
        buyWorkerButton.interactable = gm.Blood >= GameManager.WorkerCost;

        barracksInfoText.text        = $"Barracks  Lv.{gm.BarracksLevel}  —  Max {gm.MaxSoldiers} soldiers";
        barracksUpgradeCostText.text = $"Upgrade\n({GameManager.FormatNumber(gm.BarracksUpgradeCost)} wood)";
        upgradeBarracksButton.interactable = gm.Wood >= gm.BarracksUpgradeCost;
    }

    // ── Offline Earnings ──────────────────────────────────────────────────────

    void ShowOfflinePanel()
    {
        var gm = GameManager.Instance;
        if (offlinePanel == null || gm == null || gm.OfflineWoodEarned <= 0) return;
        offlinePanel.SetActive(true);
        offlineText.text = $"While you were away:\n+{GameManager.FormatNumber(gm.OfflineWoodEarned)} wood earned";
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

    // Token is loaded from Assets/Resources/bloodidle_secrets.txt (gitignored).
    // Copy bloodidle_secrets.txt.sample → bloodidle_secrets.txt and paste your PAT.
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
        byte[] bytes = Encoding.UTF8.GetBytes(json);

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
