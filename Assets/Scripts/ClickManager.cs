using UnityEngine;
using System;

public class ClickManager : MonoBehaviour
{
    public UIManager uiManager;

    public void OnFarmBlood()              => GameManager.Instance.FarmBlood();
    public void OnBuyTank()                => GameManager.Instance.BuyTank();
    public void OnBuyBerserker()           => GameManager.Instance.BuyBerserker();
    public void OnHealSelf()               => GameManager.Instance.UseHealSelf();
    public void OnUseSurge()               => GameManager.Instance.UseSurge();
    public void OnUseBloodPact()           => GameManager.Instance.UseBloodPact();
    public void OnBuyWorker()              => GameManager.Instance.BuyWorker();
    public void OnBuyBloodRitual()         => GameManager.Instance.BuyBloodRitual();
    public void OnUpgradeBarracks()        => GameManager.Instance.UpgradeBarracks();
    public void OnUpgradeFortification()   => GameManager.Instance.UpgradeFortification();
    public void OnUpgradeWeapon()          => GameManager.Instance.UpgradeWeapon();
    public void OnUpgradeArmor()           => GameManager.Instance.UpgradeArmor();
    public void OnUpgradeTalisman()        => GameManager.Instance.UpgradeTalisman();
    public void OnToggleFormation()        => GameManager.Instance.ToggleFormation();
    public void OnPrestige()               => GameManager.Instance.Prestige();
    public void OnBuyPSoldierCap()         => GameManager.Instance.BuyPSoldierCap();
    public void OnBuyPClickBonus()         => GameManager.Instance.BuyPClickBonus();
    public void OnBuyPRitualEff()          => GameManager.Instance.BuyPRitualEff();
    public void OnBuyPWeaponHeadStart()    => GameManager.Instance.BuyPWeaponHeadStart();
    public void OnBuyPBloodTithe()         => GameManager.Instance.BuyPBloodTithe();
    public void OnBuyPIronWall()           => GameManager.Instance.BuyPIronWall();
    public void OnBuySSBossTimer()         => GameManager.Instance.BuySSBossTimer();
    public void OnBuySSDoubleChest()       => GameManager.Instance.BuySSDoubleChest();
    public void OnBuySSRollback()          => GameManager.Instance.BuySSRollback();
    public void OnDepositToBank()          => GameManager.Instance.DepositToBank(Math.Floor(GameManager.Instance.Blood * 0.1));
    public void OnWithdrawFromBank()       => GameManager.Instance.WithdrawFromBank();
    public void OnOpenStats()              => uiManager.ShowStatsPanel();
    public void OnOpenSuggest()            => uiManager.ShowFeaturePanel();
}
