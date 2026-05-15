# ClickManager

Thin bridge between Unity button `onClick` events and `GameManager` public methods. Contains zero logic — every method is a one-liner forward. `SceneBuilder` wires each button to its corresponding `ClickManager` method via `UnityEventTools`.

Assembly: `GameAssembly`  
File: `Assets/Scripts/ClickManager.cs`

---

## Fields

| Field | Type | Notes |
|-------|------|-------|
| `uiManager` | `UIManager` | Set by `SceneBuilder`; used only for panel-open methods |

---

## Button → Method Mapping

| ClickManager Method | Forwards to |
|--------------------|-------------|
| `OnFarmBlood()` | `GameManager.FarmBlood()` |
| `OnBuyTank()` | `GameManager.BuyTank()` |
| `OnBuyBerserker()` | `GameManager.BuyBerserker()` |
| `OnBuyPaladin()` | `GameManager.BuyPaladin()` |
| `OnHealSelf()` | `GameManager.UseHealSelf()` |
| `OnUseSurge()` | `GameManager.UseSurge()` |
| `OnUpgradeSurge()` | `GameManager.UpgradeSurge()` |
| `OnUpgradeHealSelf()` | `GameManager.UpgradeHealSelf()` |
| `OnUseBloodPact()` | `GameManager.UseBloodPact()` |
| `OnBuyWorker()` | `GameManager.BuyWorker()` |
| `OnBuyBloodRitual()` | `GameManager.BuyBloodRitual()` |
| `OnUpgradeBarracks()` | `GameManager.UpgradeBarracks()` |
| `OnUpgradeFortification()` | `GameManager.UpgradeFortification()` |
| `OnUpgradeWeapon()` | `GameManager.UpgradeWeapon()` |
| `OnUpgradeArmor()` | `GameManager.UpgradeArmor()` |
| `OnUpgradeTalisman()` | `GameManager.UpgradeTalisman()` |
| `OnToggleFormation()` | `GameManager.ToggleFormation()` |
| `OnPrestige()` | `GameManager.RequestPrestige()` |
| `OnConfirmTalent0()` | `GameManager.ConfirmPrestige(0)` |
| `OnConfirmTalent1()` | `GameManager.ConfirmPrestige(1)` |
| `OnConfirmTalent2()` | `GameManager.ConfirmPrestige(2)` |
| `OnCancelPrestige()` | `GameManager.CancelPrestige()` |
| `OnUseSoulSacrifice()` | `GameManager.UseSoulSacrifice()` |
| `OnStartDailyChallenge()` | `GameManager.StartDailyChallenge()` |
| `OnPurify()` | `GameManager.Purify()` |
| `OnBuyPSoldierCap()` | `GameManager.BuyPSoldierCap()` |
| `OnBuyPClickBonus()` | `GameManager.BuyPClickBonus()` |
| `OnBuyPRitualEff()` | `GameManager.BuyPRitualEff()` |
| `OnBuyPWeaponHeadStart()` | `GameManager.BuyPWeaponHeadStart()` |
| `OnBuyPBloodTithe()` | `GameManager.BuyPBloodTithe()` |
| `OnBuyPIronWall()` | `GameManager.BuyPIronWall()` |
| `OnBuySSBossTimer()` | `GameManager.BuySSBossTimer()` |
| `OnBuySSDoubleChest()` | `GameManager.BuySSDoubleChest()` |
| `OnBuySSRollback()` | `GameManager.BuySSRollback()` |
| `OnBuySSBloodTap()` | `GameManager.BuySSBloodTap()` |
| `OnDepositToBank()` | `GameManager.DepositToBank(floor(Blood × 0.1))` — 10% of current blood |
| `OnWithdrawFromBank()` | `GameManager.WithdrawFromBank()` |
| `OnOpenStats()` | `UIManager.ShowStatsPanel()` |
| `OnOpenSettings()` | `UIManager.ShowSettingsPanel()` |
| `OnOpenSuggest()` | `UIManager.ShowFeaturePanel()` |
| `OnToggleSound()` | `GameManager.ToggleSound()` |
| `OnToggleNotifications()` | `GameManager.ToggleNotifications()` |
| `OnResetData()` | `GameManager.ResetAllData()` |
