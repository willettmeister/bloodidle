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

public enum EnemyModifier { None, Armored, Enraged, Regen }
public enum BossAbility    { None, Shield, Berserk, Drain }

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
    public double WoodPerSecond => WorkerCount * WorkerWoodPerSec;
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
        (FrontlineIsTank ? SoldierMaxHP : FrontlineIsBerserker ? BerserkerMaxHP : PaladinMaxHP)
        + EquipArmorBonus;
    public float TotalAttack         => (TankCount     * (SoldierAttack   + EquipAttackBonus)
                                       + BerserkerCount * (BerserkerAttack + EquipAttackBonus)
                                       + PaladinCount   * (PaladinAttack   + EquipAttackBonus))
                                       * (1f + PrestigeMilestoneDmgBonus);
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
    public const float  TankRegenRate       = 2f;
    public const float  BerserkerCritChance = 0.2f;
    public const float  BerserkerCritMult   = 2f;
    public const float  MixedArmyDmgReduction = 0.15f;

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
                                    + BloodTithePerSec + BloodTapPerSec;
    public const double BloodRitualBaseCost       = 30.0;
    public const double BloodRitualBloodPerSec    = 1.0;
    public const double BloodRitualCostMultiplier = 2.0;

    // --- Blood Pact spell ---
    public bool BloodPactUnlocked => WorkersUnlocked;
    public const double BloodPactBloodCost = 200.0;
    public const double BloodPactWoodGain  = 100.0;

    // --- Prestige ---
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
    public int   WaveStreak       { get; private set; }
    public float StreakMultiplier => Mathf.Min(1f + WaveStreak * 0.1f, 3f);
    public const float MaxStreakMultiplier = 3f;

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
        _                     => "",
    };
    public const float EnemyArmoredDmgMult = 0.5f;
    public const float EnemyEnragedAtkMult = 1.5f;
    public const float EnemyRegenPct       = 0.02f;

    // --- Wave preview ---
    public bool WavePreviewActive { get; private set; }
    float _previewTimer;
    const float WavePreviewDuration = 3f;

    // --- Flawless wave ---
    public const float FlawlessThreshold = 10f;
    public bool FlawlessActive => _flawlessTimer <= FlawlessThreshold && EnemyHP > 0 && !WavePreviewActive;
    float _flawlessTimer;

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
    public int    TotalEnemiesKilled { get; private set; }
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

        if (BloodBankDeposit > 0)
        {
            BloodBankAccrued += BloodBankDeposit * (BankInterestRatePerHour / 3600.0) * dt;
            changed = true;
        }

        if (CurrentEnemyModifier == EnemyModifier.Regen && EnemyHP > 0 && EnemyHP < EnemyMaxHP)
        {
            EnemyHP = Mathf.Min(EnemyHP + EnemyMaxHP * EnemyRegenPct * dt, EnemyMaxHP);
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

        if (IsBossWave && SoldierCount > 0 && EnemyHP > 0)
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
        float eff = TotalAttack * (SurgeActive ? SurgeMultiplier : 1f);
        if (CurrentEnemyModifier == EnemyModifier.Armored) eff *= EnemyArmoredDmgMult;
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
            bool wasBoss = IsBossWave;
            double reward = Math.Floor(25 * Math.Pow(1.4, Wave - 1) * PrestigeMultiplier * (1.0 + EquipTalismanBonus));
            if (wasBoss)
            {
                reward *= 3;
                SoulShards += 1;
                SoulShardShopUnlocked = true;
                BossTimeRemaining = 0f;
            }
            bool isFlawless = _flawlessTimer > 0f && _flawlessTimer <= FlawlessThreshold;
            reward = Math.Floor(reward * StreakMultiplier * (isFlawless ? 2.0 : 1.0));
            WaveStreak++;
            AddBlood(reward);
            if (isFlawless) OnMilestoneChest?.Invoke("⚡ FLAWLESS! ×2 blood!");

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
            PlaySound(wasBoss ? _clipBossKill : _clipKill);

            if (isMilestone) GrantMilestoneChest(Wave - 1);
            return true;
        }

        float incomingAtk = EnemyAttack;
        if (IsBossWave && CurrentBossAbility == BossAbility.Berserk && EnemyHP < EnemyMaxHP * 0.25f)
            incomingAtk *= 2f;
        if (IsMixedArmy)    incomingAtk *= (1f - MixedArmyDmgReduction);
        if (PIronWallLevel > 0) incomingAtk *= (1f - PIronWallLevel * IronWallDmgReduction);
        float totalIncoming = incomingAtk;
        if (IsBossWave && CurrentBossAbility == BossAbility.Drain && EnemyHP > 0)
            totalIncoming += BossDrainPerSec;
        SoldierHP -= totalIncoming * dt;

        if (SoldierHP <= 0f)
        {
            if (FrontlineIsTank)           TankCount--;
            else if (FrontlineIsBerserker) BerserkerCount--;
            else                           PaladinCount--;
            SoldierHP = SoldierCount > 0 ? FrontlineMaxHP : 0f;
            WaveStreak = 0;
        }

        _dmgTimer += dt;
        if (_dmgTimer >= DmgTickInterval)
        {
            _dmgTimer = 0f;
            float tickDmg = eff * DmgTickInterval;
            if (BerserkerCount > 0 && TankCount == 0 && UnityEngine.Random.value < BerserkerCritChance)
            {
                tickDmg *= BerserkerCritMult;
                EnemyHP = Mathf.Max(0f, EnemyHP - tickDmg);
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
        _flawlessTimer = 0f;
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
                CurrentEnemyModifier = (EnemyModifier)UnityEngine.Random.Range(1, 4);
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
        PlaySound(_clipFarm);
        OnStateChanged?.Invoke();
    }

    void AddBlood(double amount)
    {
        Blood += amount;
        TotalBloodEarned += amount;
        if (!WorkersUnlocked  && TotalBloodEarned >= WorkersUnlockThreshold)  WorkersUnlocked  = true;
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

    void PlaySound(AudioClip clip)
    {
        if (SoundEnabled && clip != null && _audio != null) _audio.PlayOneShot(clip, 0.7f);
    }

    public bool BuySoldier() => BuyTank();

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
        WavePreviewActive = false; _flawlessTimer = 0f;
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

        string savedDate = PlayerPrefs.GetString("LastLoginDate", "");
        string today     = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        DailyBonusAvailable = savedDate != today;

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
