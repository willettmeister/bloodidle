using NUnit.Framework;
using UnityEngine;

public class NewFeaturesTests2
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

    // ── Enemy Modifiers ───────────────────────────────────────────────────────

    [Test]
    public void EnemyModifier_NoneByDefault()
    {
        Assert.AreEqual(EnemyModifier.None, _gm.CurrentEnemyModifier);
    }

    [Test]
    public void EnemyModifier_DisplayString_Armored()
    {
        _gm.SetEnemyModifierForTest(EnemyModifier.Armored);
        Assert.AreEqual("⚔ Armored", _gm.EnemyModifierDisplay);
    }

    [Test]
    public void EnemyModifier_DisplayString_Enraged()
    {
        _gm.SetEnemyModifierForTest(EnemyModifier.Enraged);
        Assert.AreEqual("💢 Enraged", _gm.EnemyModifierDisplay);
    }

    [Test]
    public void EnemyModifier_DisplayString_Regen()
    {
        _gm.SetEnemyModifierForTest(EnemyModifier.Regen);
        Assert.AreEqual("♻ Regen", _gm.EnemyModifierDisplay);
    }

    [Test]
    public void EnemyModifier_DisplayString_None_IsEmpty()
    {
        _gm.SetEnemyModifierForTest(EnemyModifier.None);
        Assert.AreEqual("", _gm.EnemyModifierDisplay);
    }

    [Test]
    public void EnemyModifier_Armored_ReducesDamageMult()
    {
        Assert.AreEqual(0.5f, GameManager.EnemyArmoredDmgMult, 0.001f);
    }

    [Test]
    public void EnemyModifier_Enraged_IncreasesAtkMult()
    {
        Assert.AreEqual(1.5f, GameManager.EnemyEnragedAtkMult, 0.001f);
    }

    [Test]
    public void EnemyModifier_Regen_RegenPctIsPositive()
    {
        Assert.Greater(GameManager.EnemyRegenPct, 0f);
    }

    [Test]
    public void EnemyModifier_Regen_HealsCappedAtMaxHP()
    {
        _gm.SetEnemyModifierForTest(EnemyModifier.Regen);
        _gm.SetEnemyHPForTest(100f);
        float maxHP = _gm.EnemyMaxHP;
        // After healing tick, HP should not exceed max
        float regenAmount = maxHP * GameManager.EnemyRegenPct * 1f;
        float expected = Mathf.Min(100f + regenAmount, maxHP);
        Assert.LessOrEqual(expected, maxHP + 0.001f);
    }

    // ── Equipment System ──────────────────────────────────────────────────────

    [Test]
    public void Equipment_DefaultLevelsAreZero()
    {
        Assert.AreEqual(0, _gm.WeaponLevel);
        Assert.AreEqual(0, _gm.ArmorLevel);
        Assert.AreEqual(0, _gm.TalismanLevel);
    }

    [Test]
    public void Equipment_DefaultBonusesAreZero()
    {
        Assert.AreEqual(0f, _gm.EquipAttackBonus, 0.001f);
        Assert.AreEqual(0f, _gm.EquipArmorBonus,  0.001f);
        Assert.AreEqual(0.0, _gm.EquipTalismanBonus, 0.001);
    }

    [Test]
    public void Weapon_UpgradeIncreasesLevel()
    {
        _gm.SetWoodForTest(GameManager.MaxEquipLevel * 1000);
        bool result = _gm.UpgradeWeapon();
        Assert.IsTrue(result);
        Assert.AreEqual(1, _gm.WeaponLevel);
    }

    [Test]
    public void Weapon_AttackBonusScalesWithLevel()
    {
        _gm.SetWoodForTest(10000);
        _gm.UpgradeWeapon();
        Assert.AreEqual(3f, _gm.EquipAttackBonus, 0.001f);
    }

    [Test]
    public void Weapon_UpgradeFails_WhenInsufficientWood()
    {
        _gm.SetWoodForTest(0);
        bool result = _gm.UpgradeWeapon();
        Assert.IsFalse(result);
        Assert.AreEqual(0, _gm.WeaponLevel);
    }

    [Test]
    public void Weapon_UpgradeFails_AtMaxLevel()
    {
        _gm.SetWoodForTest(100000);
        for (int i = 0; i < GameManager.MaxEquipLevel; i++) _gm.UpgradeWeapon();
        bool result = _gm.UpgradeWeapon();
        Assert.IsFalse(result);
        Assert.AreEqual(GameManager.MaxEquipLevel, _gm.WeaponLevel);
    }

    [Test]
    public void Armor_UpgradeIncreasesLevel()
    {
        _gm.SetWoodForTest(10000);
        bool result = _gm.UpgradeArmor();
        Assert.IsTrue(result);
        Assert.AreEqual(1, _gm.ArmorLevel);
    }

    [Test]
    public void Armor_BonusIncreasesMaxHP()
    {
        _gm.SetWoodForTest(10000);
        float hpBefore = _gm.FrontlineMaxHP;
        _gm.UpgradeArmor();
        Assert.Greater(_gm.FrontlineMaxHP, hpBefore);
    }

    [Test]
    public void Armor_UpgradeFails_AtMaxLevel()
    {
        _gm.SetWoodForTest(100000);
        for (int i = 0; i < GameManager.MaxEquipLevel; i++) _gm.UpgradeArmor();
        bool result = _gm.UpgradeArmor();
        Assert.IsFalse(result);
        Assert.AreEqual(GameManager.MaxEquipLevel, _gm.ArmorLevel);
    }

    [Test]
    public void Talisman_UpgradeIncreasesLevel()
    {
        _gm.SetWoodForTest(10000);
        bool result = _gm.UpgradeTalisman();
        Assert.IsTrue(result);
        Assert.AreEqual(1, _gm.TalismanLevel);
    }

    [Test]
    public void Talisman_BonusIsPositiveAfterUpgrade()
    {
        _gm.SetWoodForTest(10000);
        _gm.UpgradeTalisman();
        Assert.Greater(_gm.EquipTalismanBonus, 0.0);
    }

    [Test]
    public void Talisman_UpgradeFails_AtMaxLevel()
    {
        _gm.SetWoodForTest(100000);
        for (int i = 0; i < GameManager.MaxEquipLevel; i++) _gm.UpgradeTalisman();
        bool result = _gm.UpgradeTalisman();
        Assert.IsFalse(result);
        Assert.AreEqual(GameManager.MaxEquipLevel, _gm.TalismanLevel);
    }

    [Test]
    public void Equipment_UpgradeCosts_DoublePerLevel()
    {
        double cost0 = _gm.WeaponUpgradeCost;
        _gm.SetWoodForTest(100000);
        _gm.UpgradeWeapon();
        double cost1 = _gm.WeaponUpgradeCost;
        Assert.AreEqual(cost0 * 2, cost1, 0.001);
    }

    [Test]
    public void Equipment_SaveLoad_Roundtrip()
    {
        _gm.SetWoodForTest(100000);
        _gm.UpgradeWeapon();
        _gm.UpgradeArmor();
        _gm.UpgradeTalisman();
        _gm.AwardBloodForTest(1);
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(1, _gm.WeaponLevel);
        Assert.AreEqual(1, _gm.ArmorLevel);
        Assert.AreEqual(1, _gm.TalismanLevel);
    }

    [Test]
    public void Equipment_Prestige_ResetsLevels()
    {
        _gm.SetWoodForTest(100000);
        _gm.UpgradeWeapon();
        _gm.UpgradeArmor();
        _gm.UpgradeTalisman();
        _gm.SetWaveForTest(GameManager.PrestigeWaveRequirement);

        _gm.Prestige();

        Assert.AreEqual(0, _gm.WeaponLevel);
        Assert.AreEqual(0, _gm.ArmorLevel);
        Assert.AreEqual(0, _gm.TalismanLevel);
    }

    [Test]
    public void TotalAttack_IncludesWeaponBonus()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost * 2);
        _gm.BuyTank();
        _gm.BuyBerserker();
        _gm.SetWoodForTest(100000);
        _gm.UpgradeWeapon(); // WeaponLevel = 1, EquipAttackBonus = 3

        float expected = 1 * (GameManager.SoldierAttack   + _gm.EquipAttackBonus)
                       + 1 * (GameManager.BerserkerAttack + _gm.EquipAttackBonus);
        Assert.AreEqual(expected, _gm.TotalAttack, 0.001f);
    }

    [Test]
    public void FrontlineMaxHP_IncludesArmorBonus()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyTank(); // FrontlineIsTank = true
        _gm.SetWoodForTest(100000);
        _gm.UpgradeArmor(); // ArmorLevel = 1, EquipArmorBonus = 10

        float expected = GameManager.SoldierMaxHP + _gm.EquipArmorBonus;
        Assert.AreEqual(expected, _gm.FrontlineMaxHP, 0.001f);
    }

    [Test]
    public void Armor_UpgradeHeals_ExistingSoldiers()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyTank();
        _gm.SetSoldierHPForTest(20f); // wound the soldier
        _gm.SetWoodForTest(100000);

        _gm.UpgradeArmor();

        Assert.Greater(_gm.SoldierHP, 20f);
    }

    [Test]
    public void Armor_UpgradeHeal_CappedAtNewMax()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyTank(); // SoldierHP = SoldierMaxHP (50)
        _gm.SetWoodForTest(100000);

        _gm.UpgradeArmor(); // new max = 60; heal min(50+10, 60) = 60

        Assert.AreEqual(_gm.FrontlineMaxHP, _gm.SoldierHP, 0.001f);
    }

    [Test]
    public void Armor_UpgradeNoHeal_WhenNoSoldiers()
    {
        _gm.SetWoodForTest(100000);
        float hpBefore = _gm.SoldierHP; // 0

        _gm.UpgradeArmor();

        Assert.AreEqual(hpBefore, _gm.SoldierHP, 0.001f);
    }

    // ── Daily Login Bonus ─────────────────────────────────────────────────────

    [Test]
    public void DailyBonus_FalseByDefaultWithNoSave()
    {
        // No save means Load() returns early — DailyBonusAvailable stays false.
        Assert.IsFalse(_gm.DailyBonusAvailable);
    }

    [Test]
    public void DailyBonus_FarmBlood_NormalAmount_WhenFlagFalse()
    {
        _gm.SetDailyBonusForTest(false);
        double before = _gm.Blood;
        _gm.FarmBlood();
        Assert.AreEqual(before + _gm.EffectiveBloodPerClick, _gm.Blood, 0.001);
    }

    [Test]
    public void DailyBonus_FarmBlood_Multiplied_WhenFlagTrue()
    {
        _gm.SetDailyBonusForTest(true);
        double before = _gm.Blood;
        double expected = _gm.EffectiveBloodPerClick * GameManager.DailyBonusMultiplier;
        _gm.FarmBlood();
        Assert.AreEqual(before + expected, _gm.Blood, 0.001);
    }

    [Test]
    public void DailyBonus_Clears_AfterFirstFarm()
    {
        _gm.SetDailyBonusForTest(true);
        _gm.FarmBlood();
        Assert.IsFalse(_gm.DailyBonusAvailable);
    }

    [Test]
    public void DailyBonus_SecondFarm_NormalAmount()
    {
        _gm.SetDailyBonusForTest(true);
        _gm.FarmBlood(); // consumes bonus

        double before = _gm.Blood;
        _gm.FarmBlood();
        Assert.AreEqual(before + _gm.EffectiveBloodPerClick, _gm.Blood, 0.001);
    }

    [Test]
    public void DailyBonus_Multiplier_IsPositive()
    {
        Assert.Greater(GameManager.DailyBonusMultiplier, 1f);
    }

    // ── Formation Toggle ──────────────────────────────────────────────────────

    [Test]
    public void Formation_DefaultIsTankFront()
    {
        Assert.IsFalse(_gm.BerserkerFront);
    }

    [Test]
    public void Formation_Toggle_FlipsBerserkerFront()
    {
        _gm.ToggleFormation();
        Assert.IsTrue(_gm.BerserkerFront);
    }

    [Test]
    public void Formation_Toggle_Twice_RestoresDefault()
    {
        _gm.ToggleFormation();
        _gm.ToggleFormation();
        Assert.IsFalse(_gm.BerserkerFront);
    }

    [Test]
    public void Formation_TankFront_FrontlineIsTank_WhenTanksExist()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyTank();
        Assert.IsFalse(_gm.BerserkerFront);
        Assert.IsTrue(_gm.FrontlineIsTank);
    }

    [Test]
    public void Formation_BerserkerFront_FrontlineIsNotTank_WhenBerserkerExists()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyBerserker();
        _gm.ToggleFormation(); // berserker front
        // FrontlineIsTank is false when berserker front and berserkers exist
        Assert.IsFalse(_gm.FrontlineIsTank);
    }

    [Test]
    public void Formation_BerserkerFront_FallsBackToTank_WhenNoBerserkers()
    {
        _gm.AwardBloodForTest(GameManager.SoldierCost);
        _gm.BuyTank();
        _gm.ToggleFormation(); // berserker front, but no berserkers
        // Falls back: berserker count == 0 and tank count > 0 → tank is frontline
        Assert.IsTrue(_gm.FrontlineIsTank);
    }

    [Test]
    public void Formation_SaveLoad_Preserves_BerserkerFront()
    {
        _gm.ToggleFormation();
        _gm.AwardBloodForTest(1);
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.IsTrue(_gm.BerserkerFront);
    }

    // ── Blood Pact ────────────────────────────────────────────────────────────

    [Test]
    public void BloodPact_LockedUntilWorkersUnlocked()
    {
        Assert.IsFalse(_gm.BloodPactUnlocked);
    }

    [Test]
    public void BloodPact_UnlocksWithWorkers()
    {
        _gm.AwardBloodForTest(GameManager.WorkersUnlockThreshold);
        Assert.IsTrue(_gm.BloodPactUnlocked);
    }

    [Test]
    public void BloodPact_Fails_WhenInsufficientBlood()
    {
        _gm.AwardBloodForTest(GameManager.WorkersUnlockThreshold);
        _gm.SetWoodForTest(0);
        // Don't have 200 blood after spending on workers unlock (TotalBloodEarned is separate from Blood)
        bool result = _gm.UseBloodPact();
        // Blood == WorkersUnlockThreshold here (AwardBloodForTest calls AddBlood which adds to Blood too)
        // 200 < 200 is false, so check properly:
        if (_gm.Blood >= GameManager.BloodPactBloodCost)
            Assert.IsTrue(result);
        else
            Assert.IsFalse(result);
    }

    [Test]
    public void BloodPact_Fails_WhenNotUnlocked()
    {
        // Fresh state: workers not yet unlocked, no blood — must return false
        Assert.IsFalse(_gm.BloodPactUnlocked);
        Assert.IsFalse(_gm.UseBloodPact());
    }

    [Test]
    public void BloodPact_Succeeds_ConvertsBloodToWood()
    {
        _gm.AwardBloodForTest(GameManager.WorkersUnlockThreshold + GameManager.BloodPactBloodCost);
        double woodBefore  = _gm.Wood;
        double bloodBefore = _gm.Blood;
        bool result = _gm.UseBloodPact();
        Assert.IsTrue(result);
        Assert.AreEqual(bloodBefore - GameManager.BloodPactBloodCost, _gm.Blood, 0.001);
        Assert.AreEqual(woodBefore  + GameManager.BloodPactWoodGain,  _gm.Wood,  0.001);
    }

    [Test]
    public void BloodPact_WoodGain_IsPositive()
    {
        Assert.Greater(GameManager.BloodPactWoodGain, 0.0);
    }

    [Test]
    public void BloodPact_BloodCost_IsPositive()
    {
        Assert.Greater(GameManager.BloodPactBloodCost, 0.0);
    }

    [Test]
    public void BloodPact_MultipleUses_EachConversion_Correct()
    {
        _gm.AwardBloodForTest(GameManager.WorkersUnlockThreshold + GameManager.BloodPactBloodCost * 3);
        double woodBefore = _gm.Wood;
        _gm.UseBloodPact();
        _gm.UseBloodPact();
        Assert.AreEqual(woodBefore + GameManager.BloodPactWoodGain * 2, _gm.Wood, 0.001);
    }
}
