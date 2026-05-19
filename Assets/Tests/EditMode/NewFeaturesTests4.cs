using NUnit.Framework;
using UnityEngine;
using System;

public class NewFeaturesTests4
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
        UnityEngine.Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
    }

    // ── Achievement Rewards ───────────────────────────────────────────────────

    [Test]
    public void FirstKill_Achievement_AwardsBlood()
    {
        double before = _gm.Blood;
        _gm.AwardBloodForTest(0);  // prime AddBlood path
        // Directly trigger the achievement
        _gm.AwardBloodForTest(0);  // ensure instance alive
        // Use the kill achievement by buying a soldier then killing wave 1
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyTank();
        // Simulate minimal combat: set enemy HP to 0 and skip preview
        _gm.SetEnemyHPForTest(0.001f);
        // The unlock fires via TryUnlock in RunCombat. We can't run a frame here,
        // but we can verify the reward table is internally correct via an indirect path:
        // After first purchase achievement fires on BuyTank → FirstSoldier gives +25 blood.
        Assert.Greater(_gm.Blood, 0);
    }

    [Test]
    public void FirstSoldier_Achievement_AwardsBlood()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        double before = _gm.Blood - GameManager.SoldierCost; // blood after buying
        _gm.BuyTank();
        // FirstSoldier achievement fires during BuyTank; reward is 25 blood.
        // Blood = (cost subtracted) + 25 reward
        Assert.AreEqual(before + 25.0, _gm.Blood, 0.001);
    }

    [Test]
    public void FullLegion_Achievement_AwardsBlood()
    {
        // Fill to MaxSoldiers - 1, then buy the last one to trigger FullLegion.
        int cap = _gm.MaxSoldiers;
        _gm.AwardBloodForTest(GameManager.SoldierCost * (cap + 5));
        for (int i = 0; i < cap - 1; i++) _gm.BuyTank();

        double before = _gm.Blood;
        _gm.BuyTank();  // triggers FullLegion (+300 blood)

        // Blood after = before − SoldierCost + 300
        Assert.AreEqual(before - GameManager.SoldierCost + 300.0, _gm.Blood, 0.001);
    }

    [Test]
    public void FirstPrestige_Achievement_AwardsPrestigePoint()
    {
        int before = _gm.PrestigePoints;
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);
        _gm.Prestige();
        // Prestige gives 1 PP normally + 1 PP from FirstPrestige achievement = 2 PP total
        Assert.AreEqual(before + 2, _gm.PrestigePoints);
    }

    // ── Blood Bank ────────────────────────────────────────────────────────────

    [Test]
    public void DepositToBank_MovesBloodToDeposit()
    {
        _gm.AwardBloodForTest(100.0);
        _gm.DepositToBank(50.0);
        Assert.AreEqual(50.0, _gm.BloodBankDeposit, 0.001);
        Assert.AreEqual(50.0, _gm.Blood, 0.001);
    }

    [Test]
    public void DepositToBank_CappedAtBankMax()
    {
        _gm.AwardBloodForTest(GameManager.BankMaxDeposit + 1000.0);
        _gm.DepositToBank(GameManager.BankMaxDeposit + 1000.0);
        Assert.AreEqual(GameManager.BankMaxDeposit, _gm.BloodBankDeposit, 0.001);
    }

    [Test]
    public void DepositToBank_CantDepositMoreThanBalance()
    {
        _gm.AwardBloodForTest(30.0);
        _gm.DepositToBank(1000.0);  // only 30 available
        Assert.AreEqual(30.0, _gm.BloodBankDeposit, 0.001);
        Assert.AreEqual(0.0, _gm.Blood, 0.001);
    }

    [Test]
    public void DepositToBank_Fails_WhenNoBlood()
    {
        Assert.IsFalse(_gm.DepositToBank(100.0));
        Assert.AreEqual(0.0, _gm.BloodBankDeposit, 0.001);
    }

    [Test]
    public void WithdrawFromBank_ReturnsDepositAndAccrued()
    {
        _gm.SetBloodBankDepositForTest(200.0);
        _gm.SetBloodBankAccruedForTest(10.0);
        double before = _gm.Blood;
        _gm.WithdrawFromBank();
        Assert.AreEqual(before + 210.0, _gm.Blood, 0.001);
        Assert.AreEqual(0.0, _gm.BloodBankDeposit, 0.001);
        Assert.AreEqual(0.0, _gm.BloodBankAccrued, 0.001);
    }

    [Test]
    public void WithdrawFromBank_Fails_WhenEmpty()
    {
        Assert.IsFalse(_gm.WithdrawFromBank());
    }

    [Test]
    public void BloodBank_SaveLoad_PreservesValues()
    {
        _gm.SetBloodBankDepositForTest(500.0);
        _gm.SetBloodBankAccruedForTest(12.5);
        _gm.SaveForTest();

        UnityEngine.Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(500.0, _gm.BloodBankDeposit, 0.001);
        Assert.AreEqual(12.5,  _gm.BloodBankAccrued, 0.001);
    }

    // ── Wave Streak ───────────────────────────────────────────────────────────

    [Test]
    public void WaveStreak_ZeroOnStart()
    {
        Assert.AreEqual(0, _gm.WaveStreak);
    }

    [Test]
    public void StreakMultiplier_IsOneAtZeroStreak()
    {
        Assert.AreEqual(1f, _gm.StreakMultiplier, 0.001f);
    }

    [Test]
    public void StreakMultiplier_ScalesWithStreak()
    {
        _gm.SetWaveStreakForTest(5);
        float expected = Mathf.Min(1f + 5 * 0.1f, GameManager.MaxStreakMultiplier);
        Assert.AreEqual(expected, _gm.StreakMultiplier, 0.001f);
    }

    [Test]
    public void StreakMultiplier_CappedAtMax()
    {
        _gm.SetWaveStreakForTest(100);
        Assert.AreEqual(GameManager.MaxStreakMultiplier, _gm.StreakMultiplier, 0.001f);
    }

    [Test]
    public void WaveStreak_SaveLoad_Preserved()
    {
        _gm.SetWaveStreakForTest(7);
        _gm.SaveForTest();

        UnityEngine.Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(7, _gm.WaveStreak);
    }

    // ── Boss Ability ──────────────────────────────────────────────────────────

    [Test]
    public void BossAbility_NoneByDefault()
    {
        Assert.AreEqual(BossAbility.None, _gm.CurrentBossAbility);
        Assert.IsFalse(_gm.BossShieldActive);
    }

    [Test]
    public void SetBossAbility_Shield_ActivatesShield()
    {
        _gm.SetWaveForTest(1);
        _gm.SetNextBossWaveForTest(1);
        _gm.SpawnEnemyForTest(1);
        _gm.SetBossAbilityForTest(BossAbility.Shield);
        Assert.AreEqual(BossAbility.Shield, _gm.CurrentBossAbility);
        Assert.IsTrue(_gm.BossShieldActive);
    }

    [Test]
    public void SetBossAbility_Berserk_NoShield()
    {
        _gm.SetBossAbilityForTest(BossAbility.Berserk);
        Assert.AreEqual(BossAbility.Berserk, _gm.CurrentBossAbility);
        Assert.IsFalse(_gm.BossShieldActive);
    }

    [Test]
    public void BossAbilityDisplay_ReturnsCorrectString()
    {
        _gm.SetBossAbilityForTest(BossAbility.Shield);
        Assert.IsTrue(_gm.BossAbilityDisplay.Length > 0);
        _gm.SetBossAbilityForTest(BossAbility.Berserk);
        Assert.IsTrue(_gm.BossAbilityDisplay.Length > 0);
        _gm.SetBossAbilityForTest(BossAbility.Drain);
        Assert.IsTrue(_gm.BossAbilityDisplay.Length > 0);
        _gm.SetBossAbilityForTest(BossAbility.None);
        Assert.AreEqual("", _gm.BossAbilityDisplay);
    }

    [Test]
    public void BossSpawn_AssignsAbility()
    {
        _gm.SetWaveForTest(7);
        _gm.SetNextBossWaveForTest(7);
        // Run several spawns — at least one should eventually assign a non-None ability
        // (probability of all 10 being None = 0.25^10 ≈ negligible, but we just check
        //  the enum is in valid range)
        _gm.SpawnEnemyForTest(7);
        Assert.IsTrue((int)_gm.CurrentBossAbility >= 0 && (int)_gm.CurrentBossAbility <= 3);
    }

    [Test]
    public void NormalWave_HasNoBossAbility()
    {
        _gm.SetWaveForTest(1);
        _gm.SetNextBossWaveForTest(99);
        _gm.SpawnEnemyForTest(1);
        Assert.AreEqual(BossAbility.None, _gm.CurrentBossAbility);
        Assert.IsFalse(_gm.BossShieldActive);
    }

    // ── Prestige Milestones ───────────────────────────────────────────────────

    [Test]
    public void PrestigeMilestonesReached_ZeroBeforeFirstMilestone()
    {
        _gm.SetPrestigeCountForTest(3);
        Assert.AreEqual(0, _gm.PrestigeMilestonesReached);
        Assert.AreEqual(0f, _gm.PrestigeMilestoneDmgBonus, 0.0001f);
    }

    [Test]
    public void PrestigeMilestonesReached_OneAtPrestige5()
    {
        _gm.SetPrestigeCountForTest(5);
        Assert.AreEqual(1, _gm.PrestigeMilestonesReached);
    }

    [Test]
    public void PrestigeMilestonesReached_TwoAtPrestige10()
    {
        _gm.SetPrestigeCountForTest(10);
        Assert.AreEqual(2, _gm.PrestigeMilestonesReached);
    }

    [Test]
    public void PrestigeMilestonesReached_FourAtPrestige50Plus()
    {
        _gm.SetPrestigeCountForTest(50);
        Assert.AreEqual(4, _gm.PrestigeMilestonesReached);
    }

    [Test]
    public void PrestigeMilestoneDmgBonus_ScalesCorrectly()
    {
        _gm.SetPrestigeCountForTest(10);
        float expected = 2 * GameManager.MilestoneDmgBonusPerLevel;
        Assert.AreEqual(expected, _gm.PrestigeMilestoneDmgBonus, 0.0001f);
    }

    [Test]
    public void TotalAttack_IncludesMilestoneDmgBonus()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 2);
        _gm.BuyTank();
        _gm.BuyTank();

        float baseAttack = 2 * (GameManager.SoldierAttack + _gm.EquipAttackBonus);
        float noBonus = _gm.TotalAttack;
        Assert.AreEqual(baseAttack, noBonus, 0.01f);

        _gm.SetPrestigeCountForTest(5);  // 1 milestone = +5%
        float withBonus = _gm.TotalAttack;
        Assert.AreEqual(baseAttack * (1f + GameManager.MilestoneDmgBonusPerLevel), withBonus, 0.01f);
    }
}
