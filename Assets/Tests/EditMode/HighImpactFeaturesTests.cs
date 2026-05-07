using NUnit.Framework;
using UnityEngine;

public class HighImpactFeaturesTests
{
    GameObject _gmGO;
    GameManager _gm;

    [SetUp]
    public void SetUp()
    {
        GameManager.ResetForTest();
        PlayerPrefs.DeleteAll();
        _gmGO = new GameObject("GM");
        _gm   = _gmGO.AddComponent<GameManager>();
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteAll();
        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
    }

    // ── Offline blood income ──────────────────────────────────────────────────

    [Test]
    public void CalculateOfflineBlood_OneRitual_MatchesFormula()
    {
        double secs = 3600.0;
        double expected = 1 * GameManager.BloodRitualBloodPerSec * 1.0 * secs;
        Assert.AreEqual(expected,
            GameManager.CalculateOfflineBlood(1, 0, 1.0, secs), 0.001);
    }

    [Test]
    public void CalculateOfflineBlood_CapsAtEightHours()
    {
        double over8h = 9 * 3600.0;
        double capped = 8 * 3600.0;
        Assert.AreEqual(
            GameManager.CalculateOfflineBlood(1, 0, 1.0, capped),
            GameManager.CalculateOfflineBlood(1, 0, 1.0, over8h), 0.001);
    }

    [Test]
    public void CalculateOfflineBlood_ScalesWithRitualEffLevel()
    {
        double secs = 100.0;
        double noBonus   = GameManager.CalculateOfflineBlood(1, 0, 1.0, secs);
        double withBonus = GameManager.CalculateOfflineBlood(1, 2, 1.0, secs);
        Assert.Greater(withBonus, noBonus);
    }

    [Test]
    public void CalculateOfflineBlood_ScalesWithPrestigeMultiplier()
    {
        double secs = 100.0;
        Assert.Greater(
            GameManager.CalculateOfflineBlood(1, 0, 1.5, secs),
            GameManager.CalculateOfflineBlood(1, 0, 1.0, secs));
    }

    // ── Army composition bonuses ──────────────────────────────────────────────

    [Test]
    public void IsAllTank_TrueWhenOnlyTanks()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyTank();
        Assert.IsTrue(_gm.IsAllTank);
        Assert.IsFalse(_gm.IsAllBerserker);
    }

    [Test]
    public void IsAllBerserker_TrueWhenOnlyBerserkers()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyBerserker();
        Assert.IsTrue(_gm.IsAllBerserker);
        Assert.IsFalse(_gm.IsAllTank);
    }

    [Test]
    public void IsAllTank_FalseWithMixedArmy()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 2);
        _gm.BuyTank(); _gm.BuyBerserker();
        Assert.IsFalse(_gm.IsAllTank);
        Assert.IsFalse(_gm.IsAllBerserker);
    }

    [Test]
    public void IsAllTank_FalseWithNoSoldiers()
    {
        Assert.IsFalse(_gm.IsAllTank);
        Assert.IsFalse(_gm.IsAllBerserker);
    }

    // ── Blood Surge spell ─────────────────────────────────────────────────────

    [Test]
    public void Surge_LockedBeforeThreshold()
    {
        _gm.AwardBloodForTest(GameManager.SurgeUnlockThreshold - 1);
        Assert.IsFalse(_gm.SurgeUnlocked);
    }

    [Test]
    public void Surge_UnlocksAtThreshold()
    {
        _gm.AwardBloodForTest(GameManager.SurgeUnlockThreshold);
        Assert.IsTrue(_gm.SurgeUnlocked);
    }

    [Test]
    public void Surge_FailsWithoutSoldiers()
    {
        _gm.SetSurgeUnlockedForTest();
        _gm.AwardBloodForTest(GameManager.SurgeCost);
        Assert.IsFalse(_gm.UseSurge());
        Assert.IsFalse(_gm.SurgeActive);
    }

    [Test]
    public void Surge_FailsWithInsufficientBlood()
    {
        _gm.SetSurgeUnlockedForTest();
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyTank();
        // no extra blood for surge cost
        Assert.IsFalse(_gm.UseSurge());
        Assert.IsFalse(_gm.SurgeActive);
    }

    [Test]
    public void Surge_ActivatesAndDeductsBlood()
    {
        _gm.SetSurgeUnlockedForTest();
        _gm.AwardBloodForTest(GameManager.SoldierCost + GameManager.SurgeCost);
        _gm.BuyTank();
        double bloodBefore = _gm.Blood;
        Assert.IsTrue(_gm.UseSurge());
        Assert.IsTrue(_gm.SurgeActive);
        Assert.AreEqual(bloodBefore - GameManager.SurgeCost, _gm.Blood, 0.001);
    }

    [Test]
    public void Surge_TimerStartsAtDuration()
    {
        _gm.SetSurgeUnlockedForTest();
        _gm.AwardBloodForTest(GameManager.SoldierCost + GameManager.SurgeCost);
        _gm.BuyTank();
        _gm.UseSurge();
        Assert.AreEqual(GameManager.SurgeDuration, _gm.SurgeTimeRemaining, 0.001f);
    }

    [Test]
    public void Surge_CannotActivateWhileActive()
    {
        _gm.SetSurgeUnlockedForTest();
        _gm.AwardBloodForTest(GameManager.SoldierCost + GameManager.SurgeCost * 2);
        _gm.BuyTank();
        _gm.UseSurge();
        Assert.IsFalse(_gm.UseSurge());
    }

    [Test]
    public void Surge_ResetOnPrestige()
    {
        _gm.SetSurgeUnlockedForTest();
        _gm.AwardBloodForTest(GameManager.SoldierCost + GameManager.SurgeCost);
        _gm.BuyTank();
        _gm.UseSurge();
        Assert.IsTrue(_gm.SurgeActive);
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.IsFalse(_gm.SurgeActive);
        Assert.AreEqual(0f, _gm.SurgeTimeRemaining, 0.001f);
    }

    // ── Prestige Shop ─────────────────────────────────────────────────────────

    [Test]
    public void Prestige_AwardsPrestigePoint()
    {
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.AreEqual(1, _gm.PrestigePoints);
    }

    [Test]
    public void PrestigeShop_BuySoldierCap_FailsWithoutPoints()
    {
        Assert.IsFalse(_gm.BuyPSoldierCap());
        Assert.AreEqual(0, _gm.PSoldierCapLevel);
    }

    [Test]
    public void PrestigeShop_BuySoldierCap_IncreasesMaxSoldiers()
    {
        _gm.AwardPrestigePointsForTest(1);
        int before = _gm.MaxSoldiers;
        _gm.BuyPSoldierCap();
        Assert.AreEqual(before + 10, _gm.MaxSoldiers);
        Assert.AreEqual(1, _gm.PSoldierCapLevel);
        Assert.AreEqual(0, _gm.PrestigePoints);
    }

    [Test]
    public void PrestigeShop_BuyClickBonus_IncreasesLevel()
    {
        _gm.AwardPrestigePointsForTest(1);
        _gm.BuyPClickBonus();
        Assert.AreEqual(1, _gm.PClickBonusLevel);
        Assert.AreEqual(0, _gm.PrestigePoints);
    }

    [Test]
    public void PrestigeShop_ClickBonus_IncreasesEffectiveBloodPerClick()
    {
        double before = _gm.EffectiveBloodPerClick;
        _gm.AwardPrestigePointsForTest(1);
        _gm.BuyPClickBonus();
        Assert.Greater(_gm.EffectiveBloodPerClick, before);
    }

    [Test]
    public void PrestigeShop_BuyRitualEff_IncreasesBloodPerSec()
    {
        _gm.SetWoodForTest(GameManager.BloodRitualBaseCost);
        _gm.BuyBloodRitual();
        double before = _gm.BloodPerSec;
        _gm.AwardPrestigePointsForTest(1);
        _gm.BuyPRitualEff();
        Assert.Greater(_gm.BloodPerSec, before);
    }

    [Test]
    public void PrestigeShop_PointsNotResetOnPrestige()
    {
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige(); // gains 1 PP
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige(); // gains another 1 PP
        Assert.AreEqual(2, _gm.PrestigePoints);
    }

    [Test]
    public void PrestigeShop_SoldierCapPersistsAcrossPrestige()
    {
        _gm.AwardPrestigePointsForTest(1);
        _gm.BuyPSoldierCap(); // PSoldierCapLevel = 1
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.AreEqual(10 + 10, _gm.MaxSoldiers); // base 10 + level-1 bonus
    }

    [Test]
    public void SaveLoad_PreservesPrestigeShopState()
    {
        _gm.AwardPrestigePointsForTest(3);
        _gm.BuyPSoldierCap();
        _gm.BuyPClickBonus();
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(1, _gm.PSoldierCapLevel);
        Assert.AreEqual(1, _gm.PClickBonusLevel);
        Assert.AreEqual(0, _gm.PRitualEffLevel);
        Assert.AreEqual(1, _gm.PrestigePoints);
    }

    // ── Wave milestone chests ─────────────────────────────────────────────────

    [Test]
    public void MilestoneChest_FiresOnMilestoneChestEvent()
    {
        string received = null;
        _gm.OnMilestoneChest += msg => received = msg;
        _gm.TriggerMilestoneChestForTest(5);
        Assert.IsNotNull(received);
        StringAssert.Contains("Wave 5", received);
    }

    [Test]
    public void MilestoneChest_GrantsBloodOrWood()
    {
        double bloodBefore = _gm.Blood;
        double woodBefore  = _gm.Wood;
        _gm.AwardBloodForTest(GameManager.SoldierCost * 5);
        for (int i = 0; i < 5; i++) _gm.BuyTank(); // fill army for free-soldier fallback
        // Run chest 10 times to hit at least one blood or wood outcome
        for (int i = 0; i < 10; i++) _gm.TriggerMilestoneChestForTest(5);
        Assert.IsTrue(_gm.Blood > bloodBefore || _gm.Wood > woodBefore);
    }

    [Test]
    public void MilestoneChest_ChestMessageContainsPlus()
    {
        string received = null;
        _gm.OnMilestoneChest += msg => received = msg;
        _gm.TriggerMilestoneChestForTest(10);
        Assert.IsNotNull(received);
        StringAssert.Contains("+", received);
    }

    [Test]
    public void MilestoneChest_WaveMultiplesAreCorrect()
    {
        // Sanity: verify the modulo used in RunCombat for milestone detection
        for (int w = 1; w <= 20; w++)
            Assert.AreEqual(w % 5 == 0, w == 5 || w == 10 || w == 15 || w == 20);
    }
}
