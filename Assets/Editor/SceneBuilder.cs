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
//    0–110   Header      — Blood | Wave | Wood(+Shards)
//  120–455   Enemy card  [EnemyModifierText y=393, WavePreviewBanner overlay]
//  465–775   Army card   [Formation y=618, MixedBonus y=660, UpgradeHeal y=697]
//  785–995   Farm Blood
// 1005–1140  Action row  — Tank | Berserker | Paladin | Heal Self
// 1150–1315  Workers card
// 1325–1490  Barracks card
// 1500–1665  Fortifications card
// 1675–1920  Equipment card
// 1930–2145  Blood Ritual + Blood Pact card
// 2155–2295  Prestige card
// 2305–2505  Blood Surge card  (UpgradeSurge row at bottom)
// 2515–2680  Blood Bank card
// 2690–3105  Prestige Shop card  (6 rows)
// 3115–3425  Soul Shard Shop card  (4 rows incl. Blood Tap)
// 3435–3535  Bottom row  — Stats | Settings | Suggest
// overlay    StatsPanel, SettingsPanel, FeatureRequestOverlay — modals
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
    static readonly Color Teal     = HC("00695C");

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
        contentRT.sizeDelta        = new Vector2(0, 3545);

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
        bossTimerTextGO.Stretch();
        bossTimerRowGO.SetActive(false);

        var enemyModifierTextGO = Label(content, "EnemyModifierText", "", 30,
            new Color(1f, 0.65f, 0.1f), TextAnchor.MiddleCenter);
        PF(enemyModifierTextGO, 393, 38, 60);
        enemyModifierTextGO.SetActive(false);

        // Wave preview overlay — sits on top of enemy card, hidden until preview starts
        var wavePreviewBannerGO = content.CreateChild("WavePreviewBanner");
        var wpImg = wavePreviewBannerGO.AddComponent<Image>();
        wpImg.color = new Color(0f, 0f, 0f, 0.84f);
        PF(wavePreviewBannerGO, 120, 335, 30);
        var wavePreviewTextGO = Label(wavePreviewBannerGO, "WavePreviewText",
            "Wave 2 incoming...", 48, Gold, TextAnchor.MiddleCenter);
        wavePreviewTextGO.Stretch();
        wavePreviewBannerGO.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // ARMY CARD  (y 465–715)
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "ArmyCardBg", 465, 310, Surface1, 24);

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

        var formationBtnGO = Btn(content, "FormationButton", "Formation: Tank Front", 28, HC("1A1A3A"));
        PF(formationBtnGO, 618, 38, 40);

        var mixedBonusGO = Label(content, "MixedBonusText",
            "Mixed Formation: −15% incoming damage", 26, new Color(0.6f, 0.9f, 0.6f), TextAnchor.MiddleCenter);
        PF(mixedBonusGO, 660, 34, 50);
        mixedBonusGO.SetActive(false);

        var upgradeHealBtnGO = Btn(content, "UpgradeHealSelfButton", "Upgrade Heal\n(40 blood)", 28, Purple);
        PF(upgradeHealBtnGO, 697, 64, 40);

        // ════════════════════════════════════════════════════════════════════
        // FARM BLOOD  (y 725–935)
        // ════════════════════════════════════════════════════════════════════
        var farmBtnGO = Btn(content, "FarmBloodButton", "FARM BLOOD", 90, Crimson);
        PT(farmBtnGO, 785, 210, 0, 680);

        // ════════════════════════════════════════════════════════════════════
        // ACTION ROW  (y 1005–1140) — Tank | Berserker | Paladin | Heal Self
        // ════════════════════════════════════════════════════════════════════
        var buyTankGO = Btn(content, "BuyTankButton",
            "Tank\n50HP  5atk\n10 blood", 28, Blue);
        PT(buyTankGO, 1005, 130, -393, 230);

        var buyBerserkerGO = Btn(content, "BuyBerserkerButton",
            "Berserker\n25HP  12atk\n10 blood", 24, DeepOrange);
        PT(buyBerserkerGO, 1005, 130, -131, 230);

        var buyPaladinGO = Btn(content, "BuyPaladinButton",
            "Paladin\n20HP  3atk\nHealer\n10 blood", 24, HC("00695C"));
        PT(buyPaladinGO, 1005, 130, +131, 230);

        var healPanelGO = content.CreateChild("HealSelfPanel");
        healPanelGO.AddImage(Color.clear); PT(healPanelGO, 1005, 130, +393, 230);

        var healBtnGO = Btn(healPanelGO, "HealSelfButton", "Heal Self  +20 HP\n25 blood", 30, Purple);
        healBtnGO.Stretch(); healPanelGO.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // WORKERS CARD  (y 1090–1255) — hidden until 200 blood earned
        // ════════════════════════════════════════════════════════════════════
        var workersPanel = content.CreateChild("WorkersPanel");
        workersPanel.AddImage(Color.clear);
        PF(workersPanel, 1150, 165);

        Panel(workersPanel, "WorkersCardBg", 0, 165, Surface1, 24);

        var workerInfoGO = Label(workersPanel, "WorkerInfoText", "Workers: 0",
            38, Color.white, TextAnchor.MiddleLeft);
        PT(workerInfoGO, 23, 52, -175, 500);

        var buyWorkerGO = Btn(workersPanel, "BuyWorkerButton", "Buy Worker\n50 blood", 34, Green);
        PT(buyWorkerGO, 13, 110, +232, 370);

        workersPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // BARRACKS CARD  (y 1265–1430)
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "BarracksCardBg", 1325, 165, Surface1, 24);

        var barracksInfoGO = Label(content, "BarracksInfoText",
            "Barracks  Lv.1  —  Max 10 soldiers",
            34, Color.white, TextAnchor.MiddleLeft);
        PT(barracksInfoGO, 1348, 52, -175, 540);

        var upgradeBarracksGO = Btn(content, "UpgradeBarracksButton", "Upgrade\n(20 wood)", 34, Brown);
        PT(upgradeBarracksGO, 1338, 110, +232, 370);

        // ════════════════════════════════════════════════════════════════════
        // FORTIFICATIONS CARD  (y 1440–1605) — always visible
        // ════════════════════════════════════════════════════════════════════
        Panel(content, "FortificationsCardBg", 1500, 165, Surface1, 24);

        var fortInfoGO = Label(content, "FortificationsInfoText",
            "Fortifications  Lv.0/10  (−0% enemy HP)",
            34, Color.white, TextAnchor.MiddleLeft);
        PT(fortInfoGO, 1523, 52, -175, 540);

        var upgradeFortGO = Btn(content, "UpgradeFortificationButton", "Fortify\n(50 wood)", 34, Brown);
        PT(upgradeFortGO, 1513, 110, +232, 370);

        // ════════════════════════════════════════════════════════════════════
        // EQUIPMENT CARD  (y 1615–1860) — same unlock as workers
        // ════════════════════════════════════════════════════════════════════
        var equipmentPanel = content.CreateChild("EquipmentPanel");
        equipmentPanel.AddImage(Color.clear);
        PF(equipmentPanel, 1675, 245);

        Panel(equipmentPanel, "EquipmentCardBg", 0, 245, Surface1, 24);

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
        // BLOOD RITUAL + BLOOD PACT CARD  (y 1870–2085) — same unlock as workers
        // ════════════════════════════════════════════════════════════════════
        var bloodRitualPanel = content.CreateChild("BloodRitualPanel");
        bloodRitualPanel.AddImage(Color.clear);
        PF(bloodRitualPanel, 1930, 215);

        Panel(bloodRitualPanel, "BloodRitualCardBg", 0, 215, Surface1, 24);

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
        // PRESTIGE CARD  (y 2095–2235) — visible at wave 20+
        // ════════════════════════════════════════════════════════════════════
        var prestigePanel = content.CreateChild("PrestigePanel");
        prestigePanel.AddImage(Color.clear);
        PF(prestigePanel, 2155, 140);

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
        // BLOOD SURGE CARD  (y 2245–2385) — visible after 500 blood earned
        // ════════════════════════════════════════════════════════════════════
        var bloodSurgePanel = content.CreateChild("BloodSurgePanel");
        bloodSurgePanel.AddImage(Color.clear);
        PF(bloodSurgePanel, 2305, 200);

        Panel(bloodSurgePanel, "BloodSurgeCardBg", 0, 200, Surface1, 24);

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

        bloodSurgePanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // BLOOD BANK CARD  (y 2395–2560) — always visible
        // ════════════════════════════════════════════════════════════════════
        var bloodBankPanel = content.CreateChild("BloodBankPanel");
        bloodBankPanel.AddImage(Color.clear);
        PF(bloodBankPanel, 2515, 165);

        Panel(bloodBankPanel, "BloodBankCardBg", 0, 165, Surface1, 24);

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
        // PRESTIGE SHOP CARD  (y 2560–2975) — visible after first prestige
        // ════════════════════════════════════════════════════════════════════
        var prestigeShopPanel = content.CreateChild("PrestigeShopPanel");
        prestigeShopPanel.AddImage(Color.clear);
        PF(prestigeShopPanel, 2690, 415);

        Panel(prestigeShopPanel, "PrestigeShopCardBg", 0, 415, HC("150A30"), 24);

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

        prestigeShopPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // SOUL SHARD SHOP CARD  (y 2985–3235) — visible after first boss kill
        // ════════════════════════════════════════════════════════════════════
        var soulShardShopPanel = content.CreateChild("SoulShardShopPanel");
        soulShardShopPanel.AddImage(Color.clear);
        PF(soulShardShopPanel, 3115, 310);

        Panel(soulShardShopPanel, "SoulShardShopCardBg", 0, 310, HC("0A1A30"), 24);

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

        soulShardShopPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        // BOTTOM ROW  (y 3435–3535) — Stats | Settings | Suggest
        // ════════════════════════════════════════════════════════════════════
        var statsBtnGO = Btn(content, "StatsButton", "Statistics", 36, Teal);
        PT(statsBtnGO, 3435, 100, -345, 310);

        var settingsBtnGO = Btn(content, "SettingsButton", "Settings", 36, HC("2A2A4A"));
        PT(settingsBtnGO, 3435, 100, 0, 300);

        var suggestBtnGO = Btn(content, "SuggestButton", "Suggest", 36, HC("1565C0"));
        PT(suggestBtnGO, 3435, 100, +345, 300);

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
        uim.settingsPanel           = settingsOverlay;
        uim.soundToggleText         = soundToggleGO.GetComponentInChildren<Text>();
        uim.notifToggleText         = notifToggleGO.GetComponentInChildren<Text>();
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
        UnityEventTools.AddPersistentListener(farmBtnGO.GetComponent<Button>().onClick,               clk.OnFarmBlood);
        UnityEventTools.AddPersistentListener(buyTankGO.GetComponent<Button>().onClick,               clk.OnBuyTank);
        UnityEventTools.AddPersistentListener(buyBerserkerGO.GetComponent<Button>().onClick,          clk.OnBuyBerserker);
        UnityEventTools.AddPersistentListener(buyPaladinGO.GetComponent<Button>().onClick,            clk.OnBuyPaladin);
        UnityEventTools.AddPersistentListener(healBtnGO.GetComponent<Button>().onClick,               clk.OnHealSelf);
        UnityEventTools.AddPersistentListener(surgeBtnGO.GetComponent<Button>().onClick,              clk.OnUseSurge);
        UnityEventTools.AddPersistentListener(bloodPactGO.GetComponent<Button>().onClick,             clk.OnUseBloodPact);
        UnityEventTools.AddPersistentListener(buyWorkerGO.GetComponent<Button>().onClick,             clk.OnBuyWorker);
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
        UnityEventTools.AddPersistentListener(ssBossTimerBtnGO.GetComponent<Button>().onClick,        clk.OnBuySSBossTimer);
        UnityEventTools.AddPersistentListener(ssDoubleChestBtnGO.GetComponent<Button>().onClick,      clk.OnBuySSDoubleChest);
        UnityEventTools.AddPersistentListener(ssRollbackBtnGO.GetComponent<Button>().onClick,         clk.OnBuySSRollback);
        UnityEventTools.AddPersistentListener(ssBloodTapBtnGO.GetComponent<Button>().onClick,        clk.OnBuySSBloodTap);
        UnityEventTools.AddPersistentListener(upgradeSurgeBtnGO.GetComponent<Button>().onClick,      clk.OnUpgradeSurge);
        UnityEventTools.AddPersistentListener(upgradeHealBtnGO.GetComponent<Button>().onClick,       clk.OnUpgradeHealSelf);
        UnityEventTools.AddPersistentListener(depositBtnGO.GetComponent<Button>().onClick,            clk.OnDepositToBank);
        UnityEventTools.AddPersistentListener(withdrawBtnGO.GetComponent<Button>().onClick,           clk.OnWithdrawFromBank);
        UnityEventTools.AddPersistentListener(statsBtnGO.GetComponent<Button>().onClick,              clk.OnOpenStats);
        UnityEventTools.AddPersistentListener(settingsBtnGO.GetComponent<Button>().onClick,           clk.OnOpenSettings);
        UnityEventTools.AddPersistentListener(suggestBtnGO.GetComponent<Button>().onClick,            clk.OnOpenSuggest);
        UnityEventTools.AddPersistentListener(statsCloseGO.GetComponent<Button>().onClick,            uim.HideStatsPanel);
        UnityEventTools.AddPersistentListener(settingsCloseGO.GetComponent<Button>().onClick,         uim.HideSettingsPanel);
        UnityEventTools.AddPersistentListener(soundToggleGO.GetComponent<Button>().onClick,           clk.OnToggleSound);
        UnityEventTools.AddPersistentListener(notifToggleGO.GetComponent<Button>().onClick,           clk.OnToggleNotifications);
        UnityEventTools.AddPersistentListener(resetDataGO.GetComponent<Button>().onClick,             clk.OnResetData);
        UnityEventTools.AddPersistentListener(offlineDismissGO.GetComponent<Button>().onClick,        uim.DismissOfflinePanel);
        UnityEventTools.AddPersistentListener(featureSubmitGO.GetComponent<Button>().onClick,         uim.SubmitFeature);
        UnityEventTools.AddPersistentListener(featureCancelGO.GetComponent<Button>().onClick,         uim.HideFeaturePanel);

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
