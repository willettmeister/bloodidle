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
// 1175–2385  Blood Surge card (Surge + SoulSac + Storm + StormUpgrade + Oath + OathUpgrade + WarCry + WarCryUpgrade + HexCurse + HexCurseUpgrade + BloodShield + SoldierSac + DesecrateUpgrade)
// battleContent height: 2410
//
// ── BUILD TAB (y in buildContent) ───────────────────────────────────────────
//   10–215   Barracks card (+ Auto-Buy toggle)
//  225–390   Fortifications card
//  400–905   Workers card (hidden until WorkersUnlocked)
//  915–1160  Equipment card (hidden until WorkersUnlocked)
// 1170–1385  Blood Ritual + Blood Pact (hidden until WorkersUnlocked)
// 1395–1615  Blood Bank card (+ Interest Upgrade row)
// 1625–1715  Cursed Blood toggle (hidden until wave 7)
// 1725–1815  Kill Income upgrade card (hidden until 10 kills)
// buildContent height: 1825
//
// ── PROGRESS TAB (y in progressContent) ─────────────────────────────────────
//   10–195   Prestige card (hidden until wave 20)
//  205–748   Prestige Shop (hidden until prestige 1)
//  758–1314  Soul Shard Shop (hidden until first boss)
// 1324–1389  Daily Quests row
// 1394–1459  Watch Ad row
// progressContent height: 1479
//
// ── SETTINGS TAB (y in settingsContent) ─────────────────────────────────────
//   10–240   Utility buttons (Stats | Settings row, Suggest | Shop row)
//  250–330   Speed toggle button
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

        var (battleScrollGO,   battleContent)   = MakeTabScroll(tabAreaGO, "BattleTab",   2410f);
        var (buildScrollGO,    buildContent)    = MakeTabScroll(tabAreaGO, "BuildTab",    1825f);
        var (progressScrollGO, progressContent) = MakeTabScroll(tabAreaGO, "ProgressTab", 1479f);
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

        var truceBtnGO = Btn(content, "TruceButton", "Skip Wave\n(100 blood)", 26, HC("1A1A00"));
        PT(truceBtnGO, 287, 48, +300, 380);

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
        PT(upgradeHealBtnGO, 657, 64, -160, 510);

        var autoHealBtnGO = Btn(content, "AutoHealButton", "Auto-Heal: OFF", 26, HC("6A006A"));
        PT(autoHealBtnGO, 657, 64, +245, 260);

        var corruptionTextGO = Label(content, "CorruptionText", "", 28, new Color(0.8f, 0.2f, 0.2f), TextAnchor.MiddleLeft);
        PF(corruptionTextGO, 725, 30, 50);
        corruptionTextGO.SetActive(false);

        var purifyBtnGO = Btn(content, "PurifyButton", "Purify\n(3 shards)", 28, HC("4A0A0A"));
        PT(purifyBtnGO, 757, 38, -310, 280);
        purifyBtnGO.SetActive(false);

        var desecrateBtnGO = Btn(content, "DesecrateButton", "Desecrate\n(-1 corrupt +50% burst)", 26, HC("3A006A"));
        PT(desecrateBtnGO, 757, 38, +5, 300);
        desecrateBtnGO.SetActive(false);

        var autoDesecrateBtnGO = Btn(content, "AutoDesecrateButton", "Auto-Desecrate: OFF", 24, HC("2A004A"));
        PT(autoDesecrateBtnGO, 757, 38, +320, 250);
        autoDesecrateBtnGO.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // FARM BLOOD  (battleContent y 815–1025)
        // ════════════════════════════════════════════════════════════════════
        var farmBtnGO = Btn(content, "FarmBloodButton", "FARM BLOOD", 90, Crimson);
        PT(farmBtnGO, 815, 175, 0, 680);

        var farmBloodInfoGO = Label(content, "FarmBloodInfoText", "+1/tap", 28, TextSec, TextAnchor.MiddleCenter);
        PT(farmBloodInfoGO, 993, 30, 0, 680);

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
        PF(bloodSurgePanel, 1175, 1384);

        Panel(bloodSurgePanel, "BloodSurgeCardBg", 0, 1384, Surface1, 24);
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
        PT(upgradeSurgeBtnGO, 128, 64, -160, 510);

        var autoSurgeBtnGO = Btn(bloodSurgePanel, "AutoSurgeButton", "Auto-Surge: OFF", 26, HC("6A0000"));
        PT(autoSurgeBtnGO, 128, 64, +245, 260);

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
        PT(bloodStormBtnGO, 364, 42, -165, 400);

        var autoStormBtnGO = Btn(bloodSurgePanel, "AutoStormButton", "Auto-Storm: OFF", 24, HC("0A2A4A"));
        PT(autoStormBtnGO, 364, 42, +245, 260);

        var upgradeBloodStormBtnGO = Btn(bloodSurgePanel, "UpgradeBloodStormButton",
            "Upgrade Storm\n(80 blood)", 28, HC("0A2A5A"));
        PT(upgradeBloodStormBtnGO, 414, 52, 0, 680);

        var bloodOathDivGO = bloodSurgePanel.CreateChild("BloodOathDiv");
        bloodOathDivGO.AddImage(HC("2D2D4A")); PT(bloodOathDivGO, 466, 2, 0, 640);

        var bloodOathInfoGO = Label(bloodSurgePanel, "BloodOathInfoText",
            "Blood Oath  —  Unlocks at wave 15",
            30, new Color(0.85f, 0.5f, 1f), TextAnchor.MiddleLeft);
        PT(bloodOathInfoGO, 474, 44, -140, 620);

        var bloodOathBtnGO = Btn(bloodSurgePanel, "BloodOathButton",
            "Oath! (200 blood)", 30, HC("3A006A"));
        PT(bloodOathBtnGO, 524, 42, -165, 400);

        var autoBloodOathBtnGO = Btn(bloodSurgePanel, "AutoBloodOathButton", "Auto-Oath: OFF", 26, HC("1A0040"));
        PT(autoBloodOathBtnGO, 524, 42, +245, 260);

        var upgradeBloodOathBtnGO = Btn(bloodSurgePanel, "UpgradeBloodOathButton",
            "Upgrade Blood Oath\n(150 blood)", 28, HC("2A0050"));
        PT(upgradeBloodOathBtnGO, 572, 52, 0, 680);

        var warCryDivGO = bloodSurgePanel.CreateChild("WarCryDiv");
        warCryDivGO.AddImage(HC("2D2D4A")); PT(warCryDivGO, 626, 2, 0, 640);

        var warCryInfoGO = Label(bloodSurgePanel, "WarCryInfoText",
            "War Cry  —  Unlocks at wave 5",
            30, new Color(1f, 0.7f, 0.3f), TextAnchor.MiddleLeft);
        PT(warCryInfoGO, 634, 44, -140, 620);

        var warCryBtnGO = Btn(bloodSurgePanel, "WarCryButton",
            "War Cry! (30 blood)", 30, HC("4A2800"));
        PT(warCryBtnGO, 684, 42, -165, 400);

        var autoWarCryBtnGO = Btn(bloodSurgePanel, "AutoWarCryButton", "Auto-Cry: OFF", 26, HC("2A1200"));
        PT(autoWarCryBtnGO, 684, 42, +245, 260);

        var upgradeWarCryBtnGO = Btn(bloodSurgePanel, "UpgradeWarCryButton",
            "Upgrade War Cry\n(50 blood)", 28, HC("4A3000"));
        PT(upgradeWarCryBtnGO, 732, 52, 0, 680);

        var hexCurseDivGO = bloodSurgePanel.CreateChild("HexCurseDiv");
        hexCurseDivGO.AddImage(HC("2D2D4A")); PT(hexCurseDivGO, 786, 2, 0, 640);

        var hexCurseInfoGO = Label(bloodSurgePanel, "HexCurseInfoText",
            "Hex Curse  —  Unlocks at wave 4",
            30, new Color(0.3f, 0.85f, 0.4f), TextAnchor.MiddleLeft);
        PT(hexCurseInfoGO, 794, 44, -140, 620);

        var hexCurseBtnGO = Btn(bloodSurgePanel, "HexCurseButton",
            "Hex! (20 blood)", 30, HC("003A10"));
        PT(hexCurseBtnGO, 844, 42, -165, 400);

        var autoHexCurseBtnGO = Btn(bloodSurgePanel, "AutoHexCurseButton", "Auto-Hex: OFF", 26, HC("001A08"));
        PT(autoHexCurseBtnGO, 844, 42, +245, 260);

        var upgradeHexCurseBtnGO = Btn(bloodSurgePanel, "UpgradeHexCurseButton",
            "Upgrade Hex Curse\n(40 blood)", 28, HC("003010"));
        PT(upgradeHexCurseBtnGO, 892, 52, 0, 680);

        var bloodShieldDivGO = bloodSurgePanel.CreateChild("BloodShieldDiv");
        bloodShieldDivGO.AddImage(HC("2D2D4A")); PT(bloodShieldDivGO, 946, 2, 0, 640);

        var bloodShieldInfoGO = Label(bloodSurgePanel, "BloodShieldInfoText",
            "Blood Shield  —  Unlocks at 150 total blood",
            30, new Color(0.4f, 0.8f, 1f), TextAnchor.MiddleLeft);
        PT(bloodShieldInfoGO, 954, 44, -140, 620);

        var bloodShieldBtnGO = Btn(bloodSurgePanel, "BloodShieldButton",
            "Shield! (30 blood)", 30, HC("003A5A"));
        PT(bloodShieldBtnGO, 1004, 42, -165, 400);

        var autoBloodShieldBtnGO = Btn(bloodSurgePanel, "AutoBloodShieldButton", "Auto-Shield: OFF", 26, HC("001020"));
        PT(autoBloodShieldBtnGO, 1004, 42, +245, 260);

        var soldierSacDivGO = bloodSurgePanel.CreateChild("SoldierSacDiv");
        soldierSacDivGO.AddImage(HC("2D2D4A")); PT(soldierSacDivGO, 1046, 2, 0, 640);

        var soldierSacInfoGO = Label(bloodSurgePanel, "SoldierSacInfoText",
            "Soldier Sacrifice  —  Unlocks at wave 3  (2+ soldiers)",
            30, new Color(0.9f, 0.5f, 0.5f), TextAnchor.MiddleLeft);
        PT(soldierSacInfoGO, 1054, 44, -140, 620);

        var soldierSacBtnGO = Btn(bloodSurgePanel, "SoldierSacButton",
            "Sacrifice! (3× HP burst)", 30, HC("4A0A0A"));
        PT(soldierSacBtnGO, 1104, 42, 0, 680);

        var upgradeDesecrateBtnGO = Btn(bloodSurgePanel, "UpgradeDesecrateButton",
            "Upgrade Desecrate\n(80 blood)", 28, HC("2A004A"));
        PT(upgradeDesecrateBtnGO, 1152, 52, 0, 680);
        upgradeDesecrateBtnGO.SetActive(false);

        var entropyDivGO = bloodSurgePanel.CreateChild("EntropyDiv");
        entropyDivGO.AddImage(HC("2D2D4A")); PT(entropyDivGO, 1216, 2, 0, 640);

        var entropyInfoGO = Label(bloodSurgePanel, "EntropyInfoText",
            $"Entropy  —  Unlocks at wave {GameManager.EntropyUnlockWave}",
            30, new Color(0.6f, 0.9f, 1f), TextAnchor.MiddleLeft);
        PT(entropyInfoGO, 1224, 44, -140, 620);

        var entropyBtnGO = Btn(bloodSurgePanel, "EntropyButton",
            "Entropy! (300 blood)", 30, HC("004A5A"));
        PT(entropyBtnGO, 1274, 42, 0, 680);

        var upgradeEntropyBtnGO = Btn(bloodSurgePanel, "UpgradeEntropyButton",
            "Upgrade Entropy\n(200 blood)", 28, HC("002A3A"));
        PT(upgradeEntropyBtnGO, 1322, 52, 0, 680);
        upgradeEntropyBtnGO.SetActive(false);

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
        PF(workersPanel, 400, 505);

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
        // BARRACKS CARD  (buildContent y 10–215)
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "BarracksCardBg", 10, 205, Surface1, 24);
        { var a = content.CreateChild("BarracksCardAccent"); a.AddImage(Brown); PF(a, 10, 4, 24); }

        var barracksInfoGO = Label(content, "BarracksInfoText",
            "Barracks  Lv.1  —  Max 10 soldiers",
            34, Color.white, TextAnchor.MiddleLeft);
        PT(barracksInfoGO, 33, 52, -175, 540);

        var upgradeBarracksGO = Btn(content, "UpgradeBarracksButton", "Upgrade\n(20 wood)", 34, Brown);
        PT(upgradeBarracksGO, 23, 110, +232, 370);

        var autoBuyBtnGO = Btn(content, "AutoBuyButton", "Auto-Buy: OFF", 30, HC("1A2A1A"));
        PF(autoBuyBtnGO, 140, 60, 40);

        // ════════════════════════════════════════════════════════════════════
        // FORTIFICATIONS CARD  (buildContent y 225–390)
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "FortificationsCardBg", 225, 165, Surface1, 24);
        { var a = content.CreateChild("FortCardAccent"); a.AddImage(HC("4A3728")); PF(a, 225, 4, 24); }

        var fortInfoGO = Label(content, "FortificationsInfoText",
            "Fortifications  Lv.0/10  (−0% enemy HP)",
            34, Color.white, TextAnchor.MiddleLeft);
        PT(fortInfoGO, 248, 52, -175, 540);

        var upgradeFortGO = Btn(content, "UpgradeFortificationButton", "Fortify\n(50 wood)", 34, Brown);
        PT(upgradeFortGO, 238, 110, +232, 370);

        // ════════════════════════════════════════════════════════════════════
        // EQUIPMENT CARD  (buildContent y 875–1120) — same unlock as workers
        // ════════════════════════════════════════════════════════════════════
        var equipmentPanel = content.CreateChild("EquipmentPanel");
        equipmentPanel.AddImage(Color.clear);
        PF(equipmentPanel, 915, 303);

        Panel(equipmentPanel, "EquipmentCardBg", 0, 303, Surface1, 24);
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

        // War Banner row
        var bannerInfoGO = Label(equipmentPanel, "BannerInfoText",
            "War Banner  Lv.0/5  (streak cap x3.0)", 28, Color.white, TextAnchor.MiddleLeft);
        PT(bannerInfoGO, 242, 44, -175, 500);
        var upgradeBannerGO = Btn(equipmentPanel, "UpgradeBannerButton", "Upgrade\n(30 wood)", 28, HC("BF360C"));
        PT(upgradeBannerGO, 238, 56, +232, 330);

        equipmentPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // BLOOD RITUAL + BLOOD PACT CARD  (buildContent y 1130–1345)
        // ════════════════════════════════════════════════════════════════════
        var bloodRitualPanel = content.CreateChild("BloodRitualPanel");
        bloodRitualPanel.AddImage(Color.clear);
        PF(bloodRitualPanel, 1170, 215);

        Panel(bloodRitualPanel, "BloodRitualCardBg", 0, 215, Surface1, 24);
        { var a = bloodRitualPanel.CreateChild("RitualAccent"); a.AddImage(Purple); PF(a, 0, 4, 24); }

        var bloodRitualInfoGO = Label(bloodRitualPanel, "BloodRitualInfoText",
            "Blood Ritual  —  passive blood income",
            32, Color.white, TextAnchor.MiddleLeft);
        PT(bloodRitualInfoGO, 18, 52, -175, 500);

        var buyBloodRitualGO = Btn(bloodRitualPanel, "BuyBloodRitualButton",
            "Perform\n(30 wood)", 34, Purple);
        PT(buyBloodRitualGO, 10, 110, +130, 250);

        var autoRitualBtnGO = Btn(bloodRitualPanel, "AutoBuyRitualButton", "Auto-Ritual: OFF", 26, HC("2A0A4A"));
        PT(autoRitualBtnGO, 10, 110, +365, 190);

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
        // BLOOD BANK CARD  (buildContent y 1395–1615)
        // ════════════════════════════════════════════════════════════════════
        var bloodBankPanel = content.CreateChild("BloodBankPanel");
        bloodBankPanel.AddImage(Color.clear);
        PF(bloodBankPanel, 1395, 220);

        Panel(bloodBankPanel, "BloodBankCardBg", 0, 220, Surface1, 24);
        { var a = bloodBankPanel.CreateChild("BankAccent"); a.AddImage(Gold); PF(a, 0, 4, 24); }

        var bankTitleGO = Label(bloodBankPanel, "BloodBankTitle", "Blood Bank", 40, Gold, TextAnchor.MiddleLeft);
        PT(bankTitleGO, 8, 44, -200, 400);

        var bankInfoGO = Label(bloodBankPanel, "BloodBankInfoText",
            "Blood Bank  0/10,000  (+2.0%/hr)", 30, Color.white, TextAnchor.MiddleLeft);
        PT(bankInfoGO, 58, 42, -175, 520);

        var bankAccruedGO = Label(bloodBankPanel, "BloodBankAccruedText",
            "Interest accrued: none yet", 26, TextSec, TextAnchor.MiddleLeft);
        PT(bankAccruedGO, 104, 36, -175, 520);

        var depositBtnGO = Btn(bloodBankPanel, "DepositBloodButton", "Deposit\n10%", 30, Brown);
        PT(depositBtnGO, 52, 100, +162, 130);

        var withdrawBtnGO = Btn(bloodBankPanel, "WithdrawBloodButton", "Withdraw\nAll", 30, Green);
        PT(withdrawBtnGO, 52, 100, +302, 130);

        var autoBankBtnGO = Btn(bloodBankPanel, "AutoDepositButton", "Auto: OFF", 26, HC("3A2A00"));
        PT(autoBankBtnGO, 52, 100, +442, 130);

        var bankDiv = bloodBankPanel.CreateChild("BankDiv");
        bankDiv.AddImage(HC("3A3A1A")); PT(bankDiv, 158, 2, 0, 640);

        var bankInterestUpgradeBtnGO = Btn(bloodBankPanel, "BankInterestUpgradeButton",
            "Upgrade\nInterest", 26, HC("4A3A00"));
        PT(bankInterestUpgradeBtnGO, 162, 52, +232, 200);

        var bankInterestCostGO = Label(bloodBankPanel, "BankInterestUpgradeCostText",
            "Interest Lv.0/3  2.0%→2.5%  (500 blood)", 24, TextSec, TextAnchor.MiddleLeft);
        PT(bankInterestCostGO, 166, 44, -175, 400);

        // ════════════════════════════════════════════════════════════════════
        // CURSED BLOOD TOGGLE  (buildContent y 1625–1715) — hidden until wave 7
        // ════════════════════════════════════════════════════════════════════
        var cursedBloodPanel = content.CreateChild("CursedBloodPanel");
        cursedBloodPanel.AddImage(Color.clear);
        PF(cursedBloodPanel, 1625, 90);

        Panel(cursedBloodPanel, "CursedBloodCardBg", 0, 90, HC("2A0A0A"), 24);

        var cursedBloodInfoGO = Label(cursedBloodPanel, "CursedBloodInfoText",
            "Cursed Blood: damage taken → 10% blood", 30, HC("FF6666"), TextAnchor.MiddleLeft);
        PT(cursedBloodInfoGO, 8, 34, -160, 560);

        var cursedBloodBtnGO = Btn(cursedBloodPanel, "CursedBloodButton", "Cursed Blood: OFF", 30, HC("8B0000"));
        PF(cursedBloodBtnGO, 46, 38, 20);

        cursedBloodPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // KILL INCOME UPGRADE CARD  (buildContent y 1725–1815) — hidden until 10 kills
        // ════════════════════════════════════════════════════════════════════
        var killIncomePanel = content.CreateChild("KillIncomePanel");
        killIncomePanel.AddImage(Color.clear);
        PF(killIncomePanel, 1725, 90);

        Panel(killIncomePanel, "KillIncomeBg", 0, 90, Surface1, 24);
        { var a = killIncomePanel.CreateChild("KillIncomeAccent"); a.AddImage(Crimson); PF(a, 0, 4, 24); }

        var killIncomeUpgradeBtnGO = Btn(killIncomePanel, "KillIncomeUpgradeButton",
            "Upgrade\n(200 blood)", 28, HC("5A0A0A"));
        PT(killIncomeUpgradeBtnGO, 12, 62, +232, 200);

        var killIncomeCostGO = Label(killIncomePanel, "KillIncomeUpgradeCostText",
            "Kill Income Lv.0/3  0.01→0.02/kill  (200 blood)", 26, Color.white, TextAnchor.MiddleLeft);
        PT(killIncomeCostGO, 18, 52, -175, 390);

        killIncomePanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // Switch to PROGRESS tab content
        // ════════════════════════════════════════════════════════════════════
        content = progressContent;

        // ════════════════════════════════════════════════════════════════════
        // PRESTIGE CARD  (progressContent y 10–150) — visible at wave 20+
        // ════════════════════════════════════════════════════════════════════
        var prestigePanel = content.CreateChild("PrestigePanel");
        prestigePanel.AddImage(Color.clear);
        PF(prestigePanel, 10, 185);

        Panel(prestigePanel, "PrestigeCardBg", 0, 185, HC("1A0A00"), 24);

        var prestigeInfoGO = Label(prestigePanel, "PrestigeInfoText",
            "Prestige  —  reset for a blood bonus",
            32, Gold, TextAnchor.MiddleLeft);
        PT(prestigeInfoGO, 18, 52, -175, 500);

        var prestigeBtnGO = Btn(prestigePanel, "PrestigeButton",
            "PRESTIGE\n(reset progress)", 32, Amber);
        PT(prestigeBtnGO, 10, 110, +232, 370);

        var prestigeMilestoneGO = Label(prestigePanel, "PrestigeMilestoneText",
            "Milestone 0/4", 26, HC("FFD700"), TextAnchor.MiddleCenter);
        PF(prestigeMilestoneGO, 130, 34, 20);

        prestigePanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // PRESTIGE SHOP CARD  (progressContent y 160–645) — visible after first prestige
        // ════════════════════════════════════════════════════════════════════
        var prestigeShopPanel = content.CreateChild("PrestigeShopPanel");
        prestigeShopPanel.AddImage(Color.clear);
        PF(prestigeShopPanel, 205, 1297);

        Panel(prestigeShopPanel, "PrestigeShopCardBg", 0, 1297, HC("150A30"), 24);

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

        // Row 8 — Crimson Rite
        var pBloodRitualStartInfoGO = Label(prestigeShopPanel, "PBloodRitualStartInfoText",
            "Crimson Rite +1 Ritual  (Lv.0)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pBloodRitualStartInfoGO, 466, 48, -175, 500);
        var pBloodRitualStartBtnGO = Btn(prestigeShopPanel, "PBloodRitualStartButton", "Buy (1 PP)", 30, HC("B71C1C"));
        PT(pBloodRitualStartBtnGO, 464, 54, +245, 260);

        // Row 9 — Blood Mastery
        var pBloodMasteryInfoGO = Label(prestigeShopPanel, "PBloodMasteryInfoText",
            "Blood Mastery +5 Vet cap  (Lv.0, cap 10)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pBloodMasteryInfoGO, 524, 48, -175, 500);
        var pBloodMasteryBtnGO = Btn(prestigeShopPanel, "PBloodMasteryButton", "Buy (1 PP)", 30, HC("1A237E"));
        PT(pBloodMasteryBtnGO, 522, 54, +245, 260);

        // Row 10 — Sacred Ground
        var pSacredGroundInfoGO = Label(prestigeShopPanel, "PSacredGroundInfoText",
            "Sacred Ground +25% shrine income  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pSacredGroundInfoGO, 582, 48, -175, 500);
        var pSacredGroundBtnGO = Btn(prestigeShopPanel, "PSacredGroundButton", "Buy (1 PP)", 30, HC("1B5E20"));
        PT(pSacredGroundBtnGO, 580, 54, +245, 260);

        // Row 11 — Eternal Flame
        var pEternalFlameInfoGO = Label(prestigeShopPanel, "PEternalFlameInfoText",
            "Eternal Flame +25% Blood Well yield  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pEternalFlameInfoGO, 640, 48, -175, 500);
        var pEternalFlameBtnGO = Btn(prestigeShopPanel, "PEternalFlameButton", "Buy (1 PP)", 30, HC("E65100"));
        PT(pEternalFlameBtnGO, 638, 54, +245, 260);

        // Row 12 — War Machine
        var pWarMachineInfoGO = Label(prestigeShopPanel, "PWarMachineInfoText",
            "War Machine +5% soldier attack  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pWarMachineInfoGO, 698, 48, -175, 500);
        var pWarMachineBtnGO = Btn(prestigeShopPanel, "PWarMachineButton", "Buy (1 PP)", 30, HC("B71C1C"));
        PT(pWarMachineBtnGO, 696, 54, +245, 260);

        var pCrimsonLegacyInfoGO = Label(prestigeShopPanel, "PCrimsonLegacyInfoText",
            "Crimson Legacy +1% blood income per prestige  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pCrimsonLegacyInfoGO, 756, 48, -175, 500);
        var pCrimsonLegacyBtnGO = Btn(prestigeShopPanel, "PCrimsonLegacyButton", "Buy (1 PP)", 30, HC("880E4F"));
        PT(pCrimsonLegacyBtnGO, 754, 54, +245, 260);

        var pBloodlineInfoGO = Label(prestigeShopPanel, "PBloodlineInfoText",
            "Bloodline +100 starting blood per run  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pBloodlineInfoGO, 814, 48, -175, 500);
        var pBloodlineBtnGO = Btn(prestigeShopPanel, "PBloodlineButton", "Buy (1 PP)", 30, HC("B71C1C"));
        PT(pBloodlineBtnGO, 812, 54, +245, 260);

        // Row 15 — Iron Bastion
        var pIronBastionInfoGO = Label(prestigeShopPanel, "PIronBastionInfoText",
            "Iron Bastion +5 max HP to all soldier types  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pIronBastionInfoGO, 872, 48, -175, 500);
        var pIronBastionBtnGO = Btn(prestigeShopPanel, "PIronBastionButton", "Buy (1 PP)", 30, HC("1A237E"));
        PT(pIronBastionBtnGO, 870, 54, +245, 260);

        // Row 16 — Blood Price
        var pBloodPriceInfoGO = Label(prestigeShopPanel, "PBloodPriceInfoText",
            "Blood Price +1% kill income rate  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pBloodPriceInfoGO, 930, 48, -175, 500);
        var pBloodPriceBtnGO = Btn(prestigeShopPanel, "PBloodPriceButton", "Buy (1 PP)", 30, HC("880E4F"));
        PT(pBloodPriceBtnGO, 928, 54, +245, 260);

        // Row 17 — Void Pact
        var pVoidPactInfoGO = Label(prestigeShopPanel, "PVoidPactInfoText",
            "Void Pact +1 shard per boss kill  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pVoidPactInfoGO, 988, 48, -175, 500);
        var pVoidPactBtnGO = Btn(prestigeShopPanel, "PVoidPactButton", "Buy (1 PP)", 30, HC("4A0072"));
        PT(pVoidPactBtnGO, 986, 54, +245, 260);

        // Row 18 — War Fervor (+2% soldier damage per level)
        var pWarFervorInfoGO = Label(prestigeShopPanel, "PWarFervorInfoText",
            "War Fervor +2% soldier damage  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pWarFervorInfoGO, 1046, 48, -175, 500);
        var pWarFervorBtnGO = Btn(prestigeShopPanel, "PWarFervorButton", "Buy (1 PP)", 30, HC("B71C1C"));
        PT(pWarFervorBtnGO, 1044, 54, +245, 260);

        // Row 19 — Wellspring (+20% Blood Well output per level)
        var pWellspringInfoGO = Label(prestigeShopPanel, "PWellspringInfoText",
            "Wellspring +20% Blood Well output  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pWellspringInfoGO, 1104, 48, -175, 500);
        var pWellspringBtnGO = Btn(prestigeShopPanel, "PWellspringButton", "Buy (1 PP)", 30, HC("00695C"));
        PT(pWellspringBtnGO, 1102, 54, +245, 260);

        // Row 20 — Battle Rhythm (+0.25s combo window per level)
        var pBattleRhythmInfoGO = Label(prestigeShopPanel, "PBattleRhythmInfoText",
            "Battle Rhythm +0.25s combo window  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pBattleRhythmInfoGO, 1162, 48, -175, 500);
        var pBattleRhythmBtnGO = Btn(prestigeShopPanel, "PBattleRhythmButton", "Buy (1 PP)", 30, HC("F57F17"));
        PT(pBattleRhythmBtnGO, 1160, 54, +245, 260);

        // Row 21 — Soul Tide (+2% enemy HP healed on kill per level)
        var pSoulTideInfoGO = Label(prestigeShopPanel, "PSoulTideInfoText",
            "Soul Tide +2% enemy HP healed on kill  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(pSoulTideInfoGO, 1220, 48, -175, 500);
        var pSoulTideBtnGO = Btn(prestigeShopPanel, "PSoulTideButton", "Buy (1 PP)", 30, HC("00838F"));
        PT(pSoulTideBtnGO, 1218, 54, +245, 260);

        prestigeShopPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // SOUL SHARD SHOP CARD  (y 2985–3235) — visible after first boss kill
        // ════════════════════════════════════════════════════════════════════
        var soulShardShopPanel = content.CreateChild("SoulShardShopPanel");
        soulShardShopPanel.AddImage(Color.clear);
        PF(soulShardShopPanel, 758, 1658);

        Panel(soulShardShopPanel, "SoulShardShopCardBg", 0, 1658, HC("0A1A30"), 24);

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

        // Row 6 — Soul Harvest
        var ssSoulHarvestInfoGO = Label(soulShardShopPanel, "SSSoulHarvestInfoText",
            "Soul Harvest 1.00% enemy HP → blood  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssSoulHarvestInfoGO, 352, 48, -175, 500);
        var ssSoulHarvestBtnGO = Btn(soulShardShopPanel, "SSSoulHarvestButton", "Buy (1 ⬡)", 30, HC("BF360C"));
        PT(ssSoulHarvestBtnGO, 350, 54, +245, 260);

        // Row 7 — Crimson Pulse (tier-1, costs 1 shard)
        var ssCrimsonPulseInfoGO = Label(soulShardShopPanel, "SSCrimsonPulseInfoText",
            "Crimson Pulse +15% ritual income  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssCrimsonPulseInfoGO, 410, 48, -175, 500);
        var ssCrimsonPulseBtnGO = Btn(soulShardShopPanel, "SSCrimsonPulseButton", "Buy (1 ⬡)", 30, HC("7B1FA2"));
        PT(ssCrimsonPulseBtnGO, 408, 54, +245, 260);

        // Row 8 — Crimson Brand (tier-1, costs 1 shard)
        var ssCrimsonBrandInfoGO = Label(soulShardShopPanel, "SSCrimsonBrandInfoText",
            "Crimson Brand +20% boss dmg  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssCrimsonBrandInfoGO, 468, 48, -175, 500);
        var ssCrimsonBrandBtnGO = Btn(soulShardShopPanel, "SSCrimsonBrandButton", "Buy (1 ⬡)", 30, HC("B71C1C"));
        PT(ssCrimsonBrandBtnGO, 466, 54, +245, 260);

        // Row 9 — War Spoils (tier-1, costs 1 shard)
        var ssWarSpoilsInfoGO = Label(soulShardShopPanel, "SSWarSpoilsInfoText",
            "War Spoils +15% all wave rewards  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssWarSpoilsInfoGO, 526, 48, -175, 500);
        var ssWarSpoilsBtnGO = Btn(soulShardShopPanel, "SSWarSpoilsButton", "Buy (1 ⬡)", 30, HC("F57F17"));
        PT(ssWarSpoilsBtnGO, 524, 54, +245, 260);

        // Row 10 — Ghost Strike (tier-1, costs 1 shard)
        var ssGhostStrikeInfoGO = Label(soulShardShopPanel, "SSGhostStrikeInfoText",
            "Ghost Strike +20% Blood Storm damage  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssGhostStrikeInfoGO, 584, 48, -175, 500);
        var ssGhostStrikeBtnGO = Btn(soulShardShopPanel, "SSGhostStrikeButton", "Buy (1 ⬡)", 30, HC("37474F"));
        PT(ssGhostStrikeBtnGO, 582, 54, +245, 260);

        // Row 11 — Death's Bounty (tier-1, costs 1 shard)
        var ssDeathsBountyInfoGO = Label(soulShardShopPanel, "SSDeathsBountyInfoText",
            "Death's Bounty +20% Soul Sacrifice blood  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssDeathsBountyInfoGO, 642, 48, -175, 500);
        var ssDeathsBountyBtnGO = Btn(soulShardShopPanel, "SSDeathsBountyButton", "Buy (1 ⬡)", 30, HC("4A148C"));
        PT(ssDeathsBountyBtnGO, 640, 54, +245, 260);

        // Row 12 — Void Conduit (tier-2, costs 2 shards)
        var ssVoidConduitInfoGO = Label(soulShardShopPanel, "SSVoidConduitInfoText",
            "Void Conduit +15% all income  (Lv.0/2)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssVoidConduitInfoGO, 700, 48, -175, 500);
        var ssVoidConduitBtnGO = Btn(soulShardShopPanel, "SSVoidConduitButton", "Buy (2 ⬡)", 30, HC("00695C"));
        PT(ssVoidConduitBtnGO, 698, 54, +245, 260);

        // Row 13 — Blood Echo (tier-2, costs 2 shards)
        var ssBloodEchoInfoGO = Label(soulShardShopPanel, "SSBloodEchoInfoText",
            "Blood Echo +0.5/s per boss killed  (Lv.0/2)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssBloodEchoInfoGO, 758, 48, -175, 500);
        var ssBloodEchoBtnGO = Btn(soulShardShopPanel, "SSBloodEchoButton", "Buy (2 ⬡)", 30, HC("880E4F"));
        PT(ssBloodEchoBtnGO, 756, 54, +245, 260);

        // Row 14 — Iron Marrow (tier-2, costs 2 shards)
        var ssIronMarrowInfoGO = Label(soulShardShopPanel, "SSIronMarrowInfoText",
            "Iron Marrow +3 atk all soldiers  (Lv.0/2)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssIronMarrowInfoGO, 816, 48, -175, 500);
        var ssIronMarrowBtnGO = Btn(soulShardShopPanel, "SSIronMarrowButton", "Buy (2 ⬡)", 30, HC("4A148C"));
        PT(ssIronMarrowBtnGO, 814, 54, +245, 260);

        // Row 15 — Wrath Bloom (tier-2, costs 2 shards)
        var ssWrathBloomInfoGO = Label(soulShardShopPanel, "SSWrathBloomInfoText",
            "Wrath Bloom boss kill extends Surge +10s  (Lv.0/2)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssWrathBloomInfoGO, 874, 48, -175, 500);
        var ssWrathBloomBtnGO = Btn(soulShardShopPanel, "SSWrathBloomButton", "Buy (2 ⬡)", 30, HC("BF360C"));
        PT(ssWrathBloomBtnGO, 872, 54, +245, 260);

        // Row 16 — Blood Nova (tier-2, costs 2 shards)
        var ssBloodNovaInfoGO = Label(soulShardShopPanel, "SSBloodNovaInfoText",
            "Blood Nova Storm hits +10% enemy max HP  (Lv.0/2)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssBloodNovaInfoGO, 932, 48, -175, 500);
        var ssBloodNovaBtnGO = Btn(soulShardShopPanel, "SSBloodNovaButton", "Buy (2 ⬡)", 30, HC("D32F2F"));
        PT(ssBloodNovaBtnGO, 930, 54, +245, 260);

        // Row 17 — Echo Surge (tier-2, costs 2 shards)
        var ssEchoSurgeInfoGO = Label(soulShardShopPanel, "SSEchoSurgeInfoText",
            "Echo Surge +5s Surge duration  (Lv.0/2)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssEchoSurgeInfoGO, 990, 48, -175, 500);
        var ssEchoSurgeBtnGO = Btn(soulShardShopPanel, "SSEchoSurgeButton", "Buy (2 ⬡)", 30, HC("1565C0"));
        PT(ssEchoSurgeBtnGO, 988, 54, +245, 260);

        // Row 18 — Entropy Amp (tier-2, costs 2 shards)
        var ssEntropyAmpInfoGO = Label(soulShardShopPanel, "SSEntropyAmpInfoText",
            "Entropy Amp +15% Entropy damage  (Lv.0/2)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssEntropyAmpInfoGO, 1048, 48, -175, 500);
        var ssEntropyAmpBtnGO = Btn(soulShardShopPanel, "SSEntropyAmpButton", "Buy (2 ⬡)", 30, HC("006064"));
        PT(ssEntropyAmpBtnGO, 1046, 54, +245, 260);

        // Row 19 — Rune Seal (tier-1, costs 1 shard)
        var ssRuneSealInfoGO = Label(soulShardShopPanel, "SSRuneSealInfoText",
            "Rune Seal +2 max combo stacks  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssRuneSealInfoGO, 1106, 48, -175, 500);
        var ssRuneSealBtnGO = Btn(soulShardShopPanel, "SSRuneSealButton", "Buy (1 ⬡)", 30, HC("4A148C"));
        PT(ssRuneSealBtnGO, 1104, 54, +245, 260);

        // Row 20 — Bone Ward (tier-2, costs 2 shards)
        var ssBoneWardInfoGO = Label(soulShardShopPanel, "SSBoneWardInfoText",
            "Bone Ward +50% Blood Shield capacity  (Lv.0/2)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssBoneWardInfoGO, 1164, 48, -175, 500);
        var ssBoneWardBtnGO = Btn(soulShardShopPanel, "SSBoneWardButton", "Buy (2 ⬡)", 30, HC("01579B"));
        PT(ssBoneWardBtnGO, 1162, 54, +245, 260);

        // Row 21 — War Crest (tier-1, costs 1 shard)
        var ssWarCrestInfoGO = Label(soulShardShopPanel, "SSWarCrestInfoText",
            "War Crest +0.5 streak multiplier cap  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssWarCrestInfoGO, 1222, 48, -175, 500);
        var ssWarCrestBtnGO = Btn(soulShardShopPanel, "SSWarCrestButton", "Buy (1 ⬡)", 30, HC("BF360C"));
        PT(ssWarCrestBtnGO, 1220, 54, +245, 260);

        // Row 22 — Vital Surge (tier-1, costs 1 shard)
        var ssVitalSurgeInfoGO = Label(soulShardShopPanel, "SSVitalSurgeInfoText",
            "Vital Surge Heal Self restores +5 extra HP  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssVitalSurgeInfoGO, 1280, 48, -175, 500);
        var ssVitalSurgeBtnGO = Btn(soulShardShopPanel, "SSVitalSurgeButton", "Buy (1 ⬡)", 30, HC("00838F"));
        PT(ssVitalSurgeBtnGO, 1278, 54, +245, 260);

        // Row 23 — Crimson Storm (tier-2, costs 2 shards)
        var ssCrimsonStormInfoGO = Label(soulShardShopPanel, "SSCrimsonStormInfoText",
            "Crimson Storm Blood Storm fires 2× per cast  (Lv.0/2)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssCrimsonStormInfoGO, 1338, 48, -175, 500);
        var ssCrimsonStormBtnGO = Btn(soulShardShopPanel, "SSCrimsonStormButton", "Buy (2 ⬡)", 30, HC("B71C1C"));
        PT(ssCrimsonStormBtnGO, 1336, 54, +245, 260);

        // Row 24 — War Horn (tier-1, costs 1 shard)
        var ssWarHornInfoGO = Label(soulShardShopPanel, "SSWarHornInfoText",
            "War Horn War Cry lasts +1s per level  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssWarHornInfoGO, 1396, 48, -175, 500);
        var ssWarHornBtnGO = Btn(soulShardShopPanel, "SSWarHornButton", "Buy (1 ⬡)", 30, HC("E65100"));
        PT(ssWarHornBtnGO, 1394, 54, +245, 260);

        // Row 25 — Shadow Surge (tier-2, costs 2 shards)
        var ssShadowSurgeInfoGO = Label(soulShardShopPanel, "SSShadowSurgeInfoText",
            "Shadow Surge Surge deals +10 dmg/s to enemy  (Lv.0/2)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssShadowSurgeInfoGO, 1454, 48, -175, 500);
        var ssShadowSurgeBtnGO = Btn(soulShardShopPanel, "SSShadowSurgeButton", "Buy (2 ⬡)", 30, HC("4527A0"));
        PT(ssShadowSurgeBtnGO, 1452, 54, +245, 260);

        // Row 26 — Death Ward (tier-1, costs 1 shard)
        var ssDeathWardInfoGO = Label(soulShardShopPanel, "SSDeathWardInfoText",
            "Death Ward 5pct revive chance per level on death  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssDeathWardInfoGO, 1512, 48, -175, 500);
        var ssDeathWardBtnGO = Btn(soulShardShopPanel, "SSDeathWardButton", "Buy (1 ⬡)", 30, HC("558B2F"));
        PT(ssDeathWardBtnGO, 1510, 54, +245, 260);

        // Row 27 — Kill Surge (tier-1, costs 1 shard)
        var ssKillSurgeInfoGO = Label(soulShardShopPanel, "SSKillSurgeInfoText",
            "Kill Surge +1.5s Surge per kill while active  (Lv.0/3)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssKillSurgeInfoGO, 1570, 48, -175, 500);
        var ssKillSurgeBtnGO = Btn(soulShardShopPanel, "SSKillSurgeButton", "Buy (1 ⬡)", 30, HC("E65100"));
        PT(ssKillSurgeBtnGO, 1568, 54, +245, 260);

        // Row 28 — Void Storm (tier-2, costs 2 shards)
        var ssVoidStormInfoGO = Label(soulShardShopPanel, "SSVoidStormInfoText",
            "Void Storm Blood Storm drains +5% current HP per level  (Lv.0/2)", 30, TextSec, TextAnchor.MiddleLeft);
        PT(ssVoidStormInfoGO, 1628, 48, -175, 500);
        var ssVoidStormBtnGO = Btn(soulShardShopPanel, "SSVoidStormButton", "Buy (2 ⬡)", 30, HC("4527A0"));
        PT(ssVoidStormBtnGO, 1626, 54, +245, 260);

        soulShardShopPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // DAILY QUESTS BUTTON ROW  (y 4200–4265)
        // ════════════════════════════════════════════════════════════════════
        var questsRowGO = content.CreateChild("DailyQuestsRow");
        questsRowGO.AddImage(HC("0A1A0A")); PF(questsRowGO, 1324, 65, 20);

        var openQuestsBtnGO = Btn(questsRowGO, "OpenQuestsButton", "Daily Quests", 34, HC("1B5E20"));
        openQuestsBtnGO.Stretch();

        // ════════════════════════════════════════════════════════════════════
        // WATCH AD ROW  (y 4275–4345)
        // ════════════════════════════════════════════════════════════════════
        var adBoostRowGO = content.CreateChild("AdBoostRow");
        adBoostRowGO.AddImage(HC("1A0A2E")); PF(adBoostRowGO, 1394, 65, 20);

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

        var speedToggleTabGO = Btn(content, "SpeedToggleButton", "Speed: 1×", 36, HC("2A3A1A"));
        PT(speedToggleTabGO, 250, 80, 0, 680);

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

        var questStreakGO = Label(questsCard, "QuestStreakText",
            "Streak: 0 days\nComplete all 3 today: +1 bonus ⬡",
            28, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleCenter);
        PT(questStreakGO, 398, 72, 0, 900);

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
        uim.farmBloodInfoText       = farmBloodInfoGO.GetComponent<Text>();
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
        uim.truceButton             = truceBtnGO.GetComponent<Button>();
        uim.truceButtonText         = truceBtnGO.GetComponentInChildren<Text>();
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
        uim.bannerInfoText          = bannerInfoGO.GetComponent<Text>();
        uim.upgradeBannerButton     = upgradeBannerGO.GetComponent<Button>();
        uim.bannerCostText          = upgradeBannerGO.GetComponentInChildren<Text>();
        uim.fortInfoText            = fortInfoGO.GetComponent<Text>();
        uim.upgradeFortButton       = upgradeFortGO.GetComponent<Button>();
        uim.fortCostText            = upgradeFortGO.GetComponentInChildren<Text>();
        uim.bloodRitualPanel        = bloodRitualPanel;
        uim.bloodRitualInfoText     = bloodRitualInfoGO.GetComponent<Text>();
        uim.buyBloodRitualButton    = buyBloodRitualGO.GetComponent<Button>();
        uim.bloodRitualCostText     = buyBloodRitualGO.GetComponentInChildren<Text>();
        uim.autoRitualButton        = autoRitualBtnGO.GetComponent<Button>();
        uim.autoRitualButtonText    = autoRitualBtnGO.GetComponentInChildren<Text>();
        uim.prestigePanel           = prestigePanel;
        uim.prestigeInfoText        = prestigeInfoGO.GetComponent<Text>();
        uim.prestigeButton          = prestigeBtnGO.GetComponent<Button>();
        uim.prestigeMilestoneText   = prestigeMilestoneGO.GetComponent<Text>();
        uim.bloodSurgePanel         = bloodSurgePanel;
        uim.bloodSurgeInfoText      = bloodSurgeInfoGO.GetComponent<Text>();
        uim.bloodSurgeButton        = surgeBtnGO.GetComponent<Button>();
        uim.upgradeSurgeButton      = upgradeSurgeBtnGO.GetComponent<Button>();
        uim.surgeCostText           = upgradeSurgeBtnGO.GetComponentInChildren<Text>();
        uim.autoSurgeButton         = autoSurgeBtnGO.GetComponent<Button>();
        uim.autoSurgeButtonText     = autoSurgeBtnGO.GetComponentInChildren<Text>();
        uim.bloodStormInfoText          = bloodStormInfoGO.GetComponent<Text>();
        uim.bloodStormButton            = bloodStormBtnGO.GetComponent<Button>();
        uim.upgradeBloodStormButton     = upgradeBloodStormBtnGO.GetComponent<Button>();
        uim.stormCostText               = upgradeBloodStormBtnGO.GetComponentInChildren<Text>();
        uim.autoStormButton         = autoStormBtnGO.GetComponent<Button>();
        uim.autoStormButtonText     = autoStormBtnGO.GetComponentInChildren<Text>();
        uim.bloodOathInfoText          = bloodOathInfoGO.GetComponent<Text>();
        uim.bloodOathButton            = bloodOathBtnGO.GetComponent<Button>();
        uim.autoBloodOathButton        = autoBloodOathBtnGO.GetComponent<Button>();
        uim.autoBloodOathButtonText    = autoBloodOathBtnGO.GetComponentInChildren<Text>();
        uim.upgradeBloodOathButton     = upgradeBloodOathBtnGO.GetComponent<Button>();
        uim.bloodOathUpgradeCostText   = upgradeBloodOathBtnGO.GetComponentInChildren<Text>();
        uim.warCryInfoText          = warCryInfoGO.GetComponent<Text>();
        uim.warCryButton            = warCryBtnGO.GetComponent<Button>();
        uim.autoWarCryButton        = autoWarCryBtnGO.GetComponent<Button>();
        uim.autoWarCryButtonText    = autoWarCryBtnGO.GetComponentInChildren<Text>();
        uim.upgradeWarCryButton     = upgradeWarCryBtnGO.GetComponent<Button>();
        uim.warCryUpgradeCostText   = upgradeWarCryBtnGO.GetComponentInChildren<Text>();
        uim.hexCurseInfoText          = hexCurseInfoGO.GetComponent<Text>();
        uim.hexCurseButton            = hexCurseBtnGO.GetComponent<Button>();
        uim.autoHexCurseButton        = autoHexCurseBtnGO.GetComponent<Button>();
        uim.autoHexCurseButtonText    = autoHexCurseBtnGO.GetComponentInChildren<Text>();
        uim.upgradeHexCurseButton     = upgradeHexCurseBtnGO.GetComponent<Button>();
        uim.hexCurseUpgradeCostText   = upgradeHexCurseBtnGO.GetComponentInChildren<Text>();
        uim.bloodShieldInfoText     = bloodShieldInfoGO.GetComponent<Text>();
        uim.bloodShieldButton       = bloodShieldBtnGO.GetComponent<Button>();
        uim.autoBloodShieldButton     = autoBloodShieldBtnGO.GetComponent<Button>();
        uim.autoBloodShieldButtonText = autoBloodShieldBtnGO.GetComponentInChildren<Text>();
        uim.upgradeHealSelfButton   = upgradeHealBtnGO.GetComponent<Button>();
        uim.healCostText            = upgradeHealBtnGO.GetComponentInChildren<Text>();
        uim.autoHealButton          = autoHealBtnGO.GetComponent<Button>();
        uim.autoHealButtonText      = autoHealBtnGO.GetComponentInChildren<Text>();
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
        uim.pBountyBonusInfoText         = pBountyBonusInfoGO.GetComponent<Text>();
        uim.pBountyBonusButton           = pBountyBonusBtnGO.GetComponent<Button>();
        uim.pBloodRitualStartInfoText    = pBloodRitualStartInfoGO.GetComponent<Text>();
        uim.pBloodRitualStartButton      = pBloodRitualStartBtnGO.GetComponent<Button>();
        uim.pBloodMasteryInfoText        = pBloodMasteryInfoGO.GetComponent<Text>();
        uim.pBloodMasteryButton          = pBloodMasteryBtnGO.GetComponent<Button>();
        uim.pSacredGroundInfoText        = pSacredGroundInfoGO.GetComponent<Text>();
        uim.pSacredGroundButton          = pSacredGroundBtnGO.GetComponent<Button>();
        uim.pEternalFlameInfoText        = pEternalFlameInfoGO.GetComponent<Text>();
        uim.pEternalFlameButton          = pEternalFlameBtnGO.GetComponent<Button>();
        uim.pWarMachineInfoText          = pWarMachineInfoGO.GetComponent<Text>();
        uim.pWarMachineButton            = pWarMachineBtnGO.GetComponent<Button>();
        uim.pCrimsonLegacyInfoText       = pCrimsonLegacyInfoGO.GetComponent<Text>();
        uim.pCrimsonLegacyButton         = pCrimsonLegacyBtnGO.GetComponent<Button>();
        uim.pBloodlineInfoText           = pBloodlineInfoGO.GetComponent<Text>();
        uim.pBloodlineButton             = pBloodlineBtnGO.GetComponent<Button>();
        uim.pIronBastionInfoText         = pIronBastionInfoGO.GetComponent<Text>();
        uim.pIronBastionButton           = pIronBastionBtnGO.GetComponent<Button>();
        uim.pBloodPriceInfoText          = pBloodPriceInfoGO.GetComponent<Text>();
        uim.pBloodPriceButton            = pBloodPriceBtnGO.GetComponent<Button>();
        uim.pVoidPactInfoText            = pVoidPactInfoGO.GetComponent<Text>();
        uim.pVoidPactButton              = pVoidPactBtnGO.GetComponent<Button>();
        uim.pWarFervorInfoText           = pWarFervorInfoGO.GetComponent<Text>();
        uim.pWarFervorButton             = pWarFervorBtnGO.GetComponent<Button>();
        uim.pWellspringInfoText          = pWellspringInfoGO.GetComponent<Text>();
        uim.pWellspringButton            = pWellspringBtnGO.GetComponent<Button>();
        uim.pBattleRhythmInfoText        = pBattleRhythmInfoGO.GetComponent<Text>();
        uim.pBattleRhythmButton          = pBattleRhythmBtnGO.GetComponent<Button>();
        uim.pSoulTideInfoText            = pSoulTideInfoGO.GetComponent<Text>();
        uim.pSoulTideButton              = pSoulTideBtnGO.GetComponent<Button>();
        uim.bloodBankPanel          = bloodBankPanel;
        uim.bloodBankInfoText       = bankInfoGO.GetComponent<Text>();
        uim.bloodBankAccruedText    = bankAccruedGO.GetComponent<Text>();
        uim.depositBloodButton      = depositBtnGO.GetComponent<Button>();
        uim.withdrawBloodButton     = withdrawBtnGO.GetComponent<Button>();
        uim.autoBankButton               = autoBankBtnGO.GetComponent<Button>();
        uim.autoBankButtonText           = autoBankBtnGO.GetComponentInChildren<Text>();
        uim.bankInterestUpgradeButton    = bankInterestUpgradeBtnGO.GetComponent<Button>();
        uim.bankInterestUpgradeCostText  = bankInterestCostGO.GetComponent<Text>();
        uim.cursedBloodPanel        = cursedBloodPanel;
        uim.cursedBloodButton       = cursedBloodBtnGO.GetComponent<Button>();
        uim.cursedBloodButtonText   = cursedBloodBtnGO.GetComponentInChildren<Text>();
        uim.killIncomePanel             = killIncomePanel;
        uim.killIncomeUpgradeButton     = killIncomeUpgradeBtnGO.GetComponent<Button>();
        uim.killIncomeUpgradeCostText   = killIncomeCostGO.GetComponent<Text>();
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
        uim.ssSoulHarvestInfoText   = ssSoulHarvestInfoGO.GetComponent<Text>();
        uim.ssSoulHarvestButton     = ssSoulHarvestBtnGO.GetComponent<Button>();
        uim.ssCrimsonPulseInfoText  = ssCrimsonPulseInfoGO.GetComponent<Text>();
        uim.ssCrimsonPulseButton    = ssCrimsonPulseBtnGO.GetComponent<Button>();
        uim.ssCrimsonBrandInfoText  = ssCrimsonBrandInfoGO.GetComponent<Text>();
        uim.ssCrimsonBrandButton    = ssCrimsonBrandBtnGO.GetComponent<Button>();
        uim.ssWarSpoilsInfoText     = ssWarSpoilsInfoGO.GetComponent<Text>();
        uim.ssWarSpoilsButton       = ssWarSpoilsBtnGO.GetComponent<Button>();
        uim.ssGhostStrikeInfoText   = ssGhostStrikeInfoGO.GetComponent<Text>();
        uim.ssGhostStrikeButton     = ssGhostStrikeBtnGO.GetComponent<Button>();
        uim.ssDeathsBountyInfoText  = ssDeathsBountyInfoGO.GetComponent<Text>();
        uim.ssDeathsBountyButton    = ssDeathsBountyBtnGO.GetComponent<Button>();
        uim.ssRuneSealInfoText      = ssRuneSealInfoGO.GetComponent<Text>();
        uim.ssRuneSealButton        = ssRuneSealBtnGO.GetComponent<Button>();
        uim.ssBoneWardInfoText      = ssBoneWardInfoGO.GetComponent<Text>();
        uim.ssBoneWardButton        = ssBoneWardBtnGO.GetComponent<Button>();
        uim.ssWarCrestInfoText      = ssWarCrestInfoGO.GetComponent<Text>();
        uim.ssWarCrestButton        = ssWarCrestBtnGO.GetComponent<Button>();
        uim.ssVitalSurgeInfoText    = ssVitalSurgeInfoGO.GetComponent<Text>();
        uim.ssVitalSurgeButton      = ssVitalSurgeBtnGO.GetComponent<Button>();
        uim.ssWarHornInfoText       = ssWarHornInfoGO.GetComponent<Text>();
        uim.ssWarHornButton         = ssWarHornBtnGO.GetComponent<Button>();
        uim.ssDeathWardInfoText     = ssDeathWardInfoGO.GetComponent<Text>();
        uim.ssDeathWardButton       = ssDeathWardBtnGO.GetComponent<Button>();
        uim.ssKillSurgeInfoText     = ssKillSurgeInfoGO.GetComponent<Text>();
        uim.ssKillSurgeButton       = ssKillSurgeBtnGO.GetComponent<Button>();
        uim.ssCrimsonStormInfoText  = ssCrimsonStormInfoGO.GetComponent<Text>();
        uim.ssCrimsonStormButton    = ssCrimsonStormBtnGO.GetComponent<Button>();
        uim.ssShadowSurgeInfoText   = ssShadowSurgeInfoGO.GetComponent<Text>();
        uim.ssShadowSurgeButton     = ssShadowSurgeBtnGO.GetComponent<Button>();
        uim.ssVoidStormInfoText     = ssVoidStormInfoGO.GetComponent<Text>();
        uim.ssVoidStormButton       = ssVoidStormBtnGO.GetComponent<Button>();
        uim.ssVoidConduitInfoText   = ssVoidConduitInfoGO.GetComponent<Text>();
        uim.ssVoidConduitButton     = ssVoidConduitBtnGO.GetComponent<Button>();
        uim.ssBloodEchoInfoText     = ssBloodEchoInfoGO.GetComponent<Text>();
        uim.ssBloodEchoButton       = ssBloodEchoBtnGO.GetComponent<Button>();
        uim.ssIronMarrowInfoText    = ssIronMarrowInfoGO.GetComponent<Text>();
        uim.ssIronMarrowButton      = ssIronMarrowBtnGO.GetComponent<Button>();
        uim.ssWrathBloomInfoText    = ssWrathBloomInfoGO.GetComponent<Text>();
        uim.ssWrathBloomButton      = ssWrathBloomBtnGO.GetComponent<Button>();
        uim.ssBloodNovaInfoText     = ssBloodNovaInfoGO.GetComponent<Text>();
        uim.ssBloodNovaButton       = ssBloodNovaBtnGO.GetComponent<Button>();
        uim.ssEchoSurgeInfoText     = ssEchoSurgeInfoGO.GetComponent<Text>();
        uim.ssEchoSurgeButton       = ssEchoSurgeBtnGO.GetComponent<Button>();
        uim.ssEntropyAmpInfoText    = ssEntropyAmpInfoGO.GetComponent<Text>();
        uim.ssEntropyAmpButton      = ssEntropyAmpBtnGO.GetComponent<Button>();
        uim.settingsPanel           = settingsOverlay;
        uim.soundToggleText         = soundToggleGO.GetComponentInChildren<Text>();
        uim.notifToggleText         = notifToggleGO.GetComponentInChildren<Text>();
        uim.speedToggleText         = speedToggleTabGO.GetComponentInChildren<Text>();
        uim.barracksInfoText        = barracksInfoGO.GetComponent<Text>();
        uim.upgradeBarracksButton   = upgradeBarracksGO.GetComponent<Button>();
        uim.barracksUpgradeCostText = upgradeBarracksGO.GetComponentInChildren<Text>();
        uim.autoBuyButton           = autoBuyBtnGO.GetComponent<Button>();
        uim.autoBuyButtonText       = autoBuyBtnGO.GetComponentInChildren<Text>();
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
        uim.desecrateButton           = desecrateBtnGO.GetComponent<Button>();
        uim.desecrateButtonText       = desecrateBtnGO.GetComponentInChildren<Text>();
        uim.autoDesecrateButton       = autoDesecrateBtnGO.GetComponent<Button>();
        uim.autoDesecrateButtonText   = autoDesecrateBtnGO.GetComponentInChildren<Text>();
        uim.upgradeDesecrateButton    = upgradeDesecrateBtnGO.GetComponent<Button>();
        uim.desecrateUpgradeCostText  = upgradeDesecrateBtnGO.GetComponentInChildren<Text>();
        uim.entropyInfoText           = entropyInfoGO.GetComponent<Text>();
        uim.entropyButton             = entropyBtnGO.GetComponent<Button>();
        uim.upgradeEntropyButton      = upgradeEntropyBtnGO.GetComponent<Button>();
        uim.entropyUpgradeCostText    = upgradeEntropyBtnGO.GetComponentInChildren<Text>();
        uim.soulSacrificeButton     = soulSacBtnGO.GetComponent<Button>();
        uim.soulSacrificeInfoText   = soulSacInfoGO.GetComponent<Text>();
        uim.soldierSacrificeButton  = soldierSacBtnGO.GetComponent<Button>();
        uim.soldierSacrificeInfoText = soldierSacInfoGO.GetComponent<Text>();
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
        uim.questStreakText         = questStreakGO.GetComponent<Text>();
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
        UnityEventTools.AddPersistentListener(bloodStormBtnGO.GetComponent<Button>().onClick,             clk.OnUseBloodStorm);
        UnityEventTools.AddPersistentListener(upgradeBloodStormBtnGO.GetComponent<Button>().onClick,     clk.OnUpgradeBloodStorm);
        UnityEventTools.AddPersistentListener(autoStormBtnGO.GetComponent<Button>().onClick,          clk.OnToggleAutoStorm);
        UnityEventTools.AddPersistentListener(bloodOathBtnGO.GetComponent<Button>().onClick,          clk.OnUseBloodOath);
        UnityEventTools.AddPersistentListener(autoBloodOathBtnGO.GetComponent<Button>().onClick,     clk.OnToggleAutoBloodOath);
        UnityEventTools.AddPersistentListener(upgradeBloodOathBtnGO.GetComponent<Button>().onClick,  clk.OnUpgradeBloodOath);
        UnityEventTools.AddPersistentListener(warCryBtnGO.GetComponent<Button>().onClick,             clk.OnUseWarCry);
        UnityEventTools.AddPersistentListener(autoWarCryBtnGO.GetComponent<Button>().onClick,        clk.OnToggleAutoWarCry);
        UnityEventTools.AddPersistentListener(upgradeWarCryBtnGO.GetComponent<Button>().onClick,     clk.OnUpgradeWarCry);
        UnityEventTools.AddPersistentListener(hexCurseBtnGO.GetComponent<Button>().onClick,           clk.OnUseHexCurse);
        UnityEventTools.AddPersistentListener(autoHexCurseBtnGO.GetComponent<Button>().onClick,      clk.OnToggleAutoHexCurse);
        UnityEventTools.AddPersistentListener(upgradeHexCurseBtnGO.GetComponent<Button>().onClick,   clk.OnUpgradeHexCurse);
        UnityEventTools.AddPersistentListener(bloodShieldBtnGO.GetComponent<Button>().onClick,        clk.OnUseBloodShield);
        UnityEventTools.AddPersistentListener(autoBloodShieldBtnGO.GetComponent<Button>().onClick,   clk.OnToggleAutoBloodShield);
        UnityEventTools.AddPersistentListener(truceBtnGO.GetComponent<Button>().onClick,              clk.OnUseTruce);
        UnityEventTools.AddPersistentListener(soldierSacBtnGO.GetComponent<Button>().onClick,         clk.OnUseSoldierSacrifice);
        UnityEventTools.AddPersistentListener(bloodPactGO.GetComponent<Button>().onClick,             clk.OnUseBloodPact);
        UnityEventTools.AddPersistentListener(buyWorkerGO.GetComponent<Button>().onClick,             clk.OnBuyWorker);
        UnityEventTools.AddPersistentListener(buyShrineGO.GetComponent<Button>().onClick,            clk.OnBuyShrine);
        UnityEventTools.AddPersistentListener(clickPowerGO.GetComponent<Button>().onClick,           clk.OnBuyClickPower);
        UnityEventTools.AddPersistentListener(buyBloodWellGO.GetComponent<Button>().onClick,         clk.OnBuyBloodWell);
        UnityEventTools.AddPersistentListener(buyBloodRitualGO.GetComponent<Button>().onClick,        clk.OnBuyBloodRitual);
        UnityEventTools.AddPersistentListener(autoRitualBtnGO.GetComponent<Button>().onClick,        clk.OnToggleAutoRitual);
        UnityEventTools.AddPersistentListener(upgradeBarracksGO.GetComponent<Button>().onClick,       clk.OnUpgradeBarracks);
        UnityEventTools.AddPersistentListener(upgradeFortGO.GetComponent<Button>().onClick,           clk.OnUpgradeFortification);
        UnityEventTools.AddPersistentListener(upgradeWeaponGO.GetComponent<Button>().onClick,         clk.OnUpgradeWeapon);
        UnityEventTools.AddPersistentListener(upgradeArmorGO.GetComponent<Button>().onClick,          clk.OnUpgradeArmor);
        UnityEventTools.AddPersistentListener(upgradeTalismanGO.GetComponent<Button>().onClick,       clk.OnUpgradeTalisman);
        UnityEventTools.AddPersistentListener(upgradeBannerGO.GetComponent<Button>().onClick,         clk.OnUpgradeBanner);
        UnityEventTools.AddPersistentListener(formationBtnGO.GetComponent<Button>().onClick,          clk.OnToggleFormation);
        UnityEventTools.AddPersistentListener(autoBuyBtnGO.GetComponent<Button>().onClick,            clk.OnToggleAutoBuy);
        UnityEventTools.AddPersistentListener(prestigeBtnGO.GetComponent<Button>().onClick,           clk.OnPrestige);
        UnityEventTools.AddPersistentListener(pSoldierCapBtnGO.GetComponent<Button>().onClick,        clk.OnBuyPSoldierCap);
        UnityEventTools.AddPersistentListener(pClickBonusBtnGO.GetComponent<Button>().onClick,        clk.OnBuyPClickBonus);
        UnityEventTools.AddPersistentListener(pRitualEffBtnGO.GetComponent<Button>().onClick,         clk.OnBuyPRitualEff);
        UnityEventTools.AddPersistentListener(pWeaponHeadStartBtnGO.GetComponent<Button>().onClick,   clk.OnBuyPWeaponHeadStart);
        UnityEventTools.AddPersistentListener(pBloodTitheBtnGO.GetComponent<Button>().onClick,        clk.OnBuyPBloodTithe);
        UnityEventTools.AddPersistentListener(pIronWallBtnGO.GetComponent<Button>().onClick,          clk.OnBuyPIronWall);
        UnityEventTools.AddPersistentListener(pBountyBonusBtnGO.GetComponent<Button>().onClick,         clk.OnBuyPBountyBonus);
        UnityEventTools.AddPersistentListener(pBloodRitualStartBtnGO.GetComponent<Button>().onClick,    clk.OnBuyPBloodRitualStart);
        UnityEventTools.AddPersistentListener(pBloodMasteryBtnGO.GetComponent<Button>().onClick,        clk.OnBuyPBloodMastery);
        UnityEventTools.AddPersistentListener(pSacredGroundBtnGO.GetComponent<Button>().onClick,        clk.OnBuyPSacredGround);
        UnityEventTools.AddPersistentListener(pEternalFlameBtnGO.GetComponent<Button>().onClick,        clk.OnBuyPEternalFlame);
        UnityEventTools.AddPersistentListener(pWarMachineBtnGO.GetComponent<Button>().onClick,          clk.OnBuyPWarMachine);
        UnityEventTools.AddPersistentListener(pCrimsonLegacyBtnGO.GetComponent<Button>().onClick,      clk.OnBuyPCrimsonLegacy);
        UnityEventTools.AddPersistentListener(pBloodlineBtnGO.GetComponent<Button>().onClick,          clk.OnBuyPBloodline);
        UnityEventTools.AddPersistentListener(pIronBastionBtnGO.GetComponent<Button>().onClick,       clk.OnBuyPIronBastion);
        UnityEventTools.AddPersistentListener(pBloodPriceBtnGO.GetComponent<Button>().onClick,        clk.OnBuyPBloodPrice);
        UnityEventTools.AddPersistentListener(pVoidPactBtnGO.GetComponent<Button>().onClick,          clk.OnBuyPVoidPact);
        UnityEventTools.AddPersistentListener(pWarFervorBtnGO.GetComponent<Button>().onClick,         clk.OnBuyPWarFervor);
        UnityEventTools.AddPersistentListener(pWellspringBtnGO.GetComponent<Button>().onClick,        clk.OnBuyPWellspring);
        UnityEventTools.AddPersistentListener(pBattleRhythmBtnGO.GetComponent<Button>().onClick,     clk.OnBuyPBattleRhythm);
        UnityEventTools.AddPersistentListener(pSoulTideBtnGO.GetComponent<Button>().onClick,         clk.OnBuyPSoulTide);
        UnityEventTools.AddPersistentListener(ssBossTimerBtnGO.GetComponent<Button>().onClick,        clk.OnBuySSBossTimer);
        UnityEventTools.AddPersistentListener(ssDoubleChestBtnGO.GetComponent<Button>().onClick,      clk.OnBuySSDoubleChest);
        UnityEventTools.AddPersistentListener(ssRollbackBtnGO.GetComponent<Button>().onClick,         clk.OnBuySSRollback);
        UnityEventTools.AddPersistentListener(ssBloodTapBtnGO.GetComponent<Button>().onClick,        clk.OnBuySSBloodTap);
        UnityEventTools.AddPersistentListener(ssShardHungerBtnGO.GetComponent<Button>().onClick,    clk.OnBuySSShardHunger);
        UnityEventTools.AddPersistentListener(ssSoulHarvestBtnGO.GetComponent<Button>().onClick,    clk.OnBuySSSoulHarvest);
        UnityEventTools.AddPersistentListener(ssCrimsonPulseBtnGO.GetComponent<Button>().onClick,  clk.OnBuySSCrimsonPulse);
        UnityEventTools.AddPersistentListener(ssCrimsonBrandBtnGO.GetComponent<Button>().onClick,  clk.OnBuySSCrimsonBrand);
        UnityEventTools.AddPersistentListener(ssWarSpoilsBtnGO.GetComponent<Button>().onClick,     clk.OnBuySSWarSpoils);
        UnityEventTools.AddPersistentListener(ssGhostStrikeBtnGO.GetComponent<Button>().onClick,   clk.OnBuySSGhostStrike);
        UnityEventTools.AddPersistentListener(ssDeathsBountyBtnGO.GetComponent<Button>().onClick,  clk.OnBuySSDeathsBounty);
        UnityEventTools.AddPersistentListener(ssRuneSealBtnGO.GetComponent<Button>().onClick,      clk.OnBuySSRuneSeal);
        UnityEventTools.AddPersistentListener(ssBoneWardBtnGO.GetComponent<Button>().onClick,     clk.OnBuySSBoneWard);
        UnityEventTools.AddPersistentListener(ssWarCrestBtnGO.GetComponent<Button>().onClick,     clk.OnBuySSWarCrest);
        UnityEventTools.AddPersistentListener(ssVitalSurgeBtnGO.GetComponent<Button>().onClick,   clk.OnBuySSVitalSurge);
        UnityEventTools.AddPersistentListener(ssWarHornBtnGO.GetComponent<Button>().onClick,      clk.OnBuySSWarHorn);
        UnityEventTools.AddPersistentListener(ssDeathWardBtnGO.GetComponent<Button>().onClick,    clk.OnBuySSDeathWard);
        UnityEventTools.AddPersistentListener(ssKillSurgeBtnGO.GetComponent<Button>().onClick,    clk.OnBuySSKillSurge);
        UnityEventTools.AddPersistentListener(ssCrimsonStormBtnGO.GetComponent<Button>().onClick,  clk.OnBuySSCrimsonStorm);
        UnityEventTools.AddPersistentListener(ssShadowSurgeBtnGO.GetComponent<Button>().onClick,   clk.OnBuySSShadowSurge);
        UnityEventTools.AddPersistentListener(ssVoidStormBtnGO.GetComponent<Button>().onClick,     clk.OnBuySSVoidStorm);
        UnityEventTools.AddPersistentListener(ssVoidConduitBtnGO.GetComponent<Button>().onClick,   clk.OnBuySSVoidConduit);
        UnityEventTools.AddPersistentListener(ssBloodEchoBtnGO.GetComponent<Button>().onClick,     clk.OnBuySSBloodEcho);
        UnityEventTools.AddPersistentListener(ssIronMarrowBtnGO.GetComponent<Button>().onClick,    clk.OnBuySSIronMarrow);
        UnityEventTools.AddPersistentListener(ssWrathBloomBtnGO.GetComponent<Button>().onClick,    clk.OnBuySSWrathBloom);
        UnityEventTools.AddPersistentListener(ssBloodNovaBtnGO.GetComponent<Button>().onClick,     clk.OnBuySSBloodNova);
        UnityEventTools.AddPersistentListener(ssEchoSurgeBtnGO.GetComponent<Button>().onClick,     clk.OnBuySSEchoSurge);
        UnityEventTools.AddPersistentListener(ssEntropyAmpBtnGO.GetComponent<Button>().onClick,    clk.OnBuySSEntropyAmp);
        UnityEventTools.AddPersistentListener(upgradeSurgeBtnGO.GetComponent<Button>().onClick,      clk.OnUpgradeSurge);
        UnityEventTools.AddPersistentListener(autoSurgeBtnGO.GetComponent<Button>().onClick,         clk.OnToggleAutoSurge);
        UnityEventTools.AddPersistentListener(upgradeHealBtnGO.GetComponent<Button>().onClick,       clk.OnUpgradeHealSelf);
        UnityEventTools.AddPersistentListener(autoHealBtnGO.GetComponent<Button>().onClick,          clk.OnToggleAutoHeal);
        UnityEventTools.AddPersistentListener(depositBtnGO.GetComponent<Button>().onClick,            clk.OnDepositToBank);
        UnityEventTools.AddPersistentListener(withdrawBtnGO.GetComponent<Button>().onClick,           clk.OnWithdrawFromBank);
        UnityEventTools.AddPersistentListener(autoBankBtnGO.GetComponent<Button>().onClick,               clk.OnToggleAutoBank);
        UnityEventTools.AddPersistentListener(bankInterestUpgradeBtnGO.GetComponent<Button>().onClick,    clk.OnBuyBankInterestUpgrade);
        UnityEventTools.AddPersistentListener(cursedBloodBtnGO.GetComponent<Button>().onClick,          clk.OnToggleCursedBlood);
        UnityEventTools.AddPersistentListener(killIncomeUpgradeBtnGO.GetComponent<Button>().onClick,   clk.OnBuyKillIncomeUpgrade);
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
        UnityEventTools.AddPersistentListener(speedToggleTabGO.GetComponent<Button>().onClick,         clk.OnToggleGameSpeed);
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
        UnityEventTools.AddPersistentListener(autoDesecrateBtnGO.GetComponent<Button>().onClick,     clk.OnToggleAutoDesecrate);
        UnityEventTools.AddPersistentListener(upgradeDesecrateBtnGO.GetComponent<Button>().onClick,  clk.OnUpgradeDesecrate);
        UnityEventTools.AddPersistentListener(entropyBtnGO.GetComponent<Button>().onClick,           clk.OnUseEntropy);
        UnityEventTools.AddPersistentListener(upgradeEntropyBtnGO.GetComponent<Button>().onClick,    clk.OnUpgradeEntropy);
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
