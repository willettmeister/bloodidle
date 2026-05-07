using UnityEngine;
using System;
using System.Globalization;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // --- Blood ---
    public double Blood { get; private set; }
    public double TotalBloodEarned { get; private set; }
    public const double BloodPerClick = 1.0;

    // --- Wood & Workers ---
    public double Wood { get; private set; }
    public int WorkerCount { get; private set; }
    public double WoodPerSecond => WorkerCount * WorkerWoodPerSec;
    public const double WorkerCost = 50.0;
    public const double WorkerWoodPerSec = 0.5;

    // --- Soldiers (two classes) ---
    public int TankCount       { get; private set; }
    public int BerserkerCount  { get; private set; }
    public int SoldierCount    => TankCount + BerserkerCount;
    public float SoldierHP     { get; private set; }

    public bool  FrontlineIsTank => TankCount > 0;
    public float FrontlineMaxHP  => FrontlineIsTank ? SoldierMaxHP : BerserkerMaxHP;
    public float TotalAttack     => TankCount * SoldierAttack + BerserkerCount * BerserkerAttack;

    public int MaxSoldiers { get; private set; } = 10;
    public const float  SoldierMaxHP    = 50f;    // tank HP
    public const double SoldierCost     = 10.0;
    public const float  SoldierAttack   = 5f;     // tank dmg/sec per soldier
    public const float  BerserkerMaxHP  = 25f;
    public const float  BerserkerAttack = 12f;    // berserker dmg/sec per soldier

    // --- Barracks ---
    public int BarracksLevel { get; private set; } = 1;
    public double BarracksUpgradeCost { get; private set; } = 20.0;
    public const int    BarracksSoldierBonus     = 5;
    public const double BarracksCostMultiplier   = 2.0;

    // --- Blood Ritual (passive blood income) ---
    public int    BloodRitualCount { get; private set; }
    public double BloodRitualCost  { get; private set; } = BloodRitualBaseCost;
    public double BloodPerSec      => BloodRitualCount * BloodRitualBloodPerSec * PrestigeMultiplier;
    public const double BloodRitualBaseCost        = 30.0;
    public const double BloodRitualBloodPerSec     = 1.0;
    public const double BloodRitualCostMultiplier  = 2.0;

    // --- Prestige ---
    public int    PrestigeCount      { get; private set; }
    public double PrestigeMultiplier => 1.0 + 0.5 * PrestigeCount;
    public const int PrestigeWaveRequirement = 20;

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
    public const float BossTimeLimit            = 90f;
    public const int   BossWaveRollback         = 3;
    public const float BossFailBloodPenaltyPct  = 0.25f;

    // --- Offline earnings ---
    public double OfflineWoodEarned { get; private set; }

    // --- Workers unlock ---
    public bool WorkersUnlocked { get; private set; }
    public const double WorkersUnlockThreshold = 200.0;

    // --- Heal Self ---
    public bool HealSelfUnlocked { get; private set; }
    public const double HealSelfUnlockThreshold = 250.0;
    public const double HealSelfCost   = 25.0;
    public const float  HealSelfAmount = 20f;

    public event Action OnStateChanged;
    public event Action<float, bool> OnDamageDealt;  // amount, isEnemyDamage

    float _dmgTimer;
    const float DmgTickInterval = 0.4f;

#if UNITY_INCLUDE_TESTS
    public static void ResetForTest()                     => Instance = null;
    public void SetWoodForTest(double amount)              => Wood = amount;
    public void SetSoldierHPForTest(float hp)             => SoldierHP = hp;
    public void SaveForTest()                             => Save();
    public void ClearSaveForTest()                        => PlayerPrefs.DeleteAll();

    public void AwardBloodForTest(double amount)          => AddBlood(amount);
    public void SetWaveForTest(int wave)                  => Wave = wave;
    public void SetNextBossWaveForTest(int wave)          => NextBossWave = wave;
    public void SpawnEnemyForTest(int wave)               => SpawnEnemy(wave);
    public void TriggerBossTimeoutForTest()               => BossTimerExpired();
    public void ForceUnlockHealSelfForTest()              => HealSelfUnlocked = true;

    public static double CalculateOfflineWood(int workers, double seconds) =>
        workers * WorkerWoodPerSec * Math.Min(seconds, 8.0 * 3600);
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
        new EnemyDef { Name = "Goblin",          HPMult = 0.6f, AtkMult = 0.7f, SpriteIdx = 0 },
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

        if (WorkerCount > 0)
        {
            Wood += WoodPerSecond * dt;
            changed = true;
        }

        if (BloodRitualCount > 0)
        {
            AddBlood(BloodPerSec * dt);
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
        EnemyHP = Mathf.Max(0f, EnemyHP - TotalAttack * dt);

        if (EnemyHP <= 0f)
        {
            bool wasBoss = IsBossWave;
            double reward = Math.Floor(25 * Math.Pow(1.4, Wave - 1) * PrestigeMultiplier);
            if (wasBoss) reward *= 3;
            AddBlood(reward);
            Wave++;
            if (wasBoss) NextBossWave = Wave + UnityEngine.Random.Range(5, 11);
            SpawnEnemy(Wave);
            _dmgTimer = 0f;
            return true;
        }

        SoldierHP -= EnemyAttack * dt;
        if (SoldierHP <= 0f)
        {
            if (FrontlineIsTank) TankCount--;
            else BerserkerCount--;
            SoldierHP = SoldierCount > 0 ? FrontlineMaxHP : 0f;
        }

        _dmgTimer += dt;
        if (_dmgTimer >= DmgTickInterval)
        {
            _dmgTimer = 0f;
            OnDamageDealt?.Invoke(TotalAttack * DmgTickInterval, true);
            if (SoldierCount > 0)
                OnDamageDealt?.Invoke(EnemyAttack * DmgTickInterval, false);
        }

        return true;
    }

    void SpawnEnemy(int wave)
    {
        bool isBoss = wave == NextBossWave;
        if (isBoss)
        {
            int idx          = UnityEngine.Random.Range(0, BossNames.Length);
            EnemyName        = BossNames[idx];
            EnemySpriteIndex = 6;
            EnemyMaxHP       = (float)(100 * Math.Pow(1.5, wave - 1) * 5.0);
            EnemyHP          = EnemyMaxHP;
            EnemyAttack      = (float)(3   * Math.Pow(1.3, wave - 1) * 2.0);
            BossTimeRemaining = BossTimeLimit;
        }
        else
        {
            var def          = EnemyPool[UnityEngine.Random.Range(0, EnemyPool.Length)];
            EnemyName        = def.Name;
            EnemySpriteIndex = def.SpriteIdx;
            EnemyMaxHP       = (float)(100 * Math.Pow(1.5, wave - 1) * def.HPMult);
            EnemyHP          = EnemyMaxHP;
            EnemyAttack      = (float)(3   * Math.Pow(1.3, wave - 1) * def.AtkMult);
        }
    }

    void BossTimerExpired()
    {
        TankCount         = 0;
        BerserkerCount    = 0;
        SoldierHP         = 0f;
        Blood             = Math.Floor(Blood * (1.0 - BossFailBloodPenaltyPct));
        Wave              = Math.Max(1, Wave - BossWaveRollback);
        NextBossWave      = Wave + UnityEngine.Random.Range(5, 11);
        BossTimeRemaining = 0f;
        SpawnEnemy(Wave);
        OnStateChanged?.Invoke();
    }

    public void FarmBlood()
    {
        AddBlood(BloodPerClick * PrestigeMultiplier);
        OnStateChanged?.Invoke();
    }

    void AddBlood(double amount)
    {
        Blood += amount;
        TotalBloodEarned += amount;
        if (!WorkersUnlocked  && TotalBloodEarned >= WorkersUnlockThreshold)  WorkersUnlocked  = true;
        if (!HealSelfUnlocked && TotalBloodEarned >= HealSelfUnlockThreshold) HealSelfUnlocked = true;
    }

    public bool BuySoldier() => BuyTank();

    public bool BuyTank()
    {
        if (Blood < SoldierCost || SoldierCount >= MaxSoldiers) return false;
        Blood -= SoldierCost;
        TankCount++;
        if (SoldierCount == 1) SoldierHP = SoldierMaxHP;
        OnStateChanged?.Invoke();
        return true;
    }

    public bool BuyBerserker()
    {
        if (Blood < SoldierCost || SoldierCount >= MaxSoldiers) return false;
        Blood -= SoldierCost;
        BerserkerCount++;
        if (SoldierCount == 1) SoldierHP = BerserkerMaxHP;
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

    public bool UseHealSelf()
    {
        if (!HealSelfUnlocked || Blood < HealSelfCost || SoldierCount == 0 || SoldierHP >= FrontlineMaxHP)
            return false;
        Blood -= HealSelfCost;
        SoldierHP = Mathf.Min(SoldierHP + HealSelfAmount, FrontlineMaxHP);
        OnStateChanged?.Invoke();
        return true;
    }

    public bool Prestige()
    {
        if (Wave < PrestigeWaveRequirement) return false;
        PrestigeCount++;
        Blood               = 0;
        Wood                = 0;
        TankCount           = 0;
        BerserkerCount      = 0;
        SoldierHP           = 0f;
        WorkerCount         = 0;
        BloodRitualCount    = 0;
        BloodRitualCost     = BloodRitualBaseCost;
        Wave                = 1;
        NextBossWave        = UnityEngine.Random.Range(6, 13);
        BarracksLevel       = 1;
        MaxSoldiers         = 10;
        BarracksUpgradeCost = 20.0;
        BossTimeRemaining   = 0f;
        SpawnEnemy(1);
        OnStateChanged?.Invoke();
        return true;
    }

    void OnApplicationPause(bool pausing) { if (pausing) Save(); }
    void OnApplicationQuit()              { Save(); }

    void Save()
    {
        var ic = CultureInfo.InvariantCulture;
        PlayerPrefs.SetString("Blood",               Blood.ToString("R", ic));
        PlayerPrefs.SetString("Wood",                Wood.ToString("R", ic));
        PlayerPrefs.SetInt   ("Wave",                Wave);
        PlayerPrefs.SetInt   ("TankCount",           TankCount);
        PlayerPrefs.SetInt   ("BerserkerCount",      BerserkerCount);
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
        // Migrate old "SoldierCount" saves → all tanks
        TankCount           = PlayerPrefs.GetInt("TankCount",      PlayerPrefs.GetInt("SoldierCount", 0));
        BerserkerCount      = PlayerPrefs.GetInt("BerserkerCount", 0);
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
        EnemyHP             = PlayerPrefs.GetFloat ("EnemyHP",             100f);
        EnemyMaxHP          = PlayerPrefs.GetFloat ("EnemyMaxHP",          100f);
        EnemyName           = PlayerPrefs.GetString("EnemyName",           "Goblin");
        EnemyAttack         = PlayerPrefs.GetFloat ("EnemyAttack",         3f);
        EnemySpriteIndex    = PlayerPrefs.GetInt   ("EnemySpriteIndex",    0);
        int savedNext       = PlayerPrefs.GetInt   ("NextBossWave",        0);
        NextBossWave        = savedNext > 0 ? savedNext : Wave + UnityEngine.Random.Range(5, 11);
        // Offline time doesn't count against boss timer — always reload fresh
        BossTimeRemaining   = IsBossWave ? BossTimeLimit : 0f;

        if (WorkerCount > 0 && PlayerPrefs.HasKey("SaveTime"))
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

    public void ClearOfflineEarnings() => OfflineWoodEarned = 0;

    public static string FormatNumber(double value)
    {
        if (value >= 1_000_000_000) return $"{value / 1_000_000_000:F1}B";
        if (value >= 1_000_000)     return $"{value / 1_000_000:F1}M";
        if (value >= 1_000)         return $"{value / 1_000:F1}K";
        return $"{Math.Floor(value)}";
    }

    public static string FormatHP(float value) => Mathf.CeilToInt(value).ToString();
}
