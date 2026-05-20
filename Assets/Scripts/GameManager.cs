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
    Wave50        = 1 << 9,
    Blood100K     = 1 << 10,
    Untouchable   = 1 << 11,
    Prestige3     = 1 << 12,
    Wave100       = 1 << 13,
    BloodMillion  = 1 << 14,
    BossSlayer    = 1 << 15,
    BloodBillion  = 1 << 16,
    Wave200       = 1 << 17,
}

public enum EnemyModifier { None, Armored, Enraged, Regen, Cursed, Spectral }
public enum BossAbility    { None, Shield, Berserk, Drain }
public enum QuestTrackType { Kills, Farms, Wave, Spells }

public struct DailyQuestDef
{
    public string        Desc;
    public QuestTrackType TrackType;
    public int           Target;
    public double        BloodReward;
    public double        ShardReward;
}

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
    EchoMastery  = 1 << 6,  // Blood Echo lasts 8 waves instead of 5
    Bloodlust    = 1 << 7,  // heal frontline 5% of enemy max HP on each kill
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // --- Blood ---
    public double Blood { get; private set; }
    public double TotalBloodEarned { get; private set; }
    public const double BloodPerClick = 1.0;
    public double EffectiveBloodPerClick => (BloodPerClick + PClickBonusLevel * 0.5 + ClickPowerLevel + AchievementClickBonus) * PrestigeMultiplier;
    public const double ClickPowerBaseCost  = 15.0;
    public const double ClickPowerCostMult  = 2.0;
    public const int    ClickPowerMaxLevel  = 5;
    public int    ClickPowerLevel { get; private set; }
    public double ClickPowerCost  { get; private set; } = ClickPowerBaseCost;
    public bool   ClickPowerUnlocked => WorkerCount > 0;

    // --- Click Combo ---
    public const float  ComboWindowSecs    = 0.5f;
    public const float  ComboBonusPerStack = 0.20f;
    public const int    ComboMaxStacks     = 10;
    float _comboTimer;
    public int   ComboStacks { get; private set; }
    public float ComboMult   => 1f + ComboStacks * ComboBonusPerStack;

    public const int    EchoTapInterval  = 5;
    int _tapCount;
    public bool NextTapIsEcho => (_tapCount + 1) % EchoTapInterval == 0;

    // --- Wood & Workers ---
    public double Wood { get; private set; }
    public int WorkerCount { get; private set; }
    public double WorkerEfficiencyMult => 1.0 + Math.Floor(WorkerCount / 5.0) * 0.1;
    public const double KillIncomeRate  = 0.01;
    public double KillIncomePerSec      => TotalEnemiesKilled * KillIncomeRate;
    public bool   IsBloodyWave          => Wave > 0 && Wave % 10 == 0 && !IsBossWave;
    public const double BloodMoonMult   = 2.0;
    public double WaveBloodPreview
    {
        get
        {
            double r = Math.Floor(25 * Math.Pow(1.4, Wave - 1) * PrestigeMultiplier * (1.0 + EquipTalismanBonus));
            if (CurrentEnemyModifier == EnemyModifier.Cursed)   r = Math.Floor(r * EnemyCursedRewardMult);
            if (CurrentEnemyModifier == EnemyModifier.Spectral) r = Math.Floor(r * EnemySpectralRewardMult);
            if (IsBloodyWave)  r = Math.Floor(r * BloodMoonMult);
            if (_isBountyEnemy) r = Math.Floor(r * EffectiveBountyMult);
            if (_isEliteEnemy)  r = Math.Floor(r * EliteRewardMult);
            if (IsBossWave)    r *= 3;
            if (HasTalent(TalentFlags.BloodFrenzy)) r = Math.Floor(r * (1.0 + TalentBloodFrenzyBonus));
            r = Math.Floor(r * StreakMultiplier * KillStreakBonusMult);
            if (SoulHarvestUnlocked) r += Math.Floor(EnemyMaxHP * SoulHarvestPct);
            return r;
        }
    }
    public const float  BloodMoonAtkMult= 1.20f;
    public const float  BossVictoryHealPct = 0.25f;
    public bool   IsBountyWave          => Wave % 10 == 5 && !IsBossWave;
    public const double BountyHPMult    = 2.0;
    public const double BountyRewardMult= 3.0;
    public bool   IsEliteWave           => Wave % 17 == 8 && !IsBossWave && !IsBloodyWave && !IsBountyWave;
    public const double EliteHPMult     = 1.75;
    public const float  EliteAtkMult    = 1.25f;
    public const double EliteRewardMult = 2.0;
    public const double FortWoodPerSec  = 0.1;
    public double WoodPerSecond        => WorkerCount * WorkerWoodPerSec * WorkerEfficiencyMult
                                        + FortificationLevel * FortWoodPerSec;
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
    public float TotalAttack         => (TankCount     * (SoldierAttack   + EquipAttackBonus + VeteranAttackBonus + AchievementAttackBonus)
                                       + BerserkerCount * (BerserkerAttack + EquipAttackBonus + VeteranAttackBonus + AchievementAttackBonus)
                                       + PaladinCount   * (PaladinAttack   + EquipAttackBonus + VeteranAttackBonus + AchievementAttackBonus))
                                       * (1f + PrestigeMilestoneDmgBonus) * MoraleBonusMult;
    public float EffectiveAttack     => TotalAttack
                                       * (SurgeActive        ? SurgeMultiplier   : 1f)
                                       * (WarCryActive       ? WarCryMult        : 1f)
                                       * AdrenalineMult * IdleFuryMult
                                       * (IsBloodyWave        ? BloodMoonAtkMult  : 1f)
                                       * (BloodEchoCount > 0  ? (1f + BloodEchoAtkBonus) : 1f)
                                       * (DesperationActive   ? DesperationMult   : 1f)
                                       * (LastStandActive     ? LastStandMult     : 1f)
                                       * (PackTacticsActive   ? PackTacticsMult   : 1f);
    public int    BloodEchoCount     { get; private set; }
    public const int   BloodEchoWaves        = 5;
    public const int   TalentEchoWaves       = 8;
    public const float BloodEchoAtkBonus     = 0.25f;
    public const float DesperationThreshold  = 0.25f;
    public const float DesperationMult       = 1.50f;
    public bool   DesperationActive  => SoldierHP > 0 && SoldierHP < FrontlineMaxHP * DesperationThreshold;
    public const int VeteranAttackCap = 10;
    public float VeteranAttackBonus { get; private set; }
    public bool  IsAllTank          => TankCount > 0 && BerserkerCount == 0 && PaladinCount == 0;
    public bool  IsAllBerserker     => BerserkerCount > 0 && TankCount == 0 && PaladinCount == 0;
    public bool  IsAllPaladin       => PaladinCount > 0 && TankCount == 0 && BerserkerCount == 0;
    public bool  IsMixedArmy        => SoldierCount > 0 && !IsAllTank && !IsAllBerserker && !IsAllPaladin;
    public bool  PackTacticsActive  => TankCount > 0 && BerserkerCount > 0 && PaladinCount > 0;
    public const float PackTacticsMult = 1.15f;

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
    public double BloodPerSec      => (BloodRitualCount * (BloodRitualBloodPerSec + PRitualEffLevel * 0.5) * PrestigeMultiplier
                                    * (HasTalent(TalentFlags.Glutton) ? TalentGluttonMult : 1f)
                                    + BloodTithePerSec + BloodTapPerSec + KillIncomePerSec
                                    + ShrineCount * ShrineBloodPerSec) * AchievementBloodIncomeMult * AdBoostMult;
    public const double ShrineWoodCost   = 20.0;
    public const double ShrineBloodPerSec = 0.5;
    public const int    ShrineMaxCount   = 3;
    public int ShrineCount { get; private set; }
    public bool ShrineUnlocked => WorkerCount > 0;

    // --- Blood Well ---
    public int    BloodWellCount   { get; private set; }
    public const int    BloodWellMaxCount   = 5;
    public const double BloodWellBaseCost   = 20.0;
    public double BloodWellCost    { get; private set; } = BloodWellBaseCost;
    public const double BloodWellWoodPerSec = 0.5;
    public const double BloodWellBloodRatio = 4.0;
    public double BloodWellBloodPerSec => BloodWellCount * BloodWellWoodPerSec * BloodWellBloodRatio;
    public bool   BloodWellUnlocked    => WorkerCount >= 3 && Wave >= 8;

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
    public int TotalBossesKilled  { get; private set; }

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
    public int PBountyBonusLevel     { get; private set; }
    public double EffectiveBountyMult => BountyRewardMult + PBountyBonusLevel;
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
    public int    SSBloodTapLevel       { get; private set; }
    public double BloodTapPerSec        => SSBloodTapLevel * 1.0 * PrestigeMultiplier;
    public int    SSShardHungerLevel    { get; private set; }
    public const double SSShardHungerBonus = 0.20;

    // --- Blood Bank ---
    public double BloodBankDeposit { get; private set; }
    public double BloodBankAccrued { get; private set; }
    public const double BankInterestRatePerHour = 0.02;
    public const double BankMaxDepositBase      = 10_000.0;
    public double BankMaxDeposit => BankMaxDepositBase * Math.Pow(10.0, PrestigeCount * 0.5);

    // --- Wave Streak ---
    public int   WaveStreak          { get; private set; }
    public int   BestWave            { get; private set; }
    public int   BestStreak          { get; private set; }
    public float StreakMultiplier    => Mathf.Min(1f + WaveStreak * 0.1f, 3f);
    public const float MaxStreakMultiplier = 3f;
    public float KillStreakBonusMult => 1f + Mathf.Min(WaveStreak, 5) * 0.05f;
    public float MoraleBonusMult     => WaveStreak >= 3 ? 1.15f : 1f;
    public bool  LastStandActive      => SoldierCount == 1;
    public const float LastStandMult  = 2.00f;
    public const float DeathsDoorThresh = 0.15f;
    public const float DeathsDoorMult   = 1.50f;
    public bool  DeathsDoorActive     => LastStandActive && SoldierHP < FrontlineMaxHP * DeathsDoorThresh;
    public const float MeditationThreshold = 5f;
    public const float MeditationMult      = 3f;
    float _meditationTimer;
    public bool MeditationReady => _meditationTimer >= MeditationThreshold && SoldierCount > 0 && EnemyHP > 0;
    public const float AdrenalineBonus    = 0.10f;
    public const float AdrenalineDuration = 15f;
    public const int   AdrenalineMaxStack = 3;
    int   _adrenalineStacks;
    float _adrenalineTimer;
    public int   AdrenalineStacks   => _adrenalineStacks;
    public float AdrenalineTimeLeft => _adrenalineTimer;
    public float AdrenalineMult     => 1f + _adrenalineStacks * AdrenalineBonus;

    // --- Idle Fury ---
    public const float IdleFuryStepSecs   = 5f;
    public const float IdleFuryAtkBonus   = 0.20f;
    public const int   IdleFuryMaxStacks  = 5;
    float _idleTimer;
    public int   IdleFuryStacks => Mathf.Min((int)(_idleTimer / IdleFuryStepSecs), IdleFuryMaxStacks);
    public float IdleFuryMult   => 1f + IdleFuryStacks * IdleFuryAtkBonus;

    // --- Blood Storm ---
    public const double BloodStormCost       = 50.0;
    public const float  BloodStormBaseDmg    = 50f;
    public const float  BloodStormDmgPerWave = 10f;
    public const float  BloodStormCooldown   = 30f;
    public const int    BloodStormUnlockWave = 8;
    float _bloodStormTimer;
    public bool  BloodStormReady       => _bloodStormTimer <= 0f;
    public float BloodStormCooldownLeft => _bloodStormTimer;
    public bool  BloodStormUnlocked    => Wave >= BloodStormUnlockWave;

    // --- Prestige Talent Tree ---
    public TalentFlags   Talents              { get; private set; }
    public bool          PendingPrestige      { get; private set; }
    public TalentFlags[] PendingTalentChoices { get; private set; } = new TalentFlags[0];
    public bool HasTalent(TalentFlags t)      => (Talents & t) != 0;
    public const float  TalentIronSkinHP       = 15f;
    public const double TalentBloodFrenzyBonus = 0.25;
    public const float  TalentGluttonMult      = 1.25f;
    public const float  TalentBloodlustHealPct = 0.05f;

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

    // --- Desecrate spell ---
    public const double DesecrateBloodCost = 100.0;
    public const float  DesecrateDmgPct    = 0.50f;
    public const float  DesecrateCooldown  = 30f;
    float _desecrateTimer;
    public bool  DesecrateReady    => _desecrateTimer <= 0f;
    public float DesecrateCooldownLeft => _desecrateTimer;
    public bool  DesecrateUnlocked => PrestigeCount >= 1;
    public bool  DesecrateCanCast  => DesecrateUnlocked && CorruptionLevel > 0 && Blood >= DesecrateBloodCost && DesecrateReady && EnemyHP > 0;

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

    // --- Blood Oath spell ---
    public bool  BloodOathActive        { get; private set; }
    public float BloodOathTimeRemaining { get; private set; }
    public const double BloodOathCost        = 200.0;
    public const float  BloodOathDuration    = 20f;
    public const float  BloodOathAtkMult     = 4.0f;
    public const float  BloodOathReflectPct  = 0.50f;
    public const float  BloodOathCooldown    = 60f;
    public const int    BloodOathUnlockWave  = 15;
    float _bloodOathTimer;
    public bool  BloodOathReady    => _bloodOathTimer <= 0f;
    public float BloodOathCooldownLeft => _bloodOathTimer;
    public bool  BloodOathUnlocked => Wave >= BloodOathUnlockWave;
    public bool  BloodOathCanCast  => BloodOathUnlocked && !BloodOathActive && BloodOathReady && Blood >= BloodOathCost && SoldierCount > 0 && EnemyHP > 0;

    // --- Ad Boost & IAP ---
    public const float  AdBoostDuration   = 300f;   // 5 minutes per rewarded ad
    public const double AdBoostMultiplier = 2.0;
    public bool  AdBoostActive        { get; private set; }
    public float AdBoostTimeRemaining { get; private set; }
    public bool  AdsRemoved           { get; private set; }
    public bool  StarterPackOwned     { get; private set; }
    public double AdBoostMult         => AdBoostActive ? AdBoostMultiplier : 1.0;

    // --- Daily Quests ---
    public const int DailyQuestCount = 3;
    public static readonly DailyQuestDef[] QuestPool =
    {
        new DailyQuestDef { Desc="Kill 10 enemies",      TrackType=QuestTrackType.Kills,  Target=10,  BloodReward=50,  ShardReward=0 },
        new DailyQuestDef { Desc="Kill 25 enemies",      TrackType=QuestTrackType.Kills,  Target=25,  BloodReward=120, ShardReward=0 },
        new DailyQuestDef { Desc="Kill 50 enemies",      TrackType=QuestTrackType.Kills,  Target=50,  BloodReward=300, ShardReward=1 },
        new DailyQuestDef { Desc="Kill 100 enemies",     TrackType=QuestTrackType.Kills,  Target=100, BloodReward=600, ShardReward=2 },
        new DailyQuestDef { Desc="Farm Blood 20 times",  TrackType=QuestTrackType.Farms,  Target=20,  BloodReward=40,  ShardReward=0 },
        new DailyQuestDef { Desc="Farm Blood 50 times",  TrackType=QuestTrackType.Farms,  Target=50,  BloodReward=100, ShardReward=0 },
        new DailyQuestDef { Desc="Farm Blood 100 times", TrackType=QuestTrackType.Farms,  Target=100, BloodReward=250, ShardReward=1 },
        new DailyQuestDef { Desc="Reach wave 5",         TrackType=QuestTrackType.Wave,   Target=5,   BloodReward=60,  ShardReward=0 },
        new DailyQuestDef { Desc="Reach wave 10",        TrackType=QuestTrackType.Wave,   Target=10,  BloodReward=150, ShardReward=0 },
        new DailyQuestDef { Desc="Reach wave 20",        TrackType=QuestTrackType.Wave,   Target=20,  BloodReward=400, ShardReward=2 },
        new DailyQuestDef { Desc="Use a spell 3 times",  TrackType=QuestTrackType.Spells, Target=3,   BloodReward=80,  ShardReward=0 },
        new DailyQuestDef { Desc="Use a spell 10 times", TrackType=QuestTrackType.Spells, Target=10,  BloodReward=200, ShardReward=1 },
    };

    public int[]  DailyQuestIndices  { get; private set; } = new int[DailyQuestCount];
    public int[]  DailyQuestProgress { get; private set; } = new int[DailyQuestCount];
    public bool[] DailyQuestClaimed  { get; private set; } = new bool[DailyQuestCount];
    public bool   DailyQuestsReady   { get; private set; }

    int    _dailyKillCount;
    int    _dailyFarmCount;
    int    _dailySpellCount;
    string _questDate = "";
    float  _dailyCheckTimer;
    const float DailyCheckInterval = 60f;

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
        EnemyModifier.Armored  => "⚔ Armored",
        EnemyModifier.Enraged  => "💢 Enraged",
        EnemyModifier.Regen    => "♻ Regen",
        EnemyModifier.Cursed   => "☠ Cursed",
        EnemyModifier.Spectral => "👻 Spectral",
        _                     => "",
    };
    public const float EnemyArmoredDmgMult    = 0.5f;
    public const float EnemyEnragedAtkMult    = 1.5f;
    public const float EnemyRegenPct          = 0.02f;
    public const float RegenLeechRate         = 0.5f;
    public const float EnemyCursedDotRate     = 2f;
    public const float EnemyCursedRewardMult  = 1.5f;
    public const float EnragedDeathBlowMult   = 0.5f;
    public const float EnemySpectralHPMult    = 0.7f;
    public const float EnemySpectralAtkMult   = 1.5f;
    public const float EnemySpectralRewardMult = 1.25f;

    // --- Wave preview ---
    public bool WavePreviewActive { get; private set; }
    float _previewTimer;
    const float WavePreviewDuration = 3f;

    // --- Flawless wave ---
    public const float FlawlessThreshold = 10f;
    public bool FlawlessActive => _flawlessTimer <= FlawlessThreshold && EnemyHP > 0 && !WavePreviewActive;
    float _flawlessTimer;
    bool  _undyingUsedThisWave;
    public bool UndyingAvailable => HasTalent(TalentFlags.Undying) && !_undyingUsedThisWave;
    bool  _isBountyEnemy;
    bool  _isEliteEnemy;

    // --- Settings ---
    public bool SoundEnabled         { get; private set; } = true;
    public bool NotificationsEnabled { get; private set; } = true;
    bool _resetPending;

    // --- Prestige Milestones ---
    static readonly int[] k_PrestigeMilestones = { 5, 10, 20, 50 };
    public int   PrestigeMilestonesReached { get { int c = 0; foreach (var m in k_PrestigeMilestones) if (PrestigeCount >= m) c++; return c; } }
    public float PrestigeMilestoneDmgBonus => PrestigeMilestonesReached * MilestoneDmgBonusPerLevel;
    public const float MilestoneDmgBonusPerLevel  = 0.05f;
    public const float MilestoneHPReductionPerLevel = 0.05f;
    public float PrestigeMilestoneHPReduction => Mathf.Min(PrestigeMilestonesReached * MilestoneHPReductionPerLevel, 0.40f);

    // --- Daily login bonus ---
    public bool DailyBonusAvailable { get; private set; }
    public const float DailyBonusMultiplier = 10f;

    // --- Statistics ---
    public double TimePlayed         { get; private set; }

    // --- Achievements ---
    public AchievementFlags Achievements { get; private set; }
    bool HasAchievement(AchievementFlags f) => (Achievements & f) != 0;

    public double AchievementBloodIncomeMult =>
        1.0
        + (HasAchievement(AchievementFlags.Wave10)        ? 0.05 : 0)
        + (HasAchievement(AchievementFlags.Wave25)        ? 0.05 : 0)
        + (HasAchievement(AchievementFlags.Wave50)        ? 0.10 : 0)
        + (HasAchievement(AchievementFlags.Wave100)       ? 0.15 : 0)
        + (HasAchievement(AchievementFlags.Untouchable)   ? 0.05 : 0)
        + (HasAchievement(AchievementFlags.BloodBillion)  ? 0.20 : 0);

    public double AchievementClickBonus =>
        (HasAchievement(AchievementFlags.Blood1K)         ? 0.5  : 0)
        + (HasAchievement(AchievementFlags.Blood10K)      ? 1.0  : 0)
        + (HasAchievement(AchievementFlags.Blood100K)     ? 2.0  : 0)
        + (HasAchievement(AchievementFlags.BloodMillion)  ? 3.0  : 0)
        + (HasAchievement(AchievementFlags.BloodBillion)  ? 5.0  : 0);

    public float AchievementAttackBonus =>
        (HasAchievement(AchievementFlags.FirstSoldier)    ? 1f  : 0f)
        + (HasAchievement(AchievementFlags.FullLegion)    ? 2f  : 0f)
        + (HasAchievement(AchievementFlags.BossSlayer)    ? 3f  : 0f)
        + (HasAchievement(AchievementFlags.Wave200)       ? 5f  : 0f)
        + (HasAchievement(AchievementFlags.FirstPrestige)? 2f  : 0f)
        + (HasAchievement(AchievementFlags.Prestige3)    ? 5f  : 0f);
    public event Action<AchievementFlags> OnAchievementUnlocked;

    // --- Offline earnings ---
    public double OfflineWoodEarned    { get; private set; }
    public double OfflineBloodEarned   { get; private set; }
    public double OfflineBankInterest  { get; private set; }

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
    public bool   AutoSurge           { get; private set; }
    public bool   AutoHeal            { get; private set; }
    public const float AutoHealThreshold = 0.5f;
    public bool   AutoStorm           { get; private set; }
    public bool   AutoDesecrate       { get; private set; }
    public bool   AutoBuyRituals      { get; private set; }
    public bool   AutoBankDeposit     { get; private set; }
    public bool   AutoWarCry          { get; private set; }
    public bool   AutoHexCurse        { get; private set; }
    public bool   AutoBloodOath       { get; private set; }
    public bool   AutoBloodShield     { get; private set; }
    public const float AutoBloodShieldThreshold = 0.5f;
    public const double TruceCost        = 100.0;
    public const double WarCryCost         = 30.0;
    public const float  WarCryDuration     = 10f;
    public const float  WarCryMult         = 2f;
    public const int    WarCryUnlockWave   = 5;
    float _warCryTimer;
    public bool  WarCryActive    => _warCryTimer > 0f;
    public float WarCryTimeLeft  => _warCryTimer;
    public bool  WarCryUnlocked      => Wave >= WarCryUnlockWave;
    public const double HexCurseCost        = 20.0;
    public const float  HexCurseDuration    = 15f;
    public const float  HexCurseAtkReduction = 0.20f;
    public const int    HexCurseUnlockWave  = 4;
    float _hexCurseTimer;
    public bool  HexCurseActive   => _hexCurseTimer > 0f;
    public float HexCurseTimeLeft => _hexCurseTimer;
    public bool  HexCurseUnlocked => Wave >= HexCurseUnlockWave;
    public const float CursedBloodConversionRate = 0.10f;
    public const int   CursedBloodUnlockWave     = 7;
    public bool CursedBloodEnabled { get; private set; }
    public bool CursedBloodUnlocked => Wave >= CursedBloodUnlockWave;
    public void ToggleCursedBlood() { if (CursedBloodUnlocked) { CursedBloodEnabled = !CursedBloodEnabled; OnStateChanged?.Invoke(); } }
    public const float SiphonRate    = 0.10f;
    public const int   SiphonUnlockWave = 6;
    public bool SiphonUnlocked       => Wave >= SiphonUnlockWave;
    public const double SoulHarvestPct        = 0.01;
    public const int    SoulHarvestUnlockWave = 10;
    public bool SoulHarvestUnlocked => TotalEnemiesKilled >= 10;
    bool _crimsonPactCharged;
    public bool CrimsonPactCharged => _crimsonPactCharged;
    public const float SacrificeDmgMult = 3f;
    public bool SacrificeUnlocked       => Wave >= 3 && SoldierCount >= 2;

    // --- Tutorial ---
    public int    TutorialProgress { get; private set; }
    public bool   TutorialActive   { get; private set; }
    public string TutorialTitle    { get; private set; } = "";
    public string TutorialBody     { get; private set; } = "";

    public event Action OnStateChanged;
    public event Action<float, bool> OnDamageDealt;
    public event Action<string> OnMilestoneChest;

    float _dmgTimer;
    const float DmgTickInterval = 0.4f;

    AudioSource _audio;
    AudioClip   _clipFarm, _clipKill, _clipBossKill;

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
        (AchievementFlags.Wave50,        1000.0, 0),
        (AchievementFlags.Blood100K,     1000.0, 0),
        (AchievementFlags.Untouchable,   500.0,  0),
        (AchievementFlags.Prestige3,     0.0,    1),
        (AchievementFlags.Wave100,       2000.0, 0),
        (AchievementFlags.BloodMillion,  2000.0, 0),
        (AchievementFlags.BossSlayer,    0.0,    1),
        (AchievementFlags.BloodBillion,  5000.0, 1),
        (AchievementFlags.Wave200,       5000.0, 1),
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
    public void SetBestWaveForTest(int w)                    => BestWave = w;
    public void SetBestStreakForTest(int s)                  => BestStreak = s;
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
    public void ClearOfflineEarningsForTest()                            { OfflineWoodEarned = 0; OfflineBloodEarned = 0; OfflineBankInterest = 0; }
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

        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _clipFarm     = Resources.Load<AudioClip>("Audio/blood_farm");
        _clipKill     = Resources.Load<AudioClip>("Audio/enemy_kill");
        _clipBossKill = Resources.Load<AudioClip>("Audio/boss_kill");
        CheckTutorial();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool changed = false;

        TimePlayed += dt;

        _dailyCheckTimer -= dt;
        if (_dailyCheckTimer <= 0f)
        {
            _dailyCheckTimer = DailyCheckInterval;
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (DailyQuestsReady && today != _questDate)
            {
                GenerateDailyQuests(today);
                changed = true;
            }
        }

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

        if (BloodWellCount > 0 && Wood > 0)
        {
            double consumed = Math.Min(BloodWellCount * BloodWellWoodPerSec * dt, Wood);
            Wood    -= consumed;
            AddBlood(consumed * BloodWellBloodRatio);
            changed  = true;
        }

        if (AdBoostActive)
        {
            AdBoostTimeRemaining -= dt;
            if (AdBoostTimeRemaining <= 0f) { AdBoostActive = false; AdBoostTimeRemaining = 0f; }
            changed = true;
        }

        if (AutoBuySoldiers && SoldierCount < MaxSoldiers && Blood >= SoldierCost)
        {
            BuyTank();
            changed = true;
        }

        if (AutoSurge && SurgeUnlocked && !SurgeActive && Blood >= SurgeCost && SoldierCount > 0 && EnemyHP > 0)
        {
            UseSurge();
            changed = true;
        }

        if (AutoHeal && HealSelfUnlocked && SoldierCount > 0 && Blood >= HealSelfCost
            && SoldierHP < FrontlineMaxHP * AutoHealThreshold)
        {
            UseHealSelf();
            changed = true;
        }

        if (AutoStorm && BloodStormUnlocked && BloodStormReady && Blood >= BloodStormCost && SoldierCount > 0 && EnemyHP > 0)
        {
            UseBloodStorm();
            changed = true;
        }

        if (AutoDesecrate && DesecrateCanCast)
        {
            UseDesecrate();
            changed = true;
        }

        if (AutoBuyRituals && WorkersUnlocked && Wood >= BloodRitualCost)
        {
            BuyBloodRitual();
            changed = true;
        }

        if (AutoBankDeposit && BloodBankDeposit < BankMaxDeposit && Blood > 0)
        {
            DepositToBank(Math.Floor(Blood * 0.1));
            changed = true;
        }

        if (AutoWarCry && WarCryUnlocked && !WarCryActive && Blood >= WarCryCost && SoldierCount > 0 && EnemyHP > 0)
        {
            UseWarCry();
            changed = true;
        }

        if (AutoHexCurse && HexCurseUnlocked && !HexCurseActive && Blood >= HexCurseCost && EnemyHP > 0)
        {
            UseHexCurse();
            changed = true;
        }

        if (AutoBloodOath && BloodOathCanCast)
        {
            UseBloodOath();
            changed = true;
        }

        if (AutoBloodShield && BloodShieldUnlocked && BloodShieldHP <= 0 && SoldierCount > 0
            && Blood >= BloodShieldCost && SoldierHP < FrontlineMaxHP * AutoBloodShieldThreshold)
        {
            UseBloodShield();
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
        CheckTutorial();
    }

    bool RunCombat(float dt)
    {
        if (CurrentEnemyModifier == EnemyModifier.Cursed && SoldierCount > 0)
            SoldierHP = Mathf.Max(0f, SoldierHP - EnemyCursedDotRate * dt);

        if (_warCryTimer > 0f) { _warCryTimer -= dt; if (_warCryTimer < 0f) _warCryTimer = 0f; }
        if (_hexCurseTimer > 0f) { _hexCurseTimer -= dt; if (_hexCurseTimer < 0f) _hexCurseTimer = 0f; }
        if (SoldierCount > 0) _meditationTimer += dt;
        if (_adrenalineTimer > 0f) { _adrenalineTimer -= dt; if (_adrenalineTimer <= 0f) _adrenalineStacks = 0; }
        _idleTimer += dt;
        if (_bloodStormTimer  > 0f) { _bloodStormTimer  -= dt; if (_bloodStormTimer  < 0f) _bloodStormTimer  = 0f; }
        if (_desecrateTimer   > 0f) { _desecrateTimer   -= dt; if (_desecrateTimer   < 0f) _desecrateTimer   = 0f; }
        if (_bloodOathTimer   > 0f) { _bloodOathTimer   -= dt; if (_bloodOathTimer   < 0f) _bloodOathTimer   = 0f; }
        if (BloodOathActive)
        {
            BloodOathTimeRemaining -= dt;
            if (BloodOathTimeRemaining <= 0f) { BloodOathActive = false; BloodOathTimeRemaining = 0f; }
        }
        if (_comboTimer > 0f)
        {
            _comboTimer -= dt;
            if (_comboTimer <= 0f) { _comboTimer = 0f; ComboStacks = 0; }
        }
        float eff = TotalAttack * (SurgeActive ? SurgeMultiplier : 1f) * (WarCryActive ? WarCryMult : 1f) * AdrenalineMult * IdleFuryMult * (IsBloodyWave ? BloodMoonAtkMult : 1f) * (BloodEchoCount > 0 ? (1f + BloodEchoAtkBonus) : 1f) * (DesperationActive ? DesperationMult : 1f);
        if (PackTacticsActive)   eff *= PackTacticsMult;
        if (BloodOathActive)     eff *= BloodOathAtkMult;
        if (CurrentEnemyModifier == EnemyModifier.Armored && !IsAllBerserker)
            eff *= IsAllTank ? EnemyArmoredDmgMult + 0.25f : EnemyArmoredDmgMult;
        if (CurrentEnemyModifier == EnemyModifier.Cursed && PaladinCount > 0) eff *= PaladinHolyBonus;
        if (BerserkerRageActive) eff *= BerserkerRageMult;
        if (LastStandActive)     eff *= LastStandMult;
        if (DeathsDoorActive)    eff *= DeathsDoorMult;
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
            if (CurrentEnemyModifier == EnemyModifier.Spectral)
                reward = Math.Floor(reward * EnemySpectralRewardMult);
            if (IsBloodyWave) reward = Math.Floor(reward * BloodMoonMult);
            if (_isBountyEnemy) reward = Math.Floor(reward * EffectiveBountyMult);
            if (_isEliteEnemy)  reward = Math.Floor(reward * EliteRewardMult);
            if (wasBoss)
            {
                reward *= 3;
                if (SSShardHungerLevel > 0) reward = Math.Floor(reward * (1.0 + SSShardHungerLevel * SSShardHungerBonus));
                if (VeteranAttackBonus < VeteranAttackCap) VeteranAttackBonus++;
                SoulShards += HasTalent(TalentFlags.ShardHunter) ? 2 : 1;
                SoulShardShopUnlocked = true;
                BossTimeRemaining = 0f;
                if (SoldierCount > 0)
                {
                    float healAmt = FrontlineMaxHP * BossVictoryHealPct;
                    SoldierHP = Mathf.Min(SoldierHP + healAmt, FrontlineMaxHP);
                }
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
            _dailyKillCount++;
            CheckQuestProgress(QuestTrackType.Kills);
            if (wasBoss) TotalBossesKilled++;
            WaveStreak++;
            if (wasBoss)          BloodEchoCount = HasTalent(TalentFlags.EchoMastery) ? TalentEchoWaves : BloodEchoWaves;
            else if (BloodEchoCount > 0) BloodEchoCount--;
            if (SoulHarvestUnlocked) reward += Math.Floor(EnemyMaxHP * SoulHarvestPct);
            if (HasTalent(TalentFlags.Bloodlust) && SoldierCount > 0)
                SoldierHP = Mathf.Min(SoldierHP + EnemyMaxHP * TalentBloodlustHealPct, FrontlineMaxHP);
            AddBlood(reward);
            if (isFlawless) OnMilestoneChest?.Invoke("⚡ FLAWLESS! ×2 blood!");
            if (_isBountyEnemy) OnMilestoneChest?.Invoke($"★ BOUNTY CLAIMED! +{FormatHP((float)reward)} blood!");
            if ((wasBoss || wasChallenge) && HasTalent(TalentFlags.BloodRush) && !SurgeActive)
            {
                SurgeActive        = true;
                SurgeTimeRemaining = SurgeDurationEffective;
            }

            TotalEnemiesKilled++;
            TryUnlock(AchievementFlags.FirstKill);

            bool isMilestone = (Wave % 5 == 0);
            Wave++;
            CheckQuestProgress(QuestTrackType.Wave);
            if (Wave > BestWave)   BestWave   = Wave;
            if (WaveStreak > BestStreak) BestStreak = WaveStreak;
            if (Wave >= 10)  TryUnlock(AchievementFlags.Wave10);
            if (Wave >= 25)  TryUnlock(AchievementFlags.Wave25);
            if (Wave >= 50)  TryUnlock(AchievementFlags.Wave50);
            if (Wave >= 100) TryUnlock(AchievementFlags.Wave100);
            if (Wave >= 200) TryUnlock(AchievementFlags.Wave200);
            if (WaveStreak >= 10) TryUnlock(AchievementFlags.Untouchable);
            if (TotalBossesKilled >= 25) TryUnlock(AchievementFlags.BossSlayer);

            if (wasBoss) NextBossWave = Wave + UnityEngine.Random.Range(5, 11);
            WavePreviewActive = true;
            _previewTimer = WavePreviewDuration;
            _dmgTimer = 0f;
            PlaySound(wasBoss ? _clipBossKill : _clipKill);

            if (isMilestone) GrantMilestoneChest(Wave - 1);
            return true;
        }

        float incomingAtk   = EnemyAttack * (HexCurseActive ? (1f - HexCurseAtkReduction) : 1f);
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
        if (CursedBloodEnabled && dmg > 0) AddBlood(dmg * CursedBloodConversionRate);
        if (BloodOathActive && dmg > 0 && EnemyHP > 0)
            EnemyHP = Mathf.Max(float.Epsilon, EnemyHP - dmg * BloodOathReflectPct);
        if (dmg > 0) _meditationTimer = 0f;
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
                if (SoldierCount > 0 && _adrenalineStacks < AdrenalineMaxStack) { _adrenalineStacks++; _adrenalineTimer = AdrenalineDuration; }
                SoldierHP  = SoldierCount > 0 ? FrontlineMaxHP : 0f;
                if (WaveStreak > BestStreak) BestStreak = WaveStreak;
                WaveStreak = 0;
            }
        }

        _dmgTimer += dt;
        if (_dmgTimer >= DmgTickInterval)
        {
            _dmgTimer = 0f;
            float tickDmg = eff * DmgTickInterval;
            if (_crimsonPactCharged) { tickDmg *= 2f; _crimsonPactCharged = false; }
            if (MeditationReady) { tickDmg *= MeditationMult; _meditationTimer = 0f; }
            if (BerserkerCount > 0 && TankCount == 0 && UnityEngine.Random.value < BerserkerCritChance)
            {
                tickDmg *= BerserkerCritMult;
                EnemyHP  = Mathf.Max(0f, EnemyHP - tickDmg);
            }
            if (SiphonUnlocked && SoldierCount > 0)
                SoldierHP = Mathf.Min(SoldierHP + tickDmg * SiphonRate, FrontlineMaxHP);
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

    void CheckTutorial()
    {
        if (TutorialActive || TutorialProgress >= 10) return;
        string title, body;
        bool cond;
        switch (TutorialProgress)
        {
            case 0:  cond = true;                          title = "Welcome to Blood Idle!";    body = "Tap Farm Blood to earn blood. Build an army and conquer endless waves!"; break;
            case 1:  cond = Blood >= SoldierCost;          title = "First Soldier Available";   body = "You have enough blood! Buy a Tank to start fighting. Tanks have high HP and shield the army."; break;
            case 2:  cond = SoldierCount >= 1;             title = "Combat Started!";           body = "Your soldier auto-attacks each wave. The enemy retaliates — keep soldiers alive to advance!"; break;
            case 3:  cond = TotalEnemiesKilled >= 1;       title = "First Wave Cleared!";       body = "Blood rewards grow x1.4 each wave. Keep advancing for bigger rewards!"; break;
            case 4:  cond = WorkerCount >= 1;              title = "Workers Online";            body = "Workers generate blood passively — even while away. Stack them for compounding income."; break;
            case 5:  cond = HealSelfUnlocked;              title = "Heal Self Unlocked";        body = "Spend 25 blood to restore your frontline soldier's HP. Pair with Blood Shield for defence."; break;
            case 6:  cond = SurgeUnlocked;                 title = "Blood Surge Unlocked";      body = "Blood Surge boosts attack by 2x for a short burst. Save it for boss waves!"; break;
            case 7:  cond = Wave >= WarCryUnlockWave;      title = "War Cry Available";         body = "War Cry adds extra attack. Stack with Surge against tough enemies."; break;
            case 8:  cond = Wave >= BloodStormUnlockWave;  title = "Blood Storm Unlocked";      body = "Blood Storm deals massive burst damage. Ideal for finishing a nearly-dead boss."; break;
            case 9:  cond = Wave >= PrestigeWaveRequirement; title = "Prestige Ready!";         body = "You can Prestige now! Reset for permanent bonuses, Soul Shards, and new upgrade paths."; break;
            default: return;
        }
        if (!cond) return;
        TutorialTitle  = title;
        TutorialBody   = body;
        TutorialActive = true;
        OnStateChanged?.Invoke();
    }

    public void DismissTutorial()
    {
        TutorialActive = false;
        TutorialProgress++;
        PlayerPrefs.SetInt("TutorialProgress", TutorialProgress);
        PlayerPrefs.Save();
        CheckTutorial();
        OnStateChanged?.Invoke();
    }

    void SpawnEnemy(int wave)
    {
        _flawlessTimer       = 0f;
        _undyingUsedThisWave = false;
        bool isBoss = wave == NextBossWave;
        float fortReduction = 1f - FortificationDmgReduction;
        if (isBoss)
        {
            _isBountyEnemy   = false;
            int idx          = UnityEngine.Random.Range(0, BossNames.Length);
            EnemyName        = BossNames[idx];
            EnemySpriteIndex = 6;
            EnemyMaxHP       = (float)(100 * Math.Pow(1.5, wave - 1) * 5.0 * fortReduction * (1.0 - PrestigeMilestoneHPReduction));
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
            EnemyMaxHP       = (float)(100 * Math.Pow(1.5, wave - 1) * def.HPMult * fortReduction * (1.0 - PrestigeMilestoneHPReduction));
            EnemyHP          = EnemyMaxHP;
            EnemyAttack      = (float)(3   * Math.Pow(1.3, wave - 1) * def.AtkMult);

            _isBountyEnemy = wave % 10 == 5;
            if (_isBountyEnemy)
            {
                EnemyMaxHP *= (float)BountyHPMult;
                EnemyHP     = EnemyMaxHP;
            }

            _isEliteEnemy = IsEliteWave;
            if (_isEliteEnemy)
            {
                EnemyName   = "Elite " + EnemyName;
                EnemyMaxHP *= (float)EliteHPMult;
                EnemyHP     = EnemyMaxHP;
                EnemyAttack *= EliteAtkMult;
            }

            CurrentBossAbility = BossAbility.None;
            BossShieldActive   = false;
            _bossShieldHP      = 0f;
            if (UnityEngine.Random.value < 0.25f)
            {
                CurrentEnemyModifier = (EnemyModifier)UnityEngine.Random.Range(1, 6);
                if (CurrentEnemyModifier == EnemyModifier.Enraged)
                    EnemyAttack *= EnemyEnragedAtkMult;
                else if (CurrentEnemyModifier == EnemyModifier.Spectral)
                {
                    EnemyMaxHP  *= EnemySpectralHPMult;
                    EnemyHP      = EnemyMaxHP;
                    EnemyAttack *= EnemySpectralAtkMult;
                }
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
        _tapCount++;
        _dailyFarmCount++;
        _idleTimer = 0f;
        if (SoldierCount > 0 && EnemyHP > 0) _crimsonPactCharged = true;
        double amount = EffectiveBloodPerClick * ComboMult * (_tapCount % EchoTapInterval == 0 ? 2.0 : 1.0);
        if (ComboStacks < ComboMaxStacks) ComboStacks++;
        _comboTimer = ComboWindowSecs;
        if (DailyBonusAvailable)
        {
            amount *= DailyBonusMultiplier;
            DailyBonusAvailable = false;
            PlayerPrefs.SetString("LastLoginDate", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }
        AddBlood(amount);
        PlaySound(_clipFarm);
        CheckQuestProgress(QuestTrackType.Farms);
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
        if (TotalBloodEarned >= 1_000)       TryUnlock(AchievementFlags.Blood1K);
        if (TotalBloodEarned >= 10_000)      TryUnlock(AchievementFlags.Blood10K);
        if (TotalBloodEarned >= 100_000)     TryUnlock(AchievementFlags.Blood100K);
        if (TotalBloodEarned >= 1_000_000)   TryUnlock(AchievementFlags.BloodMillion);
        if (TotalBloodEarned >= 1_000_000_000) TryUnlock(AchievementFlags.BloodBillion);
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

    void PlaySound(AudioClip clip)
    {
        if (SoundEnabled && clip != null && _audio != null) _audio.PlayOneShot(clip, 0.7f);
    }

    public bool BuySoldier() => BuyTank();
    public void ToggleAutoBuySoldiers() { AutoBuySoldiers = !AutoBuySoldiers; OnStateChanged?.Invoke(); }
    public void ToggleAutoSurge()       { AutoSurge       = !AutoSurge;       OnStateChanged?.Invoke(); }
    public void ToggleAutoHeal()        { AutoHeal        = !AutoHeal;        OnStateChanged?.Invoke(); }
    public void ToggleAutoStorm()       { AutoStorm       = !AutoStorm;       OnStateChanged?.Invoke(); }
    public void ToggleAutoDesecrate()   { AutoDesecrate   = !AutoDesecrate;   OnStateChanged?.Invoke(); }
    public void ToggleAutoRituals()     { AutoBuyRituals  = !AutoBuyRituals;  OnStateChanged?.Invoke(); }
    public void ToggleAutoBankDeposit() { AutoBankDeposit = !AutoBankDeposit; OnStateChanged?.Invoke(); }
    public void ToggleAutoWarCry()      { AutoWarCry      = !AutoWarCry;      OnStateChanged?.Invoke(); }
    public void ToggleAutoHexCurse()    { AutoHexCurse    = !AutoHexCurse;    OnStateChanged?.Invoke(); }
    public void ToggleAutoBloodOath()   { AutoBloodOath   = !AutoBloodOath;   OnStateChanged?.Invoke(); }
    public void ToggleAutoBloodShield() { AutoBloodShield = !AutoBloodShield; OnStateChanged?.Invoke(); }

    public bool UseTruce()
    {
        if (Blood < TruceCost || WavePreviewActive || EnemyHP <= 0) return false;
        Blood -= TruceCost;
        WaveStreak = 0;
        Wave++;
        if (Wave > BestWave) BestWave = Wave;
        if (Wave >= 10)  TryUnlock(AchievementFlags.Wave10);
        if (Wave >= 25)  TryUnlock(AchievementFlags.Wave25);
        if (Wave >= 200) TryUnlock(AchievementFlags.Wave200);
        WavePreviewActive = true;
        _previewTimer     = WavePreviewDuration;
        _dmgTimer         = 0f;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UseSolderSacrifice()
    {
        if (!SacrificeUnlocked || EnemyHP <= 0) return false;
        float burstDmg = FrontlineMaxHP * SacrificeDmgMult;
        if (FrontlineIsTank)           TankCount--;
        else if (FrontlineIsBerserker) BerserkerCount--;
        else                           PaladinCount--;
        TotalSoldiersLost++;
        SoldierHP = SoldierCount > 0 ? FrontlineMaxHP : 0f;
        EnemyHP   = Mathf.Max(0f, EnemyHP - burstDmg);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UseHexCurse()
    {
        if (!HexCurseUnlocked || Blood < HexCurseCost || HexCurseActive || EnemyHP <= 0) return false;
        Blood -= HexCurseCost;
        _hexCurseTimer = HexCurseDuration;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UseWarCry()
    {
        if (!WarCryUnlocked || Blood < WarCryCost || WarCryActive) return false;
        Blood -= WarCryCost;
        _warCryTimer = WarCryDuration;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UseBloodStorm()
    {
        if (!BloodStormUnlocked || !BloodStormReady || Blood < BloodStormCost || EnemyHP <= 0 || SoldierCount == 0) return false;
        Blood -= BloodStormCost;
        float dmg = BloodStormBaseDmg + (Wave - 1) * BloodStormDmgPerWave;
        EnemyHP = Mathf.Max(float.Epsilon, EnemyHP - dmg);
        _bloodStormTimer = BloodStormCooldown;
        _dailySpellCount++;
        CheckQuestProgress(QuestTrackType.Spells);
        OnDamageDealt?.Invoke(dmg, true);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UseDesecrate()
    {
        if (!DesecrateCanCast) return false;
        Blood -= DesecrateBloodCost;
        CorruptionLevel--;
        float dmg = EnemyMaxHP * DesecrateDmgPct;
        EnemyHP = Mathf.Max(float.Epsilon, EnemyHP - dmg);
        _desecrateTimer = DesecrateCooldown;
        _dailySpellCount++;
        CheckQuestProgress(QuestTrackType.Spells);
        OnDamageDealt?.Invoke(dmg, true);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool UseBloodOath()
    {
        if (!BloodOathCanCast) return false;
        Blood -= BloodOathCost;
        SoldierHP = 1f;
        BloodOathActive = true;
        BloodOathTimeRemaining = BloodOathDuration;
        _bloodOathTimer = BloodOathCooldown;
        _dailySpellCount++;
        CheckQuestProgress(QuestTrackType.Spells);
        OnStateChanged?.Invoke();
        return true;
    }

    // --- Daily quest logic ---
    void GenerateDailyQuests(string today)
    {
        _questDate = today;
        int seed = DateTime.UtcNow.DayOfYear + DateTime.UtcNow.Year * 1000;
        var rng  = new System.Random(seed);
        var used = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < DailyQuestCount; i++)
        {
            int idx;
            do { idx = rng.Next(QuestPool.Length); } while (used.Contains(idx));
            used.Add(idx);
            DailyQuestIndices[i]  = idx;
            DailyQuestProgress[i] = 0;
            DailyQuestClaimed[i]  = false;
        }
        _dailyKillCount  = 0;
        _dailyFarmCount  = 0;
        _dailySpellCount = 0;
        DailyQuestsReady = true;
    }

    void CheckQuestProgress(QuestTrackType type)
    {
        if (!DailyQuestsReady) return;
        bool changed = false;
        for (int i = 0; i < DailyQuestCount; i++)
        {
            if (DailyQuestClaimed[i]) continue;
            var def = QuestPool[DailyQuestIndices[i]];
            if (def.TrackType != type) continue;
            int current = type switch {
                QuestTrackType.Kills  => _dailyKillCount,
                QuestTrackType.Farms  => _dailyFarmCount,
                QuestTrackType.Spells => _dailySpellCount,
                QuestTrackType.Wave   => Wave,
                _                    => 0,
            };
            int newProgress = Math.Min(current, def.Target);
            if (newProgress != DailyQuestProgress[i]) { DailyQuestProgress[i] = newProgress; changed = true; }
        }
        if (changed) OnStateChanged?.Invoke();
    }

    public bool ClaimQuest(int index)
    {
        if (index < 0 || index >= DailyQuestCount || DailyQuestClaimed[index]) return false;
        var def = QuestPool[DailyQuestIndices[index]];
        if (DailyQuestProgress[index] < def.Target) return false;
        DailyQuestClaimed[index] = true;
        AddBlood(def.BloodReward);
        if (def.ShardReward > 0) SoulShards += def.ShardReward;
        OnStateChanged?.Invoke();
        return true;
    }

    // --- Ad / IAP rewards ---
    public void GrantAdReward()
    {
        AdBoostActive        = true;
        AdBoostTimeRemaining = Mathf.Max(AdBoostTimeRemaining, AdBoostDuration);
        OnStateChanged?.Invoke();
    }

    public void SetAdsRemoved()
    {
        AdsRemoved = true;
        PlayerPrefs.SetInt("AdsRemoved", 1);
        PlayerPrefs.Save();
        OnStateChanged?.Invoke();
    }

    public void SetStarterPackOwned()
    {
        if (StarterPackOwned) return;
        StarterPackOwned = true;
        AddBlood(5000);
        SoulShards += 5;
        PlayerPrefs.SetInt("StarterPackOwned", 1);
        PlayerPrefs.Save();
        OnStateChanged?.Invoke();
    }

    public void GrantBloodBoostSmall()
    {
        AdBoostActive        = true;
        AdBoostTimeRemaining = Mathf.Max(AdBoostTimeRemaining, 1800f);  // 30 min
        OnStateChanged?.Invoke();
    }

    public void GrantBloodBoostLarge()
    {
        AdBoostActive        = true;
        AdBoostTimeRemaining = Mathf.Max(AdBoostTimeRemaining, 7200f);  // 2 hr
        SoulShards += 10;
        OnStateChanged?.Invoke();
    }

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

    public bool BuyClickPower()
    {
        if (!ClickPowerUnlocked || Wood < ClickPowerCost || ClickPowerLevel >= ClickPowerMaxLevel) return false;
        Wood -= ClickPowerCost;
        ClickPowerLevel++;
        ClickPowerCost = Math.Floor(ClickPowerCost * ClickPowerCostMult);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyShrine()
    {
        if (!ShrineUnlocked || Wood < ShrineWoodCost || ShrineCount >= ShrineMaxCount) return false;
        Wood -= ShrineWoodCost;
        ShrineCount++;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyBloodWell()
    {
        if (!BloodWellUnlocked || Wood < BloodWellCost || BloodWellCount >= BloodWellMaxCount) return false;
        Wood -= BloodWellCost;
        BloodWellCount++;
        BloodWellCost = Math.Floor(BloodWellCost * 2.0);
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
        OnStateChanged?.Invoke();
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
        _dailySpellCount++;
        CheckQuestProgress(QuestTrackType.Spells);
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

    public bool BuyPBountyBonus()
    {
        if (PrestigePoints < PrestigeShopCost) return false;
        PrestigePoints -= PrestigeShopCost;
        PBountyBonusLevel++;
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

    public bool BuySSShardHunger()
    {
        if (SoulShards < SSUpgradeCost || SSShardHungerLevel >= SSMaxLevel) return false;
        SoulShards -= SSUpgradeCost;
        SSShardHungerLevel++;
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
            TalentFlags.BloodFrenzy, TalentFlags.Undying,    TalentFlags.ShardHunter,
            TalentFlags.IronSkin,    TalentFlags.BloodRush,  TalentFlags.Glutton,
            TalentFlags.EchoMastery, TalentFlags.Bloodlust,
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
        // Preserve permanent IAP purchases across resets
        bool hadAdsRemoved    = AdsRemoved;
        bool hadStarterPack   = StarterPackOwned;
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Blood = 0; TotalBloodEarned = 0; Wood = 0;
        TankCount = 0; BerserkerCount = 0; PaladinCount = 0; SoldierHP = 0f;
        WorkerCount = 0; BloodRitualCount = 0; BloodRitualCost = BloodRitualBaseCost;
        BloodWellCount = 0; BloodWellCost = BloodWellBaseCost;
        BarracksLevel = 1; MaxSoldiers = 10; BarracksUpgradeCost = 20.0;
        WorkersUnlocked = false; HealSelfUnlocked = false; SurgeUnlocked = false;
        BloodShieldUnlocked = false; BloodShieldHP = 0f;
        PrestigeCount = 0; PrestigePoints = 0;
        PSoldierCapLevel = 0; PClickBonusLevel = 0; PRitualEffLevel = 0;
        PWeaponHeadStartLevel = 0; PBloodTitheLevel = 0; PIronWallLevel = 0; PBountyBonusLevel = 0;
        WeaponLevel = 0; ArmorLevel = 0; TalismanLevel = 0;
        BerserkerFront = false; FortificationLevel = 0; FortificationCost = FortBaseCost;
        SoulShards = 0; SoulShardShopUnlocked = false;
        SSBossTimerLevel = 0; SSDoubleChestLevel = 0; SSRollbackLevel = 0; SSBloodTapLevel = 0; SSShardHungerLevel = 0;
        BloodBankDeposit = 0; BloodBankAccrued = 0; WaveStreak = 0;
        SurgeUpgradeLevel = 0; HealUpgradeLevel = 0;
        TotalEnemiesKilled = 0; TimePlayed = 0; Achievements = AchievementFlags.None;
        AutoBuySoldiers = false; AutoSurge = false; AutoHeal = false; AutoStorm = false;
        AutoDesecrate = false; AutoBuyRituals = false; AutoBankDeposit = false;
        AutoWarCry = false; AutoHexCurse = false; AutoBloodOath = false; AutoBloodShield = false;
        SoundEnabled = true; NotificationsEnabled = true;
        DailyBonusAvailable = false; OfflineWoodEarned = 0; OfflineBloodEarned = 0; OfflineBankInterest = 0;
        Talents = TalentFlags.None; PendingPrestige = false; PendingTalentChoices = new TalentFlags[0];
        CorruptionLevel = 0; DailyChallengeActive = false; DailyChallengeAvailable = false; ChallengeTimeRemaining = 0f;
        WavePreviewActive = false; _flawlessTimer = 0f; _undyingUsedThisWave = false;
        Wave = 1; NextBossWave = UnityEngine.Random.Range(6, 13);
        TutorialProgress = 0; TutorialActive = false;
        AdsRemoved    = hadAdsRemoved;
        StarterPackOwned = hadStarterPack;
        if (AdsRemoved)    { PlayerPrefs.SetInt("AdsRemoved",      1); PlayerPrefs.Save(); }
        if (StarterPackOwned) { PlayerPrefs.SetInt("StarterPackOwned", 1); PlayerPrefs.Save(); }
        GenerateDailyQuests(DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        SpawnEnemy(1);
        CheckTutorial();
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
        if (PrestigeCount >= 3) TryUnlock(AchievementFlags.Prestige3);
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
        BloodWellCount      = 0;
        BloodWellCost       = BloodWellBaseCost;
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
        PlayerPrefs.SetInt   ("AutoSurge",           AutoSurge       ? 1 : 0);
        PlayerPrefs.SetInt   ("AutoHeal",            AutoHeal        ? 1 : 0);
        PlayerPrefs.SetInt   ("AutoStorm",           AutoStorm       ? 1 : 0);
        PlayerPrefs.SetInt   ("AutoDesecrate",       AutoDesecrate   ? 1 : 0);
        PlayerPrefs.SetInt   ("AutoBuyRituals",      AutoBuyRituals  ? 1 : 0);
        PlayerPrefs.SetInt   ("AutoBankDeposit",     AutoBankDeposit ? 1 : 0);
        PlayerPrefs.SetInt   ("AutoWarCry",          AutoWarCry      ? 1 : 0);
        PlayerPrefs.SetInt   ("AutoHexCurse",        AutoHexCurse    ? 1 : 0);
        PlayerPrefs.SetInt   ("AutoBloodOath",       AutoBloodOath   ? 1 : 0);
        PlayerPrefs.SetInt   ("AutoBloodShield",     AutoBloodShield ? 1 : 0);
        PlayerPrefs.SetInt   ("BloodShieldUnlocked", BloodShieldUnlocked ? 1 : 0);
        PlayerPrefs.SetFloat ("BloodShieldHP",       BloodShieldHP);
        PlayerPrefs.SetInt   ("TotalEnemiesKilled",  TotalEnemiesKilled);
        PlayerPrefs.SetInt   ("TotalSoldiersLost",   TotalSoldiersLost);
        PlayerPrefs.SetInt   ("TotalBossesKilled",   TotalBossesKilled);
        PlayerPrefs.SetInt   ("HealSelfUnlocked",    HealSelfUnlocked ? 1 : 0);
        PlayerPrefs.SetInt   ("PrestigeCount",       PrestigeCount);
        PlayerPrefs.SetInt   ("PrestigePoints",      PrestigePoints);
        PlayerPrefs.SetInt   ("PSoldierCapLevel",    PSoldierCapLevel);
        PlayerPrefs.SetInt   ("PClickBonusLevel",    PClickBonusLevel);
        PlayerPrefs.SetInt   ("PRitualEffLevel",     PRitualEffLevel);
        PlayerPrefs.SetInt   ("PWeaponHeadStartLevel", PWeaponHeadStartLevel);
        PlayerPrefs.SetInt   ("PBloodTitheLevel",    PBloodTitheLevel);
        PlayerPrefs.SetInt   ("PIronWallLevel",      PIronWallLevel);
        PlayerPrefs.SetInt   ("PBountyBonusLevel",   PBountyBonusLevel);
        PlayerPrefs.SetInt   ("WeaponLevel",         WeaponLevel);
        PlayerPrefs.SetInt   ("ArmorLevel",          ArmorLevel);
        PlayerPrefs.SetInt   ("TalismanLevel",       TalismanLevel);
        PlayerPrefs.SetInt   ("BerserkerFront",      BerserkerFront ? 1 : 0);
        PlayerPrefs.SetInt   ("FortificationLevel",  FortificationLevel);
        PlayerPrefs.SetString("FortificationCost",   FortificationCost.ToString("R", ic));
        PlayerPrefs.SetInt   ("ClickPowerLevel",      ClickPowerLevel);
        PlayerPrefs.SetInt   ("ShrineCount",          ShrineCount);
        PlayerPrefs.SetInt   ("BloodWellCount",       BloodWellCount);
        PlayerPrefs.SetString("BloodWellCost",        BloodWellCost.ToString("R", ic));
        PlayerPrefs.SetString("ClickPowerCost",       ClickPowerCost.ToString("R", ic));
        PlayerPrefs.SetString("SoulShards",          SoulShards.ToString("R", ic));
        PlayerPrefs.SetInt   ("SoulShardShopUnlocked", SoulShardShopUnlocked ? 1 : 0);
        PlayerPrefs.SetInt   ("SSBossTimerLevel",    SSBossTimerLevel);
        PlayerPrefs.SetInt   ("SSDoubleChestLevel",  SSDoubleChestLevel);
        PlayerPrefs.SetInt   ("SSRollbackLevel",     SSRollbackLevel);
        PlayerPrefs.SetInt   ("SSBloodTapLevel",     SSBloodTapLevel);
        PlayerPrefs.SetInt   ("SSShardHungerLevel",  SSShardHungerLevel);
        PlayerPrefs.SetInt   ("SurgeUpgradeLevel",   SurgeUpgradeLevel);
        PlayerPrefs.SetInt   ("HealUpgradeLevel",    HealUpgradeLevel);
        PlayerPrefs.SetInt   ("SoundEnabled",        SoundEnabled        ? 1 : 0);
        PlayerPrefs.SetInt   ("NotificationsEnabled",NotificationsEnabled ? 1 : 0);
        PlayerPrefs.SetString("BloodBankDeposit",    BloodBankDeposit.ToString("R", ic));
        PlayerPrefs.SetString("BloodBankAccrued",    BloodBankAccrued.ToString("R", ic));
        PlayerPrefs.SetInt   ("WaveStreak",          WaveStreak);
        PlayerPrefs.SetInt   ("BestWave",            BestWave);
        PlayerPrefs.SetInt   ("BestStreak",          BestStreak);
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
        PlayerPrefs.SetInt   ("AdsRemoved",           AdsRemoved       ? 1 : 0);
        PlayerPrefs.SetInt   ("StarterPackOwned",     StarterPackOwned  ? 1 : 0);
        PlayerPrefs.SetString("QuestDate",            _questDate);
        PlayerPrefs.SetInt   ("DailyKillCount",       _dailyKillCount);
        PlayerPrefs.SetInt   ("DailyFarmCount",       _dailyFarmCount);
        PlayerPrefs.SetInt   ("DailySpellCount",      _dailySpellCount);
        for (int i = 0; i < DailyQuestCount; i++)
        {
            PlayerPrefs.SetInt($"QuestIdx{i}",       DailyQuestIndices[i]);
            PlayerPrefs.SetInt($"QuestProgress{i}",  DailyQuestProgress[i]);
            PlayerPrefs.SetInt($"QuestClaimed{i}",   DailyQuestClaimed[i] ? 1 : 0);
        }
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
        AutoSurge           = PlayerPrefs.GetInt   ("AutoSurge",           0) == 1;
        AutoHeal            = PlayerPrefs.GetInt   ("AutoHeal",            0) == 1;
        AutoStorm           = PlayerPrefs.GetInt   ("AutoStorm",           0) == 1;
        AutoDesecrate       = PlayerPrefs.GetInt   ("AutoDesecrate",       0) == 1;
        AutoBuyRituals      = PlayerPrefs.GetInt   ("AutoBuyRituals",      0) == 1;
        AutoBankDeposit     = PlayerPrefs.GetInt   ("AutoBankDeposit",     0) == 1;
        AutoWarCry          = PlayerPrefs.GetInt   ("AutoWarCry",          0) == 1;
        AutoHexCurse        = PlayerPrefs.GetInt   ("AutoHexCurse",        0) == 1;
        AutoBloodOath       = PlayerPrefs.GetInt   ("AutoBloodOath",       0) == 1;
        AutoBloodShield     = PlayerPrefs.GetInt   ("AutoBloodShield",     0) == 1;
        BloodShieldUnlocked = PlayerPrefs.GetInt   ("BloodShieldUnlocked", 0) == 1;
        BloodShieldHP       = PlayerPrefs.GetFloat ("BloodShieldHP",       0f);
        TotalEnemiesKilled  = PlayerPrefs.GetInt   ("TotalEnemiesKilled",  0);
        TotalSoldiersLost   = PlayerPrefs.GetInt   ("TotalSoldiersLost",   0);
        TotalBossesKilled   = PlayerPrefs.GetInt   ("TotalBossesKilled",   0);
        HealSelfUnlocked    = PlayerPrefs.GetInt   ("HealSelfUnlocked",    0) == 1;
        PrestigeCount       = PlayerPrefs.GetInt   ("PrestigeCount",       0);
        PrestigePoints      = PlayerPrefs.GetInt   ("PrestigePoints",      0);
        PSoldierCapLevel    = PlayerPrefs.GetInt   ("PSoldierCapLevel",    0);
        PClickBonusLevel    = PlayerPrefs.GetInt   ("PClickBonusLevel",    0);
        PRitualEffLevel     = PlayerPrefs.GetInt   ("PRitualEffLevel",     0);
        PWeaponHeadStartLevel = PlayerPrefs.GetInt ("PWeaponHeadStartLevel", 0);
        PBloodTitheLevel    = PlayerPrefs.GetInt   ("PBloodTitheLevel",    0);
        PIronWallLevel      = PlayerPrefs.GetInt   ("PIronWallLevel",      0);
        PBountyBonusLevel   = PlayerPrefs.GetInt   ("PBountyBonusLevel",   0);
        WeaponLevel         = PlayerPrefs.GetInt   ("WeaponLevel",         0);
        ArmorLevel          = PlayerPrefs.GetInt   ("ArmorLevel",          0);
        TalismanLevel       = PlayerPrefs.GetInt   ("TalismanLevel",       0);
        BerserkerFront      = PlayerPrefs.GetInt   ("BerserkerFront",      0) == 1;
        FortificationLevel  = PlayerPrefs.GetInt   ("FortificationLevel",  0);
        FortificationCost   = double.Parse(PlayerPrefs.GetString("FortificationCost", FortBaseCost.ToString("R", ic)), ic);
        ClickPowerLevel     = PlayerPrefs.GetInt   ("ClickPowerLevel", 0);
        ShrineCount         = PlayerPrefs.GetInt   ("ShrineCount",     0);
        BloodWellCount      = PlayerPrefs.GetInt   ("BloodWellCount",  0);
        BloodWellCost       = double.TryParse(PlayerPrefs.GetString("BloodWellCost", ""), System.Globalization.NumberStyles.Any, ic, out double bwc) ? bwc : BloodWellBaseCost;
        ClickPowerCost      = double.Parse(PlayerPrefs.GetString("ClickPowerCost", ClickPowerBaseCost.ToString("R", ic)), ic);
        SoulShards          = double.Parse(PlayerPrefs.GetString("SoulShards",        "0"), ic);
        SoulShardShopUnlocked = PlayerPrefs.GetInt ("SoulShardShopUnlocked", 0) == 1;
        SSBossTimerLevel    = PlayerPrefs.GetInt   ("SSBossTimerLevel",    0);
        SSDoubleChestLevel  = PlayerPrefs.GetInt   ("SSDoubleChestLevel",  0);
        SSRollbackLevel     = PlayerPrefs.GetInt   ("SSRollbackLevel",     0);
        SSBloodTapLevel     = PlayerPrefs.GetInt   ("SSBloodTapLevel",     0);
        SSShardHungerLevel  = PlayerPrefs.GetInt   ("SSShardHungerLevel",  0);
        SurgeUpgradeLevel   = PlayerPrefs.GetInt   ("SurgeUpgradeLevel",   0);
        HealUpgradeLevel    = PlayerPrefs.GetInt   ("HealUpgradeLevel",    0);
        SoundEnabled        = PlayerPrefs.GetInt   ("SoundEnabled",        1) == 1;
        NotificationsEnabled = PlayerPrefs.GetInt  ("NotificationsEnabled",1) == 1;
        BloodBankDeposit    = double.Parse(PlayerPrefs.GetString("BloodBankDeposit", "0"), ic);
        BloodBankAccrued    = double.Parse(PlayerPrefs.GetString("BloodBankAccrued", "0"), ic);
        WaveStreak          = PlayerPrefs.GetInt   ("WaveStreak",          0);
        BestWave            = PlayerPrefs.GetInt   ("BestWave",            0);
        BestStreak          = PlayerPrefs.GetInt   ("BestStreak",          0);
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

        Talents          = (TalentFlags)PlayerPrefs.GetInt("Talents",          0);
        CorruptionLevel  =              PlayerPrefs.GetInt("CorruptionLevel",  0);
        AdsRemoved       = PlayerPrefs.GetInt("AdsRemoved",       0) == 1;
        StarterPackOwned = PlayerPrefs.GetInt("StarterPackOwned",  0) == 1;

        string questToday = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string savedQDate = PlayerPrefs.GetString("QuestDate", "");
        if (savedQDate == questToday)
        {
            _questDate       = questToday;
            _dailyKillCount  = PlayerPrefs.GetInt("DailyKillCount",  0);
            _dailyFarmCount  = PlayerPrefs.GetInt("DailyFarmCount",  0);
            _dailySpellCount = PlayerPrefs.GetInt("DailySpellCount", 0);
            for (int i = 0; i < DailyQuestCount; i++)
            {
                DailyQuestIndices[i]  = PlayerPrefs.GetInt($"QuestIdx{i}",      i % QuestPool.Length);
                DailyQuestProgress[i] = PlayerPrefs.GetInt($"QuestProgress{i}", 0);
                DailyQuestClaimed[i]  = PlayerPrefs.GetInt($"QuestClaimed{i}",  0) == 1;
            }
            DailyQuestsReady = true;
        }
        else
        {
            GenerateDailyQuests(questToday);
        }

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
                if (BloodWellCount > 0 && OfflineWoodEarned > 0)
                {
                    double woodForWells = Math.Min(BloodWellCount * BloodWellWoodPerSec * secs, OfflineWoodEarned);
                    double wellBlood    = woodForWells * BloodWellBloodRatio;
                    OfflineBloodEarned += wellBlood;
                    OfflineWoodEarned  -= woodForWells;
                    Wood               -= woodForWells;
                }
                if (BloodBankDeposit > 0)
                {
                    OfflineBankInterest = BloodBankDeposit * (BankInterestRatePerHour / 3600.0) * secs;
                    BloodBankAccrued += OfflineBankInterest;
                }
                Blood            += OfflineBloodEarned;
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

        if (BestWave < Wave) BestWave = Wave;
        TutorialProgress = PlayerPrefs.GetInt("TutorialProgress", 0);
    }

    public void ClearOfflineEarnings()
    {
        OfflineWoodEarned   = 0;
        OfflineBloodEarned  = 0;
        OfflineBankInterest = 0;
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
