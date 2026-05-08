using NUnit.Framework;
using UnityEngine;

public class NewFeaturesTests5
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

    // ── Paladin ────────────────────────────────────────────────────────────────

    [Test]
    public void BuyPaladin_DeductsBloodAndIncrementsPaladinCount()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        bool result = _gm.BuyPaladin();
        Assert.IsTrue(result);
        Assert.AreEqual(1, _gm.PaladinCount);
        Assert.AreEqual(0.0, _gm.Blood, 0.001);
    }

    [Test]
    public void BuyPaladin_Fails_WhenInsufficientBlood()
    {
        Assert.IsFalse(_gm.BuyPaladin());
        Assert.AreEqual(0, _gm.PaladinCount);
    }

    [Test]
    public void BuyPaladin_Fails_WhenAtCap()
    {
        int cap = _gm.MaxSoldiers;
        _gm.AwardBloodForTest(GameManager.SoldierCost * (cap + 5));
        for (int i = 0; i < cap; i++) _gm.BuyTank();
        Assert.IsFalse(_gm.BuyPaladin());
    }

    [Test]
    public void PaladinCount_IncludedInSoldierCount()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 2);
        _gm.BuyTank();
        _gm.BuyPaladin();
        Assert.AreEqual(2, _gm.SoldierCount);
    }

    [Test]
    public void FrontlineIsPaladin_TrueWhenOnlyPaladins()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyPaladin();
        Assert.IsTrue(_gm.FrontlineIsPaladin);
        Assert.IsFalse(_gm.FrontlineIsTank);
        Assert.IsFalse(_gm.FrontlineIsBerserker);
    }

    [Test]
    public void FrontlineIsPaladin_FalseWhenTankPresent()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 2);
        _gm.BuyTank();
        _gm.BuyPaladin();
        Assert.IsFalse(_gm.FrontlineIsPaladin);
        Assert.IsTrue(_gm.FrontlineIsTank);
    }

    [Test]
    public void Prestige_ResetsPaladinCount()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 3);
        _gm.BuyPaladin();
        _gm.BuyPaladin();
        Assert.AreEqual(2, _gm.PaladinCount);

        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.AreEqual(0, _gm.PaladinCount);
    }

    [Test]
    public void PaladinCount_SaveLoad_Preserved()
    {
        _gm.SetPaladinCountForTest(3);
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(3, _gm.PaladinCount);
    }

    // ── Flawless Wave Bonus ────────────────────────────────────────────────────

    [Test]
    public void FlawlessActive_TrueWhenTimerUnderThreshold()
    {
        _gm.SetFlawlessTimerForTest(5f);
        _gm.SetEnemyHPForTest(10f);
        Assert.IsTrue(_gm.FlawlessActive);
    }

    [Test]
    public void FlawlessActive_FalseWhenTimerExceedsThreshold()
    {
        _gm.SetFlawlessTimerForTest(GameManager.FlawlessThreshold + 1f);
        _gm.SetEnemyHPForTest(10f);
        Assert.IsFalse(_gm.FlawlessActive);
    }

    [Test]
    public void FlawlessActive_FalseWhenEnemyDead()
    {
        _gm.SetFlawlessTimerForTest(1f);
        _gm.SetEnemyHPForTest(0f);
        Assert.IsFalse(_gm.FlawlessActive);
    }

    [Test]
    public void FlawlessActive_FalseAtZeroTimer()
    {
        _gm.SetFlawlessTimerForTest(0f);
        _gm.SetEnemyHPForTest(10f);
        Assert.IsFalse(_gm.FlawlessActive);
    }

    // ── Spell Upgrades ─────────────────────────────────────────────────────────

    [Test]
    public void UpgradeSurge_IncreasesLevel()
    {
        _gm.SetSurgeUnlockedForTest();
        _gm.AwardBloodForTest(_gm.SurgeUpgradeCost);
        bool result = _gm.UpgradeSurge();
        Assert.IsTrue(result);
        Assert.AreEqual(1, _gm.SurgeUpgradeLevel);
    }

    [Test]
    public void UpgradeSurge_Fails_WhenNotUnlocked()
    {
        _gm.AwardBloodForTest(1000.0);
        Assert.IsFalse(_gm.UpgradeSurge());
        Assert.AreEqual(0, _gm.SurgeUpgradeLevel);
    }

    [Test]
    public void UpgradeSurge_Fails_WhenAtMaxLevel()
    {
        _gm.SetSurgeUnlockedForTest();
        _gm.SetSurgeUpgradeLevelForTest(GameManager.MaxSpellUpgradeLevel);
        _gm.AwardBloodForTest(10000.0);
        Assert.IsFalse(_gm.UpgradeSurge());
    }

    [Test]
    public void SurgeDurationEffective_IncreasesWithLevel()
    {
        float base_ = GameManager.SurgeDuration;
        Assert.AreEqual(base_, _gm.SurgeDurationEffective, 0.001f);
        _gm.SetSurgeUpgradeLevelForTest(2);
        Assert.AreEqual(base_ + 2 * 5f, _gm.SurgeDurationEffective, 0.001f);
    }

    [Test]
    public void UpgradeHealSelf_IncreasesLevel()
    {
        _gm.ForceUnlockHealSelfForTest();
        _gm.AwardBloodForTest(_gm.HealUpgradeCost);
        bool result = _gm.UpgradeHealSelf();
        Assert.IsTrue(result);
        Assert.AreEqual(1, _gm.HealUpgradeLevel);
    }

    [Test]
    public void UpgradeHealSelf_Fails_WhenNotUnlocked()
    {
        _gm.AwardBloodForTest(1000.0);
        Assert.IsFalse(_gm.UpgradeHealSelf());
    }

    [Test]
    public void HealSelfAmountEffective_IncreasesWithLevel()
    {
        float base_ = GameManager.HealSelfAmount;
        Assert.AreEqual(base_, _gm.HealSelfAmountEffective, 0.001f);
        _gm.SetHealUpgradeLevelForTest(3);
        Assert.AreEqual(base_ + 3 * 10f, _gm.HealSelfAmountEffective, 0.001f);
    }

    [Test]
    public void SurgeUpgradeLevel_SaveLoad_Preserved()
    {
        _gm.SetSurgeUpgradeLevelForTest(2);
        _gm.SetHealUpgradeLevelForTest(1);
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(2, _gm.SurgeUpgradeLevel);
        Assert.AreEqual(1, _gm.HealUpgradeLevel);
    }

    // ── Blood Tap (Soul Shard) ─────────────────────────────────────────────────

    [Test]
    public void BuySSBloodTap_IncreasesLevel()
    {
        _gm.UnlockSoulShardShopForTest();
        _gm.SetSoulShardsForTest(GameManager.SSUpgradeCost);
        bool result = _gm.BuySSBloodTap();
        Assert.IsTrue(result);
        Assert.AreEqual(1, _gm.SSBloodTapLevel);
    }

    [Test]
    public void BuySSBloodTap_Fails_WhenAtMaxLevel()
    {
        _gm.UnlockSoulShardShopForTest();
        _gm.SetSSBloodTapLevelForTest(GameManager.SSMaxLevel);
        _gm.SetSoulShardsForTest(10.0);
        Assert.IsFalse(_gm.BuySSBloodTap());
    }

    [Test]
    public void BloodTapPerSec_ScalesWithLevel()
    {
        _gm.SetSSBloodTapLevelForTest(2);
        Assert.AreEqual(2.0 * _gm.PrestigeMultiplier, _gm.BloodTapPerSec, 0.001);
    }

    [Test]
    public void SSBloodTapLevel_SaveLoad_Preserved()
    {
        _gm.SetSSBloodTapLevelForTest(2);
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(2, _gm.SSBloodTapLevel);
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    [Test]
    public void SoundEnabled_TrueByDefault()
    {
        Assert.IsTrue(_gm.SoundEnabled);
    }

    [Test]
    public void ToggleSound_TogglesState()
    {
        Assert.IsTrue(_gm.SoundEnabled);
        _gm.ToggleSound();
        Assert.IsFalse(_gm.SoundEnabled);
        _gm.ToggleSound();
        Assert.IsTrue(_gm.SoundEnabled);
    }

    [Test]
    public void NotificationsEnabled_TrueByDefault()
    {
        Assert.IsTrue(_gm.NotificationsEnabled);
    }

    [Test]
    public void ToggleNotifications_TogglesState()
    {
        _gm.ToggleNotifications();
        Assert.IsFalse(_gm.NotificationsEnabled);
    }

    [Test]
    public void Settings_SaveLoad_Preserved()
    {
        _gm.ToggleSound();
        _gm.ToggleNotifications();
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.IsFalse(_gm.SoundEnabled);
        Assert.IsFalse(_gm.NotificationsEnabled);
    }

    [Test]
    public void ResetAllData_ClearsAllState()
    {
        _gm.AwardBloodForTest(500.0);
        _gm.BuyTank();
        _gm.BuyPaladin();
        _gm.SetSurgeUpgradeLevelForTest(2);
        _gm.SetSSBloodTapLevelForTest(1);

        _gm.ResetAllData();

        Assert.AreEqual(0.0, _gm.Blood, 0.001);
        Assert.AreEqual(0, _gm.TankCount);
        Assert.AreEqual(0, _gm.PaladinCount);
        Assert.AreEqual(0, _gm.SurgeUpgradeLevel);
        Assert.AreEqual(0, _gm.SSBloodTapLevel);
        Assert.AreEqual(1, _gm.Wave);
    }
}
