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

    // --- Soldiers ---
    public int SoldierCount { get; private set; }
    public float SoldierHP { get; private set; }
    public int MaxSoldiers { get; private set; } = 10;
    public const float SoldierMaxHP = 50f;
    public const double SoldierCost = 10.0;
    public const float SoldierAttack = 5f;          // damage/sec per soldier

    // --- Barracks ---
    public int BarracksLevel { get; private set; } = 1;
    public double BarracksUpgradeCost { get; private set; } = 20.0;
    public const int BarracksSoldierBonus = 5;      // soldiers added per upgrade
    public const double BarracksCostMultiplier = 2.0;

    // --- Enemy ---
    public int Wave { get; private set; } = 1;
    public float EnemyHP { get; private set; }
    public float EnemyMaxHP { get; private set; }
    public string EnemyName { get; private set; }
    public float EnemyAttack { get; private set; }  // total damage/sec to frontline
    public int EnemySpriteIndex { get; private set; }

    // --- Workers unlock ---
    public bool WorkersUnlocked { get; private set; }
    public const double WorkersUnlockThreshold = 200.0;

    // --- Heal Self ---
    public bool HealSelfUnlocked { get; private set; }
    public const double HealSelfUnlockThreshold = 250.0;
    public const double HealSelfCost = 25.0;
    public const float HealSelfAmount = 20f;

    public event Action OnStateChanged;

#if UNITY_INCLUDE_TESTS
    public static void ResetForTest() => Instance = null;
    public void SetWoodForTest(double amount) => Wood = amount;
    public void SetSoldierHPForTest(float hp) => SoldierHP = hp;
#endif

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

        if (SoldierCount > 0 && EnemyHP > 0)
            changed |= RunCombat(dt);

        if (changed)
            OnStateChanged?.Invoke();
    }

    // Returns true whenever state changed (always, when called)
    bool RunCombat(float dt)
    {
        EnemyHP = Mathf.Max(0f, EnemyHP - SoldierCount * SoldierAttack * dt);

        if (EnemyHP <= 0f)
        {
            AddBlood(Math.Floor(25 * Math.Pow(1.4, Wave - 1)));
            Wave++;
            SpawnEnemy(Wave);
            return true;
        }

        SoldierHP -= EnemyAttack * dt;
        if (SoldierHP <= 0f)
        {
            SoldierCount--;
            SoldierHP = SoldierCount > 0 ? SoldierMaxHP : 0f;
        }

        return true;
    }

    void SpawnEnemy(int wave)
    {
        var def          = EnemyPool[UnityEngine.Random.Range(0, EnemyPool.Length)];
        EnemyName        = def.Name;
        EnemySpriteIndex = def.SpriteIdx;
        EnemyMaxHP       = (float)(100 * Math.Pow(1.5, wave - 1) * def.HPMult);
        EnemyHP          = EnemyMaxHP;
        EnemyAttack      = (float)(3   * Math.Pow(1.3, wave - 1) * def.AtkMult);
    }

    public void FarmBlood()
    {
        AddBlood(BloodPerClick);
        Debug.Log($"[GameManager] FarmBlood called — Blood={Blood}, subscribers={OnStateChanged?.GetInvocationList().Length ?? 0}");
        OnStateChanged?.Invoke();
    }

    void AddBlood(double amount)
    {
        Blood += amount;
        TotalBloodEarned += amount;
        if (!WorkersUnlocked  && TotalBloodEarned >= WorkersUnlockThreshold)  WorkersUnlocked  = true;
        if (!HealSelfUnlocked && TotalBloodEarned >= HealSelfUnlockThreshold) HealSelfUnlocked = true;
    }

    public bool BuySoldier()
    {
        if (Blood < SoldierCost || SoldierCount >= MaxSoldiers) return false;
        Blood -= SoldierCost;
        SoldierCount++;
        if (SoldierCount == 1) SoldierHP = SoldierMaxHP;
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
        if (!HealSelfUnlocked || Blood < HealSelfCost || SoldierCount == 0 || SoldierHP >= SoldierMaxHP)
            return false;
        Blood -= HealSelfCost;
        SoldierHP = Mathf.Min(SoldierHP + HealSelfAmount, SoldierMaxHP);
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
        PlayerPrefs.SetInt   ("SoldierCount",        SoldierCount);
        PlayerPrefs.SetFloat ("SoldierHP",           SoldierHP);
        PlayerPrefs.SetInt   ("WorkerCount",         WorkerCount);
        PlayerPrefs.SetInt   ("BarracksLevel",       BarracksLevel);
        PlayerPrefs.SetInt   ("MaxSoldiers",         MaxSoldiers);
        PlayerPrefs.SetString("BarracksUpgradeCost", BarracksUpgradeCost.ToString("R", ic));
        PlayerPrefs.SetString("TotalBloodEarned",    TotalBloodEarned.ToString("R", ic));
        PlayerPrefs.SetInt   ("WorkersUnlocked",     WorkersUnlocked  ? 1 : 0);
        PlayerPrefs.SetInt   ("HealSelfUnlocked",    HealSelfUnlocked ? 1 : 0);
        PlayerPrefs.SetFloat ("EnemyHP",             EnemyHP);
        PlayerPrefs.SetFloat ("EnemyMaxHP",          EnemyMaxHP);
        PlayerPrefs.SetString("EnemyName",           EnemyName);
        PlayerPrefs.SetFloat ("EnemyAttack",         EnemyAttack);
        PlayerPrefs.SetInt   ("EnemySpriteIndex",    EnemySpriteIndex);
        PlayerPrefs.Save();
    }

    void Load()
    {
        if (!PlayerPrefs.HasKey("Blood")) return;
        var ic = CultureInfo.InvariantCulture;
        Blood               = double.Parse(PlayerPrefs.GetString("Blood",               "0"),  ic);
        Wood                = double.Parse(PlayerPrefs.GetString("Wood",                "0"),  ic);
        Wave                = PlayerPrefs.GetInt   ("Wave",                1);
        SoldierCount        = PlayerPrefs.GetInt   ("SoldierCount",        0);
        SoldierHP           = PlayerPrefs.GetFloat ("SoldierHP",           0f);
        WorkerCount         = PlayerPrefs.GetInt   ("WorkerCount",         0);
        BarracksLevel       = PlayerPrefs.GetInt   ("BarracksLevel",       1);
        MaxSoldiers         = PlayerPrefs.GetInt   ("MaxSoldiers",         10);
        BarracksUpgradeCost = double.Parse(PlayerPrefs.GetString("BarracksUpgradeCost", "20"), ic);
        TotalBloodEarned    = double.Parse(PlayerPrefs.GetString("TotalBloodEarned",    "0"),  ic);
        WorkersUnlocked     = PlayerPrefs.GetInt   ("WorkersUnlocked",     0) == 1;
        HealSelfUnlocked    = PlayerPrefs.GetInt   ("HealSelfUnlocked",    0) == 1;
        EnemyHP             = PlayerPrefs.GetFloat ("EnemyHP",             100f);
        EnemyMaxHP          = PlayerPrefs.GetFloat ("EnemyMaxHP",          100f);
        EnemyName           = PlayerPrefs.GetString("EnemyName",           "Goblin");
        EnemyAttack         = PlayerPrefs.GetFloat ("EnemyAttack",         3f);
        EnemySpriteIndex    = PlayerPrefs.GetInt   ("EnemySpriteIndex",    0);
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
