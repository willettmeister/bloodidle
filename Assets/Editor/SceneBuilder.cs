#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;

// All y coordinates: y=0 at TOP of each tab's scroll content, increasing downward.
// Canvas reference: 1080 × 1920 portrait.
// Fixed header: 0–110 (blood/wave/wood) — anchored to canvas top
// Tab content area: 110–1820 (1710px visible)
// Fixed tab bar: 1820–1920 — 4 tabs (Battle | Build | Progress | Settings)
//
// ── BATTLE TAB (y in battleContent) ─────────────────────────────────────────
//   10–345   Enemy card
//  355–415   Daily Challenge row
//  425–805   Army card  [Formation, MixedBonus, UpgradeHeal, Corruption]
//  815–1025  Farm Blood
// 1035–1165  Action row — Tank | Berserker | Paladin | Heal Self
// 1175–1995  Blood Surge card (Surge + SoulSac + Storm + Oath + WarCry + HexCurse + BloodShield)
// battleContent height: 2020
//
// ── BUILD TAB (y in buildContent) ───────────────────────────────────────────
//   10–175   Barracks card
//  185–350   Fortifications card
//  360–865   Workers card (hidden until WorkersUnlocked)
//  875–1120  Equipment card (hidden until WorkersUnlocked)
// 1130–1345  Blood Ritual + Blood Pact (hidden until WorkersUnlocked)
// 1355–1520  Blood Bank card
// buildContent height: 1540
//
// ── PROGRESS TAB (y in progressContent) ─────────────────────────────────────
//   10–150   Prestige card (hidden until wave 20)
//  160–645   Prestige Shop (hidden until prestige 1)
//  655–1035  Soul Shard Shop (hidden until first boss)
// 1045–1110  Daily Quests row
// 1120–1185  Watch Ad row
// progressContent height: 1200
//
// ── SETTINGS TAB (y in settingsContent) ─────────────────────────────────────
//   10–240   Utility buttons (Stats | Settings row, Suggest | Shop row)
//  250–330   blank / future
// settingsContent height: 400
//
// overlay  StatsPanel, SettingsPanel, IAPShopPanel, QuestsPanel, FeatureRequestOverlay
public static class SceneBuilder
{
    const string OutSprites = "Assets/Resources/Sprites/";

    static Sprite s_Rounded;
    static Sprite s_Background;

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
    static readonly Color Teal     = HC("00695C");

    // ── Build ────────────────────────────────────────────────────────────────
    [MenuItem("IdleClicker/Setup Scene", priority = 1)]
    public static void BuildScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        s_Rounded = AssetDatabase.LoadAssetAtPath<Sprite>(OutSprites + "rounded_rect.png");

        // Ensure background.png is imported as a sprite (in case GenerateAssets hasn't been re-run)
        var bgPath = OutSprites + "background.png";
        var bgImp  = AssetImporter.GetAtPath(bgPath) as TextureImporter;
        if (bgImp != null && bgImp.textureType != TextureImporterType.Sprite)
        {
            bgImp.textureType      = TextureImporterType.Sprite;
            bgImp.filterMode       = FilterMode.Bilinear;
            bgImp.mipmapEnabled    = false;
            bgImp.spriteImportMode = SpriteImportMode.Single;
            bgImp.SaveAndReimport();
        }
        s_Background = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);

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

        // Background fill — use generated background sprite if available, else solid color
        var bgGO  = cv.CreateChild("Background");
        var bgImg = bgGO.AddImage(BgBase);
        if (s_Background != null)
        {
            bgImg.sprite              = s_Background;
            bgImg.type                = Image.Type.Simple;
            bgImg.preserveAspect = false;
            bgImg.color               = Color.white;
        }
        bgGO.Stretch();

        // GameManager + components
        var gmGO = new GameObject("GameManager");
        gmGO.AddComponent<GameManager>();
        gmGO.AddComponent<IAPManager>();
        gmGO.AddComponent<AdsManager>();
        var uim = gmGO.AddComponent<UIManager>();
        var clk = gmGO.AddComponent<ClickManager>();

        // ── Fixed header (blood / wave / wood) ──────────────────────────────
        var headerFixedBg = cv.CreateChild("HeaderBg");
        headerFixedBg.AddImage(HC("0F0E1E"));
        {
            var rt = headerFixedBg.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, 110f);
        }

        // ── Tab content area (fills between header and tab bar) ──────────────
        var tabAreaGO = cv.CreateChild("TabArea");
        tabAreaGO.AddComponent<Image>().color = Color.clear;
        {
            var rt          = tabAreaGO.GetComponent<RectTransform>();
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.pivot        = new Vector2(0.5f, 0.5f);
            rt.offsetMin    = new Vector2(0f, 100f);   // above tab bar
            rt.offsetMax    = new Vector2(0f, -110f);  // below header
        }

        // Helper: create a ScrollView inside tabArea, return its content GO
        static (GameObject scrollGO, GameObject content) MakeTabScroll(
            GameObject parent, string name, float contentHeight)
        {
            var sg = parent.CreateChild(name);
            sg.AddComponent<Image>().color = Color.clear;
            sg.Stretch();

            var vp = sg.CreateChild("Viewport");
            vp.AddComponent<RectMask2D>();
            vp.Stretch();

            var cnt    = vp.CreateChild("Content");
            var cntImg = cnt.AddComponent<Image>();
            cntImg.color = Color.clear;
            cntImg.raycastTarget = false;
            var cntRT          = cnt.GetComponent<RectTransform>();
            cntRT.anchorMin        = new Vector2(0f, 1f);
            cntRT.anchorMax        = new Vector2(1f, 1f);
            cntRT.pivot            = new Vector2(0.5f, 1f);
            cntRT.anchoredPosition = Vector2.zero;
            cntRT.sizeDelta        = new Vector2(0f, contentHeight);

            var sr               = sg.AddComponent<ScrollRect>();
            sr.viewport          = vp.GetComponent<RectTransform>();
            sr.content           = cntRT;
            sr.horizontal        = false;
            sr.vertical          = true;
            sr.scrollSensitivity = 10f;
            sr.movementType      = ScrollRect.MovementType.Clamped;
            sr.inertia           = true;
            sr.decelerationRate  = 0.135f;

            return (sg, cnt);
        }

        var (battleScrollGO,   battleContent)   = MakeTabScroll(tabAreaGO, "BattleTab",   2020f);
        var (buildScrollGO,    buildContent)    = MakeTabScroll(tabAreaGO, "BuildTab",    1540f);
        var (progressScrollGO, progressContent) = MakeTabScroll(tabAreaGO, "ProgressTab", 1200f);
        var (settingsScrollGO, settingsContent) = MakeTabScroll(tabAreaGO, "SettingsTab", 400f);

        buildScrollGO.SetActive(false);
        progressScrollGO.SetActive(false);
        settingsScrollGO.SetActive(false);

        // Convenience alias so all the section code below can use "content" for the
        // correct tab. We switch this alias as we move between tabs.
        GameObject content = battleContent;

        // ════════════════════════════════════════════════════════════════════
        // HEADER  (fixed on canvas, 0–110)
        // ════════════════════════════════════════════════════════════════════
        var bloodTextGO = Label(headerFixedBg, "BloodText",  "Blood: 0", 42, Crimson, TextAnchor.MiddleLeft);
        var waveTextGO  = Label(headerFixedBg, "WaveText",   "Wave 1",   56, Gold,    TextAnchor.MiddleCenter);
        var woodTextGO  = Label(headerFixedBg, "WoodText",   "Wood: 0",  38, HC("B8963E"), TextAnchor.MiddleRight);
        PT(bloodTextGO, 8, 94, -295, 370);
        PT(waveTextGO,  8, 94,    0, 300);
        PT(woodTextGO,  8, 94, +310, 370);

        var hDivGO = headerFixedBg.CreateChild("HeaderDiv");
        hDivGO.AddImage(HC("2D2D4A")); PF(hDivGO, 108, 2);

        // ════════════════════════════════════════════════════════════════════
        // ENEMY CARD  (battleContent y 10–345)
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "EnemyCardBg", 10, 335, Surface1, 24);
        { var a = content.CreateChild("EnemyCardAccent"); a.AddImage(HC("8B0000")); PF(a, 10, 4, 24); }

        var enemyImgGO = content.CreateChild("EnemyImage");
        var enemyImg   = enemyImgGO.AddComponent<Image>();
        enemyImg.color = Color.clear; enemyImg.preserveAspect = true;
        PT(enemyImgGO, 28, 148, -390, 162);

        var enemyNameGO = Label(content, "EnemyNameText", "Goblin", 58, Color.white, TextAnchor.MiddleLeft);
        var waveSubGO   = Label(content, "WaveSubText",   "Wave 1", 32, TextSec,     TextAnchor.MiddleLeft);
        PT(enemyNameGO, 28, 68, +72, 680);
        PT(waveSubGO,   100, 38, +72, 680);

        var (_, enemyHPFill) = HPBar(content, "EnemyHP", 164, 30, EHPBg, EHPFill);
        var enemyHPTextGO    = Label(content, "EnemyHPText", "100 / 100", 28, TextSec);
        PT(enemyHPTextGO, 200, 34, 0, 660);

        var bossTimerRowGO = content.CreateChild("BossTimerRow");
        bossTimerRowGO.AddComponent<RectTransform>();
        PF(bossTimerRowGO, 240, 38, 0);
        var bossTimerTextGO = Label(bossTimerRowGO, "BossTimerText",
            "⏱ 90s — defeat the boss or face the penalty!", 26,
            new Color(1f, 0.6f, 0.1f), TextAnchor.MiddleCenter);
        bossTimerTextGO.Stretch();
        bossTimerRowGO.SetActive(false);

        var enemyModifierTextGO = Label(content, "EnemyModifierText", "", 30,
            new Color(1f, 0.65f, 0.1f), TextAnchor.MiddleCenter);
        PF(enemyModifierTextGO, 283, 38, 60);
        enemyModifierTextGO.SetActive(false);

        // Wave preview overlay — sits on top of enemy card, hidden until preview starts
        var wavePreviewBannerGO = content.CreateChild("WavePreviewBanner");
        var wpImg = wavePreviewBannerGO.AddComponent<Image>();
        wpImg.color = new Color(0f, 0f, 0f, 0.84f);
        PF(wavePreviewBannerGO, 10, 335, 30);
        var wavePreviewTextGO = Label(wavePreviewBannerGO, "WavePreviewText",
            "Wave 2 incoming...", 48, Gold, TextAnchor.MiddleCenter);
        wavePreviewTextGO.Stretch();
        wavePreviewBannerGO.SetActive(false);

        // Daily challenge row  (battleContent y 355–415)
        var dailyChallengeRowGO = content.CreateChild("DailyChallengeRow");
        dailyChallengeRowGO.AddImage(HC("0A1A00")); PF(dailyChallengeRowGO, 355, 60, 20);

        var dailyChallengeInfoGO = Label(dailyChallengeRowGO, "DailyChallengeInfoText",
            "Daily Challenge: 5× HP enemy  —  ×5 blood!", 30, Gold, TextAnchor.MiddleLeft);
        PT(dailyChallengeInfoGO, 4, 52, -100, 640);

        var dailyChallengeBtnGO = Btn(dailyChallengeRowGO, "DailyChallengeButton", "Start!", 32, Amber);
        PT(dailyChallengeBtnGO, 4, 52, +400, 140);
        dailyChallengeRowGO.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // ARMY CARD  (battleContent y 425–805)
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "ArmyCardBg", 425, 380, Surface1, 24);
        { var a = content.CreateChild("ArmyCardAccent"); a.AddImage(HC("1B5E20")); PF(a, 425, 4, 24); }

        var soldierCountGO = Label(content, "SoldierCountText",
            "No soldiers — buy one!  (max 10)", 34, TextSec);
        PF(soldierCountGO, 435, 48, 50);

        var soldierHPRowGO = content.CreateChild("SoldierHPRow");
        soldierHPRowGO.AddImage(Color.clear); PF(soldierHPRowGO, 487, 80, 40);

        var (_, soldierHPFill) = HPBar(soldierHPRowGO, "SoldierHPBar", 0, 28, SHPBg, SHPFill, stretch: true);

        var soldierHPTextGO = Label(soldierHPRowGO, "SoldierHPText",
            "Tank: 50 / 50 HP", 28, HC("81C784"));
        soldierHPTextGO.SetRT(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 22), new Vector2(700, 34));

        soldierHPRowGO.SetActive(false);

        var formationBtnGO = Btn(content, "FormationButton", "Formation: Tank Front", 28, HC("1A1A3A"));
        PF(formationBtnGO, 578, 38, 40);

        var mixedBonusGO = Label(content, "MixedBonusText",
            "Mixed Formation: −15% incoming damage", 26, new Color(0.6f, 0.9f, 0.6f), TextAnchor.MiddleCenter);
        PF(mixedBonusGO, 620, 34, 50);
        mixedBonusGO.SetActive(false);

        var upgradeHealBtnGO = Btn(content, "UpgradeHealSelfButton", "Upgrade Heal\n(40 blood)", 28, Purple);
        PF(upgradeHealBtnGO, 657, 64, 40);

        var corruptionTextGO = Label(content, "CorruptionText", "", 28, new Color(0.8f, 0.2f, 0.2f), TextAnchor.MiddleLeft);
        PF(corruptionTextGO, 725, 30, 50);
        corruptionTextGO.SetActive(false);

        var purifyBtnGO = Btn(content, "PurifyButton", "Purify\n(3 shards)", 28, HC("4A0A0A"));
        PT(purifyBtnGO, 757, 38, +232, 280);
        purifyBtnGO.SetActive(false);

        var desecrateBtnGO = Btn(content, "DesecrateButton", "Desecrate\n(-1 corrupt +50% burst)", 26, HC("3A006A"));
        PT(desecrateBtnGO, 757, 38, 0, 300);
        desecrateBtnGO.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // FARM BLOOD  (battleContent y 815–1025)
        // ════════════════════════════════════════════════════════════════════
        var farmBtnGO = Btn(content, "FarmBloodButton", "FARM BLOOD", 90, Crimson);
        PT(farmBtnGO, 815, 210, 0, 680);

        // ════════════════════════════════════════════════════════════════════
        // ACTION ROW  (battleContent y 1035–1165) — Tank | Berserker | Paladin | Heal Self
        // ════════════════════════════════════════════════════════════════════
        var buyTankGO = Btn(content, "BuyTankButton",
            "Tank\n50HP  5atk\n10 blood", 28, Blue);
        PT(buyTankGO, 1035, 130, -393, 230);

        var buyBerserkerGO = Btn(content, "BuyBerserkerButton",
            "Berserker\n25HP  12atk\n10 blood", 24, DeepOrange);
        PT(buyBerserkerGO, 1035, 130, -131, 230);

        var buyPaladinGO = Btn(content, "BuyPaladinButton",
            "Paladin\n20HP  3atk\nHealer\n10 blood", 24, HC("00695C"));
        PT(buyPaladinGO, 1035, 130, +131, 230);

        var healPanelGO = content.CreateChild("HealSelfPanel");
        healPanelGO.AddImage(Color.clear); PT(healPanelGO, 1035, 130, +393, 230);

        var healBtnGO = Btn(healPanelGO, "HealSelfButton", "Heal Self  +20 HP\n25 blood", 30, Purple);
        healBtnGO.Stretch(); healPanelGO.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // BLOOD SURGE CARD  (battleContent y 1175–1895) — shown after 500 blood earned
        // ════════════════════════════════════════════════════════════════════
        var bloodSurgePanel = content.CreateChild("BloodSurgePanel");
        bloodSurgePanel.AddImage(Color.clear);
        PF(bloodSurgePanel, 1175, 820);

        Panel(bloodSurgePanel, "BloodSurgeCardBg", 0, 820, Surface1, 24);
        { var a = bloodSurgePanel.CreateChild("SurgeAccent"); a.AddImage(Crimson); PF(a, 0, 4, 24); }

        var bloodSurgeInfoGO = Label(bloodSurgePanel, "BloodSurgeInfoText",
            "Blood Surge  —  2× attack for 10s",
            32, Color.white, TextAnchor.MiddleLeft);
        PT(bloodSurgeInfoGO, 18, 52, -175, 500);

        var surgeBtnGO = Btn(bloodSurgePanel, "BloodSurgeButton",
            "Surge!\n(50 blood)", 34, Crimson);
        PT(surgeBtnGO, 10, 110, +232, 370);

        var upgradeSurgeBtnGO = Btn(bloodSurgePanel, "UpgradeSurgeButton",
            "Upgrade Surge\n(60 blood)", 28, HC("8E2424"));
        PF(upgradeSurgeBtnGO, 128, 64, 40);

        var surgeDivGO = bloodSurgePanel.CreateChild("SurgeDiv");
        surgeDivGO.AddImage(HC("2D2D4A")); PT(surgeDivGO, 202, 2, 0, 640);

        var soulSacInfoGO = Label(bloodSurgePanel, "SoulSacrificeInfoText",
            "Soul Sacrifice  —  lose 1 soldier → ×10 blood",
            30, new Color(0.9f, 0.5f, 0.1f), TextAnchor.MiddleLeft);
        PT(soulSacInfoGO, 212, 48, -140, 620);

        var soulSacBtnGO = Btn(bloodSurgePanel, "SoulSacrificeButton",
            "Sacrifice!", 32, HC("4A1A00"));
        PT(soulSacBtnGO, 268, 28, 0, 680);

        var stormDivGO = bloodSurgePanel.CreateChild("BloodStormDiv");
        stormDivGO.AddImage(HC("2D2D4A")); PT(stormDivGO, 306, 2, 0, 640);

        var bloodStormInfoGO = Label(bloodSurgePanel, "BloodStormInfoText",
            "Blood Storm  —  Unlocks at wave 8",
            30, new Color(0.55f, 0.8f, 1f), TextAnchor.MiddleLeft);
        PT(bloodStormInfoGO, 314, 44, -140, 620);

        var bloodStormBtnGO = Btn(bloodSurgePanel, "BloodStormButton",
            "Storm! (50 blood)", 30, HC("0D3A6E"));
        PT(bloodStormBtnGO, 364, 42, 0, 680);

        var bloodOathDivGO = bloodSurgePanel.CreateChild("BloodOathDiv");
        bloodOathDivGO.AddImage(HC("2D2D4A")); PT(bloodOathDivGO, 406, 2, 0, 640);

        var bloodOathInfoGO = Label(bloodSurgePanel, "BloodOathInfoText",
            "Blood Oath  —  Unlocks at wave 15",
            30, new Color(0.85f, 0.5f, 1f), TextAnchor.MiddleLeft);
        PT(bloodOathInfoGO, 414, 44, -140, 620);

        var bloodOathBtnGO = Btn(bloodSurgePanel, "BloodOathButton",
            "Oath! (200 blood)", 30, HC("3A006A"));
        PT(bloodOathBtnGO, 464, 42, 0, 680);

        var warCryDivGO = bloodSurgePanel.CreateChild("WarCryDiv");
        warCryDivGO.AddImage(HC("2D2D4A")); PT(warCryDivGO, 506, 2, 0, 640);

        var warCryInfoGO = Label(bloodSurgePanel, "WarCryInfoText",
            "War Cry  —  Unlocks at wave 5",
            30, new Color(1f, 0.7f, 0.3f), TextAnchor.MiddleLeft);
        PT(warCryInfoGO, 514, 44, -140, 620);

        var warCryBtnGO = Btn(bloodSurgePanel, "WarCryButton",
            "War Cry! (30 blood)", 30, HC("4A2800"));
        PT(warCryBtnGO, 564, 42, 0, 680);

        var hexCurseDivGO = bloodSurgePanel.CreateChild("HexCurseDiv");
        hexCurseDivGO.AddImage(HC("2D2D4A")); PT(hexCurseDivGO, 606, 2, 0, 640);

        var hexCurseInfoGO = Label(bloodSurgePanel, "HexCurseInfoText",
            "Hex Curse  —  Unlocks at wave 4",
            30, new Color(0.3f, 0.85f, 0.4f), TextAnchor.MiddleLeft);
        PT(hexCurseInfoGO, 614, 44, -140, 620);

        var hexCurseBtnGO = Btn(bloodSurgePanel, "HexCurseButton",
            "Hex! (20 blood)", 30, HC("003A10"));
        PT(hexCurseBtnGO, 664, 42, 0, 680);

        var bloodShieldDivGO = bloodSurgePanel.CreateChild("BloodShieldDiv");
        bloodShieldDivGO.AddImage(HC("2D2D4A")); PT(bloodShieldDivGO, 706, 2, 0, 640);

        var bloodShieldInfoGO = Label(bloodSurgePanel, "BloodShieldInfoText",
            "Blood Shield  —  Unlocks at 150 total blood",
            30, new Color(0.4f, 0.8f, 1f), TextAnchor.MiddleLeft);
        PT(bloodShieldInfoGO, 714, 44, -140, 620);

        var bloodShieldBtnGO = Btn(bloodSurgePanel, "BloodShieldButton",
            "Shield! (30 blood)", 30, HC("003A5A"));
        PT(bloodShieldBtnGO, 764, 42, 0, 680);

        bloodSurgePanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // Switch to BUILD tab content
        // ════════════════════════════════════════════════════════════════════
        content = buildContent;

        // ════════════════════════════════════════════════════════════════════
        // WORKERS CARD  (buildContent y 360–865) — hidden until 200 blood earned
        // ════════════════════════════════════════════════════════════════════
        var workersPanel = content.CreateChild("WorkersPanel");
        workersPanel.AddImage(Color.clear);
        PF(workersPanel, 360, 505);

        Panel(workersPanel, "WorkersCardBg", 0, 505, Surface1, 24);
        { var a = workersPanel.CreateChild("WorkersAccent"); a.AddImage(Amber); PF(a, 0, 4, 24); }

        var workerInfoGO = Label(workersPanel, "WorkerInfoText", "Workers: 0",
            38, Color.white, TextAnchor.MiddleLeft);
        PT(workerInfoGO, 23, 52, -175, 500);

        var buyWorkerGO = Btn(workersPanel, "BuyWorkerButton", "Buy Worker\n50 blood", 34, Green);
        PT(buyWorkerGO, 13, 110, +232, 370);

        var shrineInfoGO = Label(workersPanel, "ShrineInfoText", "Blood Shrine  0/3  (+0.0/s blood)",
            34, Color.white, TextAnchor.MiddleLeft);
        PT(shrineInfoGO, 148, 52, -175, 500);

        var buyShrineGO = Btn(workersPanel, "BuyShrineButton", "Build Shrine\n(20 wood)", 34, HC("8B0000"));
        PT(buyShrineGO, 138, 110, +232, 370);

        var clickPowerInfoGO = Label(workersPanel, "ClickPowerInfoText", "Click Power  Lv.0/5  (+0/tap)",
            34, Color.white, TextAnchor.MiddleLeft);
        PT(clickPowerInfoGO, 273, 52, -175, 500);

        var clickPowerGO = Btn(workersPanel, "ClickPowerButton", "Upgrade\n(15 wood)", 34, Gold);
        PT(clickPowerGO, 263, 110, +232, 370);

        var bloodWellInfoGO = Label(workersPanel, "BloodWellInfoText", "Blood Well  0/5  (0.0/s blood)",
            34, Color.white, TextAnchor.MiddleLeft);
        PT(bloodWellInfoGO, 398, 52, -175, 500);

        var buyBloodWellGO = Btn(workersPanel, "BloodWellButton", "Build Well\n(20 wood)", 34, HC("1B5E20"));
        PT(buyBloodWellGO, 388, 110, +232, 370);

        workersPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // BARRACKS CARD  (buildContent y 10–175)
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "BarracksCardBg", 10, 165, Surface1, 24);
        { var a = content.CreateChild("BarracksCardAccent"); a.AddImage(Brown); PF(a, 10, 4, 24); }

        var barracksInfoGO = Label(content, "BarracksInfoText",
            "Barracks  Lv.1  —  Max 10 soldiers",
            34, Color.white, TextAnchor.MiddleLeft);
        PT(barracksInfoGO, 33, 52, -175, 540);

        var upgradeBarracksGO = Btn(content, "UpgradeBarracksButton", "Upgrade\n(20 wood)", 34, Brown);
        PT(upgradeBarracksGO, 23, 110, +232, 370);

        // ════════════════════════════════════════════════════════════════════
        // FORTIFICATIONS CARD  (buildContent y 185–350)
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "FortificationsCardBg", 185, 165, Surface1, 24);
        { var a = content.CreateChild("FortCardAccent"); a.AddImage(HC("4A3728")); PF(a, 185, 4, 24); }

        var fortInfoGO = Label(content, "FortificationsInfoText",
            "Fortifications  Lv.0/10  (−0% enemy HP)",
            34, Color.white, TextAnchor.MiddleLeft);
        PT(fortInfoGO, 208, 52, -175, 540);

        var upgradeFortGO = Btn(content, "UpgradeFortificationButton", "Fortify\n(50 wood)", 34, Brown);
        PT(upgradeFortGO, 198, 110, +232, 370);

        // ════════════════════════════════════════════════════════════════════
        // EQUIPMENT CARD  (buildContent y 875–1120) — same unlock as workers
        // ════════════════════════════════════════════════════════════════════
        var equipmentPanel = content.CreateChild("EquipmentPanel");
        equipmentPanel.AddImage(Color.clear);
        PF(equipmentPanel, 875, 245);

        Panel(equipmentPanel, "EquipmentCardBg", 0, 245, Surface1, 24);
        { var a = equipmentPanel.CreateChild("EquipAccent"); a.AddImage(Blue); PF(a, 0, 4, 24); }

        var equipTitleGO = Label(equipmentPanel, "EquipmentTitle", "Equipment", 40, Gold, TextAnchor.MiddleLeft);
        PT(equipTitleGO, 10, 44, -200, 400);

        // Weapon row
        var weaponInfoGO = Label(equipmentPanel, "WeaponInfoText",
            "Weapon  Lv.0/5  (+0 atk)", 28, Color.white, TextAnchor.MiddleLeft);
        PT(weaponInfoGO, 62, 44, -175, 500);

        var upgradeWeaponGO = Btn(equipmentPanel, "UpgradeWeaponButton", "Upgrade\n(20 wood)", 28, Brown);
        PT(upgradeWeaponGO, 58, 56, +232, 330);

        // Armor row
        var armorInfoGO = Label(equipmentPanel, "ArmorInfoText",
            "Armor  Lv.0/5  (+0 HP)", 28, Color.white, TextAnchor.MiddleLeft);
        PT(armorInfoGO, 122, 44, -175, 500);

        var upgradeArmorGO = Btn(equipmentPanel, "UpgradeArmorButton", "Upgrade\n(15 wood)", 28, HC("1565C0"));
        PT(upgradeArmorGO, 118, 56, +232, 330);

        // Talisman row
        var talismanInfoGO = Label(equipmentPanel, "TalismanInfoText",
            "Talisman  Lv.0/5  (+0% reward)", 28, Color.white, TextAnchor.MiddleLeft);
        PT(talismanInfoGO, 182, 44, -175, 500);

        var upgradeTalismanGO = Btn(equipmentPanel, "UpgradeTalismanButton", "Upgrade\n(25 wood)", 28, HC("6A1B9A"));
        PT(upgradeTalismanGO, 178, 56, +232, 330);

        equipmentPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // BLOOD RITUAL + BLOOD PACT CARD  (buildContent y 1130–1345)
        // ════════════════════════════════════════════════════════════════════
        var bloodRitualPanel = content.CreateChild("BloodRitualPanel");
        bloodRitualPanel.AddImage(Color.clear);
        PF(bloodRitualPanel, 1130, 215);

        Panel(bloodRitualPanel, "BloodRitualCardBg", 0, 215, Surface1, 24);
        { var a = bloodRitualPanel.CreateChild("RitualAccent"); a.AddImage(Purple); PF(a, 0, 4, 24); }

        var bloodRitualInfoGO = Label(bloodRitualPanel, "BloodRitualInfoText",
            "Blood Ritual  —  passive blood income",
            32, Color.white, TextAnchor.MiddleLeft);
        PT(bloodRitualInfoGO, 18, 52, -175, 500);

        var buyBloodRitualGO = Btn(bloodRitualPanel, "BuyBloodRitualButton",
            "Perform\n(30 wood)", 34, Purple);
        PT(buyBloodRitualGO, 10, 110, +232, 370);

        var ritualDivGO = bloodRitualPanel.CreateChild("RitualDiv");
        ritualDivGO.AddImage(HC("2D2D4A")); PT(ritualDivGO, 128, 2, 0, 640);

        var bloodPactInfoGO = Label(bloodRitualPanel, "BloodPactInfoText",
            "Blood Pact  —  200 blood → 100 wood",
            28, TextSec, TextAnchor.MiddleLeft);
        PT(bloodPactInfoGO, 137, 38, -175, 500);

        var bloodPactGO = Btn(bloodRitualPanel, "BloodPactButton",
            "Convert\n(200 blood)", 28, Crimson);
        PT(bloodPactGO, 132, 72, +232, 370);

        bloodRitualPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // BLOOD BANK CARD  (buildContent y 1355–1520)
        // ════════════════════════════════════════════════════════════════════
        var bloodBankPanel = content.CreateChild("BloodBankPanel");
        bloodBankPanel.AddImage(Color.clear);
        PF(bloodBankPanel, 1355, 165);

        Panel(bloodBankPanel, "BloodBankCardBg", 0, 165, Surface1, 24);
        { var a = bloodBankPanel.CreateChild("BankAccent"); a.AddImage(Gold); PF(a, 0, 4, 24); }

        var bankTitleGO = Label(bloodBankPanel, "BloodBankTitle", "Blood Bank", 40, Gold, TextAnchor.MiddleLeft);
        PT(bankTitleGO, 8, 44, -200, 400);

        var bankInfoGO = Label(bloodBankPanel, "BloodBankInfoText",
            "Blood Bank  0/10,000  (+2%/hr)", 30, Color.white, TextAnchor.MiddleLeft);
        PT(bankInfoGO, 58, 42, -175, 520);

        var bankAccruedGO = Label(bloodBankPanel, "BloodBankAccruedText",
            "Interest accrued: none yet", 26, TextSec, TextAnchor.MiddleLeft);
        PT(bankAccruedGO, 104, 36, -175, 520);

        var depositBtnGO = Btn(bloodBankPanel, "DepositBloodButton", "Deposit\n10%", 30, Brown);
        PT(depositBtnGO, 52, 100, +260, 210);

        var withdrawBtnGO = Btn(bloodBankPanel, "WithdrawBloodButton", "Withdraw\nAll", 30, Green);
        PT(withdrawBtnGO, 52, 100, +380, 210);

        // ════════════════════════════════════════════════════════════════════
        // Switch to PROGRESS tab content
        // ════════════════════════════════════════════════════════════════════
        content = progressContent;

        // ════════════════════════════════════════════════════════════════════
        // PRESTIGE CARD  (progressContent y 10–150) — visible at wave 20+
        // ════════════════════════════════════════════════════════════════════
        var prestigePanel = content.CreateChild("PrestigePanel");
        prestigePanel.AddImage(Color.clear);
        PF(prestigePanel, 10, 140);

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
        // PRESTIGE SHOP CARD  (progressContent y 160–645) — visible after first prestige
        // ════════════════════════════════════════════════════════════════════
        var prestigeShopPanel = content.CreateChild("PrestigeShopPanel");
        prestigeShopPanel.AddImage(Color.clear);
        PF(prestigeShopPanel, 160, 485);

        Panel(prestigeShopPanel, "PrestigeShopCardBg", 0, 485, HC("150A30"), 24);

        var shopTitleGO = Label(prestigeShopPanel, "PrestigeShopTitle",
            "Prestige Shop", 40, Gold, TextAnchor.MiddleLeft);
        PT(shopTitleGO, 8, 44, -200, 400);

        var shopPtsGO = Label(prestigeShopPanel, "PrestigeShopPointsText",
            "Prestige Points: 0", 34, Gold, TextAnchor.MiddleRight);
        PT(shopPtsGO, 8, 44, +165, 460);

        // Row 1 — Soldier Cap
        var pSoldierCapInfoGO = Label(prestigeShopPanel, "PSoldierCapInfoText",
            "Soldier Cap +10  (Lv.0)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pSoldierCapInfoGO, 60, 48, -175, 500);
        var pSoldierCapBtnGO = Btn(prestigeShopPanel, "PSoldierCapButton", "Buy (1 PP)", 30, HC("6A1B9A"));
        PT(pSoldierCapBtnGO, 58, 54, +245, 260);

        // Row 2 — Click Bonus
        var pClickBonusInfoGO = Label(prestigeShopPanel, "PClickBonusInfoText",
            "Click Bonus +0.5  (Lv.0)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pClickBonusInfoGO, 118, 48, -175, 500);
        var pClickBonusBtnGO = Btn(prestigeShopPanel, "PClickBonusButton", "Buy (1 PP)", 30, HC("6A1B9A"));
        PT(pClickBonusBtnGO, 116, 54, +245, 260);

        // Row 3 — Ritual Efficiency
        var pRitualEffInfoGO = Label(prestigeShopPanel, "PRitualEffInfoText",
            "Ritual Eff. +0.5/s  (Lv.0)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pRitualEffInfoGO, 176, 48, -175, 500);
        var pRitualEffBtnGO = Btn(prestigeShopPanel, "PRitualEffButton", "Buy (1 PP)", 30, HC("6A1B9A"));
        PT(pRitualEffBtnGO, 174, 54, +245, 260);

        // Row 4 — Weapon Head Start
        var pWeaponHeadStartInfoGO = Label(prestigeShopPanel, "PWeaponHeadStartInfoText",
            "Weapon Head Start  (Lv.0)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pWeaponHeadStartInfoGO, 234, 48, -175, 500);
        var pWeaponHeadStartBtnGO = Btn(prestigeShopPanel, "PWeaponHeadStartButton", "Buy (1 PP)", 30, HC("5D4037"));
        PT(pWeaponHeadStartBtnGO, 232, 54, +245, 260);

        // Row 5 — Blood Tithe
        var pBloodTitheInfoGO = Label(prestigeShopPanel, "PBloodTitheInfoText",
            "Blood Tithe +0.5/s  (Lv.0)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pBloodTitheInfoGO, 292, 48, -175, 500);
        var pBloodTitheBtnGO = Btn(prestigeShopPanel, "PBloodTitheButton", "Buy (1 PP)", 30, HC("C62828"));
        PT(pBloodTitheBtnGO, 290, 54, +245, 260);

        // Row 6 — Iron Wall
        var pIronWallInfoGO = Label(prestigeShopPanel, "PIronWallInfoText",
            "Iron Wall −10% dmg  (Lv.0)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pIronWallInfoGO, 350, 48, -175, 500);
        var pIronWallBtnGO = Btn(prestigeShopPanel, "PIronWallButton", "Buy (1 PP)", 30, HC("1565C0"));
        PT(pIronWallBtnGO, 348, 54, +245, 260);

        // Row 7 — Bounty Mastery
        var pBountyBonusInfoGO = Label(prestigeShopPanel, "PBountyBonusInfoText",
            "Bounty Mastery +1x  (Lv.0)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pBountyBonusInfoGO, 408, 48, -175, 500);
        var pBountyBonusBtnGO = Btn(prestigeShopPanel, "PBountyBonusButton", "Buy (1 PP)", 30, HC("E65100"));
        PT(pBountyBonusBtnGO, 406, 54, +245, 260);

        prestigeShopPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // SOUL SHARD SHOP CARD  (y 2985–3235) — visible after first boss kill
        // ════════════════════════════════════════════════════════════════════
        var soulShardShopPanel = content.CreateChild("SoulShardShopPanel");
        soulShardShopPanel.AddImage(Color.clear);
        PF(soulShardShopPanel, 655, 380);

        Panel(soulShardShopPanel, "SoulShardShopCardBg", 0, 380, HC("0A1A30"), 24);

        var ssShopTitleGO = Label(soulShardShopPanel, "SoulShardShopTitle",
            "Soul Shard Shop", 40, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleLeft);
        PT(ssShopTitleGO, 8, 44, -200, 440);

        var ssShopPtsGO = Label(soulShardShopPanel, "SoulShardShopPointsText",
            "Soul Shards: 0", 34, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleRight);
        PT(ssShopPtsGO, 8, 44, +165, 420);

        // Row 1 — Boss Timer
        var ssBossTimerInfoGO = Label(soulShardShopPanel, "SSBossTimerInfoText",
            "Boss Timer +15s  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssBossTimerInfoGO, 60, 48, -175, 500);
        var ssBossTimerBtnGO = Btn(soulShardShopPanel, "SSBossTimerButton", "Buy (1 ⬡)", 30, HC("1565C0"));
        PT(ssBossTimerBtnGO, 58, 54, +245, 260);

        // Row 2 — Double Chest
        var ssDoubleChestInfoGO = Label(soulShardShopPanel, "SSDoubleChestInfoText",
            "Double Chest  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssDoubleChestInfoGO, 118, 48, -175, 500);
        var ssDoubleChestBtnGO = Btn(soulShardShopPanel, "SSDoubleChestButton", "Buy (1 ⬡)", 30, HC("F9A825"));
        PT(ssDoubleChestBtnGO, 116, 54, +245, 260);

        // Row 3 — Rollback Shield
        var ssRollbackInfoGO = Label(soulShardShopPanel, "SSRollbackInfoText",
            "Rollback Shield  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssRollbackInfoGO, 176, 48, -175, 500);
        var ssRollbackBtnGO = Btn(soulShardShopPanel, "SSRollbackButton", "Buy (1 ⬡)", 30, HC("2E7D32"));
        PT(ssRollbackBtnGO, 174, 54, +245, 260);

        // Row 4 — Blood Tap
        var ssBloodTapInfoGO = Label(soulShardShopPanel, "SSBloodTapInfoText",
            "Blood Tap +1/s  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssBloodTapInfoGO, 236, 48, -175, 500);
        var ssBloodTapBtnGO = Btn(soulShardShopPanel, "SSBloodTapButton", "Buy (1 ⬡)", 30, HC("D32F2F"));
        PT(ssBloodTapBtnGO, 234, 54, +245, 260);

        // Row 5 — Shard Hunger
        var ssShardHungerInfoGO = Label(soulShardShopPanel, "SSShardHungerInfoText",
            "Shard Hunger +20% boss blood  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssShardHungerInfoGO, 294, 48, -175, 500);
        var ssShardHungerBtnGO = Btn(soulShardShopPanel, "SSShardHungerButton", "Buy (1 ⬡)", 30, HC("6A1B9A"));
        PT(ssShardHungerBtnGO, 292, 54, +245, 260);

        soulShardShopPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // DAILY QUESTS BUTTON ROW  (y 4200–4265)
        // ════════════════════════════════════════════════════════════════════
        var questsRowGO = content.CreateChild("DailyQuestsRow");
        questsRowGO.AddImage(HC("0A1A0A")); PF(questsRowGO, 1045, 65, 20);

        var openQuestsBtnGO = Btn(questsRowGO, "OpenQuestsButton", "Daily Quests", 34, HC("1B5E20"));
        openQuestsBtnGO.Stretch();

        // ════════════════════════════════════════════════════════════════════
        // WATCH AD ROW  (y 4275–4345)
        // ════════════════════════════════════════════════════════════════════
        var adBoostRowGO = content.CreateChild("AdBoostRow");
        adBoostRowGO.AddImage(HC("1A0A2E")); PF(adBoostRowGO, 1120, 65, 20);

        var watchAdBtnGO = Btn(adBoostRowGO, "WatchAdButton", "Watch Ad  →  2× Blood (5 min)", 34, Purple);
        watchAdBtnGO.Stretch();

        // ════════════════════════════════════════════════════════════════════
        // Switch to SETTINGS tab content
        // ════════════════════════════════════════════════════════════════════
        content = settingsContent;

        // ════════════════════════════════════════════════════════════════════
        // SETTINGS CONTENT  (settingsContent y 10–240) — 2×2 utility button grid
        // ════════════════════════════════════════════════════════════════════
        var statsBtnGO = Btn(content, "StatsButton", "Statistics", 32, Teal);
        PT(statsBtnGO, 10, 110, -265, 508);

        var settingsBtnGO = Btn(content, "SettingsButton", "Settings", 32, HC("2A2A4A"));
        PT(settingsBtnGO, 10, 110, +265, 508);

        var suggestBtnGO = Btn(content, "SuggestButton", "Suggest", 32, HC("1565C0"));
        PT(suggestBtnGO, 130, 110, -265, 508);

        var shopBtnGO = Btn(content, "ShopButton", "Shop", 32, HC("8B0000"));
        PT(shopBtnGO, 130, 110, +265, 508);

        // ════════════════════════════════════════════════════════════════════
        // FIXED TAB BAR  (canvas bottom 100px)
        // ════════════════════════════════════════════════════════════════════
        var tabBarGO = cv.CreateChild("TabBar");
        tabBarGO.AddImage(HC("0F0E1E"));
        {
            var rt              = tabBarGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, 100f);
        }
        var tabTopDivGO = tabBarGO.CreateChild("TabTopDiv");
        tabTopDivGO.AddImage(HC("2D2D4A"));
        {
            var rt              = tabTopDivGO.GetComponent<RectTransform>();
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 98f);
            rt.sizeDelta        = new Vector2(0f, 2f);
        }
        var tabBattleBtnGO   = Btn(tabBarGO, "TabBattleButton",   "Battle",   28, Color.clear);
        var tabBuildBtnGO    = Btn(tabBarGO, "TabBuildButton",    "Build",    28, Color.clear);
        var tabProgressBtnGO = Btn(tabBarGO, "TabProgressButton", "Progress", 28, Color.clear);
        var tabSettingsBtnGO = Btn(tabBarGO, "TabSettingsButton", "Settings", 28, Color.clear);
        PT(tabBattleBtnGO,   0, 100, -405, 250);
        PT(tabBuildBtnGO,    0, 100, -135, 250);
        PT(tabProgressBtnGO, 0, 100, +135, 250);
        PT(tabSettingsBtnGO, 0, 100, +405, 250);

        // ── Damage number layer ───────────────────────────────────────────────
        var dmgLayerGO = cv.CreateChild("DamageLayer");
        var dmgImg     = dmgLayerGO.AddComponent<Image>();
        dmgImg.color         = Color.clear;
        dmgImg.raycastTarget = false;
        dmgLayerGO.Stretch();

        // ── Tutorial panel (fixed bottom overlay, dismissable) ───────────────
        var tutPanelGO = cv.CreateChild("TutorialPanel");
        RImg(tutPanelGO, new Color(0.05f, 0.04f, 0.12f, 0.95f));
        var tutPanelRT = tutPanelGO.GetComponent<RectTransform>();
        tutPanelRT.anchorMin        = new Vector2(0f, 0f);
        tutPanelRT.anchorMax        = new Vector2(1f, 0f);
        tutPanelRT.pivot            = new Vector2(0.5f, 0f);
        tutPanelRT.anchoredPosition = new Vector2(0f, 80f);
        tutPanelRT.sizeDelta        = new Vector2(0f, 210f);

        var tutBorderGO = tutPanelGO.CreateChild("TutBorder");
        tutBorderGO.AddImage(HC("9A1A2A"));
        var tutBorderRT = tutBorderGO.GetComponent<RectTransform>();
        tutBorderRT.anchorMin = Vector2.zero; tutBorderRT.anchorMax = new Vector2(1f, 0f);
        tutBorderRT.pivot = new Vector2(0.5f, 0f); tutBorderRT.anchoredPosition = new Vector2(0f, 0f);
        tutBorderRT.sizeDelta = new Vector2(0f, 3f);

        var tutTitleGO = Label(tutPanelGO, "TutorialTitleText", "Welcome!", 38, Gold, TextAnchor.MiddleLeft);
        PT(tutTitleGO, 8, 48, -20, 760);

        var tutBodyGO = Label(tutPanelGO, "TutorialBodyText",
            "Tap Farm Blood to earn blood. Build an army and conquer endless waves!",
            28, Color.white, TextAnchor.UpperLeft);
        PT(tutBodyGO, 62, 88, -20, 720);
        tutBodyGO.GetComponent<Text>().horizontalOverflow = HorizontalWrapMode.Wrap;
        tutBodyGO.GetComponent<Text>().verticalOverflow   = VerticalWrapMode.Overflow;

        var tutDismissGO = Btn(tutPanelGO, "TutorialDismissButton", "Got it!", 32, HC("1A4A1A"));
        PT(tutDismissGO, 158, 46, 400, 180);

        tutPanelGO.SetActive(false);

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

        // ── Settings overlay (modal on canvas) ────────────────────────────────
        var settingsOverlay = cv.CreateChild("SettingsPanel");
        settingsOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);
        settingsOverlay.Stretch();

        var settingsCard   = settingsOverlay.CreateChild("Card");
        RImg(settingsCard, Surface1);
        var settingsCardRT = settingsCard.GetComponent<RectTransform>();
        settingsCardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        settingsCardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        settingsCardRT.anchoredPosition = Vector2.zero;
        settingsCardRT.sizeDelta        = new Vector2(800, 580);

        var settingsTitleGO = Label(settingsCard, "SettingsTitle", "Settings", 52, Gold);
        PT(settingsTitleGO, 22, 62, 0, 760);

        var settingsDivGO = settingsCard.CreateChild("SettingsDiv");
        settingsDivGO.AddImage(HC("2D2D4A")); PT(settingsDivGO, 88, 2, 0, 760);

        var soundToggleGO = Btn(settingsCard, "SoundToggleButton", "Sound: ON", 36, HC("1A3A1A"));
        PT(soundToggleGO, 104, 80, 0, 680);

        var notifToggleGO = Btn(settingsCard, "NotifToggleButton", "Notifications: ON", 36, HC("1A1A3A"));
        PT(notifToggleGO, 196, 80, 0, 680);

        var resetDataGO = Btn(settingsCard, "ResetDataButton", "Reset All Data", 36, Crimson);
        PT(resetDataGO, 294, 80, 0, 680);

        var settingsCloseGO = Btn(settingsCard, "SettingsCloseButton", "Close", 42, HC("252440"));
        PT(settingsCloseGO, 484, 70, 0, 400);

        settingsOverlay.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // IAP SHOP OVERLAY
        // ════════════════════════════════════════════════════════════════════
        var iapOverlay = cv.CreateChild("IAPShopPanel");
        iapOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);
        iapOverlay.Stretch();

        var iapCard   = iapOverlay.CreateChild("Card");
        RImg(iapCard, Surface1);
        var iapCardRT = iapCard.GetComponent<RectTransform>();
        iapCardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        iapCardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        iapCardRT.anchoredPosition = Vector2.zero;
        iapCardRT.sizeDelta        = new Vector2(960, 800);

        var iapTitleGO = Label(iapCard, "IAPTitle", "Blood Shop", 52, Crimson);
        PT(iapTitleGO, 22, 62, 0, 920);

        var iapDivGO = iapCard.CreateChild("IAPDiv");
        iapDivGO.AddImage(HC("2D2D4A")); PT(iapDivGO, 88, 2, 0, 900);

        // Row: Remove Ads
        var removeAdsInfoGO = Label(iapCard, "RemoveAdsInfo",
            "Remove Ads  —  no more interruptions forever", 30, TextSec, TextAnchor.MiddleLeft);
        PT(removeAdsInfoGO, 102, 48, -110, 620);
        var removeAdsBtnGO = Btn(iapCard, "RemoveAdsButton", "$1.99", 30, HC("5D4037"));
        PT(removeAdsBtnGO, 100, 54, +330, 200);

        // Row: Starter Pack
        var starterInfoGO = Label(iapCard, "StarterPackInfo",
            "Starter Pack  —  5,000 Blood + 5 Soul Shards", 30, TextSec, TextAnchor.MiddleLeft);
        PT(starterInfoGO, 162, 48, -110, 620);
        var starterBtnGO = Btn(iapCard, "StarterPackButton", "$0.99", 30, Gold);
        PT(starterBtnGO, 160, 54, +330, 200);

        // Row: Blood Boost Small
        var boostSmallInfoGO = Label(iapCard, "BloodBoostSmallInfo",
            "Blood Boost  —  2× income for 30 minutes", 30, TextSec, TextAnchor.MiddleLeft);
        PT(boostSmallInfoGO, 222, 48, -110, 620);
        var boostSmallBtnGO = Btn(iapCard, "BloodBoostSmallButton", "$0.99", 30, Purple);
        PT(boostSmallBtnGO, 220, 54, +330, 200);

        // Row: Blood Boost Large
        var boostLargeInfoGO = Label(iapCard, "BloodBoostLargeInfo",
            "Mega Boost  —  2× income for 2 hours + 10 Shards", 30, TextSec, TextAnchor.MiddleLeft);
        PT(boostLargeInfoGO, 282, 48, -110, 620);
        var boostLargeBtnGO = Btn(iapCard, "BloodBoostLargeButton", "$1.99", 30, Crimson);
        PT(boostLargeBtnGO, 280, 54, +330, 200);

        var iapCloseGO = Btn(iapCard, "IAPCloseButton", "Close", 42, HC("252440"));
        PT(iapCloseGO, 680, 80, 0, 400);

        iapOverlay.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // DAILY QUESTS OVERLAY
        // ════════════════════════════════════════════════════════════════════
        var questsOverlay = cv.CreateChild("QuestsPanel");
        questsOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);
        questsOverlay.Stretch();

        var questsCard   = questsOverlay.CreateChild("Card");
        RImg(questsCard, Surface1);
        var questsCardRT = questsCard.GetComponent<RectTransform>();
        questsCardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        questsCardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        questsCardRT.anchoredPosition = Vector2.zero;
        questsCardRT.sizeDelta        = new Vector2(960, 800);

        var questsTitleGO = Label(questsCard, "QuestsTitle", "Daily Quests", 52, HC("1B5E20"));
        PT(questsTitleGO, 16, 62, 0, 920);

        var questsSubGO = Label(questsCard, "QuestsSubtitle", "Resets at midnight UTC", 26, TextSec);
        PT(questsSubGO, 76, 34, 0, 920);

        var questsDivGO = questsCard.CreateChild("QuestsDiv");
        questsDivGO.AddImage(HC("2D2D4A")); PT(questsDivGO, 114, 2, 0, 900);

        // Quest rows (3 slots, each 150px tall)
        var questInfo0GO = Label(questsCard, "QuestInfoText0", "Quest 1", 30, Color.white, TextAnchor.MiddleLeft);
        questInfo0GO.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
        PT(questInfo0GO, 124, 80, -110, 620);
        var questClaim0GO = Btn(questsCard, "QuestClaimButton0", "Locked", 30, HC("2A2A4A"));
        PT(questClaim0GO, 128, 66, +330, 220);

        var questDiv0GO = questsCard.CreateChild("QuestDiv0");
        questDiv0GO.AddImage(HC("1A1A30")); PT(questDiv0GO, 206, 2, 0, 880);

        var questInfo1GO = Label(questsCard, "QuestInfoText1", "Quest 2", 30, Color.white, TextAnchor.MiddleLeft);
        questInfo1GO.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
        PT(questInfo1GO, 216, 80, -110, 620);
        var questClaim1GO = Btn(questsCard, "QuestClaimButton1", "Locked", 30, HC("2A2A4A"));
        PT(questClaim1GO, 220, 66, +330, 220);

        var questDiv1GO = questsCard.CreateChild("QuestDiv1");
        questDiv1GO.AddImage(HC("1A1A30")); PT(questDiv1GO, 298, 2, 0, 880);

        var questInfo2GO = Label(questsCard, "QuestInfoText2", "Quest 3", 30, Color.white, TextAnchor.MiddleLeft);
        questInfo2GO.GetComponent<Text>().verticalOverflow = VerticalWrapMode.Overflow;
        PT(questInfo2GO, 308, 80, -110, 620);
        var questClaim2GO = Btn(questsCard, "QuestClaimButton2", "Locked", 30, HC("2A2A4A"));
        PT(questClaim2GO, 312, 66, +330, 220);

        var questsCloseGO = Btn(questsCard, "QuestsCloseButton", "Close", 42, HC("252440"));
        PT(questsCloseGO, 686, 80, 0, 400);

        questsOverlay.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // TALENT SELECTION OVERLAY
        // ════════════════════════════════════════════════════════════════════
        var talentOverlay = cv.CreateChild("TalentSelectionPanel");
        talentOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);
        talentOverlay.Stretch();

        var talentCard   = talentOverlay.CreateChild("Card");
        RImg(talentCard, HC("1A0A28"));
        var talentCardRT = talentCard.GetComponent<RectTransform>();
        talentCardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        talentCardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        talentCardRT.anchoredPosition = Vector2.zero;
        talentCardRT.sizeDelta        = new Vector2(900, 700);

        var talentHeaderGO = Label(talentCard, "TalentHeaderText",
            "Choose a Prestige Talent", 46, Gold, TextAnchor.MiddleCenter);
        PT(talentHeaderGO, 20, 72, 0, 860);

        var talentDivGO = talentCard.CreateChild("TalentDiv");
        talentDivGO.AddImage(HC("4A2A6A")); PT(talentDivGO, 96, 2, 0, 860);

        var talent0BtnGO = Btn(talentCard, "TalentButton0", "...", 30, HC("2A1A40"));
        PT(talent0BtnGO, 106, 130, 0, 840);

        var talent1BtnGO = Btn(talentCard, "TalentButton1", "...", 30, HC("2A1A40"));
        PT(talent1BtnGO, 246, 130, 0, 840);

        var talent2BtnGO = Btn(talentCard, "TalentButton2", "...", 30, HC("2A1A40"));
        PT(talent2BtnGO, 386, 130, 0, 840);

        var talentCancelGO = Btn(talentCard, "TalentCancelButton", "Cancel Prestige", 36, HC("252440"));
        PT(talentCancelGO, 540, 70, 0, 500);

        talentOverlay.SetActive(false);

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
        PT(featureCancelGO, 692, 86, -225, 430);

        var voteOpenGO = Btn(card, "CommunityVotesButton", "Community Votes", 36, HC("1A3A5A"));
        PT(voteOpenGO, 692, 86, 225, 430);

        overlay.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // FEATURE VOTE OVERLAY
        // ════════════════════════════════════════════════════════════════════
        var voteOverlay = cv.CreateChild("FeatureVoteOverlay");
        voteOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);
        voteOverlay.Stretch();

        var voteCard   = voteOverlay.CreateChild("VoteCard");
        RImg(voteCard, Surface1);
        var voteCardRT = voteCard.GetComponent<RectTransform>();
        voteCardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        voteCardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        voteCardRT.anchoredPosition = Vector2.zero;
        voteCardRT.sizeDelta        = new Vector2(960, 720);

        var voteCardTitleGO = Label(voteCard, "VoteCardTitle", "Community Votes", 52, Crimson);
        PT(voteCardTitleGO, 22, 62, 0, 900);

        var voteDividerGO = voteCard.CreateChild("VoteDivider");
        voteDividerGO.AddImage(HC("2D2D4A")); PT(voteDividerGO, 88, 2, 0, 880);

        var voteIssueTxtGO  = Label(voteCard, "VoteIssueText",  "Loading...", 34, Color.white);
        PT(voteIssueTxtGO, 100, 140, 0, 880);
        voteIssueTxtGO.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

        var votePrevGO = Btn(voteCard, "VotePrevButton", "< Prev", 32, HC("252440"));
        PT(votePrevGO, 252, 70, -270, 260);

        var voteNextGO = Btn(voteCard, "VoteNextButton", "Next >", 32, HC("252440"));
        PT(voteNextGO, 252, 70, 270, 260);

        var voteSubmitGO = Btn(voteCard, "VoteSubmitButton", "+1 Vote", 42, Crimson);
        PT(voteSubmitGO, 336, 96, 0, 460);

        var voteStatusTxtGO = Label(voteCard, "VoteStatusText", "", 30, HC("81C784"));
        PT(voteStatusTxtGO, 444, 44, 0, 880);

        var voteRefreshGO = Btn(voteCard, "VoteRefreshButton", "Refresh List", 30, HC("1A3A2A"));
        PT(voteRefreshGO, 500, 68, 0, 400);

        var voteCancelGO = Btn(voteCard, "VoteCloseButton", "Close", 36, HC("252440"));
        PT(voteCancelGO, 582, 86, 0, 880);

        voteOverlay.SetActive(false);

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
        uim.enemyModifierText       = enemyModifierTextGO.GetComponent<Text>();
        uim.enemyHPFill             = enemyHPFill;
        uim.enemyHPText             = enemyHPTextGO.GetComponent<Text>();
        uim.bossTimerText           = bossTimerTextGO.GetComponent<Text>();
        uim.bossTimerRow            = bossTimerRowGO;
        uim.wavePreviewBanner       = wavePreviewBannerGO;
        uim.wavePreviewText         = wavePreviewTextGO.GetComponent<Text>();
        uim.soldierCountText        = soldierCountGO.GetComponent<Text>();
        uim.soldierHPRow            = soldierHPRowGO;
        uim.soldierHPFill           = soldierHPFill;
        uim.soldierHPText           = soldierHPTextGO.GetComponent<Text>();
        uim.buyTankButton           = buyTankGO.GetComponent<Button>();
        uim.buyBerserkerButton      = buyBerserkerGO.GetComponent<Button>();
        uim.buyPaladinButton        = buyPaladinGO.GetComponent<Button>();
        uim.formationButton         = formationBtnGO.GetComponent<Button>();
        uim.formationButtonText     = formationBtnGO.GetComponentInChildren<Text>();
        uim.mixedBonusText          = mixedBonusGO.GetComponent<Text>();
        uim.healSelfPanel           = healPanelGO;
        uim.healSelfButton          = healBtnGO.GetComponent<Button>();
        uim.workersPanel            = workersPanel;
        uim.workerInfoText          = workerInfoGO.GetComponent<Text>();
        uim.buyWorkerButton         = buyWorkerGO.GetComponent<Button>();
        uim.bloodPactButton         = bloodPactGO.GetComponent<Button>();
        uim.bloodPactText           = bloodPactGO.GetComponentInChildren<Text>();
        uim.shrineInfoText          = shrineInfoGO.GetComponent<Text>();
        uim.buyShrineButton         = buyShrineGO.GetComponent<Button>();
        uim.clickPowerInfoText      = clickPowerInfoGO.GetComponent<Text>();
        uim.clickPowerButton        = clickPowerGO.GetComponent<Button>();
        uim.bloodWellInfoText       = bloodWellInfoGO.GetComponent<Text>();
        uim.buyBloodWellButton      = buyBloodWellGO.GetComponent<Button>();
        uim.equipmentPanel          = equipmentPanel;
        uim.weaponInfoText          = weaponInfoGO.GetComponent<Text>();
        uim.upgradeWeaponButton     = upgradeWeaponGO.GetComponent<Button>();
        uim.weaponCostText          = upgradeWeaponGO.GetComponentInChildren<Text>();
        uim.armorInfoText           = armorInfoGO.GetComponent<Text>();
        uim.upgradeArmorButton      = upgradeArmorGO.GetComponent<Button>();
        uim.armorCostText           = upgradeArmorGO.GetComponentInChildren<Text>();
        uim.talismanInfoText        = talismanInfoGO.GetComponent<Text>();
        uim.upgradeTalismanButton   = upgradeTalismanGO.GetComponent<Button>();
        uim.talismanCostText        = upgradeTalismanGO.GetComponentInChildren<Text>();
        uim.fortInfoText            = fortInfoGO.GetComponent<Text>();
        uim.upgradeFortButton       = upgradeFortGO.GetComponent<Button>();
        uim.fortCostText            = upgradeFortGO.GetComponentInChildren<Text>();
        uim.bloodRitualPanel        = bloodRitualPanel;
        uim.bloodRitualInfoText     = bloodRitualInfoGO.GetComponent<Text>();
        uim.buyBloodRitualButton    = buyBloodRitualGO.GetComponent<Button>();
        uim.bloodRitualCostText     = buyBloodRitualGO.GetComponentInChildren<Text>();
        uim.prestigePanel           = prestigePanel;
        uim.prestigeInfoText        = prestigeInfoGO.GetComponent<Text>();
        uim.prestigeButton          = prestigeBtnGO.GetComponent<Button>();
        uim.bloodSurgePanel         = bloodSurgePanel;
        uim.bloodSurgeInfoText      = bloodSurgeInfoGO.GetComponent<Text>();
        uim.bloodSurgeButton        = surgeBtnGO.GetComponent<Button>();
        uim.upgradeSurgeButton      = upgradeSurgeBtnGO.GetComponent<Button>();
        uim.surgeCostText           = upgradeSurgeBtnGO.GetComponentInChildren<Text>();
        uim.bloodStormInfoText      = bloodStormInfoGO.GetComponent<Text>();
        uim.bloodStormButton        = bloodStormBtnGO.GetComponent<Button>();
        uim.bloodOathInfoText       = bloodOathInfoGO.GetComponent<Text>();
        uim.bloodOathButton         = bloodOathBtnGO.GetComponent<Button>();
        uim.warCryInfoText          = warCryInfoGO.GetComponent<Text>();
        uim.warCryButton            = warCryBtnGO.GetComponent<Button>();
        uim.hexCurseInfoText        = hexCurseInfoGO.GetComponent<Text>();
        uim.hexCurseButton          = hexCurseBtnGO.GetComponent<Button>();
        uim.bloodShieldInfoText     = bloodShieldInfoGO.GetComponent<Text>();
        uim.bloodShieldButton       = bloodShieldBtnGO.GetComponent<Button>();
        uim.upgradeHealSelfButton   = upgradeHealBtnGO.GetComponent<Button>();
        uim.healCostText            = upgradeHealBtnGO.GetComponentInChildren<Text>();
        uim.prestigeShopPanel       = prestigeShopPanel;
        uim.prestigeShopPointsText  = shopPtsGO.GetComponent<Text>();
        uim.pSoldierCapInfoText     = pSoldierCapInfoGO.GetComponent<Text>();
        uim.pSoldierCapButton       = pSoldierCapBtnGO.GetComponent<Button>();
        uim.pClickBonusInfoText     = pClickBonusInfoGO.GetComponent<Text>();
        uim.pClickBonusButton       = pClickBonusBtnGO.GetComponent<Button>();
        uim.pRitualEffInfoText      = pRitualEffInfoGO.GetComponent<Text>();
        uim.pRitualEffButton        = pRitualEffBtnGO.GetComponent<Button>();
        uim.pWeaponHeadStartInfoText = pWeaponHeadStartInfoGO.GetComponent<Text>();
        uim.pWeaponHeadStartButton  = pWeaponHeadStartBtnGO.GetComponent<Button>();
        uim.pBloodTitheInfoText     = pBloodTitheInfoGO.GetComponent<Text>();
        uim.pBloodTitheButton       = pBloodTitheBtnGO.GetComponent<Button>();
        uim.pIronWallInfoText       = pIronWallInfoGO.GetComponent<Text>();
        uim.pIronWallButton         = pIronWallBtnGO.GetComponent<Button>();
        uim.pBountyBonusInfoText    = pBountyBonusInfoGO.GetComponent<Text>();
        uim.pBountyBonusButton      = pBountyBonusBtnGO.GetComponent<Button>();
        uim.bloodBankPanel          = bloodBankPanel;
        uim.bloodBankInfoText       = bankInfoGO.GetComponent<Text>();
        uim.bloodBankAccruedText    = bankAccruedGO.GetComponent<Text>();
        uim.depositBloodButton      = depositBtnGO.GetComponent<Button>();
        uim.withdrawBloodButton     = withdrawBtnGO.GetComponent<Button>();
        uim.soulShardShopPanel      = soulShardShopPanel;
        uim.soulShardShopPointsText = ssShopPtsGO.GetComponent<Text>();
        uim.ssBossTimerInfoText     = ssBossTimerInfoGO.GetComponent<Text>();
        uim.ssBossTimerButton       = ssBossTimerBtnGO.GetComponent<Button>();
        uim.ssDoubleChestInfoText   = ssDoubleChestInfoGO.GetComponent<Text>();
        uim.ssDoubleChestButton     = ssDoubleChestBtnGO.GetComponent<Button>();
        uim.ssRollbackInfoText      = ssRollbackInfoGO.GetComponent<Text>();
        uim.ssRollbackButton        = ssRollbackBtnGO.GetComponent<Button>();
        uim.ssBloodTapInfoText      = ssBloodTapInfoGO.GetComponent<Text>();
        uim.ssBloodTapButton        = ssBloodTapBtnGO.GetComponent<Button>();
        uim.ssShardHungerInfoText   = ssShardHungerInfoGO.GetComponent<Text>();
        uim.ssShardHungerButton     = ssShardHungerBtnGO.GetComponent<Button>();
        uim.settingsPanel           = settingsOverlay;
        uim.soundToggleText         = soundToggleGO.GetComponentInChildren<Text>();
        uim.notifToggleText         = notifToggleGO.GetComponentInChildren<Text>();
        uim.barracksInfoText        = barracksInfoGO.GetComponent<Text>();
        uim.upgradeBarracksButton   = upgradeBarracksGO.GetComponent<Button>();
        uim.barracksUpgradeCostText = upgradeBarracksGO.GetComponentInChildren<Text>();
        uim.damageLayer             = dmgLayerGO.GetComponent<RectTransform>();
        uim.tutorialPanel           = tutPanelGO;
        uim.tutorialTitleText       = tutTitleGO.GetComponent<Text>();
        uim.tutorialBodyText        = tutBodyGO.GetComponent<Text>();
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
        uim.featureVotePanel        = voteOverlay;
        uim.voteIssueText           = voteIssueTxtGO.GetComponent<Text>();
        uim.voteStatusText          = voteStatusTxtGO.GetComponent<Text>();
        uim.votePrevButton          = votePrevGO.GetComponent<Button>();
        uim.voteNextButton          = voteNextGO.GetComponent<Button>();
        uim.voteButton              = voteSubmitGO.GetComponent<Button>();
        uim.talentSelectionPanel    = talentOverlay;
        uim.talentHeaderText        = talentHeaderGO.GetComponent<Text>();
        uim.talentButton0           = talent0BtnGO.GetComponent<Button>();
        uim.talentButton1           = talent1BtnGO.GetComponent<Button>();
        uim.talentButton2           = talent2BtnGO.GetComponent<Button>();
        uim.talentButtonText0       = talent0BtnGO.GetComponentInChildren<Text>();
        uim.talentButtonText1       = talent1BtnGO.GetComponentInChildren<Text>();
        uim.talentButtonText2       = talent2BtnGO.GetComponentInChildren<Text>();
        uim.dailyChallengeRow       = dailyChallengeRowGO;
        uim.dailyChallengeButton    = dailyChallengeBtnGO.GetComponent<Button>();
        uim.dailyChallengeInfoText  = dailyChallengeInfoGO.GetComponent<Text>();
        uim.corruptionText          = corruptionTextGO.GetComponent<Text>();
        uim.purifyButton            = purifyBtnGO.GetComponent<Button>();
        uim.purifyButtonText        = purifyBtnGO.GetComponentInChildren<Text>();
        uim.desecrateButton         = desecrateBtnGO.GetComponent<Button>();
        uim.desecrateButtonText     = desecrateBtnGO.GetComponentInChildren<Text>();
        uim.soulSacrificeButton     = soulSacBtnGO.GetComponent<Button>();
        uim.soulSacrificeInfoText   = soulSacInfoGO.GetComponent<Text>();
        uim.questsPanel             = questsOverlay;
        uim.questInfoText0          = questInfo0GO.GetComponent<Text>();
        uim.questInfoText1          = questInfo1GO.GetComponent<Text>();
        uim.questInfoText2          = questInfo2GO.GetComponent<Text>();
        uim.questClaimButton0       = questClaim0GO.GetComponent<Button>();
        uim.questClaimButton1       = questClaim1GO.GetComponent<Button>();
        uim.questClaimButton2       = questClaim2GO.GetComponent<Button>();
        uim.questClaimButtonText0   = questClaim0GO.GetComponentInChildren<Text>();
        uim.questClaimButtonText1   = questClaim1GO.GetComponentInChildren<Text>();
        uim.questClaimButtonText2   = questClaim2GO.GetComponentInChildren<Text>();
        uim.adBoostRow              = adBoostRowGO;
        uim.watchAdButton           = watchAdBtnGO.GetComponent<Button>();
        uim.adBoostButtonText       = watchAdBtnGO.GetComponentInChildren<Text>();
        uim.iapShopPanel            = iapOverlay;
        uim.removeAdsButton         = removeAdsBtnGO.GetComponent<Button>();
        uim.removeAdsButtonText     = removeAdsBtnGO.GetComponentInChildren<Text>();
        uim.starterPackButton       = starterBtnGO.GetComponent<Button>();
        uim.starterPackButtonText   = starterBtnGO.GetComponentInChildren<Text>();
        uim.bloodBoostSmallButton   = boostSmallBtnGO.GetComponent<Button>();
        uim.bloodBoostLargeButton   = boostLargeBtnGO.GetComponent<Button>();
        uim.battleTabPanel          = battleScrollGO;
        uim.buildTabPanel           = buildScrollGO;
        uim.progressTabPanel        = progressScrollGO;
        uim.settingsTabPanel        = settingsScrollGO;
        clk.uiManager               = uim;

        // Wire buttons
        UnityEventTools.AddPersistentListener(farmBtnGO.GetComponent<Button>().onClick,               clk.OnFarmBlood);
        UnityEventTools.AddPersistentListener(buyTankGO.GetComponent<Button>().onClick,               clk.OnBuyTank);
        UnityEventTools.AddPersistentListener(buyBerserkerGO.GetComponent<Button>().onClick,          clk.OnBuyBerserker);
        UnityEventTools.AddPersistentListener(buyPaladinGO.GetComponent<Button>().onClick,            clk.OnBuyPaladin);
        UnityEventTools.AddPersistentListener(healBtnGO.GetComponent<Button>().onClick,               clk.OnHealSelf);
        UnityEventTools.AddPersistentListener(surgeBtnGO.GetComponent<Button>().onClick,              clk.OnUseSurge);
        UnityEventTools.AddPersistentListener(bloodStormBtnGO.GetComponent<Button>().onClick,         clk.OnUseBloodStorm);
        UnityEventTools.AddPersistentListener(bloodOathBtnGO.GetComponent<Button>().onClick,          clk.OnUseBloodOath);
        UnityEventTools.AddPersistentListener(warCryBtnGO.GetComponent<Button>().onClick,             clk.OnUseWarCry);
        UnityEventTools.AddPersistentListener(hexCurseBtnGO.GetComponent<Button>().onClick,           clk.OnUseHexCurse);
        UnityEventTools.AddPersistentListener(bloodShieldBtnGO.GetComponent<Button>().onClick,        clk.OnUseBloodShield);
        UnityEventTools.AddPersistentListener(bloodPactGO.GetComponent<Button>().onClick,             clk.OnUseBloodPact);
        UnityEventTools.AddPersistentListener(buyWorkerGO.GetComponent<Button>().onClick,             clk.OnBuyWorker);
        UnityEventTools.AddPersistentListener(buyShrineGO.GetComponent<Button>().onClick,            clk.OnBuyShrine);
        UnityEventTools.AddPersistentListener(clickPowerGO.GetComponent<Button>().onClick,           clk.OnBuyClickPower);
        UnityEventTools.AddPersistentListener(buyBloodWellGO.GetComponent<Button>().onClick,         clk.OnBuyBloodWell);
        UnityEventTools.AddPersistentListener(buyBloodRitualGO.GetComponent<Button>().onClick,        clk.OnBuyBloodRitual);
        UnityEventTools.AddPersistentListener(upgradeBarracksGO.GetComponent<Button>().onClick,       clk.OnUpgradeBarracks);
        UnityEventTools.AddPersistentListener(upgradeFortGO.GetComponent<Button>().onClick,           clk.OnUpgradeFortification);
        UnityEventTools.AddPersistentListener(upgradeWeaponGO.GetComponent<Button>().onClick,         clk.OnUpgradeWeapon);
        UnityEventTools.AddPersistentListener(upgradeArmorGO.GetComponent<Button>().onClick,          clk.OnUpgradeArmor);
        UnityEventTools.AddPersistentListener(upgradeTalismanGO.GetComponent<Button>().onClick,       clk.OnUpgradeTalisman);
        UnityEventTools.AddPersistentListener(formationBtnGO.GetComponent<Button>().onClick,          clk.OnToggleFormation);
        UnityEventTools.AddPersistentListener(prestigeBtnGO.GetComponent<Button>().onClick,           clk.OnPrestige);
        UnityEventTools.AddPersistentListener(pSoldierCapBtnGO.GetComponent<Button>().onClick,        clk.OnBuyPSoldierCap);
        UnityEventTools.AddPersistentListener(pClickBonusBtnGO.GetComponent<Button>().onClick,        clk.OnBuyPClickBonus);
        UnityEventTools.AddPersistentListener(pRitualEffBtnGO.GetComponent<Button>().onClick,         clk.OnBuyPRitualEff);
        UnityEventTools.AddPersistentListener(pWeaponHeadStartBtnGO.GetComponent<Button>().onClick,   clk.OnBuyPWeaponHeadStart);
        UnityEventTools.AddPersistentListener(pBloodTitheBtnGO.GetComponent<Button>().onClick,        clk.OnBuyPBloodTithe);
        UnityEventTools.AddPersistentListener(pIronWallBtnGO.GetComponent<Button>().onClick,          clk.OnBuyPIronWall);
        UnityEventTools.AddPersistentListener(pBountyBonusBtnGO.GetComponent<Button>().onClick,      clk.OnBuyPBountyBonus);
        UnityEventTools.AddPersistentListener(ssBossTimerBtnGO.GetComponent<Button>().onClick,        clk.OnBuySSBossTimer);
        UnityEventTools.AddPersistentListener(ssDoubleChestBtnGO.GetComponent<Button>().onClick,      clk.OnBuySSDoubleChest);
        UnityEventTools.AddPersistentListener(ssRollbackBtnGO.GetComponent<Button>().onClick,         clk.OnBuySSRollback);
        UnityEventTools.AddPersistentListener(ssBloodTapBtnGO.GetComponent<Button>().onClick,        clk.OnBuySSBloodTap);
        UnityEventTools.AddPersistentListener(ssShardHungerBtnGO.GetComponent<Button>().onClick,    clk.OnBuySSShardHunger);
        UnityEventTools.AddPersistentListener(upgradeSurgeBtnGO.GetComponent<Button>().onClick,      clk.OnUpgradeSurge);
        UnityEventTools.AddPersistentListener(upgradeHealBtnGO.GetComponent<Button>().onClick,       clk.OnUpgradeHealSelf);
        UnityEventTools.AddPersistentListener(depositBtnGO.GetComponent<Button>().onClick,            clk.OnDepositToBank);
        UnityEventTools.AddPersistentListener(withdrawBtnGO.GetComponent<Button>().onClick,           clk.OnWithdrawFromBank);
        UnityEventTools.AddPersistentListener(statsBtnGO.GetComponent<Button>().onClick,              clk.OnOpenStats);
        UnityEventTools.AddPersistentListener(settingsBtnGO.GetComponent<Button>().onClick,           clk.OnOpenSettings);
        UnityEventTools.AddPersistentListener(suggestBtnGO.GetComponent<Button>().onClick,            clk.OnOpenSuggest);
        UnityEventTools.AddPersistentListener(shopBtnGO.GetComponent<Button>().onClick,               clk.OnOpenShop);
        UnityEventTools.AddPersistentListener(watchAdBtnGO.GetComponent<Button>().onClick,            clk.OnWatchAd);
        UnityEventTools.AddPersistentListener(removeAdsBtnGO.GetComponent<Button>().onClick,          clk.OnBuyRemoveAds);
        UnityEventTools.AddPersistentListener(starterBtnGO.GetComponent<Button>().onClick,            clk.OnBuyStarterPack);
        UnityEventTools.AddPersistentListener(boostSmallBtnGO.GetComponent<Button>().onClick,         clk.OnBuyBloodBoostSmall);
        UnityEventTools.AddPersistentListener(boostLargeBtnGO.GetComponent<Button>().onClick,         clk.OnBuyBloodBoostLarge);
        UnityEventTools.AddPersistentListener(iapCloseGO.GetComponent<Button>().onClick,              clk.OnCloseShop);
        UnityEventTools.AddPersistentListener(openQuestsBtnGO.GetComponent<Button>().onClick,         clk.OnOpenQuests);
        UnityEventTools.AddPersistentListener(questsCloseGO.GetComponent<Button>().onClick,           clk.OnCloseQuests);
        UnityEventTools.AddPersistentListener(questClaim0GO.GetComponent<Button>().onClick,           clk.OnClaimQuest0);
        UnityEventTools.AddPersistentListener(questClaim1GO.GetComponent<Button>().onClick,           clk.OnClaimQuest1);
        UnityEventTools.AddPersistentListener(questClaim2GO.GetComponent<Button>().onClick,           clk.OnClaimQuest2);
        UnityEventTools.AddPersistentListener(statsCloseGO.GetComponent<Button>().onClick,            uim.HideStatsPanel);
        UnityEventTools.AddPersistentListener(settingsCloseGO.GetComponent<Button>().onClick,         uim.HideSettingsPanel);
        UnityEventTools.AddPersistentListener(soundToggleGO.GetComponent<Button>().onClick,           clk.OnToggleSound);
        UnityEventTools.AddPersistentListener(notifToggleGO.GetComponent<Button>().onClick,           clk.OnToggleNotifications);
        UnityEventTools.AddPersistentListener(resetDataGO.GetComponent<Button>().onClick,             clk.OnResetData);
        UnityEventTools.AddPersistentListener(offlineDismissGO.GetComponent<Button>().onClick,        uim.DismissOfflinePanel);
        UnityEventTools.AddPersistentListener(featureSubmitGO.GetComponent<Button>().onClick,         uim.SubmitFeature);
        UnityEventTools.AddPersistentListener(featureCancelGO.GetComponent<Button>().onClick,         uim.HideFeaturePanel);
        UnityEventTools.AddPersistentListener(voteOpenGO.GetComponent<Button>().onClick,              uim.ShowVotePanel);
        UnityEventTools.AddPersistentListener(votePrevGO.GetComponent<Button>().onClick,              uim.VotePrev);
        UnityEventTools.AddPersistentListener(voteNextGO.GetComponent<Button>().onClick,              uim.VoteNext);
        UnityEventTools.AddPersistentListener(voteSubmitGO.GetComponent<Button>().onClick,            uim.VoteOnCurrent);
        UnityEventTools.AddPersistentListener(voteRefreshGO.GetComponent<Button>().onClick,           uim.RefreshVoteList);
        UnityEventTools.AddPersistentListener(voteCancelGO.GetComponent<Button>().onClick,            uim.HideVotePanel);
        UnityEventTools.AddPersistentListener(talent0BtnGO.GetComponent<Button>().onClick,            clk.OnConfirmTalent0);
        UnityEventTools.AddPersistentListener(talent1BtnGO.GetComponent<Button>().onClick,            clk.OnConfirmTalent1);
        UnityEventTools.AddPersistentListener(talent2BtnGO.GetComponent<Button>().onClick,            clk.OnConfirmTalent2);
        UnityEventTools.AddPersistentListener(talentCancelGO.GetComponent<Button>().onClick,          clk.OnCancelPrestige);
        UnityEventTools.AddPersistentListener(dailyChallengeBtnGO.GetComponent<Button>().onClick,     clk.OnStartDailyChallenge);
        UnityEventTools.AddPersistentListener(purifyBtnGO.GetComponent<Button>().onClick,             clk.OnPurify);
        UnityEventTools.AddPersistentListener(desecrateBtnGO.GetComponent<Button>().onClick,          clk.OnUseDesecrate);
        UnityEventTools.AddPersistentListener(soulSacBtnGO.GetComponent<Button>().onClick,            clk.OnUseSoulSacrifice);
        UnityEventTools.AddPersistentListener(tutDismissGO.GetComponent<Button>().onClick,            uim.DismissTutorial);
        UnityEventTools.AddPersistentListener(tabBattleBtnGO.GetComponent<Button>().onClick,          clk.OnSelectBattleTab);
        UnityEventTools.AddPersistentListener(tabBuildBtnGO.GetComponent<Button>().onClick,           clk.OnSelectBuildTab);
        UnityEventTools.AddPersistentListener(tabProgressBtnGO.GetComponent<Button>().onClick,        clk.OnSelectProgressTab);
        UnityEventTools.AddPersistentListener(tabSettingsBtnGO.GetComponent<Button>().onClick,        clk.OnSelectSettingsTab);

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
