using NUnit.Framework;
using UnityEngine;

public class GameManagerTests
{
    GameManager _gm;

    [SetUp]
    public void Setup()
    {
        GameManager.ResetForTest();
        _gm = new GameObject("GameManager").AddComponent<GameManager>();
    }

    [TearDown]
    public void Teardown()
    {
        if (_gm != null) Object.DestroyImmediate(_gm.gameObject);
        GameManager.ResetForTest();
    }

    // --- Initial state ---

    [Test]
    public void InitialState_Wave1NoBloodNoUnits()
    {
        Assert.AreEqual(1, _gm.Wave);
        Assert.AreEqual(0.0, _gm.Blood);
        Assert.AreEqual(0, _gm.SoldierCount);
        Assert.AreEqual(0, _gm.WorkerCount);
    }

    [Test]
    public void InitialState_Wave1EnemyHas100HP()
    {
        Assert.AreEqual(100f, _gm.EnemyMaxHP, 0.01f);
        Assert.AreEqual(100f, _gm.EnemyHP, 0.01f);
    }

    [Test]
    public void InitialState_BarracksLevel1Cap10()
    {
        Assert.AreEqual(1, _gm.BarracksLevel);
        Assert.AreEqual(10, _gm.MaxSoldiers);
        Assert.AreEqual(20.0, _gm.BarracksUpgradeCost, 0.001);
    }

    // --- FarmBlood ---

    [Test]
    public void FarmBlood_IncreasesBloodByOne()
    {
        _gm.FarmBlood();
        Assert.AreEqual(1.0, _gm.Blood);
    }

    [Test]
    public void FarmBlood_TracksTotalEarned()
    {
        for (int i = 0; i < 3; i++) _gm.FarmBlood();
        Assert.AreEqual(3.0, _gm.TotalBloodEarned);
    }

    // --- HealSelf unlock ---

    [Test]
    public void HealSelf_LockedInitially()
        => Assert.IsFalse(_gm.HealSelfUnlocked);

    [Test]
    public void HealSelf_UnlocksAt50TotalBlood()
    {
        for (int i = 0; i < 50; i++) _gm.FarmBlood();
        Assert.IsTrue(_gm.HealSelfUnlocked);
    }

    [Test]
    public void HealSelf_StaysLockedAt49()
    {
        for (int i = 0; i < 49; i++) _gm.FarmBlood();
        Assert.IsFalse(_gm.HealSelfUnlocked);
    }

    // --- BuySoldier ---

    [Test]
    public void BuySoldier_FailsWithNoBlood()
    {
        Assert.IsFalse(_gm.BuySoldier());
        Assert.AreEqual(0, _gm.SoldierCount);
    }

    [Test]
    public void BuySoldier_SucceedsAndDeductsBlood()
    {
        for (int i = 0; i < 10; i++) _gm.FarmBlood();
        Assert.IsTrue(_gm.BuySoldier());
        Assert.AreEqual(1, _gm.SoldierCount);
        Assert.AreEqual(0.0, _gm.Blood);
    }

    [Test]
    public void BuySoldier_FirstSoldierStartsAtFullHP()
    {
        for (int i = 0; i < 10; i++) _gm.FarmBlood();
        _gm.BuySoldier();
        Assert.AreEqual(GameManager.SoldierMaxHP, _gm.SoldierHP);
    }

    [Test]
    public void BuySoldier_BlockedAtMaxCap()
    {
        int needed = (int)(GameManager.SoldierCost * (_gm.MaxSoldiers + 1));
        for (int i = 0; i < needed; i++) _gm.FarmBlood();
        for (int i = 0; i < _gm.MaxSoldiers; i++) _gm.BuySoldier();
        Assert.IsFalse(_gm.BuySoldier());
        Assert.AreEqual(10, _gm.SoldierCount);
    }

    // --- BuyWorker ---

    [Test]
    public void BuyWorker_FailsWithNoBlood()
        => Assert.IsFalse(_gm.BuyWorker());

    [Test]
    public void BuyWorker_SucceedsAndDeductsBlood()
    {
        for (int i = 0; i < 50; i++) _gm.FarmBlood();
        Assert.IsTrue(_gm.BuyWorker());
        Assert.AreEqual(1, _gm.WorkerCount);
        Assert.AreEqual(0.0, _gm.Blood);
    }

    [Test]
    public void WoodPerSecond_ScalesWithWorkerCount()
    {
        for (int i = 0; i < 100; i++) _gm.FarmBlood();
        _gm.BuyWorker();
        _gm.BuyWorker();
        Assert.AreEqual(GameManager.WorkerWoodPerSec * 2, _gm.WoodPerSecond, 0.0001);
    }

    // --- UpgradeBarracks ---

    [Test]
    public void UpgradeBarracks_FailsWithNoWood()
        => Assert.IsFalse(_gm.UpgradeBarracks());

    [Test]
    public void UpgradeBarracks_IncreasesLevelAndSoldierCap()
    {
        _gm.SetWoodForTest(20.0);
        Assert.IsTrue(_gm.UpgradeBarracks());
        Assert.AreEqual(2, _gm.BarracksLevel);
        Assert.AreEqual(15, _gm.MaxSoldiers);
    }

    [Test]
    public void UpgradeBarracks_DoublesNextCost()
    {
        _gm.SetWoodForTest(20.0);
        _gm.UpgradeBarracks();
        Assert.AreEqual(40.0, _gm.BarracksUpgradeCost, 0.001);
    }

    [Test]
    public void UpgradeBarracks_DeductsWood()
    {
        _gm.SetWoodForTest(30.0);
        _gm.UpgradeBarracks();
        Assert.AreEqual(10.0, _gm.Wood, 0.001);
    }

    // --- UseHealSelf ---

    [Test]
    public void UseHealSelf_FailsWhenLocked()
        => Assert.IsFalse(_gm.UseHealSelf());

    [Test]
    public void UseHealSelf_FailsWithNoSoldiers()
    {
        for (int i = 0; i < 50; i++) _gm.FarmBlood(); // unlock, no soldier
        Assert.IsFalse(_gm.UseHealSelf());
    }

    [Test]
    public void UseHealSelf_FailsAtFullHP()
    {
        // Unlock (50 earned) + buy soldier (10) = need >= 60; farm 85 for blood headroom
        for (int i = 0; i < 85; i++) _gm.FarmBlood();
        _gm.BuySoldier(); // soldier at full HP
        Assert.IsFalse(_gm.UseHealSelf());
    }

    [Test]
    public void UseHealSelf_FailsWithInsufficientBlood()
    {
        // Farm 60 (unlocks at 50), buy 4 soldiers (40 blood) → 20 blood left < 25 cost
        for (int i = 0; i < 60; i++) _gm.FarmBlood();
        for (int i = 0; i < 4; i++) _gm.BuySoldier();
        _gm.SetSoldierHPForTest(10f);
        Assert.IsFalse(_gm.UseHealSelf());
    }

    [Test]
    public void UseHealSelf_HealsAndDeductsBlood()
    {
        for (int i = 0; i < 85; i++) _gm.FarmBlood(); // blood=85, unlocked
        _gm.BuySoldier(); // blood=75
        _gm.SetSoldierHPForTest(10f);
        Assert.IsTrue(_gm.UseHealSelf());
        Assert.AreEqual(50.0, _gm.Blood, 0.001);     // 75 - 25
        Assert.AreEqual(30f, _gm.SoldierHP, 0.001f); // 10 + 20
    }

    [Test]
    public void UseHealSelf_CapsHPAtMax()
    {
        for (int i = 0; i < 85; i++) _gm.FarmBlood();
        _gm.BuySoldier();
        _gm.SetSoldierHPForTest(40f); // 10 below cap; heal +20 would overshoot
        _gm.UseHealSelf();
        Assert.AreEqual(GameManager.SoldierMaxHP, _gm.SoldierHP);
    }
}
