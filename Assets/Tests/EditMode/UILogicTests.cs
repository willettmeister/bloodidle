using NUnit.Framework;
using UnityEngine;

// Tests for game-state rules that drive UI visibility and interactability.
// These verify the GameManager logic that UIManager.Refresh() reads directly.
public class UILogicTests
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

    // ── Workers panel unlock ──────────────────────────────────────────────────

    [Test]
    public void Workers_LockedBeforeThreshold()
    {
        _gm.AwardBloodForTest(GameManager.WorkersUnlockThreshold - 1);
        Assert.IsFalse(_gm.WorkersUnlocked);
    }

    [Test]
    public void Workers_UnlockedAtExactThreshold()
    {
        _gm.AwardBloodForTest(GameManager.WorkersUnlockThreshold);
        Assert.IsTrue(_gm.WorkersUnlocked);
    }

    [Test]
    public void Workers_UnlockThreshold_Is200Blood()
    {
        Assert.AreEqual(200.0, GameManager.WorkersUnlockThreshold);
    }

    // ── Heal Self panel unlock ────────────────────────────────────────────────

    [Test]
    public void HealSelf_LockedBeforeThreshold()
    {
        _gm.AwardBloodForTest(GameManager.HealSelfUnlockThreshold - 1);
        Assert.IsFalse(_gm.HealSelfUnlocked);
    }

    [Test]
    public void HealSelf_UnlockedAtExactThreshold()
    {
        _gm.AwardBloodForTest(GameManager.HealSelfUnlockThreshold);
        Assert.IsTrue(_gm.HealSelfUnlocked);
    }

    [Test]
    public void HealSelf_UnlockThreshold_Is250Blood()
    {
        Assert.AreEqual(250.0, GameManager.HealSelfUnlockThreshold);
    }

    // ── Unlock ordering ───────────────────────────────────────────────────────

    [Test]
    public void Workers_UnlocksBeforeHealSelf()
    {
        Assert.Less(GameManager.WorkersUnlockThreshold,
                    GameManager.HealSelfUnlockThreshold,
                    "Workers should unlock before Heal Self");
    }

    // ── Buy Soldier button ────────────────────────────────────────────────────

    [Test]
    public void BuySoldier_SucceedsWhenAffordable()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        bool bought = _gm.BuySoldier();
        Assert.IsTrue(bought);
        Assert.AreEqual(1, _gm.SoldierCount);
    }

    [Test]
    public void BuySoldier_FailsWhenBroke()
    {
        Assert.IsFalse(_gm.BuySoldier());
        Assert.AreEqual(0, _gm.SoldierCount);
    }

    [Test]
    public void BuySoldier_FailsAtMaxCapacity()
    {
        // Buy up to the cap
        for (int i = 0; i < _gm.MaxSoldiers; i++)
        {
            _gm.AwardBloodForTest(GameManager.SoldierCost);
            _gm.BuySoldier();
        }
        Assert.AreEqual(_gm.MaxSoldiers, _gm.SoldierCount);
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        Assert.IsFalse(_gm.BuySoldier(), "Cannot buy beyond MaxSoldiers");
    }

    // ── Heal Self button ──────────────────────────────────────────────────────

    [Test]
    public void HealSelf_FailsWithNoSoldiers()
    {
        _gm.AwardBloodForTest(GameManager.HealSelfCost + GameManager.HealSelfUnlockThreshold);
        Assert.IsFalse(_gm.UseHealSelf(), "Heal Self requires at least one soldier");
    }

    [Test]
    public void HealSelf_FailsWhenSoldierAtFullHP()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost + GameManager.HealSelfCost
                              + GameManager.HealSelfUnlockThreshold);
        _gm.BuySoldier();
        // Soldier starts at full HP
        Assert.IsFalse(_gm.UseHealSelf(), "Heal Self must not fire when HP is already full");
    }

    [Test]
    public void HealSelf_SucceedsWhenSoldierIsDamaged()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost + GameManager.HealSelfCost
                              + GameManager.HealSelfUnlockThreshold);
        _gm.BuySoldier();
        _gm.SetSoldierHPForTest(1f);
        Assert.IsTrue(_gm.UseHealSelf());
        Assert.Greater(_gm.SoldierHP, 1f);
    }

    // ── Barracks upgrade ──────────────────────────────────────────────────────

    [Test]
    public void UpgradeBarracks_FailsWithInsufficientWood()
    {
        Assert.IsFalse(_gm.UpgradeBarracks());
    }

    [Test]
    public void UpgradeBarracks_IncreasesMaxSoldiers()
    {
        int before = _gm.MaxSoldiers;
        _gm.SetWoodForTest(_gm.BarracksUpgradeCost);
        _gm.UpgradeBarracks();
        Assert.AreEqual(before + GameManager.BarracksSoldierBonus, _gm.MaxSoldiers);
    }

    [Test]
    public void UpgradeBarracks_CostDoublesEachLevel()
    {
        double firstCost = _gm.BarracksUpgradeCost;
        _gm.SetWoodForTest(firstCost);
        _gm.UpgradeBarracks();
        Assert.AreEqual(
            System.Math.Floor(firstCost * GameManager.BarracksCostMultiplier),
            _gm.BarracksUpgradeCost, 0.001);
    }

    // ── Buy Worker button ─────────────────────────────────────────────────────

    [Test]
    public void BuyWorker_SucceedsWhenAffordable()
    {
        _gm.AwardBloodForTest(GameManager.WorkerCost);
        Assert.IsTrue(_gm.BuyWorker());
        Assert.AreEqual(1, _gm.WorkerCount);
    }

    [Test]
    public void BuyWorker_FailsWhenBroke()
    {
        Assert.IsFalse(_gm.BuyWorker());
    }

    // ── Wood per second ───────────────────────────────────────────────────────

    [Test]
    public void WoodPerSecond_ScalesWithWorkerCount()
    {
        _gm.AwardBloodForTest(GameManager.WorkerCost * 3);
        _gm.BuyWorker(); _gm.BuyWorker(); _gm.BuyWorker();
        Assert.AreEqual(3 * GameManager.WorkerWoodPerSec, _gm.WoodPerSecond, 0.001);
    }
}
