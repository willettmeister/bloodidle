#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

public static class SceneBuilder
{
    static readonly Color BgDark       = new Color(0.06f, 0.03f, 0.08f);
    static readonly Color Crimson      = new Color(0.72f, 0.05f, 0.05f);
    static readonly Color EnemyBarBg   = new Color(0.28f, 0.05f, 0.05f);
    static readonly Color EnemyBarFill = new Color(0.85f, 0.10f, 0.10f);
    static readonly Color SoldierBg    = new Color(0.05f, 0.22f, 0.05f);
    static readonly Color SoldierFill  = new Color(0.10f, 0.78f, 0.10f);
    static readonly Color BlueBtn      = new Color(0.15f, 0.28f, 0.62f);
    static readonly Color PurpleBtn    = new Color(0.48f, 0.15f, 0.65f);
    static readonly Color GreenBtn     = new Color(0.15f, 0.45f, 0.15f);
    static readonly Color BrownBtn     = new Color(0.45f, 0.28f, 0.10f);
    static readonly Color DividerCol   = new Color(0.45f, 0.05f, 0.05f, 0.55f);

    [MenuItem("IdleClicker/Setup Scene", priority = 1)]
    public static void BuildScene()
    {
        Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ──────────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgDark;
        cam.orthographic = true;
        camGO.AddComponent<AudioListener>();

        // ── EventSystem ─────────────────────────────────────────────────────
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // ── Canvas ──────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = canvasGO.CreateChild("Background");
        bgGO.AddImage(BgDark);
        bgGO.Stretch();

        // ── GameManager object ───────────────────────────────────────────────
        var gmGO = new GameObject("GameManager");
        gmGO.AddComponent<GameManager>();
        var uim = gmGO.AddComponent<UIManager>();
        var clk = gmGO.AddComponent<ClickManager>();

        // ════════════════════════════════════════════════════════════════════
        // TOP — Enemy info (anchored to top)
        // ════════════════════════════════════════════════════════════════════
        var waveTextGO = CreateLabel(canvasGO, "WaveText", "Wave 1", 52,
            new Color(0.90f, 0.72f, 0.08f),
            Top, new Vector2(0f, -65f), new Vector2(600f, 56f));

        // Enemy sprite image (hidden until GenerateAssets has run)
        var enemyImgGO = canvasGO.CreateChild("EnemyImage");
        var enemyImg   = enemyImgGO.AddComponent<Image>();
        enemyImg.color          = Color.clear;
        enemyImg.preserveAspect = true;
        enemyImgGO.SetRT(Top, Top, new Vector2(0f, -138f), new Vector2(96f, 96f));

        var enemyNameGO = CreateLabel(canvasGO, "EnemyNameText", "Goblin", 72,
            Color.white,
            Top, new Vector2(0f, -205f), new Vector2(740f, 76f));

        var (_, enemyHPFill) = CreateHPBar(canvasGO, "EnemyHPBar",
            EnemyBarBg, EnemyBarFill,
            Top, new Vector2(0f, -262f), new Vector2(820f, 38f));

        var enemyHPTextGO = CreateLabel(canvasGO, "EnemyHPText", "100 / 100", 36,
            new Color(0.92f, 0.58f, 0.58f),
            Top, new Vector2(0f, -308f), new Vector2(600f, 40f));

        Divider(canvasGO, Top, new Vector2(0f, -338f));

        // ════════════════════════════════════════════════════════════════════
        // CENTER — Farm Blood + Blood + Wood
        // ════════════════════════════════════════════════════════════════════
        var farmBtnGO = CreateButton(canvasGO, "FarmBloodButton", "FARM BLOOD", 80,
            Crimson, Mid, new Vector2(0f, 160f), new Vector2(580f, 200f));

        var bloodTextGO = CreateLabel(canvasGO, "BloodText", "Blood: 0", 60,
            new Color(0.96f, 0.22f, 0.22f),
            Mid, new Vector2(0f, 30f), new Vector2(700f, 68f));

        var woodTextGO = CreateLabel(canvasGO, "WoodText", "Wood: 0", 50,
            new Color(0.72f, 0.52f, 0.18f),
            Mid, new Vector2(0f, -45f), new Vector2(700f, 58f));

        // ════════════════════════════════════════════════════════════════════
        // BOTTOM — Army / Workers / Barracks (all anchored to bottom)
        //
        // Layout from bottom up (y = distance from bottom edge):
        //   y= 60  Upgrade Barracks button      h=90
        //   y=165  Barracks info text            h=50
        //   y=205  ── divider ──
        //   y=290  Buy Worker button             h=90
        //   y=380  Worker info text              h=50
        //   y=420  ── divider ──
        //   y=495  Heal Self panel               h=90
        //   y=600  Buy Soldier button            h=100
        //   y=705  Soldier HP row                h=82
        //   y=795  Soldier count text            h=50
        //   y=840  ── divider ──
        // ════════════════════════════════════════════════════════════════════
        Divider(canvasGO, Bot, new Vector2(0f, 840f));

        var soldierCountGO = CreateLabel(canvasGO, "SoldierCountText",
            "No soldiers — buy one to fight!  (max 10)", 38, Color.white,
            Bot, new Vector2(0f, 795f), new Vector2(940f, 50f));

        // Soldier HP row — hidden when no soldiers present
        var soldierHPRowGO = canvasGO.CreateChild("SoldierHPRow");
        soldierHPRowGO.AddImage(Color.clear);
        soldierHPRowGO.SetRT(Bot, Bot, new Vector2(0f, 705f), new Vector2(940f, 82f));

        var (_, soldierHPFill) = CreateHPBar(soldierHPRowGO, "SoldierHPBar",
            SoldierBg, SoldierFill,
            Mid, new Vector2(0f, 20f), new Vector2(820f, 36f));

        var soldierHPTextGO = CreateLabel(soldierHPRowGO, "SoldierHPText",
            "Frontline: 50 / 50 HP", 36, new Color(0.55f, 0.92f, 0.55f),
            Mid, new Vector2(0f, -22f), new Vector2(720f, 38f));

        soldierHPRowGO.SetActive(false);

        var buySoldierGO = CreateButton(canvasGO, "BuySoldierButton",
            "Buy Soldier\n(10 blood)", 48,
            BlueBtn, Bot, new Vector2(0f, 600f), new Vector2(560f, 100f));

        // Heal Self — hidden until 50 total blood earned
        var healPanelGO = canvasGO.CreateChild("HealSelfPanel");
        healPanelGO.AddImage(Color.clear);
        healPanelGO.SetRT(Bot, Bot, new Vector2(0f, 495f), new Vector2(560f, 90f));

        var healBtnGO = CreateButton(healPanelGO, "HealSelfButton",
            "Heal Self (+20 HP)\n(25 blood)", 42,
            PurpleBtn, Mid, Vector2.zero, new Vector2(560f, 90f));

        healPanelGO.SetActive(false);

        // ── Worker section ───────────────────────────────────────────────────
        Divider(canvasGO, Bot, new Vector2(0f, 420f));

        var workerInfoGO = CreateLabel(canvasGO, "WorkerInfoText", "Workers: 0", 42,
            new Color(0.75f, 0.85f, 0.60f),
            Bot, new Vector2(0f, 380f), new Vector2(700f, 50f));

        var buyWorkerGO = CreateButton(canvasGO, "BuyWorkerButton",
            "Buy Worker — Farm Forest\n(50 blood)", 42,
            GreenBtn, Bot, new Vector2(0f, 290f), new Vector2(620f, 90f));

        // ── Barracks section ─────────────────────────────────────────────────
        Divider(canvasGO, Bot, new Vector2(0f, 205f));

        var barracksInfoGO = CreateLabel(canvasGO, "BarracksInfoText",
            "Barracks  Lv.1  —  Max 10 soldiers", 38,
            new Color(0.80f, 0.65f, 0.35f),
            Bot, new Vector2(0f, 165f), new Vector2(820f, 50f));

        var upgradeBarracksGO = CreateButton(canvasGO, "UpgradeBarracksButton",
            "Upgrade Barracks\n(20 wood)", 44,
            BrownBtn, Bot, new Vector2(0f, 60f), new Vector2(600f, 90f));

        // ════════════════════════════════════════════════════════════════════
        // Wire UIManager references
        // ════════════════════════════════════════════════════════════════════
        uim.enemyImage              = enemyImg;
        uim.enemySprites            = LoadEnemySprites();
        uim.bloodText               = bloodTextGO.GetComponent<Text>();
        uim.woodText                = woodTextGO.GetComponent<Text>();
        uim.waveText                = waveTextGO.GetComponent<Text>();
        uim.enemyNameText           = enemyNameGO.GetComponent<Text>();
        uim.enemyHPFill             = enemyHPFill;
        uim.enemyHPText             = enemyHPTextGO.GetComponent<Text>();
        uim.soldierCountText        = soldierCountGO.GetComponent<Text>();
        uim.soldierHPRow            = soldierHPRowGO;
        uim.soldierHPFill           = soldierHPFill;
        uim.soldierHPText           = soldierHPTextGO.GetComponent<Text>();
        uim.buySoldierButton        = buySoldierGO.GetComponent<Button>();
        uim.healSelfPanel           = healPanelGO;
        uim.healSelfButton          = healBtnGO.GetComponent<Button>();
        uim.workerInfoText          = workerInfoGO.GetComponent<Text>();
        uim.buyWorkerButton         = buyWorkerGO.GetComponent<Button>();
        uim.barracksInfoText        = barracksInfoGO.GetComponent<Text>();
        uim.upgradeBarracksButton   = upgradeBarracksGO.GetComponent<Button>();
        uim.barracksUpgradeCostText = upgradeBarracksGO.GetComponentInChildren<Text>();

        // Wire button listeners
        farmBtnGO.GetComponent<Button>().onClick.AddListener(clk.OnFarmBlood);
        buySoldierGO.GetComponent<Button>().onClick.AddListener(clk.OnBuySoldier);
        healBtnGO.GetComponent<Button>().onClick.AddListener(clk.OnHealSelf);
        buyWorkerGO.GetComponent<Button>().onClick.AddListener(clk.OnBuyWorker);
        upgradeBarracksGO.GetComponent<Button>().onClick.AddListener(clk.OnUpgradeBarracks);

        // ── Save ─────────────────────────────────────────────────────────────
        const string scenePath = "Assets/Scenes/MainScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        AssetDatabase.Refresh();
        Debug.Log("[IdleClicker] Scene built: " + scenePath);
    }

    // ── Anchor presets ───────────────────────────────────────────────────────
    static readonly Vector2 Top = new Vector2(0.5f, 1.0f);
    static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);
    static readonly Vector2 Bot = new Vector2(0.5f, 0.0f);

    // ── Helpers ──────────────────────────────────────────────────────────────
    static GameObject CreateLabel(GameObject parent, string name, string text,
        int fontSize, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = parent.CreateChild(name);
        var t  = go.AddComponent<Text>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.SetRT(anchor, anchor, pos, size);
        return go;
    }

    static GameObject CreateButton(GameObject parent, string name, string label,
        int fontSize, Color bgColor, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = parent.CreateChild(name);
        go.AddComponent<Image>().color = bgColor;
        go.AddComponent<Button>();
        go.SetRT(anchor, anchor, pos, size);

        var textGO = go.CreateChild("Text");
        var t      = textGO.AddComponent<Text>();
        t.text      = label;
        t.fontSize  = fontSize;
        t.color     = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textGO.Stretch();
        return go;
    }

    static (GameObject bg, Image fill) CreateHPBar(GameObject parent, string name,
        Color bgColor, Color fillColor, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var bg = parent.CreateChild(name);
        bg.AddComponent<Image>().color = bgColor;
        bg.SetRT(anchor, anchor, pos, size);

        var fillGO  = bg.CreateChild("Fill");
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color      = fillColor;
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        fillGO.Stretch();
        return (bg, fillImg);
    }

    static Sprite[] LoadEnemySprites()
    {
        string[] files = { "goblin", "orc_warrior", "cave_troll", "stone_ogre",
                           "demon_knight", "vampire_lord", "ancient_dragon" };
        var list = new System.Collections.Generic.List<Sprite>();
        foreach (var f in files)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Sprites/{f}.png");
            list.Add(s); // null when GenerateAssets hasn't run yet — UIManager handles gracefully
        }
        return list.ToArray();
    }

    static void Divider(GameObject parent, Vector2 anchor, Vector2 pos)
    {
        var go = parent.CreateChild("Divider");
        go.AddComponent<Image>().color = DividerCol;
        go.SetRT(anchor, anchor, pos, new Vector2(940f, 2f));
    }
}

// ── Extension helpers ────────────────────────────────────────────────────────
static class GOExt
{
    public static GameObject CreateChild(this GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    public static Image AddImage(this GameObject go, Color color)
    {
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    public static void SetRT(this GameObject go,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
    }

    public static void Stretch(this GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}
#endif
