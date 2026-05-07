#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

// All y coordinates: y=0 at TOP of canvas, increasing downward.
// Canvas reference: 1080 × 1920 portrait.
//
// Section map (y in px from top):
//   0–110   Header     — Blood | Wave | Wood
// 120–455   Enemy card
// 465–670   Army card
// 690–900   Farm Blood
// 910–1045  Action row — Buy Tank | Buy Berserker | Heal Self
// 1060–1225 Workers card
// 1240–1405 Barracks card
// 1415–1555 Blood Ritual card
// 1565–1705 Prestige card (visible at wave 20+)
// 1720–1820 Suggest button
// overlay   FeatureRequestOverlay — modal, hidden by default
public static class SceneBuilder
{
    const string OutSprites = "Assets/Resources/Sprites/";

    static Sprite s_Rounded;

    // ── Palette ──────────────────────────────────────────────────────────────
    static readonly Color BgBase   = HC("0B0B18");
    static readonly Color Surface1 = HC("161625");
    static readonly Color Crimson  = HC("D32F2F");
    static readonly Color Blue     = HC("1565C0");
    static readonly Color Purple   = HC("6A1B9A");
    static readonly Color Green    = HC("2E7D32");
    static readonly Color Brown    = HC("5D4037");
    static readonly Color Gold     = HC("F9A825");
    static readonly Color EHPFill  = HC("C62828");
    static readonly Color EHPBg    = HC("3D1010");
    static readonly Color SHPFill  = HC("2E7D32");
    static readonly Color SHPBg    = HC("0F2A10");
    static readonly Color TextSec  = HC("B0B0C8");
    static readonly Color DeepOrange = HC("BF360C");
    static readonly Color Amber    = HC("FF6F00");

    // ── Build ────────────────────────────────────────────────────────────────
    [MenuItem("IdleClicker/Setup Scene", priority = 1)]
    public static void BuildScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        s_Rounded = AssetDatabase.LoadAssetAtPath<Sprite>(OutSprites + "rounded_rect.png");

        // Camera
        var camGO = new GameObject("Main Camera");
        var cam   = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgBase;
        cam.orthographic    = true;
        camGO.AddComponent<AudioListener>();

        // EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();

        // Canvas
        var cv = new GameObject("Canvas");
        cv.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cv.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        cv.AddComponent<GraphicRaycaster>();

        // Background fill
        var bgGO = cv.CreateChild("Background");
        bgGO.AddImage(BgBase); bgGO.Stretch();

        // GameManager + components
        var gmGO = new GameObject("GameManager");
        gmGO.AddComponent<GameManager>();
        var uim = gmGO.AddComponent<UIManager>();
        var clk = gmGO.AddComponent<ClickManager>();

        // ── Scrollable content wrapper ───────────────────────────────────────
        var scrollGO = cv.CreateChild("ScrollView");
        scrollGO.AddComponent<Image>().color = Color.clear;
        scrollGO.Stretch();

        var viewportGO = scrollGO.CreateChild("Viewport");
        viewportGO.AddComponent<RectMask2D>();
        viewportGO.Stretch();

        var content    = viewportGO.CreateChild("Content");
        var contentImg = content.AddComponent<Image>();
        contentImg.color = Color.clear;
        contentImg.raycastTarget = false;
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin        = new Vector2(0f, 1f);
        contentRT.anchorMax        = new Vector2(1f, 1f);
        contentRT.pivot            = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta        = new Vector2(0, 1930);

        var scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.viewport          = viewportGO.GetComponent<RectTransform>();
        scroll.content           = contentRT;
        scroll.horizontal        = false;
        scroll.vertical          = true;
        scroll.scrollSensitivity = 10f;
        scroll.movementType      = ScrollRect.MovementType.Clamped;
        scroll.inertia           = true;
        scroll.decelerationRate  = 0.135f;

        // ════════════════════════════════════════════════════════════════════
        // HEADER  (y 0–110)
        // ════════════════════════════════════════════════════════════════════
        var headerBg = content.CreateChild("HeaderBg");
        headerBg.AddImage(HC("0F0E1E")); PF(headerBg, 0, 110);

        var bloodTextGO = Label(content, "BloodText",  "Blood: 0", 42, Crimson, TextAnchor.MiddleLeft);
        var waveTextGO  = Label(content, "WaveText",   "Wave 1",   56, Gold,    TextAnchor.MiddleCenter);
        var woodTextGO  = Label(content, "WoodText",   "Wood: 0",  38, HC("B8963E"), TextAnchor.MiddleRight);
        PT(bloodTextGO, 8, 94, -295, 370);
        PT(waveTextGO,  8, 94,    0, 300);
        PT(woodTextGO,  8, 94, +310, 370);

        var hDivGO = content.CreateChild("HeaderDiv");
        hDivGO.AddImage(HC("2D2D4A")); PF(hDivGO, 110, 2);

        // ════════════════════════════════════════════════════════════════════
        // ENEMY CARD  (y 120–455)
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "EnemyCardBg", 120, 335, Surface1, 24);

        var enemyImgGO = content.CreateChild("EnemyImage");
        var enemyImg   = enemyImgGO.AddComponent<Image>();
        enemyImg.color = Color.clear; enemyImg.preserveAspect = true;
        PT(enemyImgGO, 138, 148, -390, 162);

        var enemyNameGO = Label(content, "EnemyNameText", "Goblin", 58, Color.white, TextAnchor.MiddleLeft);
        var waveSubGO   = Label(content, "WaveSubText",   "Wave 1", 32, TextSec,     TextAnchor.MiddleLeft);
        PT(enemyNameGO, 138, 68, +72, 680);
        PT(waveSubGO,   210, 38, +72, 680);

        var (_, enemyHPFill) = HPBar(content, "EnemyHP", 274, 30, EHPBg, EHPFill);
        var enemyHPTextGO    = Label(content, "EnemyHPText", "100 / 100", 28, TextSec);
        PT(enemyHPTextGO, 310, 34, 0, 660);

        var bossTimerRowGO = content.CreateChild("BossTimerRow");
        bossTimerRowGO.AddComponent<RectTransform>();
        PF(bossTimerRowGO, 350, 38, 0);
        var bossTimerTextGO = Label(bossTimerRowGO, "BossTimerText",
            "⏱ 90s — defeat the boss or face the penalty!", 26,
            new Color(1f, 0.6f, 0.1f), TextAnchor.MiddleCenter);
        Stretch(bossTimerTextGO.GetComponent<RectTransform>());
        bossTimerRowGO.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // ARMY CARD  (y 465–670)
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "ArmyCardBg", 465, 205, Surface1, 24);

        var soldierCountGO = Label(content, "SoldierCountText",
            "No soldiers — buy one!  (max 10)", 34, TextSec);
        PF(soldierCountGO, 475, 48, 50);

        var soldierHPRowGO = content.CreateChild("SoldierHPRow");
        soldierHPRowGO.AddImage(Color.clear); PF(soldierHPRowGO, 527, 80, 40);

        var (_, soldierHPFill) = HPBar(soldierHPRowGO, "SoldierHPBar", 0, 28, SHPBg, SHPFill, stretch: true);

        var soldierHPTextGO = Label(soldierHPRowGO, "SoldierHPText",
            "Tank: 50 / 50 HP", 28, HC("81C784"));
        soldierHPTextGO.SetRT(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 22), new Vector2(700, 34));

        soldierHPRowGO.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // FARM BLOOD  (y 690–900)
        // ════════════════════════════════════════════════════════════════════
        var farmBtnGO = Btn(content, "FarmBloodButton", "FARM BLOOD", 90, Crimson);
        PT(farmBtnGO, 690, 210, 0, 680);

        // ════════════════════════════════════════════════════════════════════
        // ACTION ROW  (y 910–1045) — Buy Tank | Buy Berserker | Heal Self
        // ════════════════════════════════════════════════════════════════════
        var buyTankGO = Btn(content, "BuyTankButton",
            "Tank\n50HP  5atk\n10 blood", 32, Blue);
        PT(buyTankGO, 910, 130, -345, 320);

        var buyBerserkerGO = Btn(content, "BuyBerserkerButton",
            "Berserker\n25HP  12atk\n10 blood", 28, DeepOrange);
        PT(buyBerserkerGO, 910, 130, -10, 320);

        var healPanelGO = content.CreateChild("HealSelfPanel");
        healPanelGO.AddImage(Color.clear); PT(healPanelGO, 910, 130, +315, 310);

        var healBtnGO = Btn(healPanelGO, "HealSelfButton", "Heal Self  +20 HP\n25 blood", 36, Purple);
        healBtnGO.Stretch(); healPanelGO.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // WORKERS CARD  (y 1060–1225) — hidden until 200 blood earned
        // ════════════════════════════════════════════════════════════════════
        var workersPanel = content.CreateChild("WorkersPanel");
        workersPanel.AddImage(Color.clear);
        PF(workersPanel, 1060, 165);

        Panel(workersPanel, "WorkersCardBg", 0, 165, Surface1, 24);

        var workerInfoGO = Label(workersPanel, "WorkerInfoText", "Workers: 0",
            38, Color.white, TextAnchor.MiddleLeft);
        PT(workerInfoGO, 23, 52, -175, 500);

        var buyWorkerGO = Btn(workersPanel, "BuyWorkerButton", "Buy Worker\n50 blood", 34, Green);
        PT(buyWorkerGO, 13, 110, +232, 370);

        workersPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // BARRACKS CARD  (y 1240–1405)
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "BarracksCardBg", 1240, 165, Surface1, 24);

        var barracksInfoGO = Label(content, "BarracksInfoText",
            "Barracks  Lv.1  —  Max 10 soldiers",
            34, Color.white, TextAnchor.MiddleLeft);
        PT(barracksInfoGO, 1263, 52, -175, 540);

        var upgradeBarracksGO = Btn(content, "UpgradeBarracksButton", "Upgrade\n(20 wood)", 34, Brown);
        PT(upgradeBarracksGO, 1253, 110, +232, 370);

        // ════════════════════════════════════════════════════════════════════
        // BLOOD RITUAL CARD  (y 1415–1555) — same unlock as workers
        // ════════════════════════════════════════════════════════════════════
        var bloodRitualPanel = content.CreateChild("BloodRitualPanel");
        bloodRitualPanel.AddImage(Color.clear);
        PF(bloodRitualPanel, 1415, 140);

        Panel(bloodRitualPanel, "BloodRitualCardBg", 0, 140, Surface1, 24);

        var bloodRitualInfoGO = Label(bloodRitualPanel, "BloodRitualInfoText",
            "Blood Ritual  —  passive blood income",
            32, Color.white, TextAnchor.MiddleLeft);
        PT(bloodRitualInfoGO, 18, 52, -175, 500);

        var buyBloodRitualGO = Btn(bloodRitualPanel, "BuyBloodRitualButton",
            "Perform\n(30 wood)", 34, Purple);
        PT(buyBloodRitualGO, 10, 110, +232, 370);

        bloodRitualPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // PRESTIGE CARD  (y 1565–1705) — visible at wave 20+
        // ════════════════════════════════════════════════════════════════════
        var prestigePanel = content.CreateChild("PrestigePanel");
        prestigePanel.AddImage(Color.clear);
        PF(prestigePanel, 1565, 140);

        Panel(prestigePanel, "PrestigeCardBg", 0, 140, HC("1A0A00"), 24);

        var prestigeInfoGO = Label(prestigePanel, "PrestigeInfoText",
            "Prestige  —  reset for a blood bonus",
            32, Gold, TextAnchor.MiddleLeft);
        PT(prestigeInfoGO, 18, 52, -175, 500);

        var prestigeBtnGO = Btn(prestigePanel, "PrestigeButton",
            "PRESTIGE\n(reset progress)", 32, Amber);
        PT(prestigeBtnGO, 10, 110, +232, 370);

        prestigePanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // BOTTOM ROW  (y 1720–1820) — Stats | Suggest
        // ════════════════════════════════════════════════════════════════════
        var statsBtnGO = Btn(content, "StatsButton", "Statistics", 40, HC("00695C"));
        PT(statsBtnGO, 1720, 100, -175, 320);

        var suggestBtnGO = Btn(content, "SuggestButton", "Suggest a Feature", 36, HC("1565C0"));
        PT(suggestBtnGO, 1720, 100, +175, 320);

        // ── Damage number layer ───────────────────────────────────────────────
        var dmgLayerGO = cv.CreateChild("DamageLayer");
        var dmgImg     = dmgLayerGO.AddComponent<Image>();
        dmgImg.color         = Color.clear;
        dmgImg.raycastTarget = false;
        dmgLayerGO.Stretch();

        // ── Achievement toast (bottom strip, always on canvas) ───────────────
        var toastGO = cv.CreateChild("AchievementToast");
        RImg(toastGO, new Color(0.8f, 0.55f, 0f, 0.96f));
        var toastRT = toastGO.GetComponent<RectTransform>();
        toastRT.anchorMin        = new Vector2(0f, 0f);
        toastRT.anchorMax        = new Vector2(1f, 0f);
        toastRT.pivot            = new Vector2(0.5f, 0f);
        toastRT.anchoredPosition = new Vector2(0f, 12f);
        toastRT.sizeDelta        = new Vector2(0f, 64f);
        var toastTextGO = Label(toastGO, "AchievementToastText", "Achievement!", 34, Color.white);
        toastTextGO.Stretch();
        toastGO.SetActive(false);

        // ── Stats overlay (modal on canvas) ──────────────────────────────────
        var statsOverlay = cv.CreateChild("StatsPanel");
        statsOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
        statsOverlay.Stretch();

        var statsCard   = statsOverlay.CreateChild("Card");
        RImg(statsCard, Surface1);
        var statsCardRT = statsCard.GetComponent<RectTransform>();
        statsCardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        statsCardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        statsCardRT.anchoredPosition = Vector2.zero;
        statsCardRT.sizeDelta        = new Vector2(900, 840);

        var statsTitleGO = Label(statsCard, "StatsTitle", "Statistics", 52, Gold);
        PT(statsTitleGO, 22, 62, 0, 860);

        var statsDivGO = statsCard.CreateChild("StatsDiv");
        statsDivGO.AddImage(HC("2D2D4A")); PT(statsDivGO, 88, 2, 0, 860);

        var statsTextGO = Label(statsCard, "StatsText", "", 28, Color.white, TextAnchor.UpperLeft);
        statsTextGO.GetComponent<Text>().verticalOverflow   = VerticalWrapMode.Overflow;
        statsTextGO.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;
        PT(statsTextGO, 100, 640, 0, 840);

        var statsCloseGO = Btn(statsCard, "StatsCloseButton", "Close", 42, Crimson);
        PT(statsCloseGO, 754, 70, 0, 400);

        statsOverlay.SetActive(false);

        // ── Offline earnings modal ────────────────────────────────────────────
        var offlineOverlay = cv.CreateChild("OfflineEarningsPanel");
        offlineOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);
        offlineOverlay.Stretch();

        var offlineCard = offlineOverlay.CreateChild("Card");
        RImg(offlineCard, Surface1);
        var offlineCardRT        = offlineCard.GetComponent<RectTransform>();
        offlineCardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        offlineCardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        offlineCardRT.anchoredPosition = Vector2.zero;
        offlineCardRT.sizeDelta        = new Vector2(800, 380);

        var offlineTitleGO = Label(offlineCard, "OfflineTitle", "Welcome Back!", 52, Gold);
        PT(offlineTitleGO, 30, 70, 0, 720);

        var offlineTextGO = Label(offlineCard, "OfflineText", "", 38, Color.white);
        PT(offlineTextGO, 115, 90, 0, 720);

        var offlineDismissGO = Btn(offlineCard, "OfflineDismissButton", "Collect", 46, Crimson);
        PT(offlineDismissGO, 228, 110, 0, 400);

        offlineOverlay.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // FEATURE REQUEST OVERLAY
        // ════════════════════════════════════════════════════════════════════
        var overlay = cv.CreateChild("FeatureRequestOverlay");
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);
        overlay.Stretch();

        var card   = overlay.CreateChild("Card");
        RImg(card, Surface1);
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        cardRT.anchoredPosition = Vector2.zero;
        cardRT.sizeDelta        = new Vector2(960, 820);

        var cardTitleGO = Label(card, "CardTitle", "Suggest a Feature", 52, Crimson);
        PT(cardTitleGO, 22, 62, 0, 900);

        var cardDivGO = card.CreateChild("CardDiv");
        cardDivGO.AddImage(HC("2D2D4A")); PT(cardDivGO, 88, 2, 0, 880);

        var ftLabelGO = Label(card, "FTLabel", "Feature title  *", 28,
            TextSec, TextAnchor.MiddleLeft);
        PT(ftLabelGO, 100, 38, 4, 880);

        var titleField = InputWidget(card, "FeatureTitleInput", 142, 80,
            "e.g. More enemy types…");

        var fdLabelGO = Label(card, "FDLabel", "Description  (optional)", 28,
            TextSec, TextAnchor.MiddleLeft);
        PT(fdLabelGO, 234, 38, 4, 880);

        var descField = InputWidget(card, "FeatureDescInput", 278, 230,
            "Any extra details…", multiline: true);

        var featureSubmitGO = Btn(card, "FeatureSubmitButton", "Submit", 46, Crimson);
        PT(featureSubmitGO, 522, 110, 0, 880);

        var statusTextGO = Label(card, "FeatureStatusText", "", 30, HC("81C784"));
        PT(statusTextGO, 640, 44, 0, 880);

        var featureCancelGO = Btn(card, "FeatureCancelButton", "Cancel", 36, HC("252440"));
        PT(featureCancelGO, 692, 86, 0, 880);

        overlay.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // Wire UIManager
        // ════════════════════════════════════════════════════════════════════
        uim.bloodText               = bloodTextGO.GetComponent<Text>();
        uim.woodText                = woodTextGO.GetComponent<Text>();
        uim.waveText                = waveTextGO.GetComponent<Text>();
        uim.waveSubText             = waveSubGO.GetComponent<Text>();
        uim.enemyImage              = enemyImg;
        uim.enemySprites            = LoadEnemySprites();
        uim.enemyNameText           = enemyNameGO.GetComponent<Text>();
        uim.enemyHPFill             = enemyHPFill;
        uim.enemyHPText             = enemyHPTextGO.GetComponent<Text>();
        uim.bossTimerText           = bossTimerTextGO.GetComponent<Text>();
        uim.bossTimerRow            = bossTimerRowGO;
        uim.soldierCountText        = soldierCountGO.GetComponent<Text>();
        uim.soldierHPRow            = soldierHPRowGO;
        uim.soldierHPFill           = soldierHPFill;
        uim.soldierHPText           = soldierHPTextGO.GetComponent<Text>();
        uim.buyTankButton           = buyTankGO.GetComponent<Button>();
        uim.buyBerserkerButton      = buyBerserkerGO.GetComponent<Button>();
        uim.healSelfPanel           = healPanelGO;
        uim.healSelfButton          = healBtnGO.GetComponent<Button>();
        uim.workersPanel            = workersPanel;
        uim.workerInfoText          = workerInfoGO.GetComponent<Text>();
        uim.buyWorkerButton         = buyWorkerGO.GetComponent<Button>();
        uim.bloodRitualPanel        = bloodRitualPanel;
        uim.bloodRitualInfoText     = bloodRitualInfoGO.GetComponent<Text>();
        uim.buyBloodRitualButton    = buyBloodRitualGO.GetComponent<Button>();
        uim.bloodRitualCostText     = buyBloodRitualGO.GetComponentInChildren<Text>();
        uim.prestigePanel           = prestigePanel;
        uim.prestigeInfoText        = prestigeInfoGO.GetComponent<Text>();
        uim.prestigeButton          = prestigeBtnGO.GetComponent<Button>();
        uim.barracksInfoText        = barracksInfoGO.GetComponent<Text>();
        uim.upgradeBarracksButton   = upgradeBarracksGO.GetComponent<Button>();
        uim.barracksUpgradeCostText = upgradeBarracksGO.GetComponentInChildren<Text>();
        uim.damageLayer             = dmgLayerGO.GetComponent<RectTransform>();
        uim.offlinePanel            = offlineOverlay;
        uim.offlineText             = offlineTextGO.GetComponent<Text>();
        uim.statsPanel              = statsOverlay;
        uim.statsText               = statsTextGO.GetComponent<Text>();
        uim.achievementToast        = toastGO;
        uim.achievementToastText    = toastTextGO.GetComponent<Text>();
        uim.featureRequestPanel     = overlay;
        uim.featureTitleField       = titleField;
        uim.featureDescField        = descField;
        uim.featureStatusText       = statusTextGO.GetComponent<Text>();
        uim.featureSubmitButton     = featureSubmitGO.GetComponent<Button>();
        clk.uiManager               = uim;

        // Wire buttons
        UnityEventTools.AddPersistentListener(farmBtnGO.GetComponent<Button>().onClick,          clk.OnFarmBlood);
        UnityEventTools.AddPersistentListener(buyTankGO.GetComponent<Button>().onClick,          clk.OnBuyTank);
        UnityEventTools.AddPersistentListener(buyBerserkerGO.GetComponent<Button>().onClick,     clk.OnBuyBerserker);
        UnityEventTools.AddPersistentListener(healBtnGO.GetComponent<Button>().onClick,          clk.OnHealSelf);
        UnityEventTools.AddPersistentListener(buyWorkerGO.GetComponent<Button>().onClick,        clk.OnBuyWorker);
        UnityEventTools.AddPersistentListener(buyBloodRitualGO.GetComponent<Button>().onClick,   clk.OnBuyBloodRitual);
        UnityEventTools.AddPersistentListener(upgradeBarracksGO.GetComponent<Button>().onClick,  clk.OnUpgradeBarracks);
        UnityEventTools.AddPersistentListener(prestigeBtnGO.GetComponent<Button>().onClick,      clk.OnPrestige);
        UnityEventTools.AddPersistentListener(statsBtnGO.GetComponent<Button>().onClick,          clk.OnOpenStats);
        UnityEventTools.AddPersistentListener(suggestBtnGO.GetComponent<Button>().onClick,       clk.OnOpenSuggest);
        UnityEventTools.AddPersistentListener(statsCloseGO.GetComponent<Button>().onClick,       uim.HideStatsPanel);
        UnityEventTools.AddPersistentListener(offlineDismissGO.GetComponent<Button>().onClick,   uim.DismissOfflinePanel);
        UnityEventTools.AddPersistentListener(featureSubmitGO.GetComponent<Button>().onClick,    uim.SubmitFeature);
        UnityEventTools.AddPersistentListener(featureCancelGO.GetComponent<Button>().onClick,    uim.HideFeaturePanel);

        const string scenePath = "Assets/Scenes/MainScene.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        AssetDatabase.Refresh();
        Debug.Log("[IdleClicker] Scene built: " + scenePath);
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    static void PT(GameObject go, float topY, float h, float x, float w)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 1f);
        rt.anchorMax        = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, -(topY + h * 0.5f));
        rt.sizeDelta        = new Vector2(w, h);
    }

    static void PF(GameObject go, float topY, float h, float sidePad = 0)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(0, -(topY + h * 0.5f));
        rt.sizeDelta        = new Vector2(-sidePad * 2, h);
    }

    // ── Factory helpers ───────────────────────────────────────────────────────

    static Image RImg(GameObject go, Color color)
    {
        var img = go.AddComponent<Image>();
        img.color = color;
        if (s_Rounded != null) { img.sprite = s_Rounded; img.type = Image.Type.Sliced; }
        return img;
    }

    static void Panel(GameObject parent, string name, float topY, float h, Color bg, float sidePad = 0)
    {
        var go = parent.CreateChild(name);
        RImg(go, bg);
        PF(go, topY, h, sidePad);
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
        RImg(go, bgColor);
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

    static InputField InputWidget(GameObject parent, string name,
        float topY, float h, string placeholder, bool multiline = false)
    {
        var go = parent.CreateChild(name);
        var bg = go.AddComponent<Image>();
        bg.color = HC("1A1A30");
        if (s_Rounded != null) { bg.sprite = s_Rounded; bg.type = Image.Type.Sliced; }
        PT(go, topY, h, 0, 880);

        var phGO = go.CreateChild("Placeholder");
        var ph = phGO.AddComponent<Text>();
        phGO.Stretch();
        var phRT = phGO.GetComponent<RectTransform>();
        phRT.offsetMin = new Vector2(14, 6);
        phRT.offsetMax = new Vector2(-14, -6);
        ph.text      = placeholder;
        ph.color     = new Color(0.45f, 0.38f, 0.52f);
        ph.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ph.fontSize  = 30;
        ph.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;

        var txGO = go.CreateChild("Text");
        var tx = txGO.AddComponent<Text>();
        txGO.Stretch();
        var txRT = txGO.GetComponent<RectTransform>();
        txRT.offsetMin = new Vector2(14, 6);
        txRT.offsetMax = new Vector2(-14, -6);
        tx.color     = Color.white;
        tx.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tx.fontSize  = 30;
        tx.alignment = multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;

        var field = go.AddComponent<InputField>();
        field.targetGraphic = bg;
        field.textComponent = tx;
        field.placeholder   = ph;
        field.lineType      = multiline
            ? InputField.LineType.MultiLineNewline
            : InputField.LineType.SingleLine;
        return field;
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

    static Color HC(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
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
