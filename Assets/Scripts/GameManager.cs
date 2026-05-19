using UnityEngine;
using System;
using System.Globalization;

// ── Achievement bitmask ───────────────────────────────────────────────────────
[Flags]
public enum AchievementFlags
{
    None          = 0,
    FirstKill     = 1 << 0,
    Wave10        = 1 << 1,
    Wave25        = 1 << 2,
    Blood1K       = 1 << 3,
    Blood10K      = 1 << 4,
    FirstSoldier  = 1 << 5,
    FullLegion    = 1 << 6,
    FirstRitual   = 1 << 7,
    FirstPrestige = 1 << 8,
}

public enum EnemyModifier { None, Armored, Enraged, Regen, Cursed }
public enum BossAbility    { None, Shield, Berserk, Drain }

[Flags]
public enum TalentFlags
{
    None         = 0,
    BloodFrenzy  = 1 << 0,  // +25% kill blood rewards
    Undying      = 1 << 1,  // frontline revives once per wave at 1 HP
    ShardHunter  = 1 << 2,  // bosses drop 2 soul shards instead of 1
    IronSkin     = 1 << 3,  // +15 flat max HP to frontline soldier
    BloodRush    = 1 << 4,  // boss kill immediately activates Blood Surge
    Glutton      = 1 << 5,  // Blood Rituals produce 25% more blood/s
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // --- Blood ---
    public double Blood { get; private set; }
    public double TotalBloodEarned { get; private set; }
    public const double BloodPerClick = 1.0;
    public double EffectiveBloodPerClick => (BloodPerClick + PClickBonusLevel * 0.5) * PrestigeMultiplier;

    // --- Wood & Workers ---
    public double Wood { get; private set; }
    public int WorkerCount { get; private set; }
    public double WorkerEfficiencyMult => 1.0 + Math.Floor(WorkerCount / 5.0) * 0.1;
    public const double KillIncomeRate  = 0.01;
    public double KillIncomePerSec      => TotalEnemiesKilled * KillIncomeRate;
    public bool   IsBloodyWave          => Wave > 0 && Wave % 10 == 0 && !IsBossWave;
    public const double BloodMoonMult   = 2.0;
    public double WoodPerSecond        => WorkerCount * WorkerWoodPerSec * WorkerEfficiencyMult;
    public const double WorkerCost = 50.0;
    public const double WorkerWoodPerSec = 0.5;

    // --- Soldiers (two classes) ---
    public int TankCount       { get; private set; }
    public int BerserkerCount  { get; private set; }
    public int PaladinCount    { get; private set; }
    public int SoldierCount    => TankCount + BerserkerCount + PaladinCount;
    public float SoldierHP     { get; private set; }

    public bool  BerserkerFront      { get; private set; }
    public bool  FrontlineIsTank     => BerserkerFront
        ? (BerserkerCount == 0 && TankCount > 0)
        : (TankCount > 0);
    public bool  FrontlineIsBerserker => BerserkerFront
        ? (BerserkerCount > 0)
        : (TankCount == 0 && BerserkerCount > 0);
    public bool  FrontlineIsPaladin  => TankCount == 0 && BerserkerCount == 0 && PaladinCount > 0;
    public float FrontlineMaxHP      =>
        Mathf.Max(10f,
            (FrontlineIsTank ? SoldierMaxHP : FrontlineIsBerserker ? BerserkerMaxHP : PaladinMaxHP)
            + EquipArmorBonus
            + (HasTalent(TalentFlags.IronSkin) ? TalentIronSkinHP : 0f)
            - CorruptionLevel * CorruptionHPPenalty);
    public float TotalAttack         => (TankCount     * (SoldierAttack   + EquipAttackBonus)
                                       + BerserkerCount * (BerserkerAttack + EquipAttackBonus)
                                       + PaladinCount   * (PaladinAttack   + EquipAttackBonus))
                                       * (1f + PrestigeMilestoneDmgBonus) * MoraleBonusMult;
    public bool  IsAllTank       => TankCount > 0 && BerserkerCount == 0 && PaladinCount == 0;
    public bool  IsAllBerserker  => BerserkerCount > 0 && TankCount == 0 && PaladinCount == 0;
    public bool  IsAllPaladin    => PaladinCount > 0 && TankCount == 0 && BerserkerCount == 0;
    public bool  IsMixedArmy     => SoldierCount > 0 && !IsAllTank && !IsAllBerserker && !IsAllPaladin;

    public int MaxSoldiers { get; private set; } = 10;
    public const float  SoldierMaxHP        = 50f;
    public const double SoldierCost         = 10.0;
    public const float  SoldierAttack       = 5f;
    public const float  BerserkerMaxHP      = 25f;
    public const float  BerserkerAttack     = 12f;
    public const float  PaladinMaxHP        = 20f;
    public const float  PaladinAttack       = 3f;
    public const float  PaladinHealRate     = 1f;
    public const float  PaladinHolyBonus   = 1.25f;
    public const float  TankRegenRate        = 2f;
    public const float  BetweenWaveRegenRate = 5f;
    public const float  BerserkerCritChance  = 0.2f;
    public const float  BerserkerCritMult    = 2f;
    public const float  BerserkerRageMult    = 1.5f;
    public const float  BerserkerRageThresh  = 0.3f;
    public bool         BerserkerRageActive  => FrontlineIsBerserker && SoldierHP < FrontlineMaxHP * BerserkerRageThresh;
    public const float  MixedArmyDmgReduction  = 0.15f;
    public const float  TankShieldWallReduction = 0.10f;

    // --- Equipment ---
    public int WeaponLevel   { get; private set; }
    public int ArmorLevel    { get; private set; }
    public int TalismanLevel { get; private set; }
    public float  EquipAttackBonus    => WeaponLevel   * 3f;
    public float  EquipArmorBonus     => ArmorLevel    * 10f;
    public double EquipTalismanBonus  => TalismanLevel * 0.15;
    public double WeaponUpgradeCost   => Math.Floor(20  * Math.Pow(2, WeaponLevel));
    public double ArmorUpgradeCost    => Math.Floor(15  * Math.Pow(2, ArmorLevel));
    public double TalismanUpgradeCost => Math.Floor(25  * Math.Pow(2, TalismanLevel));
    public const int MaxEquipLevel = 5;

    // --- Fortifications ---
    public int    FortificationLevel { get; private set; }
    public double FortificationCost  { get; private set; } = FortBaseCost;
    public const int    MaxFortificationLevel   = 10;
    public const double FortBaseCost            = 50.0;
    public const double FortCostMultiplier      = 2.0;
    public const float  FortHPReductionPerLevel = 0.02f;
    public float FortificationDmgReduction => FortificationLevel * FortHPReductionPerLevel;

    // --- Barracks ---
    public int BarracksLevel { get; private set; } = 1;
    public double BarracksUpgradeCost { get; private set; } = 20.0;
    public const int    BarracksSoldierBonus    = 5;
    public const double BarracksCostMultiplier  = 2.0;

    // --- Blood Ritual ---
    public int    BloodRitualCount { get; private set; }
    public double BloodRitualCost  { get; private set; } = BloodRitualBaseCost;
    public double BloodPerSec      => BloodRitualCount * (BloodRitualBloodPerSec + PRitualEffLevel * 0.5) * PrestigeMultiplier
                                    * (HasTalent(TalentFlags.Glutton) ? TalentGluttonMult : 1f)
                                    + BloodTithePerSec + BloodTapPerSec + KillIncomePerSec;
    public const double BloodRitualBaseCost       = 30.0;
    public const double BloodRitualBloodPerSec    = 1.0;
    public const double BloodRitualCostMultiplier = 2.0;

    // --- Blood Pact spell ---
    public bool BloodPactUnlocked => WorkersUnlocked;
    public const double BloodPactBloodCost = 200.0;
    public const double BloodPactWoodGain  = 100.0;

    // --- Prestige ---
    public int TotalEnemiesKilled { get; private set; }
    public int TotalSoldiersLost  { get; private set; }

    public int    PrestigeCount      { get; private set; }
    public double PrestigeMultiplier => 1.0 + 0.5 * PrestigeCount;
    public const int PrestigeWaveRequirement = 20;

    // --- Prestige Shop ---
    public int PrestigePoints        { get; private set; }
    public int PSoldierCapLevel      { get; private set; }
    public int PClickBonusLevel      { get; private set; }
    public int PRitualEffLevel       { get; private set; }
    public int PWeaponHeadStartLevel { get; private set; }
    public int PBloodTitheLevel      { get; private set; }
    public int PIronWallLevel        { get; private set; }
    public const int   PrestigeShopCost      = 1;
    public const float IronWallDmgReduction  = 0.10f;
    public double BloodTithePerSec => PBloodTitheLevel * 0.5 * PrestigeMultiplier;

    // --- Soul Shards ---
    public double SoulShards            { get; private set; }
    public bool   SoulShardShopUnlocked { get; private set; }
    public int    SSBossTimerLevel      { get; private set; }
    public int    SSDoubleChestLevel    { get; private set; }
    public int    SSRollbackLevel       { get; private set; }
    public const int    SSMaxLevel    = 3;
    public const double SSUpgradeCost = 1.0;
    public int    SSBloodTapLevel { get; private set; }
    public double BloodTapPerSec  => SSBloodTapLevel * 1.0 * PrestigeMultiplier;

    // --- Blood Bank ---
    public double BloodBankDeposit { get; private set; }
    public double BloodBankAccrued { get; private set; }
    public const double BankInterestRatePerHour = 0.02;
    public const double BankMaxDeposit          = 10_000.0;

    // --- Wave Streak ---
    public int   WaveStreak          { get; private set; }
    public float StreakMultiplier    => Mathf.Min(1f + WaveStreak * 0.1f, 3f);
    public const float MaxStreakMultiplier = 3f;
    public float KillStreakBonusMult => 1f + Mathf.Min(WaveStreak, 5) * 0.05f;
    public float MoraleBonusMult     => WaveStreak >= 3 ? 1.15f : 1f;
    public bool  LastStandActive     => SoldierCount == 1;
    public const float LastStandMult  = 1.20f;

    // --- Prestige Talent Tree ---
    public TalentFlags   Talents              { get; private set; }
    public bool          PendingPrestige      { get; private set; }
    public TalentFlags[] PendingTalentChoices { get; private set; } = new TalentFlags[0];
    public bool HasTalent(TalentFlags t)      => (Talents & t) != 0;
    public const float  TalentIronSkinHP      = 15f;
    public const double TalentBloodFrenzyBonus = 0.25;
    public const float  TalentGluttonMult     = 1.25f;

    // --- Soul Sacrifice ---
    public bool SoulSacrificeUnlocked  => PrestigeCount >= 1;
    public const double SoulSacrificeBloodMult = 10.0;

    // --- Daily Challenge ---
    public bool  DailyChallengeAvailable { get; private set; }
    public bool  DailyChallengeActive    { get; private set; }
    public float ChallengeTimeRemaining  { get; private set; }
    public const float  ChallengeDuration  = 60f;
    public const float  ChallengeHPMult   = 5f;
    public const float  ChallengeAtkMult  = 2f;
    public const double ChallengeBloodMult = 5.0;

    // --- Blood Corruption ---
    public int  CorruptionLevel { get; private set; }
    public const int    MaxCorruptionLevel  = 5;
    public const float  CorruptionHPPenalty = 5f;
    public const double PurifyCost          = 3.0;

    // --- Boss Ability ---
    public BossAbility CurrentBossAbility { get; private set; }
    public bool        BossShieldActive   { get; private set; }
    float _bossShieldHP;
    public const float BossShieldFraction = 0.20f;
    public const float BossDrainPerSec    = 5f;
    public string BossAbilityDisplay => CurrentBossAbility switch
    {
        BossAbility.Shield => "🛡 Shielded",
        BossAbility.Berserk => "💀 Berserk",
        BossAbility.Drain   => "🩸 Drain",
        _                   => "",
    };

    // --- Spell Upgrades ---
    public int    SurgeUpgradeLevel  { get; private set; }
    public int    HealUpgradeLevel   { get; private set; }
    public const int    MaxSpellUpgradeLevel  = 3;
    public const double SurgeUpgradeBaseCost  = 60.0;
    public const double HealUpgradeBaseCost   = 40.0;
    public double SurgeUpgradeCost   => Math.Floor(SurgeUpgradeBaseCost * Math.Pow(2, SurgeUpgradeLevel));
    public double HealUpgradeCost    => Math.Floor(HealUpgradeBaseCost  * Math.Pow(2, HealUpgradeLevel));
    public float  SurgeDurationEffective  => SurgeDuration  + SurgeUpgradeLevel * 5f;
    public float  HealSelfAmountEffective => HealSelfAmount + HealUpgradeLevel  * 10f;

    // --- Blood Surge spell ---
    public bool  SurgeActive        { get; private set; }
    public float SurgeTimeRemaining { get; private set; }
    public bool  SurgeUnlocked      { get; private set; }
    public const double SurgeCost            = 50.0;
    public const float  SurgeDuration        = 10f;
    public const float  SurgeMultiplier      = 2f;
    public const double SurgeUnlockThreshold = 500.0;

    // --- Enemy ---
    public int Wave { get; private set; } = 1;
    public float EnemyHP { get; private set; }
    public float EnemyMaxHP { get; private set; }
    public string EnemyName { get; private set; }
    public float EnemyAttack { get; private set; }
    public int EnemySpriteIndex { get; private set; }
    public int NextBossWave { get; private set; }
    public bool IsBossWave => Wave == NextBossWave;
    public int WavesUntilBoss => NextBossWave - Wave;
    public float BossTimeRemaining { get; private set; }
    public const float BossTimeLimit           = 90f;
    public const int   BossWaveRollback        = 3;
    public const float BossFailBloodPenaltyPct = 0.25f;

    // --- Enemy Modifier ---
    public EnemyModifier CurrentEnemyModifier { get; private set; }
    public string EnemyModifierDisplay => CurrentEnemyModifier switch
    {
        EnemyModifier.Armored => "⚔ Armored",
        EnemyModifier.Enraged => "💢 Enraged",
        EnemyModifier.Regen   => "♻ Regen",
        EnemyModifier.Cursed  => "☠ Cursed",
        _                     => "",
    };
    public const float EnemyArmoredDmgMult  = 0.5f;
    public const float EnemyEnragedAtkMult  = 1.5f;
    public const float EnemyRegenPct        = 0.02f;
    public const float RegenLeechRate       = 0.5f;
    public const float EnemyCursedDotRate    = 2f;
    public const float EnemyCursedRewardMult = 1.5f;
    public const float EnragedDeathBlowMult  = 0.5f;

    // --- Wave preview ---
    public bool WavePreviewActive { get; private set; }
    float _previewTimer;
    const float WavePreviewDuration = 3f;

    // --- Flawless wave ---
    public const float FlawlessThreshold = 10f;
    public bool FlawlessActive => _flawlessTimer <= FlawlessThreshold && EnemyHP > 0 && !WavePreviewActive;
    float _flawlessTimer;
    bool  _undyingUsedThisWave;

    // --- Settings ---
    public bool SoundEnabled         { get; private set; } = true;
    public bool NotificationsEnabled { get; private set; } = true;
    bool _resetPending;

    // --- Prestige Milestones ---
    static readonly int[] k_PrestigeMilestones = { 5, 10, 20, 50 };
    public int   PrestigeMilestonesReached { get { int c = 0; foreach (var m in k_PrestigeMilestones) if (PrestigeCount >= m) c++; return c; } }
    public float PrestigeMilestoneDmgBonus => PrestigeMilestonesReached * MilestoneDmgBonusPerLevel;
    public const float MilestoneDmgBonusPerLevel = 0.05f;

    // --- Daily login bonus ---
    public bool DailyBonusAvailable { get; private set; }
    public const float DailyBonusMultiplier = 10f;

    // --- Statistics ---
    public double TimePlayed         { get; private set; }

    // --- Achievements ---
    public AchievementFlags Achievements { get; private set; }
    public event Action<AchievementFlags> OnAchievementUnlocked;

    // --- Offline earnings ---
    public double OfflineWoodEarned  { get; private set; }
    public double OfflineBloodEarned { get; private set; }

    // --- Workers unlock ---
    public bool WorkersUnlocked { get; private set; }
    public const double WorkersUnlockThreshold = 200.0;

    // --- Heal Self ---
    public bool HealSelfUnlocked { get; private set; }
    public const double HealSelfUnlockThreshold = 250.0;
    public const double HealSelfCost   = 25.0;
    public const float  HealSelfAmount = 20f;
    public const double BloodShieldCost            = 30.0;
    public const float  BloodShieldAmount          = 30f;
    public const double BloodShieldUnlockThreshold = 150.0;
    public float  BloodShieldHP       { get; private set; }
    public bool   BloodShieldUnlocked { get; private set; }
    public bool   AutoBuySoldiers     { get; private set; }

    public event Action OnStateChanged;
    public event Action<float, bool> OnDamageDealt;
    public event Action<string> OnMilestoneChest;

    float _dmgTimer;
    const float DmgTickInterval = 0.4f;

    // Audio removed — UnityEngine.AudioModule not available in this assembly config

    static readonly (AchievementFlags flag, double blood, int pp)[] k_AchievRewards =
    {
        (AchievementFlags.FirstKill,     50.0,  0),
        (AchievementFlags.Wave10,        200.0, 0),
        (AchievementFlags.Wave25,        500.0, 0),
        (AchievementFlags.Blood1K,       100.0, 0),
        (AchievementFlags.Blood10K,      500.0, 0),
        (AchievementFlags.FirstSoldier,  25.0,  0),
        (AchievementFlags.FullLegion,    300.0, 0),
        (AchievementFlags.FirstRitual,   100.0, 0),
        (AchievementFlags.FirstPrestige, 0.0,   1),
    };

#if UNITY_INCLUDE_TESTS
    public static void ResetForTest()                        => Instance = null;
    public void SetWoodForTest(double amount)                => Wood = amount;
    public void SetSoldierHPForTest(float hp)                => SoldierHP = hp;
    public void SaveForTest()                                => Save();
    public void ClearSaveForTest()                           => PlayerPrefs.DeleteAll();
    public void AwardBloodForTest(double amount)             => AddBlood(amount);
    public void SetWaveForTest(int wave)                     => Wave = wave;
    public void SetNextBossWaveForTest(int wave)             => NextBossWave = wave;
    public void SpawnEnemyForTest(int wave)                  => SpawnEnemy(wave);
    public void TriggerBossTimeoutForTest()                  => BossTimerExpired();
    public void ForceUnlockHealSelfForTest()                 => HealSelfUnlocked = true;
    public void SetSurgeUnlockedForTest()                    => SurgeUnlocked = true;
    public void AwardPrestigePointsForTest(int pts)          => PrestigePoints += pts;
    public void SetPRitualEffLevelForTest(int level)         => PRitualEffLevel = level;
    public void TriggerMilestoneChestForTest(int wave)       => GrantMilestoneChest(wave);
    public void SetDailyBonusForTest(bool available)         => DailyBonusAvailable = available;
    public void SetEnemyModifierForTest(EnemyModifier m)     => CurrentEnemyModifier = m;
    public void SetEnemyHPForTest(float hp)                  { EnemyHP = hp; EnemyMaxHP = hp; }
    public void SetSoulShardsForTest(double amount)          => SoulShards = amount;
    public void UnlockSoulShardShopForTest()                 => SoulShardShopUnlocked = true;
    public void SetFortLevelForTest(int level)               { FortificationLevel = level; }
    public void SkipWavePreviewForTest()                     { WavePreviewActive = false; SpawnEnemy(Wave); }
    public void SetWaveStreakForTest(int s)                  => WaveStreak = s;
    public void SetBossAbilityForTest(BossAbility a)         { CurrentBossAbility = a; BossShieldActive = a == BossAbility.Shield; _bossShieldHP = BossShieldActive ? EnemyMaxHP * BossShieldFraction : 0f; }
    public void SetBloodBankDepositForTest(double d)         => BloodBankDeposit = d;
    public void SetBloodBankAccruedForTest(double a)         => BloodBankAccrued = a;
    public void SetPrestigeCountForTest(int c)               => PrestigeCount = c;
    public void SetPaladinCountForTest(int count)            => PaladinCount = count;
    public void SetFlawlessTimerForTest(float t)             => _flawlessTimer = t;
    public void SetSurgeUpgradeLevelForTest(int l)           => SurgeUpgradeLevel = l;
    public void SetHealUpgradeLevelForTest(int l)            => HealUpgradeLevel = l;
    public void SetSSBloodTapLevelForTest(int l)             => SSBloodTapLevel = l;
    public void TickCombatForTest(float dt)                  => RunCombat(dt);
    public void SetSurgeActiveForTest(bool active)           { SurgeActive = active; SurgeTimeRemaining = active ? 999f : 0f; }
    public void SetSSDoubleChestLevelForTest(int l)          => SSDoubleChestLevel = l;
    public void SetPIronWallLevelForTest(int l)              => PIronWallLevel = l;
    public void ClearOfflineEarningsForTest()                            { OfflineWoodEarned = 0; OfflineBloodEarned = 0; }
    public void SetOfflineEarningsForTest(double blood, double wood)     { OfflineBloodEarned = blood; OfflineWoodEarned = wood; }
    public void SetTalentsForTest(TalentFlags t)                         => Talents = t;
    public void SetCorruptionLevelForTest(int l)                         => CorruptionLevel = l;
    public void SetDailyChallengeAvailableForTest(bool v)                => DailyChallengeAvailable = v;
    public void SetDailyChallengeActiveForTest(bool v)                   { DailyChallengeActive = v; if (v) ChallengeTimeRemaining = ChallengeDuration; }
    public void SetUndyingUsedForTest(bool v)                            => _undyingUsedThisWave = v;
    public TalentFlags[] GetTalentOptionsForTest()                       => GetTalentOptions();
    public void SetPendingPrestigeForTest(bool v)                        { PendingPrestige = v; if (v) PendingTalentChoices = GetTalentOptions(); }

    public static double CalculateOfflineWood(int workers, double seconds) =>
        workers * WorkerWoodPerSec * Math.Min(seconds, 8.0 * 3600);

    public static double CalculateOfflineBlood(int rituals, int ritualEffLevel, double prestigeMult, double seconds, int bloodTitheLevel = 0) =>
        (rituals * (BloodRitualBloodPerSec + ritualEffLevel * 0.5) * prestigeMult
         + bloodTitheLevel * 0.5 * prestigeMult) * Math.Min(seconds, 8.0 * 3600);
#endif

    private static readonly string[] BossNames =
    {
        "Blood Tyrant", "Skull King", "Bone Colossus", "Shadow Warlord",
        "Crimson Devourer", "Abyssal Overlord", "Plague Archon", "Death Incarnate",
    };

    private struct EnemyDef
    {
        public string Name;
        public float  HPMult;
        public float  AtkMult;
        public int    SpriteIdx;
    }

    private static readonly EnemyDef[] EnemyPool =
    {
        new EnemyDef { Name = "Goblin",         HPMult = 0.6f, AtkMult = 0.7f, SpriteIdx = 0 },
        new EnemyDef { Name = "Skeleton",        HPMult = 0.7f, AtkMult = 0.9f, SpriteIdx = 0 },
        new EnemyDef { Name = "Orc Warrior",     HPMult = 1.0f, AtkMult = 1.0f, SpriteIdx = 1 },
        new EnemyDef { Name = "Cave Troll",      HPMult = 1.5f, AtkMult = 0.8f, SpriteIdx = 2 },
        new EnemyDef { Name = "Werewolf",        HPMult = 1.1f, AtkMult = 1.3f, SpriteIdx = 2 },
        new EnemyDef { Name = "Stone Ogre",      HPMult = 2.0f, AtkMult = 0.6f, SpriteIdx = 3 },
        new EnemyDef { Name = "Ice Giant",       HPMult = 1.8f, AtkMult = 0.9f, SpriteIdx = 3 },
        new EnemyDef { Name = "Demon Knight",    HPMult = 1.2f, AtkMult = 1.4f, SpriteIdx = 4 },
        new EnemyDef { Name = "Dark Witch",      HPMult = 0.8f, AtkMult = 1.7f, SpriteIdx = 4 },
        new EnemyDef { Name = "Vampire Lord",    HPMult = 1.0f, AtkMult = 1.6f, SpriteIdx = 5 },
        new EnemyDef { Name = "Lich",            HPMult = 0.9f, AtkMult = 1.5f, SpriteIdx = 5 },
        new EnemyDef { Name = "Ancient Dragon",  HPMult = 2.5f, AtkMult = 2.0f, SpriteIdx = 6 },
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        NextBossWave = UnityEngine.Random.Range(6, 13);
        SpawnEnemy(1);
        Load();

    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool changed = false;

        TimePlayed += dt;

        if (SurgeActive)
        {
            SurgeTimeRemaining -= dt;
            if (SurgeTimeRemaining <= 0f) { SurgeActive = false; SurgeTimeRemaining = 0f; }
            changed = true;
        }

        if (IsAllTank && SoldierCount > 0 && SoldierHP < FrontlineMaxHP)
        {
            SoldierHP = Mathf.Min(SoldierHP + TankRegenRate * dt, FrontlineMaxHP);
            changed = true;
        }

        if (PaladinCount > 0 && SoldierCount > 0 && SoldierHP < FrontlineMaxHP)
        {
            SoldierHP = Mathf.Min(SoldierHP + PaladinCount * PaladinHealRate * dt, FrontlineMaxHP);
            changed = true;
        }

        if (WavePreviewActive && SoldierCount > 0 && SoldierHP < FrontlineMaxHP)
        {
            SoldierHP = Mathf.Min(SoldierHP + BetweenWaveRegenRate * dt, FrontlineMaxHP);
            changed = true;
        }

        if (SSBloodTapLevel > 0)
        {
            AddBlood(BloodTapPerSec * dt);
            changed = true;
        }

        if (EnemyHP > 0 && !WavePreviewActive)
        {
            _flawlessTimer += dt;
            changed = true;
        }

        if (WorkerCount > 0)
        {
            Wood += WoodPerSecond * dt;
            changed = true;
        }

        if (BloodPerSec > 0)
        {
            AddBlood(BloodPerSec * dt);
            changed = true;
        }

        if (AutoBuySoldiers && SoldierCount < MaxSoldiers && Blood >= SoldierCost)
        {
            BuyTank();
            changed = true;
        }

        if (BloodBankDeposit > 0)
        {
            BloodBankAccrued += BloodBankDeposit * (BankInterestRatePerHour / 3600.0) * dt;
            changed = true;
        }

        if (CurrentEnemyModifier == EnemyModifier.Regen && EnemyHP > 0 && EnemyHP < EnemyMaxHP)
        {
            EnemyHP = Mathf.Min(EnemyHP + EnemyMaxHP * EnemyRegenPct * dt, EnemyMaxHP);
            if (SoldierCount > 0) SoldierHP = Mathf.Max(0f, SoldierHP - RegenLeechRate * dt);
            changed = true;
        }

        if (WavePreviewActive)
        {
            _previewTimer -= dt;
            if (_previewTimer <= 0f)
            {
                WavePreviewActive = false;
                SpawnEnemy(Wave);
            }
            changed = true;
        }

        if (SoldierCount > 0 && EnemyHP > 0)
            changed |= RunCombat(dt);

        if (DailyChallengeActive && SoldierCount > 0 && EnemyHP > 0)
        {
            ChallengeTimeRemaining -= dt;
            if (ChallengeTimeRemaining <= 0f)
            {
                DailyChallengeActive = false;
                ChallengeTimeRemaining = 0f;
            }
            changed = true;
        }

        if (IsBossWave && !DailyChallengeActive && SoldierCount > 0 && EnemyHP > 0)
        {
            BossTimeRemaining -= dt;
            if (BossTimeRemaining <= 0f)
            {
                BossTimerExpired();
                return;
            }
            changed = true;
        }

        if (changed)
            OnStateChanged?.Invoke();
    }

    bool RunCombat(float dt)
    {
        if (CurrentEnemyModifier == EnemyModifier.Cursed && SoldierCount > 0)
            SoldierHP = Mathf.Max(0f, SoldierHP - EnemyCursedDotRate * dt);

        float eff = TotalAttack * (SurgeActive ? SurgeMultiplier : 1f);
        if (CurrentEnemyModifier == EnemyModifier.Armored && !IsAllBerserker)
            eff *= IsAllTank ? EnemyArmoredDmgMult + 0.25f : EnemyArmoredDmgMult;
        if (CurrentEnemyModifier == EnemyModifier.Cursed && PaladinCount > 0) eff *= PaladinHolyBonus;
        if (BerserkerRageActive) eff *= BerserkerRageMult;
        if (LastStandActive)     eff *= LastStandMult;
        if (BossShieldActive)
        {
            _bossShieldHP -= eff * dt;
            if (_bossShieldHP <= 0) { _bossShieldHP = 0; BossShieldActive = false; }
        }
        else
        {
            EnemyHP = Mathf.Max(0f, EnemyHP - eff * dt);
        }

        if (EnemyHP <= 0f)
        {
            if (CurrentEnemyModifier == EnemyModifier.Enraged && SoldierCount > 0)
                SoldierHP = Mathf.Max(0f, SoldierHP - EnemyAttack * EnragedDeathBlowMult);

            bool wasBoss      = IsBossWave;
            bool wasChallenge = DailyChallengeActive;
            double reward = Math.Floor(25 * Math.Pow(1.4, Wave - 1) * PrestigeMultiplier * (1.0 + EquipTalismanBonus));
            if (CurrentEnemyModifier == EnemyModifier.Cursed)
                reward = Math.Floor(reward * EnemyCursedRewardMult);
            if (IsBloodyWave) reward = Math.Floor(reward * BloodMoonMult);
            if (wasBoss)
            {
                reward *= 3;
                SoulShards += HasTalent(TalentFlags.ShardHunter) ? 2 : 1;
                SoulShardShopUnlocked = true;
                BossTimeRemaining = 0f;
            }
            if (wasChallenge)
            {
                reward = Math.Floor(reward * ChallengeBloodMult);
                DailyChallengeActive   = false;
                ChallengeTimeRemaining = 0f;
            }
            if (HasTalent(TalentFlags.BloodFrenzy))
                reward = Math.Floor(reward * (1.0 + TalentBloodFrenzyBonus));
            bool isFlawless = _flawlessTimer > 0f && _flawlessTimer <= FlawlessThreshold;
            reward = Math.Floor(reward * StreakMultiplier * KillStreakBonusMult * (isFlawless ? 2.0 : 1.0));
            TotalEnemiesKilled++;
            WaveStreak++;
            AddBlood(reward);
            if (isFlawless) OnMilestoneChest?.Invoke("⚡ FLAWLESS! ×2 blood!");
            if ((wasBoss || wasChallenge) && HasTalent(TalentFlags.BloodRush) && !SurgeActive)
            {
                SurgeActive        = true;
                SurgeTimeRemaining = SurgeDurationEffective;
            }

            TotalEnemiesKilled++;
            TryUnlock(AchievementFlags.FirstKill);

            bool isMilestone = (Wave % 5 == 0);
            Wave++;
            if (Wave >= 10) TryUnlock(AchievementFlags.Wave10);
            if (Wave >= 25) TryUnlock(AchievementFlags.Wave25);

            if (wasBoss) NextBossWave = Wave + UnityEngine.Random.Range(5, 11);
            WavePreviewActive = true;
            _previewTimer = WavePreviewDuration;
            _dmgTimer = 0f;

            if (isMilestone) GrantMilestoneChest(Wave - 1);
            return true;
        }

        float incomingAtk   = EnemyAttack;
        bool  isSpecialFoe  = IsBossWave || DailyChallengeActive;
        if (isSpecialFoe && CurrentBossAbility == BossAbility.Berserk && EnemyHP < EnemyMaxHP * 0.25f)
            incomingAtk *= 2f;
        if (IsMixedArmy)        incomingAtk *= (1f - MixedArmyDmgReduction);
        if (IsAllTank)          incomingAtk *= (1f - TankShieldWallReduction);
        if (PIronWallLevel > 0) incomingAtk *= (1f - PIronWallLevel * IronWallDmgReduction);
        float totalIncoming = incomingAtk;
        if (isSpecialFoe && CurrentBossAbility == BossAbility.Drain && EnemyHP > 0)
            totalIncoming += BossDrainPerSec;
        float dmg = totalIncoming * dt;
        if (BloodShieldHP > 0f)
        {
            float absorbed = Mathf.Min(BloodShieldHP, dmg);
            BloodShieldHP -= absorbed;
            dmg           -= absorbed;
        }
        SoldierHP -= dmg;

        if (SoldierHP <= 0f)
        {
            if (HasTalent(TalentFlags.Undying) && !_undyingUsedThisWave)
            {
                SoldierHP            = 1f;
                _undyingUsedThisWave = true;
            }
            else
            {
                if (FrontlineIsTank)           TankCount--;
                else if (FrontlineIsBerserker) BerserkerCount--;
                else                           PaladinCount--;
                TotalSoldiersLost++;
                SoldierHP  = SoldierCount > 0 ? FrontlineMaxHP : 0f;
                WaveStreak = 0;
            }
        }

        _dmgTimer += dt;
        if (_dmgTimer >= DmgTickInterval)
        {
            _dmgTimer = 0f;
            float tickDmg = eff * DmgTickInterval;
            if (BerserkerCount > 0 && TankCount == 0 && UnityEngine.Random.value < BerserkerCritChance)
            {
                tickDmg *= BerserkerCritMult;
                EnemyHP  = Mathf.Max(0f, EnemyHP - tickDmg);
            }
            OnDamageDealt?.Invoke(tickDmg, true);
            if (SoldierCount > 0)
                OnDamageDealt?.Invoke(totalIncoming * DmgTickInterval, false);
        }

        return true;
    }

    void GrantMilestoneChest(int completedWave)
    {
        int mult = 1 + SSDoubleChestLevel;
        int roll = UnityEngine.Random.Range(0, 3);
        string msg;
        switch (roll)
        {
            case 0:
                double bloodBonus = Math.Floor(100 * completedWave * PrestigeMultiplier * mult);
                AddBlood(bloodBonus);
                msg = $"Wave {completedWave} Chest: +{FormatNumber(bloodBonus)} Blood!";
                break;
            case 1:
                if (SoldierCount < MaxSoldiers)
                {
                    if (UnityEngine.Random.value < 0.5f) TankCount++;
                    else BerserkerCount++;
                    if (SoldierCount == 1) SoldierHP = FrontlineMaxHP;
                    msg = $"Wave {completedWave} Chest: Free Soldier!";
                }
                else
                {
                    double bonus = Math.Floor(50 * completedWave * PrestigeMultiplier * mult);
                    AddBlood(bonus);
                    msg = $"Wave {completedWave} Chest: +{FormatNumber(bonus)} Blood!";
                }
                break;
            default:
                double woodBonus = Math.Floor(25.0 * completedWave * mult);
                Wood += woodBonus;
                msg = $"Wave {completedWave} Chest: +{FormatNumber(woodBonus)} Wood!";
                break;
        }
        OnMilestoneChest?.Invoke(msg);
    }

    void SpawnEnemy(int wave)
    {
        _flawlessTimer       = 0f;
        _undyingUsedThisWave = false;
        bool isBoss = wave == NextBossWave;
        float fortReduction = 1f - FortificationDmgReduction;
        if (isBoss)
        {
            int idx          = UnityEngine.Random.Range(0, BossNames.Length);
            EnemyName        = BossNames[idx];
            EnemySpriteIndex = 6;
            EnemyMaxHP       = (float)(100 * Math.Pow(1.5, wave - 1) * 5.0 * fortReduction);
            EnemyHP          = EnemyMaxHP;
            EnemyAttack      = (float)(3   * Math.Pow(1.3, wave - 1) * 2.0);
            BossTimeRemaining = BossTimeLimit + SSBossTimerLevel * 15f;
            CurrentEnemyModifier = EnemyModifier.None;
            CurrentBossAbility   = (BossAbility)UnityEngine.Random.Range(0, 4);
            BossShieldActive     = CurrentBossAbility == BossAbility.Shield;
            _bossShieldHP        = BossShieldActive ? EnemyMaxHP * BossShieldFraction : 0f;
        }
        else
        {
            var def          = EnemyPool[UnityEngine.Random.Range(0, EnemyPool.Length)];
            EnemyName        = def.Name;
            EnemySpriteIndex = def.SpriteIdx;
            EnemyMaxHP       = (float)(100 * Math.Pow(1.5, wave - 1) * def.HPMult * fortReduction);
            EnemyHP          = EnemyMaxHP;
            EnemyAttack      = (float)(3   * Math.Pow(1.3, wave - 1) * def.AtkMult);

            CurrentBossAbility = BossAbility.None;
            BossShieldActive   = false;
            _bossShieldHP      = 0f;
            if (UnityEngine.Random.value < 0.25f)
            {
                CurrentEnemyModifier = (EnemyModifier)UnityEngine.Random.Range(1, 5);
                if (CurrentEnemyModifier == EnemyModifier.Enraged)
                    EnemyAttack *= EnemyEnragedAtkMult;
            }
            else
            {
                CurrentEnemyModifier = EnemyModifier.None;
            }
        }
    }

    void BossTimerExpired()
    {
        TankCount         = 0;
        BerserkerCount    = 0;
        SoldierHP         = 0f;
        Blood             = Math.Floor(Blood * (1.0 - BossFailBloodPenaltyPct));
        int rollback      = Math.Max(0, BossWaveRollback - SSRollbackLevel);
        Wave              = Math.Max(1, Wave - rollback);
        NextBossWave      = Wave + UnityEngine.Random.Range(5, 11);
        BossTimeRemaining = 0f;
        WavePreviewActive = false;
        SpawnEnemy(Wave);
        OnStateChanged?.Invoke();
    }

    public void FarmBlood()
    {
        double amount = EffectiveBloodPerClick;
        if (DailyBonusAvailable)
        {
            amount *= DailyBonusMultiplier;
            DailyBonusAvailable = false;
            PlayerPrefs.SetString("LastLoginDate", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }
        AddBlood(amount);
        OnStateChanged?.Invoke();
    }

    void AddBlood(double amount)
    {
        Blood += amount;
        TotalBloodEarned += amount;
        if (!WorkersUnlocked  && TotalBloodEarned >= WorkersUnlockThreshold)  WorkersUnlocked  = true;
        if (!BloodShieldUnlocked && TotalBloodEarned >= BloodShieldUnlockThreshold) BloodShieldUnlocked = true;
        if (!HealSelfUnlocked && TotalBloodEarned >= HealSelfUnlockThreshold) HealSelfUnlocked = true;
        if (!SurgeUnlocked    && TotalBloodEarned >= SurgeUnlockThreshold)    SurgeUnlocked    = true;
        if (TotalBloodEarned >= 1_000)  TryUnlock(AchievementFlags.Blood1K);
        if (TotalBloodEarned >= 10_000) TryUnlock(AchievementFlags.Blood10K);
    }

    void TryUnlock(AchievementFlags flag)
    {
        if ((Achievements & flag) != 0) return;
        Achievements |= flag;
        foreach (var (f, blood, pp) in k_AchievRewards)
        {
            if (f != flag) continue;
            if (blood > 0) AddBlood(blood);
            if (pp    > 0) PrestigePoints += pp;
            break;
        }
        OnAchievementUnlocked?.Invoke(flag);
    }

    void PlaySound(object clip) { } // stub — audio module unavailable in this assembly

    public bool BuySoldier() => BuyTank();
    public void ToggleAutoBuySoldiers() { AutoBuySoldiers = !AutoBuySoldiers; FireStateChanged(); }

    public bool BuyTank()
    {
        if (Blood < SoldierCost || SoldierCount >= MaxSoldiers) return false;
        Blood -= SoldierCost;
        TankCount++;
        if (SoldierCount == 1) { SoldierHP = FrontlineMaxHP; TryUnlock(AchievementFlags.FirstSoldier); }
        if (SoldierCount >= MaxSoldiers) TryUnlock(AchievementFlags.FullLegion);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyBerserker()
    {
        if (Blood < SoldierCost || SoldierCount >= MaxSoldiers) return false;
        Blood -= SoldierCost;
        BerserkerCount++;
        if (SoldierCount == 1) { SoldierHP = FrontlineMaxHP; TryUnlock(AchievementFlags.FirstSoldier); }
        if (SoldierCount >= MaxSoldiers) TryUnlock(AchievementFlags.FullLegion);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyWorker()
    {
        if (Blood < WorkerCost) return false;
        Blood -= WorkerCost;
        WorkerCount++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyBloodRitual()
    {
        if (Wood < BloodRitualCost) return false;
        Wood -= BloodRitualCost;
        BloodRitualCount++;
        if (BloodRitualCount == 1) TryUnlock(AchievementFlags.FirstRitual);
        BloodRitualCost = Math.Floor(BloodRitualCost * BloodRitualCostMultiplier);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UpgradeBarracks()
    {
        if (Wood < BarracksUpgradeCost) return false;
        Wood -= BarracksUpgradeCost;
        BarracksLevel++;
        MaxSoldiers += BarracksSoldierBonus;
        BarracksUpgradeCost = Math.Floor(BarracksUpgradeCost * BarracksCostMultiplier);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UpgradeFortification()
    {
        if (Wood < FortificationCost || FortificationLevel >= MaxFortificationLevel) return false;
        Wood -= FortificationCost;
        FortificationLevel++;
        FortificationCost = Math.Floor(FortificationCost * FortCostMultiplier);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UseBloodShield()
    {
        if (!BloodShieldUnlocked || Blood < BloodShieldCost || SoldierCount == 0) return false;
        Blood -= BloodShieldCost;
        BloodShieldHP = BloodShieldAmount;
        FireStateChanged();
        return true;
    }

    public bool UseHealSelf()
    {
        if (!HealSelfUnlocked || Blood < HealSelfCost || SoldierCount == 0 || SoldierHP >= FrontlineMaxHP)
            return false;
        Blood -= HealSelfCost;
        SoldierHP = Mathf.Min(SoldierHP + HealSelfAmountEffective, FrontlineMaxHP);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UseSurge()
    {
        if (!SurgeUnlocked || Blood < SurgeCost || SurgeActive || SoldierCount == 0) return false;
        Blood -= SurgeCost;
        SurgeActive = true;
        SurgeTimeRemaining = SurgeDurationEffective;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UseBloodPact()
    {
        if (!BloodPactUnlocked || Blood < BloodPactBloodCost) return false;
        Blood -= BloodPactBloodCost;
        Wood  += BloodPactWoodGain;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool DepositToBank(double amount)
    {
        double toDeposit = Math.Min(Math.Min(amount, Blood), BankMaxDeposit - BloodBankDeposit);
        if (toDeposit < 0.01) return false;
        Blood            -= toDeposit;
        BloodBankDeposit += toDeposit;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool WithdrawFromBank()
    {
        if (BloodBankDeposit <= 0 && BloodBankAccrued <= 0) return false;
        AddBlood(BloodBankDeposit + BloodBankAccrued);
        BloodBankDeposit = 0;
        BloodBankAccrued = 0;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UpgradeWeapon()
    {
        if (WeaponLevel >= MaxEquipLevel || Wood < WeaponUpgradeCost) return false;
        Wood -= WeaponUpgradeCost;
        WeaponLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UpgradeArmor()
    {
        if (ArmorLevel >= MaxEquipLevel || Wood < ArmorUpgradeCost) return false;
        Wood -= ArmorUpgradeCost;
        ArmorLevel++;
        if (SoldierCount > 0) SoldierHP = Mathf.Min(SoldierHP + 10f, FrontlineMaxHP);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UpgradeTalisman()
    {
        if (TalismanLevel >= MaxEquipLevel || Wood < TalismanUpgradeCost) return false;
        Wood -= TalismanUpgradeCost;
        TalismanLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public void ToggleFormation()
    {
        BerserkerFront = !BerserkerFront;
        OnStateChanged?.Invoke();
    }

    public bool BuyPSoldierCap()
    {
        if (PrestigePoints < PrestigeShopCost) return false;
        PrestigePoints -= PrestigeShopCost;
        PSoldierCapLevel++;
        MaxSoldiers += 10;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyPClickBonus()
    {
        if (PrestigePoints < PrestigeShopCost) return false;
        PrestigePoints -= PrestigeShopCost;
        PClickBonusLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyPRitualEff()
    {
        if (PrestigePoints < PrestigeShopCost) return false;
        PrestigePoints -= PrestigeShopCost;
        PRitualEffLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyPWeaponHeadStart()
    {
        if (PrestigePoints < PrestigeShopCost) return false;
        PrestigePoints -= PrestigeShopCost;
        PWeaponHeadStartLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyPBloodTithe()
    {
        if (PrestigePoints < PrestigeShopCost) return false;
        PrestigePoints -= PrestigeShopCost;
        PBloodTitheLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyPIronWall()
    {
        if (PrestigePoints < PrestigeShopCost) return false;
        PrestigePoints -= PrestigeShopCost;
        PIronWallLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuySSBossTimer()
    {
        if (SoulShards < SSUpgradeCost || SSBossTimerLevel >= SSMaxLevel) return false;
        SoulShards -= SSUpgradeCost;
        SSBossTimerLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuySSDoubleChest()
    {
        if (SoulShards < SSUpgradeCost || SSDoubleChestLevel >= SSMaxLevel) return false;
        SoulShards -= SSUpgradeCost;
        SSDoubleChestLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuySSRollback()
    {
        if (SoulShards < SSUpgradeCost || SSRollbackLevel >= SSMaxLevel) return false;
        SoulShards -= SSUpgradeCost;
        SSRollbackLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyPaladin()
    {
        if (Blood < SoldierCost || SoldierCount >= MaxSoldiers) return false;
        Blood -= SoldierCost;
        PaladinCount++;
        if (SoldierCount == 1) { SoldierHP = FrontlineMaxHP; TryUnlock(AchievementFlags.FirstSoldier); }
        if (SoldierCount >= MaxSoldiers) TryUnlock(AchievementFlags.FullLegion);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UpgradeSurge()
    {
        if (!SurgeUnlocked || SurgeUpgradeLevel >= MaxSpellUpgradeLevel || Blood < SurgeUpgradeCost) return false;
        Blood -= SurgeUpgradeCost;
        SurgeUpgradeLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UpgradeHealSelf()
    {
        if (!HealSelfUnlocked || HealUpgradeLevel >= MaxSpellUpgradeLevel || Blood < HealUpgradeCost) return false;
        Blood -= HealUpgradeCost;
        HealUpgradeLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuySSBloodTap()
    {
        if (SoulShards < SSUpgradeCost || SSBloodTapLevel >= SSMaxLevel) return false;
        SoulShards -= SSUpgradeCost;
        SSBloodTapLevel++;
        OnStateChanged?.Invoke();
        return true;
    }

    public void RequestPrestige()
    {
        if (Wave < PrestigeWaveRequirement || PendingPrestige) return;
        PendingTalentChoices = GetTalentOptions();
        PendingPrestige      = true;
        OnStateChanged?.Invoke();
    }

    public bool ConfirmPrestige(int choiceIdx)
    {
        if (!PendingPrestige) return false;
        if (choiceIdx >= 0 && choiceIdx < PendingTalentChoices.Length)
            Talents |= PendingTalentChoices[choiceIdx];
        PendingPrestige      = false;
        PendingTalentChoices = new TalentFlags[0];
        return Prestige();
    }

    public void CancelPrestige()
    {
        PendingPrestige      = false;
        PendingTalentChoices = new TalentFlags[0];
        OnStateChanged?.Invoke();
    }

    TalentFlags[] GetTalentOptions()
    {
        var all = new TalentFlags[]
        {
            TalentFlags.BloodFrenzy, TalentFlags.Undying, TalentFlags.ShardHunter,
            TalentFlags.IronSkin,    TalentFlags.BloodRush, TalentFlags.Glutton,
        };
        int availCount = 0;
        for (int k = 0; k < all.Length; k++)
            if (!HasTalent(all[k])) availCount++;
        var available = new TalentFlags[availCount];
        int idx = 0;
        for (int k = 0; k < all.Length; k++)
            if (!HasTalent(all[k])) available[idx++] = all[k];
        for (int i = available.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            TalentFlags tmp = available[i];
            available[i]    = available[j];
            available[j]    = tmp;
        }
        int count  = Math.Min(3, available.Length);
        var result = new TalentFlags[count];
        for (int i = 0; i < count; i++) result[i] = available[i];
        return result;
    }

    public bool UseSoulSacrifice()
    {
        if (!SoulSacrificeUnlocked || SoldierCount == 0) return false;
        double reward = Math.Floor(25 * Math.Pow(1.4, Wave - 1) * PrestigeMultiplier * SoulSacrificeBloodMult);
        if (FrontlineIsTank)           TankCount--;
        else if (FrontlineIsBerserker) BerserkerCount--;
        else                           PaladinCount--;
        SoldierHP = SoldierCount > 0 ? FrontlineMaxHP : 0f;
        AddBlood(reward);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool StartDailyChallenge()
    {
        if (!DailyChallengeAvailable || DailyChallengeActive || EnemyHP <= 0 || WavePreviewActive) return false;
        DailyChallengeAvailable = false;
        DailyChallengeActive    = true;
        ChallengeTimeRemaining  = ChallengeDuration;
        EnemyMaxHP *= ChallengeHPMult;
        EnemyHP     = EnemyMaxHP;
        EnemyAttack *= ChallengeAtkMult;
        PlayerPrefs.SetString("LastChallengeDate", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
        OnStateChanged?.Invoke();
        return true;
    }

    public bool Purify()
    {
        if (CorruptionLevel <= 0 || SoulShards < PurifyCost) return false;
        SoulShards -= PurifyCost;
        CorruptionLevel--;
        OnStateChanged?.Invoke();
        return true;
    }

    public void ToggleSound()
    {
        SoundEnabled = !SoundEnabled;
        OnStateChanged?.Invoke();
    }

    public void ToggleNotifications()
    {
        NotificationsEnabled = !NotificationsEnabled;
        OnStateChanged?.Invoke();
    }

    public void ResetAllData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Blood = 0; TotalBloodEarned = 0; Wood = 0;
        TankCount = 0; BerserkerCount = 0; PaladinCount = 0; SoldierHP = 0f;
        WorkerCount = 0; BloodRitualCount = 0; BloodRitualCost = BloodRitualBaseCost;
        BarracksLevel = 1; MaxSoldiers = 10; BarracksUpgradeCost = 20.0;
        WorkersUnlocked = false; HealSelfUnlocked = false; SurgeUnlocked = false;
        BloodShieldUnlocked = false; BloodShieldHP = 0f;
        PrestigeCount = 0; PrestigePoints = 0;
        PSoldierCapLevel = 0; PClickBonusLevel = 0; PRitualEffLevel = 0;
        PWeaponHeadStartLevel = 0; PBloodTitheLevel = 0; PIronWallLevel = 0;
        WeaponLevel = 0; ArmorLevel = 0; TalismanLevel = 0;
        BerserkerFront = false; FortificationLevel = 0; FortificationCost = FortBaseCost;
        SoulShards = 0; SoulShardShopUnlocked = false;
        SSBossTimerLevel = 0; SSDoubleChestLevel = 0; SSRollbackLevel = 0; SSBloodTapLevel = 0;
        BloodBankDeposit = 0; BloodBankAccrued = 0; WaveStreak = 0;
        SurgeUpgradeLevel = 0; HealUpgradeLevel = 0;
        TotalEnemiesKilled = 0; TimePlayed = 0; Achievements = AchievementFlags.None;
        SoundEnabled = true; NotificationsEnabled = true;
        DailyBonusAvailable = false; OfflineWoodEarned = 0; OfflineBloodEarned = 0;
        Talents = TalentFlags.None; PendingPrestige = false; PendingTalentChoices = new TalentFlags[0];
        CorruptionLevel = 0; DailyChallengeActive = false; DailyChallengeAvailable = false; ChallengeTimeRemaining = 0f;
        WavePreviewActive = false; _flawlessTimer = 0f; _undyingUsedThisWave = false;
        Wave = 1; NextBossWave = UnityEngine.Random.Range(6, 13);
        SpawnEnemy(1);
        OnStateChanged?.Invoke();
    }

    public bool Prestige()
    {
        if (Wave < PrestigeWaveRequirement) return false;
        int milestonesBefore = PrestigeMilestonesReached;
        PrestigeCount++;
        PrestigePoints++;
        if (CorruptionLevel < MaxCorruptionLevel) CorruptionLevel++;
        DailyChallengeActive   = false;
        ChallengeTimeRemaining = 0f;
        _undyingUsedThisWave   = false;
        TryUnlock(AchievementFlags.FirstPrestige);
        if (PrestigeMilestonesReached > milestonesBefore)
            OnMilestoneChest?.Invoke($"⭐ Prestige Milestone! +{PrestigeMilestoneDmgBonus * 100:F0}% attack!");
        Blood               = 0;
        Wood                = 0;
        TankCount           = 0;
        BerserkerCount      = 0;
        PaladinCount        = 0;
        SoldierHP           = 0f;
        WorkerCount         = 0;
        BloodRitualCount    = 0;
        BloodRitualCost     = BloodRitualBaseCost;
        Wave                = 1;
        NextBossWave        = UnityEngine.Random.Range(6, 13);
        BarracksLevel       = 1;
        MaxSoldiers         = 10 + PSoldierCapLevel * 10;
        BarracksUpgradeCost = 20.0;
        BossTimeRemaining   = 0f;
        SurgeActive         = false;
        SurgeTimeRemaining  = 0f;
        WavePreviewActive   = false;
        WeaponLevel         = Math.Min(PWeaponHeadStartLevel, MaxEquipLevel);
        ArmorLevel          = 0;
        TalismanLevel       = 0;
        SpawnEnemy(1);
        OnStateChanged?.Invoke();
        return true;
    }

    void OnApplicationPause(bool pausing)
    {
        if (pausing)
        {
            Save();
            NotificationManager.ScheduleIdleReminder(SoldierCount == 0);
        }
        else
        {
            NotificationManager.CancelAll();
        }
    }

    void OnApplicationQuit() { Save(); }

    void Save()
    {
        var ic = CultureInfo.InvariantCulture;
        PlayerPrefs.SetString("Blood",               Blood.ToString("R", ic));
        PlayerPrefs.SetString("Wood",                Wood.ToString("R", ic));
        PlayerPrefs.SetInt   ("Wave",                Wave);
        PlayerPrefs.SetInt   ("TankCount",           TankCount);
        PlayerPrefs.SetInt   ("BerserkerCount",      BerserkerCount);
        PlayerPrefs.SetInt   ("PaladinCount",        PaladinCount);
        PlayerPrefs.SetFloat ("SoldierHP",           SoldierHP);
        PlayerPrefs.SetInt   ("WorkerCount",         WorkerCount);
        PlayerPrefs.SetInt   ("BloodRitualCount",    BloodRitualCount);
        PlayerPrefs.SetString("BloodRitualCost",     BloodRitualCost.ToString("R", ic));
        PlayerPrefs.SetInt   ("BarracksLevel",       BarracksLevel);
        PlayerPrefs.SetInt   ("MaxSoldiers",         MaxSoldiers);
        PlayerPrefs.SetString("BarracksUpgradeCost", BarracksUpgradeCost.ToString("R", ic));
        PlayerPrefs.SetString("TotalBloodEarned",    TotalBloodEarned.ToString("R", ic));
        PlayerPrefs.SetInt   ("WorkersUnlocked",     WorkersUnlocked  ? 1 : 0);
        PlayerPrefs.SetInt   ("AutoBuySoldiers",     AutoBuySoldiers ? 1 : 0);
        PlayerPrefs.SetInt   ("BloodShieldUnlocked", BloodShieldUnlocked ? 1 : 0);
        PlayerPrefs.SetFloat ("BloodShieldHP",       BloodShieldHP);
        PlayerPrefs.SetInt   ("TotalEnemiesKilled",  TotalEnemiesKilled);
        PlayerPrefs.SetInt   ("TotalSoldiersLost",   TotalSoldiersLost);
        PlayerPrefs.SetInt   ("HealSelfUnlocked",    HealSelfUnlocked ? 1 : 0);
        PlayerPrefs.SetInt   ("PrestigeCount",       PrestigeCount);
        PlayerPrefs.SetInt   ("PrestigePoints",      PrestigePoints);
        PlayerPrefs.SetInt   ("PSoldierCapLevel",    PSoldierCapLevel);
        PlayerPrefs.SetInt   ("PClickBonusLevel",    PClickBonusLevel);
        PlayerPrefs.SetInt   ("PRitualEffLevel",     PRitualEffLevel);
        PlayerPrefs.SetInt   ("PWeaponHeadStartLevel", PWeaponHeadStartLevel);
        PlayerPrefs.SetInt   ("PBloodTitheLevel",    PBloodTitheLevel);
        PlayerPrefs.SetInt   ("PIronWallLevel",      PIronWallLevel);
        PlayerPrefs.SetInt   ("WeaponLevel",         WeaponLevel);
        PlayerPrefs.SetInt   ("ArmorLevel",          ArmorLevel);
        PlayerPrefs.SetInt   ("TalismanLevel",       TalismanLevel);
        PlayerPrefs.SetInt   ("BerserkerFront",      BerserkerFront ? 1 : 0);
        PlayerPrefs.SetInt   ("FortificationLevel",  FortificationLevel);
        PlayerPrefs.SetString("FortificationCost",   FortificationCost.ToString("R", ic));
        PlayerPrefs.SetString("SoulShards",          SoulShards.ToString("R", ic));
        PlayerPrefs.SetInt   ("SoulShardShopUnlocked", SoulShardShopUnlocked ? 1 : 0);
        PlayerPrefs.SetInt   ("SSBossTimerLevel",    SSBossTimerLevel);
        PlayerPrefs.SetInt   ("SSDoubleChestLevel",  SSDoubleChestLevel);
        PlayerPrefs.SetInt   ("SSRollbackLevel",     SSRollbackLevel);
        PlayerPrefs.SetInt   ("SSBloodTapLevel",     SSBloodTapLevel);
        PlayerPrefs.SetInt   ("SurgeUpgradeLevel",   SurgeUpgradeLevel);
        PlayerPrefs.SetInt   ("HealUpgradeLevel",    HealUpgradeLevel);
        PlayerPrefs.SetInt   ("SoundEnabled",        SoundEnabled        ? 1 : 0);
        PlayerPrefs.SetInt   ("NotificationsEnabled",NotificationsEnabled ? 1 : 0);
        PlayerPrefs.SetString("BloodBankDeposit",    BloodBankDeposit.ToString("R", ic));
        PlayerPrefs.SetString("BloodBankAccrued",    BloodBankAccrued.ToString("R", ic));
        PlayerPrefs.SetInt   ("WaveStreak",          WaveStreak);
        PlayerPrefs.SetInt   ("TotalEnemiesKilled",  TotalEnemiesKilled);
        PlayerPrefs.SetString("TimePlayed",          TimePlayed.ToString("R", ic));
        PlayerPrefs.SetInt   ("Achievements",        (int)Achievements);
        PlayerPrefs.SetFloat ("EnemyHP",             EnemyHP);
        PlayerPrefs.SetFloat ("EnemyMaxHP",          EnemyMaxHP);
        PlayerPrefs.SetString("EnemyName",           EnemyName);
        PlayerPrefs.SetFloat ("EnemyAttack",         EnemyAttack);
        PlayerPrefs.SetInt   ("EnemySpriteIndex",    EnemySpriteIndex);
        PlayerPrefs.SetInt   ("NextBossWave",        NextBossWave);
        PlayerPrefs.SetInt   ("Talents",             (int)Talents);
        PlayerPrefs.SetInt   ("CorruptionLevel",     CorruptionLevel);
        PlayerPrefs.SetString("SaveTime",            DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
    }

    void Load()
    {
        if (!PlayerPrefs.HasKey("Blood")) return;
        var ic = CultureInfo.InvariantCulture;
        Blood               = double.Parse(PlayerPrefs.GetString("Blood",               "0"),  ic);
        Wood                = double.Parse(PlayerPrefs.GetString("Wood",                "0"),  ic);
        Wave                = PlayerPrefs.GetInt   ("Wave",                1);
        TankCount           = PlayerPrefs.GetInt("TankCount", PlayerPrefs.GetInt("SoldierCount", 0));
        BerserkerCount      = PlayerPrefs.GetInt("BerserkerCount", 0);
        PaladinCount        = PlayerPrefs.GetInt   ("PaladinCount",        0);
        SoldierHP           = PlayerPrefs.GetFloat ("SoldierHP",           0f);
        WorkerCount         = PlayerPrefs.GetInt   ("WorkerCount",         0);
        BloodRitualCount    = PlayerPrefs.GetInt   ("BloodRitualCount",    0);
        BloodRitualCost     = double.Parse(PlayerPrefs.GetString("BloodRitualCost", BloodRitualBaseCost.ToString("R", ic)), ic);
        BarracksLevel       = PlayerPrefs.GetInt   ("BarracksLevel",       1);
        MaxSoldiers         = PlayerPrefs.GetInt   ("MaxSoldiers",         10);
        BarracksUpgradeCost = double.Parse(PlayerPrefs.GetString("BarracksUpgradeCost", "20"), ic);
        TotalBloodEarned    = double.Parse(PlayerPrefs.GetString("TotalBloodEarned",    "0"),  ic);
        WorkersUnlocked     = PlayerPrefs.GetInt   ("WorkersUnlocked",     0) == 1;
        AutoBuySoldiers     = PlayerPrefs.GetInt   ("AutoBuySoldiers",     0) == 1;
        BloodShieldUnlocked = PlayerPrefs.GetInt   ("BloodShieldUnlocked", 0) == 1;
        BloodShieldHP       = PlayerPrefs.GetFloat ("BloodShieldHP",       0f);
        TotalEnemiesKilled  = PlayerPrefs.GetInt   ("TotalEnemiesKilled",  0);
        TotalSoldiersLost   = PlayerPrefs.GetInt   ("TotalSoldiersLost",   0);
        HealSelfUnlocked    = PlayerPrefs.GetInt   ("HealSelfUnlocked",    0) == 1;
        PrestigeCount       = PlayerPrefs.GetInt   ("PrestigeCount",       0);
        PrestigePoints      = PlayerPrefs.GetInt   ("PrestigePoints",      0);
        PSoldierCapLevel    = PlayerPrefs.GetInt   ("PSoldierCapLevel",    0);
        PClickBonusLevel    = PlayerPrefs.GetInt   ("PClickBonusLevel",    0);
        PRitualEffLevel     = PlayerPrefs.GetInt   ("PRitualEffLevel",     0);
        PWeaponHeadStartLevel = PlayerPrefs.GetInt ("PWeaponHeadStartLevel", 0);
        PBloodTitheLevel    = PlayerPrefs.GetInt   ("PBloodTitheLevel",    0);
        PIronWallLevel      = PlayerPrefs.GetInt   ("PIronWallLevel",      0);
        WeaponLevel         = PlayerPrefs.GetInt   ("WeaponLevel",         0);
        ArmorLevel          = PlayerPrefs.GetInt   ("ArmorLevel",          0);
        TalismanLevel       = PlayerPrefs.GetInt   ("TalismanLevel",       0);
        BerserkerFront      = PlayerPrefs.GetInt   ("BerserkerFront",      0) == 1;
        FortificationLevel  = PlayerPrefs.GetInt   ("FortificationLevel",  0);
        FortificationCost   = double.Parse(PlayerPrefs.GetString("FortificationCost", FortBaseCost.ToString("R", ic)), ic);
        SoulShards          = double.Parse(PlayerPrefs.GetString("SoulShards",        "0"), ic);
        SoulShardShopUnlocked = PlayerPrefs.GetInt ("SoulShardShopUnlocked", 0) == 1;
        SSBossTimerLevel    = PlayerPrefs.GetInt   ("SSBossTimerLevel",    0);
        SSDoubleChestLevel  = PlayerPrefs.GetInt   ("SSDoubleChestLevel",  0);
        SSRollbackLevel     = PlayerPrefs.GetInt   ("SSRollbackLevel",     0);
        SSBloodTapLevel     = PlayerPrefs.GetInt   ("SSBloodTapLevel",     0);
        SurgeUpgradeLevel   = PlayerPrefs.GetInt   ("SurgeUpgradeLevel",   0);
        HealUpgradeLevel    = PlayerPrefs.GetInt   ("HealUpgradeLevel",    0);
        SoundEnabled        = PlayerPrefs.GetInt   ("SoundEnabled",        1) == 1;
        NotificationsEnabled = PlayerPrefs.GetInt  ("NotificationsEnabled",1) == 1;
        BloodBankDeposit    = double.Parse(PlayerPrefs.GetString("BloodBankDeposit", "0"), ic);
        BloodBankAccrued    = double.Parse(PlayerPrefs.GetString("BloodBankAccrued", "0"), ic);
        WaveStreak          = PlayerPrefs.GetInt   ("WaveStreak",          0);
        SurgeUnlocked       = TotalBloodEarned >= SurgeUnlockThreshold;
        TotalEnemiesKilled  = PlayerPrefs.GetInt   ("TotalEnemiesKilled",  0);
        TimePlayed          = double.Parse(PlayerPrefs.GetString("TimePlayed", "0"), ic);
        Achievements        = (AchievementFlags)PlayerPrefs.GetInt("Achievements", 0);
        EnemyHP             = PlayerPrefs.GetFloat ("EnemyHP",             100f);
        EnemyMaxHP          = PlayerPrefs.GetFloat ("EnemyMaxHP",          100f);
        EnemyName           = PlayerPrefs.GetString("EnemyName",           "Goblin");
        EnemyAttack         = PlayerPrefs.GetFloat ("EnemyAttack",         3f);
        EnemySpriteIndex    = PlayerPrefs.GetInt   ("EnemySpriteIndex",    0);
        int savedNext       = PlayerPrefs.GetInt   ("NextBossWave",        0);
        NextBossWave        = savedNext > 0 ? savedNext : Wave + UnityEngine.Random.Range(5, 11);
        BossTimeRemaining   = IsBossWave ? (BossTimeLimit + SSBossTimerLevel * 15f) : 0f;

        Talents         = (TalentFlags)PlayerPrefs.GetInt("Talents",         0);
        CorruptionLevel =              PlayerPrefs.GetInt("CorruptionLevel", 0);

        string today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string savedDate      = PlayerPrefs.GetString("LastLoginDate",    "");
        string lastChallenge  = PlayerPrefs.GetString("LastChallengeDate","");
        DailyBonusAvailable     = savedDate     != today;
        DailyChallengeAvailable = lastChallenge != today;

        if (BloodPerSec > 0 && PlayerPrefs.HasKey("SaveTime"))
        {
            var styles = System.Globalization.DateTimeStyles.RoundtripKind;
            if (DateTime.TryParse(PlayerPrefs.GetString("SaveTime"), null, styles, out DateTime lastSave))
            {
                double secs = Math.Min((DateTime.UtcNow - lastSave).TotalSeconds, 8 * 3600);
                if (WorkerCount > 0)
                {
                    OfflineWoodEarned = WorkerCount * WorkerWoodPerSec * secs;
                    Wood += OfflineWoodEarned;
                }
                OfflineBloodEarned = BloodPerSec * secs;
                Blood += OfflineBloodEarned;
                TotalBloodEarned += OfflineBloodEarned;
            }
        }
        else if (WorkerCount > 0 && PlayerPrefs.HasKey("SaveTime"))
        {
            var styles = System.Globalization.DateTimeStyles.RoundtripKind;
            if (DateTime.TryParse(PlayerPrefs.GetString("SaveTime"), null, styles, out DateTime lastSave))
            {
                double secs = Math.Min((DateTime.UtcNow - lastSave).TotalSeconds, 8 * 3600);
                OfflineWoodEarned = WorkerCount * WorkerWoodPerSec * secs;
                Wood += OfflineWoodEarned;
            }
        }
    }

    public void ClearOfflineEarnings()
    {
        OfflineWoodEarned  = 0;
        OfflineBloodEarned = 0;
    }

    public static string FormatNumber(double value)
    {
        if (value >= 1_000_000_000) return $"{value / 1_000_000_000:F1}B";
        if (value >= 1_000_000)     return $"{value / 1_000_000:F1}M";
        if (value >= 1_000)         return $"{value / 1_000:F1}K";
        return $"{Math.Floor(value)}";
    }

    public static string FormatHP(float value) => Mathf.CeilToInt(value).ToString();
}
