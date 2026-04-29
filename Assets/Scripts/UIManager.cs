using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Resources")]
    public Text bloodText;
    public Text woodText;

    [Header("Enemy")]
    public Text waveText;
    public Text enemyNameText;
    public Image enemyHPFill;
    public Text enemyHPText;

    [Header("Soldiers")]
    public Text soldierCountText;
    public GameObject soldierHPRow;     // activated only when soldiers exist
    public Image soldierHPFill;
    public Text soldierHPText;
    public Button buySoldierButton;
    public GameObject healSelfPanel;    // hidden until HealSelf is unlocked
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
    public Text barracksUpgradeCostText; // Text component inside the upgrade button

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

        // Resources
        bloodText.text = $"Blood: {GameManager.FormatNumber(gm.Blood)}";
        woodText.text  = gm.WoodPerSecond > 0
            ? $"Wood: {GameManager.FormatNumber(gm.Wood)}  (+{gm.WoodPerSecond:F1}/s)"
            : $"Wood: {GameManager.FormatNumber(gm.Wood)}";

        // Enemy sprite
        if (enemyImage != null && enemySprites != null && enemySprites.Length > 0)
        {
            int idx = Mathf.Min(gm.Wave - 1, enemySprites.Length - 1);
            var spr = idx >= 0 ? enemySprites[idx] : null;
            enemyImage.sprite  = spr;
            enemyImage.color   = spr != null ? Color.white : Color.clear;
        }

        // Enemy
        waveText.text       = $"Wave {gm.Wave}";
        enemyNameText.text  = gm.EnemyName;
        enemyHPFill.fillAmount = gm.EnemyMaxHP > 0 ? gm.EnemyHP / gm.EnemyMaxHP : 0f;
        enemyHPText.text    = $"{GameManager.FormatHP(gm.EnemyHP)} / {GameManager.FormatHP(gm.EnemyMaxHP)}";

        // Soldiers
        bool hasSoldiers = gm.SoldierCount > 0;
        bool atCap       = gm.SoldierCount >= gm.MaxSoldiers;

        soldierCountText.text = hasSoldiers
            ? $"Soldiers: {gm.SoldierCount} / {gm.MaxSoldiers}"
            : $"No soldiers — buy one to fight!  (max {gm.MaxSoldiers})";

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

        // Workers
        workerInfoText.text     = $"Workers: {gm.WorkerCount}";
        buyWorkerButton.interactable = gm.Blood >= GameManager.WorkerCost;

        // Barracks
        barracksInfoText.text        = $"Barracks  Lv.{gm.BarracksLevel}  —  Max {gm.MaxSoldiers} soldiers";
        barracksUpgradeCostText.text = $"Upgrade Barracks\n({GameManager.FormatNumber(gm.BarracksUpgradeCost)} wood)";
        upgradeBarracksButton.interactable = gm.Wood >= gm.BarracksUpgradeCost;
    }
}
