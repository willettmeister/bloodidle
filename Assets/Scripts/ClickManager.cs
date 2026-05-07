using UnityEngine;

public class ClickManager : MonoBehaviour
{
    public UIManager uiManager;

    public void OnFarmBlood()        => GameManager.Instance.FarmBlood();
    public void OnBuyTank()          => GameManager.Instance.BuyTank();
    public void OnBuyBerserker()     => GameManager.Instance.BuyBerserker();
    public void OnHealSelf()         => GameManager.Instance.UseHealSelf();
    public void OnBuyWorker()        => GameManager.Instance.BuyWorker();
    public void OnBuyBloodRitual()   => GameManager.Instance.BuyBloodRitual();
    public void OnUpgradeBarracks()  => GameManager.Instance.UpgradeBarracks();
    public void OnPrestige()         => GameManager.Instance.Prestige();
    public void OnOpenStats()        => uiManager.ShowStatsPanel();
    public void OnOpenSuggest()      => uiManager.ShowFeaturePanel();
}
