// Manages Unity Ads (rewarded video).
// UNITY_ADS is defined via versionDefines in GameAssembly.asmdef when
// com.unity.ads >= 4.0.0 is installed.
// Replace AndroidGameId/IOSGameId with your IDs from the Unity Dashboard.
using UnityEngine;
#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

public class AdsManager : MonoBehaviour
#if UNITY_ADS
    , IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
#endif
{
    public const string AndroidGameId   = "YOUR_ANDROID_GAME_ID";
    public const string IOSGameId       = "YOUR_IOS_GAME_ID";
    const string        k_RewardedAndroid = "Rewarded_Android";
    const string        k_RewardedIOS     = "Rewarded_iOS";

    public static AdsManager Instance { get; private set; }

    bool          _adLoaded;
    System.Action _onRewardEarned;

    public bool IsAdLoaded => _adLoaded;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
#if UNITY_ADS
        if (GameManager.Instance != null && GameManager.Instance.AdsRemoved) return;
        string gameId = Application.platform == RuntimePlatform.IPhonePlayer ? IOSGameId : AndroidGameId;
        Advertisement.Initialize(gameId, false, this);
#endif
    }

    string RewardedUnitId => Application.platform == RuntimePlatform.IPhonePlayer
        ? k_RewardedIOS : k_RewardedAndroid;

    public void LoadAd()
    {
#if UNITY_ADS
        if (GameManager.Instance != null && GameManager.Instance.AdsRemoved) return;
        _adLoaded = false;
        Advertisement.Load(RewardedUnitId, this);
#endif
    }

    // Grant reward immediately in editor (no ads package) to aid testing.
    public void ShowRewardedAd(System.Action onRewardEarned)
    {
#if UNITY_ADS
        if (!_adLoaded)
        {
            Debug.Log("[Ads] Ad not ready — loading.");
            LoadAd();
            return;
        }
        _onRewardEarned = onRewardEarned;
        Advertisement.Show(RewardedUnitId, this);
#else
        onRewardEarned?.Invoke();
#endif
    }

#if UNITY_ADS
    public void OnInitializationComplete()
    {
        Debug.Log("[Ads] Initialized.");
        LoadAd();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message) =>
        Debug.LogWarning($"[Ads] Init failed: {error} — {message}");

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        _adLoaded = true;
        Debug.Log($"[Ads] Loaded: {adUnitId}");
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        _adLoaded = false;
        Debug.LogWarning($"[Ads] Failed to load {adUnitId}: {error} — {message}");
    }

    public void OnUnityAdsShowStart(string adUnitId)   { }
    public void OnUnityAdsShowClick(string adUnitId)   { }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState state)
    {
        if (state == UnityAdsShowCompletionState.COMPLETED)
            _onRewardEarned?.Invoke();
        _onRewardEarned = null;
        _adLoaded = false;
        LoadAd();
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogWarning($"[Ads] Show failed ({adUnitId}): {error} — {message}");
        _onRewardEarned = null;
        _adLoaded = false;
        LoadAd();
    }
#endif
}
