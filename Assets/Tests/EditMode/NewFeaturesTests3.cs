using NUnit.Framework;
using UnityEngine;
using System;

public class NewFeaturesTests3
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

    // ── Mixed Army Formation ──────────────────────────────────────────────────

    [Test]
    public void IsMixedArmy_TrueWhenBothTankAndBerserker()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 2);
        _gm.BuyTank();
        _gm.BuyBerserker();
        Assert.IsTrue(_gm.IsMixedArmy);
    }

    [Test]
    public void IsMixedArmy_FalseWhenAllTanks()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 2);
        _gm.BuyTank();
        _gm.BuyTank();
        Assert.IsFalse(_gm.IsMixedArmy);
    }

    [Test]
    public void IsMixedArmy_FalseWhenAllBerserkers()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 2);
        _gm.BuyBerserker();
        _gm.BuyBerserker();
        Assert.IsFalse(_gm.IsMixedArmy);
    }

    [Test]
    public void IsMixedArmy_FalseWithNoSoldiers()
    {
        Assert.IsFalse(_gm.IsMixedArmy);
    }

    // ── Fortifications ───────────────────────────────────────────────────────

    [Test]
    public void UpgradeFortification_ConsumesWoodAndIncrementsLevel()
    {
        _gm.SetWoodForTest(GameManager.FortBaseCost);
        Assert.IsTrue(_gm.UpgradeFortification());
        Assert.AreEqual(1, _gm.FortificationLevel);
        Assert.AreEqual(0.0, _gm.Wood, 0.001);
    }

    [Test]
    public void UpgradeFortification_Fails_WithoutWood()
    {
        Assert.IsFalse(_gm.UpgradeFortification());
        Assert.AreEqual(0, _gm.FortificationLevel);
    }

    [Test]
    public void UpgradeFortification_CostDoublesEachLevel()
    {
        _gm.SetWoodForTest(GameManager.FortBaseCost * 4);
        _gm.UpgradeFortification();
        Assert.AreEqual(
            GameManager.FortBaseCost * GameManager.FortCostMultiplier,
            _gm.FortificationCost, 0.001);
    }

    [Test]
    public void UpgradeFortification_CappedAtMaxLevel()
    {
        _gm.SetFortLevelForTest(GameManager.MaxFortificationLevel);
        _gm.SetWoodForTest(99999);
        Assert.IsFalse(_gm.UpgradeFortification());
        Assert.AreEqual(GameManager.MaxFortificationLevel, _gm.FortificationLevel);
    }

    [Test]
    public void FortificationDmgReduction_ScalesWithLevel()
    {
        _gm.SetFortLevelForTest(5);
        float expected = 5 * GameManager.FortHPReductionPerLevel;
        Assert.AreEqual(expected, _gm.FortificationDmgReduction, 0.0001f);
    }

    [Test]
    public void Fortification_ReducesBossEnemyHP()
    {
        // Boss at wave 1 is deterministic: base HP = 100 * 1.5^0 * 5 = 500
        _gm.SetWaveForTest(1);
        _gm.SetNextBossWaveForTest(1);
        _gm.SpawnEnemyForTest(1);
        float baseHP = _gm.EnemyMaxHP;

        _gm.SetFortLevelForTest(5);
        _gm.SpawnEnemyForTest(1);
        float reducedHP = _gm.EnemyMaxHP;

        Assert.AreEqual(baseHP * (1f - 5 * GameManager.FortHPReductionPerLevel), reducedHP, 0.5f);
    }

    [Test]
    public void Fortification_SaveLoad_PreservesLevel()
    {
        _gm.SetFortLevelForTest(4);
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(4, _gm.FortificationLevel);
    }

    // ── Soul Shards ──────────────────────────────────────────────────────────

    [Test]
    public void SoulShardShop_LockedByDefault()
    {
        Assert.IsFalse(_gm.SoulShardShopUnlocked);
        Assert.AreEqual(0.0, _gm.SoulShards, 0.001);
    }

    [Test]
    public void BuySSBossTimer_ConsumesShardAndIncrementsLevel()
    {
        _gm.SetSoulShardsForTest(GameManager.SSUpgradeCost);
        Assert.IsTrue(_gm.BuySSBossTimer());
        Assert.AreEqual(1, _gm.SSBossTimerLevel);
        Assert.AreEqual(0.0, _gm.SoulShards, 0.001);
    }

    [Test]
    public void BuySSBossTimer_Fails_WithoutShards()
    {
        Assert.IsFalse(_gm.BuySSBossTimer());
        Assert.AreEqual(0, _gm.SSBossTimerLevel);
    }

    [Test]
    public void BuySSBossTimer_CappedAtSSMaxLevel()
    {
        _gm.SetSoulShardsForTest(100);
        for (int i = 0; i < GameManager.SSMaxLevel; i++)
            _gm.BuySSBossTimer();
        Assert.AreEqual(GameManager.SSMaxLevel, _gm.SSBossTimerLevel);
        Assert.IsFalse(_gm.BuySSBossTimer());
    }

    [Test]
    public void BuySSDoubleChest_ConsumesShardAndIncrementsLevel()
    {
        _gm.SetSoulShardsForTest(GameManager.SSUpgradeCost);
        Assert.IsTrue(_gm.BuySSDoubleChest());
        Assert.AreEqual(1, _gm.SSDoubleChestLevel);
    }

    [Test]
    public void BuySSDoubleChest_CappedAtSSMaxLevel()
    {
        _gm.SetSoulShardsForTest(100);
        for (int i = 0; i < GameManager.SSMaxLevel; i++)
            _gm.BuySSDoubleChest();
        Assert.IsFalse(_gm.BuySSDoubleChest());
        Assert.AreEqual(GameManager.SSMaxLevel, _gm.SSDoubleChestLevel);
    }

    [Test]
    public void BuySSRollback_ConsumesShardAndIncrementsLevel()
    {
        _gm.SetSoulShardsForTest(GameManager.SSUpgradeCost);
        Assert.IsTrue(_gm.BuySSRollback());
        Assert.AreEqual(1, _gm.SSRollbackLevel);
    }

    [Test]
    public void BuySSRollback_CappedAtSSMaxLevel()
    {
        _gm.SetSoulShardsForTest(100);
        for (int i = 0; i < GameManager.SSMaxLevel; i++)
            _gm.BuySSRollback();
        Assert.IsFalse(_gm.BuySSRollback());
        Assert.AreEqual(GameManager.SSMaxLevel, _gm.SSRollbackLevel);
    }

    [Test]
    public void SSBossTimer_ExtendsBossTimerOnSpawn()
    {
        _gm.SetSoulShardsForTest(GameManager.SSUpgradeCost);
        _gm.BuySSBossTimer();
        _gm.SetWaveForTest(7);
        _gm.SetNextBossWaveForTest(7);
        _gm.SpawnEnemyForTest(7);
        Assert.AreEqual(GameManager.BossTimeLimit + 15f, _gm.BossTimeRemaining, 0.001f);
    }

    [Test]
    public void SSBossTimer_TwoLevels_AddsThirtySeconds()
    {
        _gm.SetSoulShardsForTest(100);
        _gm.BuySSBossTimer();
        _gm.BuySSBossTimer();
        _gm.SetWaveForTest(7);
        _gm.SetNextBossWaveForTest(7);
        _gm.SpawnEnemyForTest(7);
        Assert.AreEqual(GameManager.BossTimeLimit + 30f, _gm.BossTimeRemaining, 0.001f);
    }

    [Test]
    public void SSRollback_ReducesBossTimeoutWaveRollback()
    {
        _gm.SetSoulShardsForTest(GameManager.SSUpgradeCost);
        _gm.BuySSRollback();
        _gm.SetWaveForTest(10);
        _gm.SetNextBossWaveForTest(10);

        _gm.TriggerBossTimeoutForTest();

        int expectedRollback = Math.Max(0, GameManager.BossWaveRollback - 1);
        Assert.AreEqual(Math.Max(1, 10 - expectedRollback), _gm.Wave);
    }

    [Test]
    public void SSRollback_AtMaxLevel_CanReduceRollbackToZero()
    {
        _gm.SetSoulShardsForTest(100);
        // Buy enough rollback levels to cover the full BossWaveRollback
        for (int i = 0; i < GameManager.SSMaxLevel; i++)
            _gm.BuySSRollback();

        _gm.SetWaveForTest(10);
        _gm.SetNextBossWaveForTest(10);
        _gm.TriggerBossTimeoutForTest();

        int effectiveRollback = Math.Max(0, GameManager.BossWaveRollback - GameManager.SSMaxLevel);
        Assert.AreEqual(Math.Max(1, 10 - effectiveRollback), _gm.Wave);
    }

    [Test]
    public void SoulShards_SaveLoad_PreservesValue()
    {
        _gm.SetSoulShardsForTest(7.0);
        _gm.UnlockSoulShardShopForTest();
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(7.0, _gm.SoulShards, 0.001);
        Assert.IsTrue(_gm.SoulShardShopUnlocked);
    }

    [Test]
    public void SoulShardShopLevels_SaveLoad_Preserved()
    {
        _gm.SetSoulShardsForTest(100);
        _gm.BuySSBossTimer();
        _gm.BuySSDoubleChest();
        _gm.BuySSRollback();
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(1, _gm.SSBossTimerLevel);
        Assert.AreEqual(1, _gm.SSDoubleChestLevel);
        Assert.AreEqual(1, _gm.SSRollbackLevel);
    }

    // ── Prestige Shop: Weapon Head Start ─────────────────────────────────────

    [Test]
    public void BuyPWeaponHeadStart_ConsumesPrestigePoint()
    {
        _gm.AwardPrestigePointsForTest(1);
        Assert.IsTrue(_gm.BuyPWeaponHeadStart());
        Assert.AreEqual(1, _gm.PWeaponHeadStartLevel);
        Assert.AreEqual(0, _gm.PrestigePoints);
    }

    [Test]
    public void BuyPWeaponHeadStart_Fails_WithoutPoints()
    {
        Assert.IsFalse(_gm.BuyPWeaponHeadStart());
        Assert.AreEqual(0, _gm.PWeaponHeadStartLevel);
    }

    [Test]
    public void PWeaponHeadStart_AppliesWeaponLevelAfterPrestige()
    {
        _gm.AwardPrestigePointsForTest(2);
        _gm.BuyPWeaponHeadStart();
        _gm.BuyPWeaponHeadStart();
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.AreEqual(2, _gm.WeaponLevel);
    }

    [Test]
    public void PWeaponHeadStart_WeaponLevelCappedAtMaxEquipLevel()
    {
        // Buy more PP than MaxEquipLevel
        int excess = GameManager.MaxEquipLevel + 2;
        _gm.AwardPrestigePointsForTest(excess);
        for (int i = 0; i < excess; i++)
            _gm.BuyPWeaponHeadStart();

        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();

        Assert.AreEqual(GameManager.MaxEquipLevel, _gm.WeaponLevel);
    }

    // ── Prestige Shop: Blood Tithe ────────────────────────────────────────────

    [Test]
    public void BuyPBloodTithe_ConsumesPrestigePoint()
    {
        _gm.AwardPrestigePointsForTest(1);
        Assert.IsTrue(_gm.BuyPBloodTithe());
        Assert.AreEqual(1, _gm.PBloodTitheLevel);
        Assert.AreEqual(0, _gm.PrestigePoints);
    }

    [Test]
    public void BuyPBloodTithe_Fails_WithoutPoints()
    {
        Assert.IsFalse(_gm.BuyPBloodTithe());
    }

    [Test]
    public void BloodTithePerSec_ScalesWithLevelAndPrestige()
    {
        _gm.AwardPrestigePointsForTest(2);
        _gm.BuyPBloodTithe();
        _gm.BuyPBloodTithe();
        double expected = 2 * 0.5 * _gm.PrestigeMultiplier;
        Assert.AreEqual(expected, _gm.BloodTithePerSec, 0.001);
    }

    [Test]
    public void BloodPerSec_IncludesBloodTitheWithNoRituals()
    {
        _gm.AwardPrestigePointsForTest(1);
        _gm.BuyPBloodTithe();
        // No rituals — BloodPerSec should equal BloodTithePerSec
        Assert.AreEqual(_gm.BloodTithePerSec, _gm.BloodPerSec, 0.001);
    }

    [Test]
    public void OfflineBlood_IncludesBloodTithe()
    {
        double seconds = 3600;
        int titheLevel = 2;
        double prestigeMult = 1.5;
        double offline = GameManager.CalculateOfflineBlood(0, 0, prestigeMult, seconds, titheLevel);
        double expected = titheLevel * 0.5 * prestigeMult * Math.Min(seconds, 8.0 * 3600);
        Assert.AreEqual(expected, offline, 0.001);
    }

    // ── Prestige Shop: Iron Wall ──────────────────────────────────────────────

    [Test]
    public void BuyPIronWall_ConsumesPrestigePoint()
    {
        _gm.AwardPrestigePointsForTest(1);
        Assert.IsTrue(_gm.BuyPIronWall());
        Assert.AreEqual(1, _gm.PIronWallLevel);
        Assert.AreEqual(0, _gm.PrestigePoints);
    }

    [Test]
    public void BuyPIronWall_Fails_WithoutPoints()
    {
        Assert.IsFalse(_gm.BuyPIronWall());
        Assert.AreEqual(0, _gm.PIronWallLevel);
    }

    [Test]
    public void IronWallDmgReduction_ConstantIsCorrect()
    {
        Assert.AreEqual(0.10f, GameManager.IronWallDmgReduction, 0.0001f);
    }

    // ── Wave Preview ─────────────────────────────────────────────────────────

    [Test]
    public void WavePreview_InactiveOnStart()
    {
        Assert.IsFalse(_gm.WavePreviewActive);
    }

    [Test]
    public void SkipWavePreview_DeactivatesPreviewAndSpawnsEnemy()
    {
        _gm.SetWaveForTest(3);
        _gm.SetNextBossWaveForTest(99);
        _gm.SkipWavePreviewForTest();

        Assert.IsFalse(_gm.WavePreviewActive);
        Assert.Greater(_gm.EnemyHP, 0f);
    }

    [Test]
    public void SkipWavePreview_OnBossWave_SpawnsBossStats()
    {
        _gm.SetWaveForTest(7);
        _gm.SetNextBossWaveForTest(7);
        _gm.SkipWavePreviewForTest();

        Assert.IsFalse(_gm.WavePreviewActive);
        Assert.Greater(_gm.EnemyHP, 0f);
        // Boss HP at wave 7 is much higher than normal — spot-check it's > normal wave 1 HP
        Assert.Greater(_gm.EnemyMaxHP, 100f);
    }

    // ── Prestige resets WavePreview ───────────────────────────────────────────

    [Test]
    public void Prestige_ResetsWavePreviewActive()
    {
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        Assert.IsFalse(_gm.WavePreviewActive);
    }
}
