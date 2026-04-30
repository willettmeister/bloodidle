#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

// All layout coordinates: y=0 at TOP of canvas, increasing downward.
// Canvas reference: 1080 × 1920 portrait.
//
// Section map (y in px from top):
//   0–105   Header bar  — Wave | Blood | Wood
// 115–440   Enemy card  — sprite + name + HP bar
// 450–660   Army panel  — soldier count + HP bar
// 675–895   Farm Blood  — main tap button
// 910–1050  Action row  — Buy Soldier | Heal Self (side-by-side)
// 1065–1230 Workers row — info left, Buy Worker right
// 1245–1410 Barracks row— info left, Upgrade right
public static class SceneBuilder
{
    // ── Palette ──────────────────────────────────────────────────────────────
    static readonly Color BgBase      = HC("0F0816");
    static readonly Color PanelEnemy  = HC("1A0508");
    static readonly Color PanelArmy   = HC("051408");
    static readonly Color PanelUpg    = HC("0A0812");
    static readonly Color HeaderBg    = HC("140A1E");
    static readonly Color Crimson     = HC("B80C0C");
    static readonly Color BlueBtn     = HC("1A3FA0");
    static readonly Color PurpleBtn   = HC("6E1FA0");
    static readonly Color GreenBtn    = HC("1A6B28");
    static readonly Color BrownBtn    = HC("6B4018");
    static readonly Color EnemyFgFill = HC("D81818");
    static readonly Color EnemyFgBg   = HC("481010");
    static readonly Color SoldFgFill  = HC("18C818");
    static readonly Color SoldFgBg    = HC("104810");

    // ── Build ────────────────────────────────────────────────────────────────
    [MenuItem("IdleClicker/Setup Scene", priority = 1)]
    public static void BuildScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGO = new GameObject("Main Camera");
        var cam   = camGO.AddComponent<Camera>();
        cam.clearFlags    = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgBase;
        cam.orthographic  = true;
        camGO.AddComponent<AudioListener>();

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // Canvas
        var cv = new GameObject("Canvas");
        cv.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cv.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        cv.AddComponent<GraphicRaycaster>();

        // Full-screen background
        var bg = cv.CreateChild("Background");
        bg.AddImage(BgBase); bg.Stretch();

        // GameManager
        var gmGO = new GameObject("GameManager");
        gmGO.AddComponent<GameManager>();
        var uim = gmGO.AddComponent<UIManager>();
        var clk = gmGO.AddComponent<ClickManager>();

        // ════════════════════════════════════════════════════════════════════
        // HEADER BAR  (y 0–105)
        // ════════════════════════════════════════════════════════════════════
        Panel(cv, "HeaderBg", 0, 105, HeaderBg);

        var waveTextGO  = Label(cv, "WaveText",  "Wave 1", 44, Tone(0.88f, 0.70f, 0.10f));
        var bloodTextGO = Label(cv, "BloodText", "Blood: 0", 46, Tone(0.95f, 0.22f, 0.22f));
        var woodTextGO  = Label(cv, "WoodText",  "Wood: 0",  42, Tone(0.72f, 0.54f, 0.18f));
        PT(waveTextGO,  14, 78, -335, 310); // left third
        PT(bloodTextGO, 14, 78,    0, 360); // center
        PT(woodTextGO,  14, 78, +335, 310); // right third

        // ════════════════════════════════════════════════════════════════════
        // ENEMY CARD  (y 115–440)
        // ════════════════════════════════════════════════════════════════════
        Panel(cv, "EnemyPanelBg", 115, 325, PanelEnemy);

        // Enemy sprite — left side of card
        var enemyImgGO = cv.CreateChild("EnemyImage");
        var enemyImg   = enemyImgGO.AddComponent<Image>();
        enemyImg.color = Color.clear; enemyImg.preserveAspect = true;
        PT(enemyImgGO, 128, 108, -415, 108);

        // Name + wave sub-label — right of sprite
        var enemyNameGO = Label(cv, "EnemyNameText", "Goblin", 62, Color.white, TextAnchor.MiddleLeft);
        PT(enemyNameGO, 130, 62, +75, 720);

        var waveSubGO = Label(cv, "WaveSubText", "Wave 1", 36,
            Tone(0.88f, 0.70f, 0.10f), TextAnchor.MiddleLeft);
        PT(waveSubGO, 198, 42, +75, 720);

        // HP bar (full width with padding)
        var (_, enemyHPFill) = HPBar(cv, "EnemyHP", 260, 34, EnemyFgBg, EnemyFgFill);

        var enemyHPTextGO = Label(cv, "EnemyHPText", "100 / 100", 34, Tone(0.90f, 0.58f, 0.58f));
        PT(enemyHPTextGO, 300, 38, 0, 660);

        // Thin accent line at bottom of card
        var accentGO = cv.CreateChild("EnemyAccent");
        accentGO.AddImage(Tone(0.55f, 0.08f, 0.08f));
        PF(accentGO, 430, 3, 40);

        // ════════════════════════════════════════════════════════════════════
        // ARMY PANEL  (y 450–660)
        // ════════════════════════════════════════════════════════════════════
        Panel(cv, "ArmyPanelBg", 450, 210, PanelArmy);

        var soldierCountGO = Label(cv, "SoldierCountText",
            "No soldiers — buy one to fight!  (max 10)", 38, Color.white);
        PF(soldierCountGO, 460, 48, 50);

        // Soldier HP row — hidden when no soldiers
        var soldierHPRowGO = cv.CreateChild("SoldierHPRow");
        soldierHPRowGO.AddImage(Color.clear);
        PF(soldierHPRowGO, 515, 82, 40);

        var (_, soldierHPFill) = HPBar(soldierHPRowGO, "SoldierHPBar", 0, 36,
            SoldFgBg, SoldFgFill, stretch: true);

        var soldierHPTextGO = Label(soldierHPRowGO, "SoldierHPText",
            "Frontline: 50 / 50 HP", 34, Tone(0.55f, 0.92f, 0.55f));
        // Positioned within container (centre-anchored)
        soldierHPTextGO.SetRT(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 24), new Vector2(700, 38));

        soldierHPRowGO.SetActive(false);

        // Thin accent line at bottom of panel
        var accentGO2 = cv.CreateChild("ArmyAccent");
        accentGO2.AddImage(Tone(0.10f, 0.42f, 0.10f));
        PF(accentGO2, 650, 3, 40);

        // ════════════════════════════════════════════════════════════════════
        // FARM BLOOD  (y 675–895)
        // ════════════════════════════════════════════════════════════════════
        var farmBtnGO = Btn(cv, "FarmBloodButton", "FARM BLOOD", 82, Crimson);
        PT(farmBtnGO, 675, 220, 0, 640);

        // ════════════════════════════════════════════════════════════════════
        // ACTION ROW  (y 910–1045)  — Buy Soldier | Heal Self side-by-side
        // ════════════════════════════════════════════════════════════════════
        var buySoldierGO = Btn(cv, "BuySoldierButton", "Buy Soldier\n(10 blood)", 42, BlueBtn);
        PT(buySoldierGO, 910, 135, -267, 506);

        var healPanelGO = cv.CreateChild("HealSelfPanel");
        healPanelGO.AddImage(Color.clear);
        PT(healPanelGO, 910, 135, +267, 506);

        var healBtnGO = Btn(healPanelGO, "HealSelfButton",
            "Heal Self (+20 HP)\n(25 blood)", 42, PurpleBtn);
        healBtnGO.Stretch();
        healPanelGO.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // WORKERS ROW  (y 1065–1230)
        // ════════════════════════════════════════════════════════════════════
        Panel(cv, "WorkersBg", 1065, 165, PanelUpg);

        var workerInfoGO = Label(cv, "WorkerInfoText", "Workers: 0",
            40, Tone(0.74f, 0.86f, 0.58f), TextAnchor.MiddleLeft);
        PT(workerInfoGO, 1090, 50, -175, 520);

        var buyWorkerGO = Btn(cv, "BuyWorkerButton",
            "Buy Worker\n(50 blood)", 38, GreenBtn);
        PT(buyWorkerGO, 1075, 115, +225, 380);

        // ════════════════════════════════════════════════════════════════════
        // BARRACKS ROW  (y 1245–1410)
        // ════════════════════════════════════════════════════════════════════
        Panel(cv, "BarracksBg", 1245, 165, PanelUpg);

        var barracksInfoGO = Label(cv, "BarracksInfoText",
            "Barracks  Lv.1  —  Max 10 soldiers",
            36, Tone(0.80f, 0.66f, 0.34f), TextAnchor.MiddleLeft);
        PT(barracksInfoGO, 1268, 50, -175, 560);

        var upgradeBarracksGO = Btn(cv, "UpgradeBarracksButton",
            "Upgrade Barracks\n(20 wood)", 38, BrownBtn);
        PT(upgradeBarracksGO, 1255, 115, +225, 380);

        // ════════════════════════════════════════════════════════════════════
        // Wire UIManager
        // ════════════════════════════════════════════════════════════════════
        uim.bloodText               = bloodTextGO.GetComponent<Text>();
        uim.woodText                = woodTextGO.GetComponent<Text>();
        uim.waveText                = waveTextGO.GetComponent<Text>();
        uim.enemyImage              = enemyImg;
        uim.enemySprites            = LoadEnemySprites();
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

        // Wire ClickManager
        farmBtnGO.GetComponent<Button>().onClick.AddListener(clk.OnFarmBlood);
        buySoldierGO.GetComponent<Button>().onClick.AddListener(clk.OnBuySoldier);
        healBtnGO.GetComponent<Button>().onClick.AddListener(clk.OnHealSelf);
        buyWorkerGO.GetComponent<Button>().onClick.AddListener(clk.OnBuyWorker);
        upgradeBarracksGO.GetComponent<Button>().onClick.AddListener(clk.OnUpgradeBarracks);

        // ── Also hide the duplicate wave display — UIManager drives waveText ──
        // waveSubGO mirrors waveText; wire it too so it stays in sync
        // We do this by assigning waveSubGO to the same field via a second Text ref
        // (UIManager only has one waveText; update the sub-label via the same code)
        // Store it as waveText's companion — simplest: overwrite enemy name's Text
        // Actually: keep waveSubGO as a second label; set it in Refresh().
        // Wire it to a spare exposed field we'll add:
        uim.waveSubText = waveSubGO.GetComponent<Text>();

        // Save
        const string scenePath = "Assets/Scenes/MainScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        AssetDatabase.Refresh();
        Debug.Log("[IdleClicker] Scene built: " + scenePath);
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    // Place element: center-horizontal anchor, y measured from TOP of canvas.
    static void PT(GameObject go, float topY, float h, float x, float w)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, -(topY + h * 0.5f));
        rt.sizeDelta        = new Vector2(w, h);
    }

    // Full-width stretch, y from TOP, optional side padding.
    static void PF(GameObject go, float topY, float h, float sidePad = 0)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(0, -(topY + h * 0.5f));
        rt.sizeDelta        = new Vector2(-sidePad * 2, h);
    }

    // ── Factory helpers ───────────────────────────────────────────────────────

    static void Panel(GameObject cv, string name, float topY, float h, Color bg)
    {
        var go = cv.CreateChild(name);
        go.AddImage(bg);
        PF(go, topY, h);
    }

    static GameObject Label(GameObject parent, string name, string text,
        int fontSize, Color color, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var go = parent.CreateChild(name);
        var t  = go.AddComponent<Text>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = color;
        t.alignment = align;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return go;
    }

    static GameObject Btn(GameObject parent, string name, string label,
        int fontSize, Color bgColor)
    {
        var go = parent.CreateChild(name);
        go.AddComponent<Image>().color = bgColor;
        go.AddComponent<Button>();

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

    // Returns (container, fillImage). If stretch=true, fills parent width.
    static (GameObject bg, Image fill) HPBar(GameObject parent, string name,
        float topY, float h, Color bgColor, Color fillColor, bool stretch = false)
    {
        var bg = parent.CreateChild(name + "Bg");
        bg.AddComponent<Image>().color = bgColor;
        if (stretch) PF(bg, topY, h);
        else         PF(bg, topY, h, 40);

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
            list.Add(AssetDatabase.LoadAssetAtPath<Sprite>(
                $"Assets/Resources/Sprites/{f}.png"));
        return list.ToArray();
    }

    // ── Color utilities ───────────────────────────────────────────────────────

    static Color HC(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    static Color Tone(float r, float g, float b) => new Color(r, g, b);
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
