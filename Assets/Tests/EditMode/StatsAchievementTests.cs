using NUnit.Framework;
using UnityEngine;

public class StatsAchievementTests
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

    // ── Statistics ────────────────────────────────────────────────────────────

    [Test]
    public void TotalEnemiesKilled_StartsAtZero()
        => Assert.AreEqual(0, _gm.TotalEnemiesKilled);

    [Test]
    public void TimePlayed_StartsAtZero()
        => Assert.AreEqual(0.0, _gm.TimePlayed, 0.001);

    [Test]
    public void SaveLoad_PreservesStats()
    {
        // Manually set via wave kill flow: award enough kills by triggering enemy deaths
        // (We can't directly set TotalEnemiesKilled, so we verify save/load via prestige count proxy)
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige(); // PrestigeCount = 1
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(1, _gm.PrestigeCount);
    }

    // ── Achievements ──────────────────────────────────────────────────────────

    [Test]
    public void Achievements_StartAtNone()
        => Assert.AreEqual(AchievementFlags.None, _gm.Achievements);

    [Test]
    public void Achievement_FirstSoldier_UnlocksOnFirstBuy()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyTank();
        Assert.IsTrue((_gm.Achievements & AchievementFlags.FirstSoldier) != 0);
    }

    [Test]
    public void Achievement_FirstSoldier_UnlocksOnFirstBerserkerBuy()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyBerserker();
        Assert.IsTrue((_gm.Achievements & AchievementFlags.FirstSoldier) != 0);
    }

    [Test]
    public void Achievement_FullLegion_UnlocksWhenAtCap()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * (_gm.MaxSoldiers + 1));
        for (int i = 0; i < _gm.MaxSoldiers; i++) _gm.BuyTank();
        Assert.IsTrue((_gm.Achievements & AchievementFlags.FullLegion) != 0);
    }

    [Test]
    public void Achievement_Blood1K_UnlocksAt1000TotalEarned()
    {
        _gm.AwardBloodForTest(999);
        Assert.IsFalse((_gm.Achievements & AchievementFlags.Blood1K) != 0);
        _gm.AwardBloodForTest(1);
        Assert.IsTrue((_gm.Achievements & AchievementFlags.Blood1K) != 0);
    }

    [Test]
    public void Achievement_Blood10K_UnlocksAt10000TotalEarned()
    {
        _gm.AwardBloodForTest(9_999);
        Assert.IsFalse((_gm.Achievements & AchievementFlags.Blood10K) != 0);
        _gm.AwardBloodForTest(1);
        Assert.IsTrue((_gm.Achievements & AchievementFlags.Blood10K) != 0);
    }

    [Test]
    public void Achievement_FirstRitual_UnlocksOnFirstPurchase()
    {
        _gm.SetWoodForTest(GameManager.BloodRitualBaseCost);
        _gm.BuyBloodRitual();
        Assert.IsTrue((_gm.Achievements & AchievementFlags.FirstRitual) != 0);
    }

    [Test]
    public void Achievement_FirstPrestige_UnlocksOnPrestige()
    {
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.IsTrue((_gm.Achievements & AchievementFlags.FirstPrestige) != 0);
    }

    [Test]
    public void Achievement_NotDoubleUnlocked()
    {
        int eventCount = 0;
        _gm.OnAchievementUnlocked += _ => eventCount++;

        _gm.AwardBloodForTest(1_000);  // unlocks Blood1K
        _gm.AwardBloodForTest(1_000);  // should NOT fire again
        Assert.AreEqual(1, eventCount);
    }

    [Test]
    public void SaveLoad_PreservesAchievements()
    {
        _gm.AwardBloodForTest(1_000);  // unlocks Blood1K
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.IsTrue((_gm.Achievements & AchievementFlags.Blood1K) != 0);
    }

    [Test]
    public void AchievementEvent_FiresOnUnlock()
    {
        AchievementFlags fired = AchievementFlags.None;
        _gm.OnAchievementUnlocked += f => fired = f;

        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyTank();

        Assert.AreEqual(AchievementFlags.FirstSoldier, fired);
    }
}
