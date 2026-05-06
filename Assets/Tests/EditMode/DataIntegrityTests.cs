using NUnit.Framework;
using UnityEngine;

public class DataIntegrityTests
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

    // ── Offline earnings math ─────────────────────────────────────────────────

    [Test]
    public void OfflineEarnings_TwoWorkersOneHour()
    {
        // 2 workers × 0.5 wood/sec × 3600 sec = 3600
        double result = GameManager.CalculateOfflineWood(2, 3600);
        Assert.AreEqual(3600.0, result, 0.001);
    }

    [Test]
    public void OfflineEarnings_ZeroWorkers_GivesZero()
    {
        Assert.AreEqual(0.0, GameManager.CalculateOfflineWood(0, 3600));
    }

    [Test]
    public void OfflineEarnings_CappedAt8Hours()
    {
        double result8h  = GameManager.CalculateOfflineWood(1, 8 * 3600);
        double result24h = GameManager.CalculateOfflineWood(1, 24 * 3600);
        Assert.AreEqual(result8h, result24h, 0.001,
            "Offline earnings must be capped at 8 hours");
    }

    [Test]
    public void OfflineEarnings_ExactlyAtCap()
    {
        double atCap      = GameManager.CalculateOfflineWood(1, 8 * 3600);
        double slightOver = GameManager.CalculateOfflineWood(1, 8 * 3600 + 1);
        Assert.AreEqual(atCap, slightOver, 0.001);
    }

    // ── Save / load round-trip ────────────────────────────────────────────────

    [Test]
    public void SaveLoad_PreservesBlood()
    {
        _gm.AwardBloodForTest(123.456);
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(123.456, _gm.Blood, 0.0001);
    }

    [Test]
    public void SaveLoad_PreservesWave()
    {
        // Simulate wave advancing by checking the save key directly
        _gm.SaveForTest();
        PlayerPrefs.SetInt("Wave", 7);

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(7, _gm.Wave);
    }

    [Test]
    public void SaveLoad_PreservesWorkersUnlocked()
    {
        _gm.AwardBloodForTest(GameManager.WorkersUnlockThreshold);
        Assert.IsTrue(_gm.WorkersUnlocked);
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.IsTrue(_gm.WorkersUnlocked, "WorkersUnlocked flag must survive save/load");
    }

    [Test]
    public void SaveLoad_PreservesHealSelfUnlocked()
    {
        _gm.AwardBloodForTest(GameManager.HealSelfUnlockThreshold);
        Assert.IsTrue(_gm.HealSelfUnlocked);
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.IsTrue(_gm.HealSelfUnlocked, "HealSelfUnlocked flag must survive save/load");
    }

    [Test]
    public void Load_WithNoSaveData_UsesDefaults()
    {
        // Fresh instance with no PlayerPrefs — defaults should apply
        Assert.AreEqual(0.0,  _gm.Blood,       0.001);
        Assert.AreEqual(0.0,  _gm.Wood,        0.001);
        Assert.AreEqual(1,    _gm.Wave);
        Assert.AreEqual(0,    _gm.SoldierCount);
        Assert.AreEqual(0,    _gm.WorkerCount);
        Assert.AreEqual(1,    _gm.BarracksLevel);
        Assert.AreEqual(10,   _gm.MaxSoldiers);
        Assert.IsFalse(_gm.WorkersUnlocked);
        Assert.IsFalse(_gm.HealSelfUnlocked);
    }

    // ── FormatNumber ──────────────────────────────────────────────────────────

    [Test]
    public void FormatNumber_Zero()           => Assert.AreEqual("0",     GameManager.FormatNumber(0));
    [Test]
    public void FormatNumber_BelowThousand()  => Assert.AreEqual("42",    GameManager.FormatNumber(42.9));
    [Test]
    public void FormatNumber_Thousands()      => Assert.AreEqual("1.0K",  GameManager.FormatNumber(1000));
    [Test]
    public void FormatNumber_Millions()       => Assert.AreEqual("1.0M",  GameManager.FormatNumber(1_000_000));
    [Test]
    public void FormatNumber_Billions()       => Assert.AreEqual("1.0B",  GameManager.FormatNumber(1_000_000_000));
    [Test]
    public void FormatNumber_Floors_NotRounds() => Assert.AreEqual("999", GameManager.FormatNumber(999.9));
}
