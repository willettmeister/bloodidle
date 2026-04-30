using UnityEngine;

public class ClickManager : MonoBehaviour
{
    public UIManager uiManager;

    public void OnFarmBlood()       => GameManager.Instance.FarmBlood();
    public void OnBuySoldier()      => GameManager.Instance.BuySoldier();
    public void OnHealSelf()        => GameManager.Instance.UseHealSelf();
    public void OnBuyWorker()       => GameManager.Instance.BuyWorker();
    public void OnUpgradeBarracks() => GameManager.Instance.UpgradeBarracks();
    public void OnOpenSuggest()     => uiManager.ShowFeaturePanel();
}
