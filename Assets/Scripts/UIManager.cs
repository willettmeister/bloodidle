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

    void Start()
    {
        GameManager.Instance.OnStateChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= Refresh;
    }

    void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null || bloodText == null) return;

        bloodText.text = $"Blood: {GameManager.FormatNumber(gm.Blood)}";
        woodText.text  = gm.WoodPerSecond > 0
            ? $"Wood: {GameManager.FormatNumber(gm.Wood)}  +{gm.WoodPerSecond:F1}/s"
            : $"Wood: {GameManager.FormatNumber(gm.Wood)}";

        if (enemyImage != null && enemySprites != null && enemySprites.Length > 0)
        {
            int idx = Mathf.Min(gm.Wave - 1, enemySprites.Length - 1);
            var spr = idx >= 0 ? enemySprites[idx] : null;
            enemyImage.sprite = spr;
            enemyImage.color  = spr != null ? Color.white : Color.clear;
        }

        waveText.text      = $"Wave {gm.Wave}";
        if (waveSubText != null) waveSubText.text = $"Wave {gm.Wave}";
        enemyNameText.text = gm.EnemyName;
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

        workerInfoText.text          = $"Workers: {gm.WorkerCount}";
        buyWorkerButton.interactable = gm.Blood >= GameManager.WorkerCost;

        barracksInfoText.text        = $"Barracks  Lv.{gm.BarracksLevel}  —  Max {gm.MaxSoldiers} soldiers";
        barracksUpgradeCostText.text = $"Upgrade\n({GameManager.FormatNumber(gm.BarracksUpgradeCost)} wood)";
        upgradeBarracksButton.interactable = gm.Wood >= gm.BarracksUpgradeCost;
    }

    // ── Feature Request ───────────────────────────────────────────────────────

    // Create a Fine-grained PAT at github.com/settings/tokens with
    // "Issues: Write" permission scoped to the bloodidle repo, then paste it here.
    const string k_GhToken = "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";
    const string k_GhApi   = "https://api.github.com/repos/willettmeister/bloodidle/issues";

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
        featureSubmitButton.interactable = false;
        featureStatusText.text           = "Submitting…";

        string body  = "**Community Request**\n\n" +
                       (rawBody.Length > 0 ? rawBody : "_No description provided._");
        string json  = "{\"title\":" + JStr(title) + ",\"body\":" + JStr(body) + "}";
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        var req = new UnityWebRequest(k_GhApi, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization",        "Bearer " + k_GhToken);
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

    static string JStr(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\n", "\\n").Replace("\r", "\\r")
                .Replace("\t", "\\t") + "\"";
}
