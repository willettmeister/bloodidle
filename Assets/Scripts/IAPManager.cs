// Manages in-app purchases via Unity IAP.
// UNITY_PURCHASING is defined via versionDefines in GameAssembly.asmdef when
// com.unity.purchasing >= 4.0.0 is installed.
using UnityEngine;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

public class IAPManager : MonoBehaviour
#if UNITY_PURCHASING
    , IDetailedStoreListener
#endif
{
    public const string PID_REMOVE_ADS   = "bloodidle.removeads";
    public const string PID_STARTER_PACK = "bloodidle.starterpack";
    public const string PID_BOOST_SMALL  = "bloodidle.boost.small";
    public const string PID_BOOST_LARGE  = "bloodidle.boost.large";

    public static IAPManager Instance { get; private set; }

#if UNITY_PURCHASING
    IStoreController   _controller;
    IExtensionProvider _extensions;
#endif

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start() => InitIAP();

    void InitIAP()
    {
#if UNITY_PURCHASING
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(PID_REMOVE_ADS,   ProductType.NonConsumable);
        builder.AddProduct(PID_STARTER_PACK, ProductType.NonConsumable);
        builder.AddProduct(PID_BOOST_SMALL,  ProductType.Consumable);
        builder.AddProduct(PID_BOOST_LARGE,  ProductType.Consumable);
        UnityPurchasing.Initialize(this, builder);
#endif
    }

    public void BuyRemoveAds()   => Purchase(PID_REMOVE_ADS);
    public void BuyStarterPack() => Purchase(PID_STARTER_PACK);
    public void BuyBoostSmall()  => Purchase(PID_BOOST_SMALL);
    public void BuyBoostLarge()  => Purchase(PID_BOOST_LARGE);

    void Purchase(string productId)
    {
#if UNITY_PURCHASING
        if (_controller == null) { Debug.LogWarning("[IAP] Store not initialized."); return; }
        _controller.InitiatePurchase(productId);
#else
        Debug.Log($"[IAP] Purchase unavailable — com.unity.purchasing not installed. Product: {productId}");
#endif
    }

#if UNITY_PURCHASING
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        _controller = controller;
        _extensions = extensions;
        // Restore non-consumable receipts in case PlayerPrefs was cleared
        var removeAdsProduct = controller.products.WithID(PID_REMOVE_ADS);
        if (removeAdsProduct != null && removeAdsProduct.hasReceipt)
            GameManager.Instance?.SetAdsRemoved();
        var starterProduct = controller.products.WithID(PID_STARTER_PACK);
        if (starterProduct != null && starterProduct.hasReceipt)
            GameManager.Instance?.SetStarterPackOwned();
    }

    public void OnInitializeFailed(InitializationFailureReason error) =>
        Debug.LogWarning($"[IAP] Init failed: {error}");

    public void OnInitializeFailed(InitializationFailureReason error, string message) =>
        Debug.LogWarning($"[IAP] Init failed: {error} — {message}");

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        switch (args.purchasedProduct.definition.id)
        {
            case PID_REMOVE_ADS:   GameManager.Instance?.SetAdsRemoved();       break;
            case PID_STARTER_PACK: GameManager.Instance?.SetStarterPackOwned(); break;
            case PID_BOOST_SMALL:  GameManager.Instance?.GrantBloodBoostSmall(); break;
            case PID_BOOST_LARGE:  GameManager.Instance?.GrantBloodBoostLarge(); break;
        }
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason) =>
        Debug.LogWarning($"[IAP] Purchase failed: {product.definition.id} — {failureReason}");

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription) =>
        Debug.LogWarning($"[IAP] Purchase failed: {product.definition.id} — {failureDescription.reason}: {failureDescription.message}");
#endif
}
