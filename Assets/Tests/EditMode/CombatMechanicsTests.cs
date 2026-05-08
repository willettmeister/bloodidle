using NUnit.Framework;
using UnityEngine;

public class CombatMechanicsTests
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

    // 3 tanks (TotalAttack=15/sec) + wave 1 enemy spawned
    void BuyTanksAndSpawn(int n)
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * n);
        for (int i = 0; i < n; i++) _gm.BuyTank();
        _gm.SkipWavePreviewForTest();
    }

    // ── Boss Shield ───────────────────────────────────────────────────────────

    [Test]
    public void BossShield_AbsorbsDamage_EnemyHPUnchanged()
    {
        BuyTanksAndSpawn(3);
        _gm.SetEnemyHPForTest(100f);                    // shieldHP = 20
        _gm.SetBossAbilityForTest(BossAbility.Shield);
        float hpBefore = _gm.EnemyHP;
        _gm.TickCombatForTest(0.1f);                    // 15 * 0.1 = 1.5 < 20 → shield holds
        Assert.AreEqual(hpBefore, _gm.EnemyHP, 0.001f);
        Assert.IsTrue(_gm.BossShieldActive);
    }

    [Test]
    public void BossShield_BreaksWhenDepleted()
    {
        BuyTanksAndSpawn(3);                            // TotalAttack = 15/sec
        _gm.SetEnemyHPForTest(100f);                    // shieldHP = 20
        _gm.SetBossAbilityForTest(BossAbility.Shield);
        _gm.TickCombatForTest(2f);                      // 15 * 2 = 30 > 20 → breaks
        Assert.IsFalse(_gm.BossShieldActive);
    }

    [Test]
    public void BossShield_EnemyHPDropsOnlyAfterShieldBreaks()
    {
        BuyTanksAndSpawn(3);
        _gm.SetEnemyHPForTest(100f);                    // shieldHP = 20
        _gm.SetBossAbilityForTest(BossAbility.Shield);
        _gm.TickCombatForTest(2f);                      // breaks shield; EnemyHP if-else prevents same-tick HP damage
        Assert.IsFalse(_gm.BossShieldActive);
        float hpAfterBreak = _gm.EnemyHP;
        _gm.TickCombatForTest(0.1f);                    // 15 * 0.1 = 1.5 → now hits enemy
        Assert.Less(_gm.EnemyHP, hpAfterBreak);
    }

    // ── Boss Drain ────────────────────────────────────────────────────────────

    [Test]
    public void BossDrain_AddsBossDrainPerSec_ToIncomingDamage()
    {
        _gm.SetNextBossWaveForTest(1);
        _gm.SetWaveForTest(1);
        _gm.AwardBloodForTest(GameManager.SoldierCost * 3);
        _gm.BuyTank(); _gm.BuyTank(); _gm.BuyTank();
        _gm.SkipWavePreviewForTest();                   // boss wave 1: EnemyAttack = 6f (deterministic)
        float atk = _gm.EnemyAttack;
        _gm.SetEnemyHPForTest(10000f);
        _gm.SetSoldierHPForTest(GameManager.SoldierMaxHP);
        _gm.SetBossAbilityForTest(BossAbility.Drain);
        float before = _gm.SoldierHP;
        _gm.TickCombatForTest(1f);
        Assert.AreEqual(before - (atk + GameManager.BossDrainPerSec), _gm.SoldierHP, 0.001f);
    }

    // ── Flawless Reward ───────────────────────────────────────────────────────

    [Test]
    public void FlawlessReward_DoublesBlood_WhenTimerInRange()
    {
        BuyTanksAndSpawn(3);                            // Blood=0 after buying
        _gm.SetEnemyHPForTest(1f);
        _gm.SetFlawlessTimerForTest(5f);                // 0 < 5 ≤ FlawlessThreshold → flawless
        _gm.TickCombatForTest(1f);
        // wave=1, prestige=0, streak=0, talisman=0 → base=25, ×2 = 50
        Assert.AreEqual(50.0, _gm.Blood, 0.001);
    }

    [Test]
    public void FlawlessReward_NotDoubled_WhenTimerAboveThreshold()
    {
        BuyTanksAndSpawn(3);
        _gm.SetEnemyHPForTest(1f);
        _gm.SetFlawlessTimerForTest(GameManager.FlawlessThreshold + 1f);
        _gm.TickCombatForTest(1f);
        Assert.AreEqual(25.0, _gm.Blood, 0.001);
    }

    [Test]
    public void FlawlessReward_NotDoubled_WhenTimerIsZero()
    {
        BuyTanksAndSpawn(3);
        _gm.SetEnemyHPForTest(1f);
        _gm.SetFlawlessTimerForTest(0f);                // timer == 0 → condition (_timer > 0) fails
        _gm.TickCombatForTest(1f);
        Assert.AreEqual(25.0, _gm.Blood, 0.001);
    }

    // ── Surge ─────────────────────────────────────────────────────────────────

    [Test]
    public void SurgeActive_DoublesEffectiveDamageToEnemy()
    {
        BuyTanksAndSpawn(3);
        _gm.SetEnemyHPForTest(10000f);
        _gm.SetEnemyModifierForTest(EnemyModifier.None); // prevent Armored halving eff
        float totalAtk = _gm.TotalAttack;               // 3 * SoldierAttack = 15 (no equip)
        float before   = _gm.EnemyHP;
        _gm.SetSurgeActiveForTest(true);
        _gm.TickCombatForTest(1f);
        Assert.AreEqual(before - totalAtk * GameManager.SurgeMultiplier * 1f, _gm.EnemyHP, 0.001f);
    }

    // ── Iron Wall ─────────────────────────────────────────────────────────────

    [Test]
    public void IronWall_ReducesIncomingDamage_ByLevelTimesTenPct()
    {
        BuyTanksAndSpawn(3);
        _gm.SetEnemyHPForTest(10000f);
        float atk = _gm.EnemyAttack;                    // whatever spawn set (Enraged already baked in)
        _gm.SetSoldierHPForTest(GameManager.SoldierMaxHP);
        _gm.SetPIronWallLevelForTest(2);
        float before = _gm.SoldierHP;
        _gm.TickCombatForTest(1f);
        float expected = before - atk * (1f - 2 * GameManager.IronWallDmgReduction);
        Assert.AreEqual(expected, _gm.SoldierHP, 0.001f);
    }

    // ── Wave Streak ───────────────────────────────────────────────────────────

    [Test]
    public void StreakMultiplier_ScalesKillReward()
    {
        BuyTanksAndSpawn(3);                            // Blood=0 after buying
        _gm.SetEnemyHPForTest(1f);
        _gm.SetWaveStreakForTest(5);                    // StreakMultiplier = min(1 + 5*0.1, 3) = 1.5
        _gm.SetFlawlessTimerForTest(0f);                // not flawless
        _gm.TickCombatForTest(1f);
        // reward = floor(25 * 1.5) = floor(37.5) = 37
        Assert.AreEqual(37.0, _gm.Blood, 0.001);
    }

    [Test]
    public void SoldierDeath_ResetsWaveStreak()
    {
        BuyTanksAndSpawn(3);
        _gm.SetEnemyHPForTest(10000f);                  // enemy won't die during test
        _gm.SetWaveStreakForTest(5);
        _gm.SetSoldierHPForTest(0.1f);                  // dies on first tick (EnemyAttack >> 0.1)
        _gm.TickCombatForTest(1f);
        Assert.AreEqual(0, _gm.WaveStreak);
    }

    // ── Milestone Chest ───────────────────────────────────────────────────────

    [Test]
    public void MilestoneChest_FiresEventAndGrantsReward()
    {
        BuyTanksAndSpawn(3);
        bool fired = false;
        _gm.OnMilestoneChest += _ => fired = true;
        double bloodBefore   = _gm.Blood;
        double woodBefore    = _gm.Wood;
        int    soldierBefore = _gm.SoldierCount;
        _gm.TriggerMilestoneChestForTest(5);
        Assert.IsTrue(fired, "OnMilestoneChest event must fire");
        bool rewarded = _gm.Blood > bloodBefore
                     || _gm.Wood  > woodBefore
                     || _gm.SoldierCount > soldierBefore;
        Assert.IsTrue(rewarded, "Chest must grant blood, wood, or a free soldier");
    }

    // ── Offline Earnings ──────────────────────────────────────────────────────

    [Test]
    public void ClearOfflineEarnings_ZeroesBothFields()
    {
        _gm.SetOfflineEarningsForTest(123.0, 456.0);
        Assert.AreNotEqual(0.0, _gm.OfflineBloodEarned);
        Assert.AreNotEqual(0.0, _gm.OfflineWoodEarned);
        _gm.ClearOfflineEarningsForTest();
        Assert.AreEqual(0.0, _gm.OfflineBloodEarned, 0.001);
        Assert.AreEqual(0.0, _gm.OfflineWoodEarned,  0.001);
    }
}
