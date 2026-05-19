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
    public void OnUseBloodStorm()          => GameManager.Instance.UseBloodStorm();
    public void OnUseBloodPact()           => GameManager.Instance.UseBloodPact();
    public void OnBuyWorker()              => GameManager.Instance.BuyWorker();
    public void OnBuyBloodRitual()         => GameManager.Instance.BuyBloodRitual();
    public void OnUpgradeBarracks()        => GameManager.Instance.UpgradeBarracks();
    public void OnUpgradeFortification()   => GameManager.Instance.UpgradeFortification();
    public void OnUpgradeWeapon()          => GameManager.Instance.UpgradeWeapon();
    public void OnUpgradeArmor()           => GameManager.Instance.UpgradeArmor();
    public void OnUpgradeTalisman()        => GameManager.Instance.UpgradeTalisman();
    public void OnToggleFormation()        => GameManager.Instance.ToggleFormation();
    public void OnPrestige()               => GameManager.Instance.RequestPrestige();
    public void OnConfirmTalent0()         => GameManager.Instance.ConfirmPrestige(0);
    public void OnConfirmTalent1()         => GameManager.Instance.ConfirmPrestige(1);
    public void OnConfirmTalent2()         => GameManager.Instance.ConfirmPrestige(2);
    public void OnCancelPrestige()         => GameManager.Instance.CancelPrestige();
    public void OnUseSoulSacrifice()       => GameManager.Instance.UseSoulSacrifice();
    public void OnStartDailyChallenge()    => GameManager.Instance.StartDailyChallenge();
    public void OnPurify()                 => GameManager.Instance.Purify();
    public void OnBuyPSoldierCap()         => GameManager.Instance.BuyPSoldierCap();
    public void OnBuyPClickBonus()         => GameManager.Instance.BuyPClickBonus();
    public void OnBuyPRitualEff()          => GameManager.Instance.BuyPRitualEff();
    public void OnBuyPWeaponHeadStart()    => GameManager.Instance.BuyPWeaponHeadStart();
    public void OnBuyPBloodTithe()         => GameManager.Instance.BuyPBloodTithe();
    public void OnBuyPIronWall()           => GameManager.Instance.BuyPIronWall();
    public void OnBuySSBossTimer()         => GameManager.Instance.BuySSBossTimer();
    public void OnBuySSDoubleChest()       => GameManager.Instance.BuySSDoubleChest();
    public void OnBuySSRollback()          => GameManager.Instance.BuySSRollback();
    public void OnBuySSBloodTap()          => GameManager.Instance.BuySSBloodTap();
    public void OnDepositToBank()          => GameManager.Instance.DepositToBank(Math.Floor(GameManager.Instance.Blood * 0.1));
    public void OnWithdrawFromBank()       => GameManager.Instance.WithdrawFromBank();
    public void OnBuyPaladin()             => GameManager.Instance.BuyPaladin();
    public void OnUpgradeSurge()           => GameManager.Instance.UpgradeSurge();
    public void OnUpgradeHealSelf()        => GameManager.Instance.UpgradeHealSelf();
    public void OnOpenStats()              => uiManager.ShowStatsPanel();
    public void OnOpenSettings()           => uiManager.ShowSettingsPanel();
    public void OnOpenSuggest()            => uiManager.ShowFeaturePanel();
    public void OnToggleSound()            => GameManager.Instance.ToggleSound();
    public void OnToggleNotifications()    => GameManager.Instance.ToggleNotifications();
    public void OnResetData()              => GameManager.Instance.ResetAllData();
}
