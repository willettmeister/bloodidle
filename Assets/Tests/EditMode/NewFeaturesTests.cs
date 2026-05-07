using NUnit.Framework;
using UnityEngine;

public class NewFeaturesTests
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

    // ── Soldier classes ───────────────────────────────────────────────────────

    [Test]
    public void BuyTank_IncrementsTankCount()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyTank();
        Assert.AreEqual(1, _gm.TankCount);
        Assert.AreEqual(0, _gm.BerserkerCount);
    }

    [Test]
    public void BuyBerserker_IncrementsBerserkerCount()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyBerserker();
        Assert.AreEqual(0, _gm.TankCount);
        Assert.AreEqual(1, _gm.BerserkerCount);
    }

    [Test]
    public void SoldierCount_IsSumOfBothClasses()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 3);
        _gm.BuyTank(); _gm.BuyBerserker(); _gm.BuyTank();
        Assert.AreEqual(2, _gm.TankCount);
        Assert.AreEqual(1, _gm.BerserkerCount);
        Assert.AreEqual(3, _gm.SoldierCount);
    }

    [Test]
    public void BuyBerserker_FirstSoldierStartsAtBerserkerMaxHP()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyBerserker();
        Assert.AreEqual(GameManager.BerserkerMaxHP, _gm.SoldierHP, 0.001f);
    }

    [Test]
    public void FrontlineIsTank_TrueWhenTanksPresent()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 2);
        _gm.BuyTank(); _gm.BuyBerserker();
        Assert.IsTrue(_gm.FrontlineIsTank);
    }

    [Test]
    public void FrontlineIsTank_FalseWhenOnlyBerserkers()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyBerserker();
        Assert.IsFalse(_gm.FrontlineIsTank);
    }

    [Test]
    public void TotalAttack_ReflectsBothClasses()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 3);
        _gm.BuyTank(); _gm.BuyTank(); _gm.BuyBerserker();
        float expected = 2 * GameManager.SoldierAttack + 1 * GameManager.BerserkerAttack;
        Assert.AreEqual(expected, _gm.TotalAttack, 0.001f);
    }

    [Test]
    public void HealSelf_CapsAtBerserkerMaxHPWhenBerserkerFrontline()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost + GameManager.HealSelfUnlockThreshold);
        _gm.BuyBerserker();
        _gm.SetSoldierHPForTest(1f);
        _gm.UseHealSelf();
        Assert.LessOrEqual(_gm.SoldierHP, GameManager.BerserkerMaxHP);
    }

    [Test]
    public void BuySoldier_ActsAsBuyTank()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuySoldier();
        Assert.AreEqual(1, _gm.TankCount);
        Assert.AreEqual(0, _gm.BerserkerCount);
    }

    [Test]
    public void SaveLoad_PreservesBothSoldierCounts()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 3);
        _gm.BuyTank(); _gm.BuyBerserker(); _gm.BuyTank();
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(2, _gm.TankCount);
        Assert.AreEqual(1, _gm.BerserkerCount);
    }

    [Test]
    public void MigrationSave_OldSoldierCountLoadsAsTanks()
    {
        // Simulate an old save with only "SoldierCount" key (no TankCount).
        PlayerPrefs.SetString("Blood", "0");
        PlayerPrefs.SetInt("SoldierCount", 5);
        PlayerPrefs.DeleteKey("TankCount");
        PlayerPrefs.DeleteKey("BerserkerCount");

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(5, _gm.TankCount);
        Assert.AreEqual(0, _gm.BerserkerCount);
    }

    // ── Blood Ritual ──────────────────────────────────────────────────────────

    [Test]
    public void BuyBloodRitual_FailsWithoutWood()
    {
        Assert.IsFalse(_gm.BuyBloodRitual());
        Assert.AreEqual(0, _gm.BloodRitualCount);
    }

    [Test]
    public void BuyBloodRitual_SucceedsAndDeductsWood()
    {
        _gm.SetWoodForTest(GameManager.BloodRitualBaseCost);
        Assert.IsTrue(_gm.BuyBloodRitual());
        Assert.AreEqual(1, _gm.BloodRitualCount);
    }

    [Test]
    public void BuyBloodRitual_CostDoublesEachPurchase()
    {
        _gm.SetWoodForTest(GameManager.BloodRitualBaseCost * 3);
        _gm.BuyBloodRitual();
        double expectedSecondCost = System.Math.Floor(GameManager.BloodRitualBaseCost * GameManager.BloodRitualCostMultiplier);
        Assert.AreEqual(expectedSecondCost, _gm.BloodRitualCost, 0.001);
    }

    [Test]
    public void BloodPerSec_CorrectAfterOnePurchase()
    {
        _gm.SetWoodForTest(GameManager.BloodRitualBaseCost);
        _gm.BuyBloodRitual();
        Assert.AreEqual(GameManager.BloodRitualBloodPerSec, _gm.BloodPerSec, 0.001);
    }

    [Test]
    public void BloodPerSec_ZeroWithNoRituals()
    {
        Assert.AreEqual(0.0, _gm.BloodPerSec, 0.001);
    }

    [Test]
    public void SaveLoad_PreservesBloodRitual()
    {
        _gm.SetWoodForTest(GameManager.BloodRitualBaseCost);
        _gm.BuyBloodRitual();
        double savedCost = _gm.BloodRitualCost;
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(1, _gm.BloodRitualCount);
        Assert.AreEqual(savedCost, _gm.BloodRitualCost, 0.001);
    }

    // ── Prestige ──────────────────────────────────────────────────────────────

    [Test]
    public void Prestige_FailsBelowWaveRequirement()
    {
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement - 1);
        Assert.IsFalse(_gm.Prestige());
        Assert.AreEqual(0, _gm.PrestigeCount);
    }

    [Test]
    public void Prestige_SucceedsAtWaveRequirement()
    {
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        Assert.IsTrue(_gm.Prestige());
        Assert.AreEqual(1, _gm.PrestigeCount);
    }

    [Test]
    public void Prestige_ResetsBloodAndWood()
    {
        _gm.AwardBloodForTest(1000);
        _gm.SetWoodForTest(500);
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.AreEqual(0.0, _gm.Blood, 0.001);
        Assert.AreEqual(0.0, _gm.Wood, 0.001);
    }

    [Test]
    public void Prestige_ResetsWaveToOne()
    {
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.AreEqual(1, _gm.Wave);
    }

    [Test]
    public void Prestige_ResetsSoldiers()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 3);
        _gm.BuyTank(); _gm.BuyBerserker(); _gm.BuyTank();
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.AreEqual(0, _gm.TankCount);
        Assert.AreEqual(0, _gm.BerserkerCount);
        Assert.AreEqual(0f, _gm.SoldierHP, 0.001f);
    }

    [Test]
    public void Prestige_ResetsBloodRitual()
    {
        _gm.SetWoodForTest(GameManager.BloodRitualBaseCost);
        _gm.BuyBloodRitual();
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.AreEqual(0, _gm.BloodRitualCount);
        Assert.AreEqual(GameManager.BloodRitualBaseCost, _gm.BloodRitualCost, 0.001);
    }

    [Test]
    public void Prestige_MultiplierIncreasesWithCount()
    {
        double before = _gm.PrestigeMultiplier;
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.Greater(_gm.PrestigeMultiplier, before);
    }

    [Test]
    public void Prestige_MultiplierIsOneBeforeFirstPrestige()
    {
        Assert.AreEqual(1.0, _gm.PrestigeMultiplier, 0.001);
    }

    [Test]
    public void FarmBlood_AppliesPrestigeMultiplier()
    {
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        double multiplier = _gm.PrestigeMultiplier;
        _gm.FarmBlood();
        Assert.AreEqual(GameManager.BloodPerClick * multiplier, _gm.Blood, 0.001);
    }

    [Test]
    public void SaveLoad_PreservesPrestigeCount()
    {
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(1, _gm.PrestigeCount);
    }
}
