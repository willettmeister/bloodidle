using UnityEngine;
using System;

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

    // --- Heal Self ---
    public bool HealSelfUnlocked { get; private set; }
    public const double HealSelfUnlockThreshold = 50.0;
    public const double HealSelfCost = 25.0;
    public const float HealSelfAmount = 20f;

    public event Action OnStateChanged;

    private static readonly string[] EnemyNames =
    {
        "Goblin", "Orc Warrior", "Cave Troll", "Stone Ogre",
        "Demon Knight", "Vampire Lord", "Ancient Dragon"
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SpawnEnemy(1);
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
        int idx = Math.Min(wave - 1, EnemyNames.Length - 1);
        EnemyName = EnemyNames[idx];
        EnemyMaxHP = (float)(100 * Math.Pow(1.5, wave - 1));
        EnemyHP = EnemyMaxHP;
        EnemyAttack = (float)(3 * Math.Pow(1.3, wave - 1));
    }

    public void FarmBlood()
    {
        AddBlood(BloodPerClick);
        OnStateChanged?.Invoke();
    }

    void AddBlood(double amount)
    {
        Blood += amount;
        TotalBloodEarned += amount;
        if (!HealSelfUnlocked && TotalBloodEarned >= HealSelfUnlockThreshold)
            HealSelfUnlocked = true;
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

    public static string FormatNumber(double value)
    {
        if (value >= 1_000_000_000) return $"{value / 1_000_000_000:F1}B";
        if (value >= 1_000_000)     return $"{value / 1_000_000:F1}M";
        if (value >= 1_000)         return $"{value / 1_000:F1}K";
        return $"{Math.Floor(value)}";
    }

    public static string FormatHP(float value) => Mathf.CeilToInt(value).ToString();
}
