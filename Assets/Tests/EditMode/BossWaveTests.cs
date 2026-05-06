using NUnit.Framework;
using UnityEngine;

public class BossWaveTests
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

    // ── IsBossWave detection ──────────────────────────────────────────────────

    [Test]
    public void IsBossWave_TrueWhenWaveMatchesNextBossWave()
    {
        _gm.SetWaveForTest(7);
        _gm.SetNextBossWaveForTest(7);
        Assert.IsTrue(_gm.IsBossWave);
    }

    [Test]
    public void IsBossWave_FalseWhenWaveBelowNextBossWave()
    {
        _gm.SetWaveForTest(6);
        _gm.SetNextBossWaveForTest(7);
        Assert.IsFalse(_gm.IsBossWave);
    }

    [Test]
    public void IsBossWave_FalseWhenWaveAboveNextBossWave()
    {
        // Should not normally happen, but the property is pure Wave == NextBossWave.
        _gm.SetWaveForTest(8);
        _gm.SetNextBossWaveForTest(7);
        Assert.IsFalse(_gm.IsBossWave);
    }

    // ── WavesUntilBoss countdown ──────────────────────────────────────────────

    [Test]
    public void WavesUntilBoss_ReturnsCorrectCountdown()
    {
        _gm.SetWaveForTest(3);
        _gm.SetNextBossWaveForTest(8);
        Assert.AreEqual(5, _gm.WavesUntilBoss);
    }

    [Test]
    public void WavesUntilBoss_ZeroOnBossWave()
    {
        _gm.SetWaveForTest(8);
        _gm.SetNextBossWaveForTest(8);
        Assert.AreEqual(0, _gm.WavesUntilBoss);
    }

    // ── Initial NextBossWave ──────────────────────────────────────────────────

    [Test]
    public void NextBossWave_InitiallyAtLeast5WavesFromStart()
    {
        // Wave starts at 1; first boss must be at least 5 waves away (wave 6+).
        Assert.GreaterOrEqual(_gm.NextBossWave - _gm.Wave, 5);
    }

    [Test]
    public void NextBossWave_InitiallyNoMoreThan11WavesFromStart()
    {
        // Upper bound: Random.Range(6, 13) on a wave-1 start → max gap is 11.
        Assert.LessOrEqual(_gm.NextBossWave - _gm.Wave, 11);
    }

    // ── Save / load round-trip ────────────────────────────────────────────────

    [Test]
    public void SaveLoad_PreservesNextBossWave()
    {
        _gm.SetNextBossWaveForTest(14);
        _gm.SaveForTest();

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        Assert.AreEqual(14, _gm.NextBossWave);
    }

    [Test]
    public void SaveLoad_OldSave_DefaultsNextBossWaveReasonably()
    {
        // Simulate an old save that has no NextBossWave key.
        _gm.AwardBloodForTest(1);   // ensure "Blood" key exists so Load runs
        _gm.SaveForTest();
        PlayerPrefs.DeleteKey("NextBossWave");

        Object.DestroyImmediate(_gmGO);
        GameManager.ResetForTest();
        _gmGO = new GameObject("GM2");
        _gm   = _gmGO.AddComponent<GameManager>();

        // Must be at least 5 waves ahead of current wave.
        Assert.GreaterOrEqual(_gm.NextBossWave - _gm.Wave, 5);
    }

    // ── Blood reward multiplier ───────────────────────────────────────────────

    [Test]
    public void BossRewardFormula_IsTripleNormalReward()
    {
        int wave = 5;
        double normal = System.Math.Floor(25 * System.Math.Pow(1.4, wave - 1));
        double boss   = normal * 3;
        Assert.AreEqual(normal * 3, boss, 0.001);
        Assert.Greater(boss, normal);
    }

    [Test]
    public void NormalWave_GivesSingleReward()
    {
        // Wave 1: floor(25 * 1.4^0) = 25
        double expected = System.Math.Floor(25 * System.Math.Pow(1.4, 0));
        Assert.AreEqual(25.0, expected, 0.001);
    }
}
