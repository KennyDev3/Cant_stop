# Hub Upgrade System – Overview and How to Extend

Short reference for how the hub upgrade economy works and how to add or debug upgrades.

---

## 1. High-level flow

```
[Hub] Player buys upgrade in panel (HubUpgradeNodeUI)
        → GameManager.TryPurchaseHubUpgrade(HubUpgradeSO)
        → cost deducted from hub bank, upgrade id stored in HubUnlockState
        → OnHubUpgradeUnlocked(id) fired

[Any scene] Skills / abilities check GameManager.IsHubUpgradeUnlocked(id)
        → if true: ability works (or component enabled)
        → if false: ability locked (or component disabled in Start)

Unlock state lives on GameManager (_hubUnlocks). Never cleared on scene load or when entering Hub.
```

So: **one source of truth** (GameManager + HubUnlockState), **one id per upgrade** (string on the SO and in code). Purchase writes the id; skills read it.

---

## 2. Main pieces

| Piece | Role |
|-------|------|
| **HubUpgradeSO** (ScriptableObject) | Defines one upgrade: id, name, description, tree, order, prerequisite id, cost (list of ResourceSO + amount). Create via Assets → Create → Game → Hub Upgrade. |
| **HubUpgradeKeys** (static class) | String constants for ids (e.g. `ParryUnlock = "parry_unlock"`). Use these in code so ids match the SOs. |
| **HubUnlockState** (class, owned by GameManager) | HashSet of unlocked ids. IsUnlocked(id), Unlock(id), Clear(). |
| **GameManager** | Holds _hubUnlocks, hub bank, TryPurchaseHubUpgrade, IsHubUpgradeUnlocked, UnlockHubUpgrade. Fires OnHubUpgradeUnlocked when an id is unlocked. |
| **ShopManager** | Hub panel controller: Parry/Dash upgrade lists, content parents, node prefab, resource display. OnEnable populates trees and RefreshAll(); purchase calls GameManager.TryPurchaseHubUpgrade. |
| **HubUpgradeNodeUI** | One row per upgrade: name, description, cost, state (Locked / Purchasable / Owned), purchase button. Setup(HubUpgradeSO), Refresh(); on purchase calls GameManager.TryPurchaseHubUpgrade then RefreshAll. |
| **HubUpgradeHouseInteractable** | One house in hub: E opens the panel (assign upgrade panel GameObject). Optional Outline. |
| **HubUpgradeApplyToPlayer** | On player. Subscribes to OnHubUpgradeUnlocked; when id is parry_unlock, enables PlayerParryController and ParryShield so parry works in hub right after purchase. |

Skills that depend on unlocks (examples):

- **Parry:** PlayerParryController.Start() disables self (and ParryShield) if !IsHubUpgradeUnlocked(ParryUnlock). HubUpgradeApplyToPlayer re-enables them when parry_unlock is unlocked (e.g. after purchase in hub).
- **Dash:** ThirdPersonController.HandleDashInput() returns early if !IsHubUpgradeUnlocked(DashUnlock).
- **Parry turret buff:** PlayerParryController.OnSuccessfulParry checks ParryTurretBuff, then TruckTurret.ApplyTempDamageBuff(2f, 10f).
- **Parry return damage:** Projectile.OnParried checks ParryReturnDamage, then damage *= 2f.
- **Dash turret buff:** DashTurretBuffListener subscribes to OnDashStart, checks DashTurretAttackSpeed, then TruckTurret.ApplyTempFireRateBuff.

---

## 3. How to add a new upgrade

**1. Data (ScriptableObject)**  
- Create a new **Hub Upgrade** asset (Assets → Create → Game → Hub Upgrade).  
- Set **id** (e.g. `"my_new_upgrade"`), display name, description, **upgradeTree** (Parry / Dash / etc.), **orderInTree**, **prerequisiteUpgradeId** if needed, **cost** (list of ResourceSO + amount).

**2. Key constant (optional but recommended)**  
- In **HubUpgradeKeys.cs**, add e.g. `public const string MyNewUpgrade = "my_new_upgrade";` so code and SO stay in sync.

**3. Wire into the panel**  
- Add the new **HubUpgradeSO** to the correct list on **ShopManager** (Parry Upgrades or Dash Upgrades, in the right order). No extra UI code; the existing node prefab and Refresh logic handle it.

**4. Use the unlock in code**  
- Where the effect should apply (e.g. a weapon, a movement script), call:
  - `GameManager.Instance != null && GameManager.Instance.IsHubUpgradeUnlocked(HubUpgradeKeys.MyNewUpgrade)`  
  (or the raw string if you didn’t add a constant).  
- If the upgrade **enables a component** that’s disabled by default: either keep “enable by default and disable in Start if not unlocked” (like parry), or have **HubUpgradeApplyToPlayer** (or similar) listen to **OnHubUpgradeUnlocked** and enable that component when the id matches, so it works in the hub right after purchase.

**5. Temporary / timed effects**  
- If the upgrade triggers a timed buff (e.g. turret damage for 10s), the object that owns the buff (e.g. TruckTurret) exposes something like `ApplyTempDamageBuff(mult, duration)` and the trigger (e.g. parry success, dash start) checks the unlock and calls it.

No need to touch RunState, level managers, or save/load for “unlock and use in this session.” Persistence across sessions would be a separate save/load layer later.

---

## 4. Debug and expansion

- **“Unlock not applying”**  
  - Check: SO **id** exactly matches what code uses (HubUpgradeKeys or string).  
  - Check: GameManager exists and is the same instance (e.g. DontDestroyOnLoad).  
  - Check: **Apply Debug Unlocks In Editor** and **Debug Unlock Upgrade Ids** on GameManager if you expect debug unlocks.

- **“Purchased in hub but skill still off”**  
  - For parry: **HubUpgradeApplyToPlayer** must be on the player and listen to **OnHubUpgradeUnlocked**; it enables PlayerParryController and ParryShield when id is parry_unlock.  
  - For dash: no component to enable; HandleDashInput checks each frame, so it works next input after purchase.

- **Adding a new tree (e.g. Passive)**  
  - Add the enum value to **HubUpgradeTree** in HubUpgradeSO.cs.  
  - Add a new list on ShopManager (e.g. passiveUpgrades) and a new content parent + section in the panel.  
  - Populate that list in PopulateTreesOnce (same pattern as Parry/Dash).  
  - Use the same HubUpgradeSO and HubUnlockState; no change to GameManager unlock API.

- **More “enable on purchase” abilities**  
  - In **HubUpgradeApplyToPlayer.ApplyUnlock(string upgradeId)**, add a branch for the new id and enable the right component(s), same pattern as parry.

---

## 5. File map (quick reference)

- **Data / keys:** HubUpgradeSO.cs, HubUpgradeKeys.cs, HubUnlockState.cs, HubUpgrades/*.asset  
- **State / API:** GameManager (hub bank, _hubUnlocks, TryPurchaseHubUpgrade, IsHubUpgradeUnlocked, OnHubUpgradeUnlocked)  
- **Hub UI:** ShopManager.cs, HubUpgradeNodeUI.cs, HubUpgradeHouseInteractable.cs  
- **Apply unlock in hub:** HubUpgradeApplyToPlayer.cs  
- **Skills:** PlayerParryController, ThirdPersonController (dash), Projectile (return damage), TruckTurret (temp buffs), DashTurretBuffListener  

This document plus the code comments should be enough to add upgrades, debug unlocks, and extend the system (new trees, new “enable on purchase” behaviours).
