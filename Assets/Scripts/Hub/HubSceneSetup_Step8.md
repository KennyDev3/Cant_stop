# Step 8: Hub Scene Setup Checklist

Use this when building the Hub scene so one house opens the upgrade panel (both trees) and resources display correctly.

## 1. Resources (already exist)

- **Resource_1, Resource_2, Resource_3** in `Assets/ScriptableObjects/PlayerResources/` are used for costs and bank display. Rename or set display names to Wood / Gold / Iron in the Inspector if desired.

## 2. Upgrade panel (Canvas)

- Create or use a **Canvas** (Screen Space – Overlay or Camera).
- Create a **Panel** (full-screen or centred) that will be the root of the hub upgrade UI. This is the GameObject that will be shown/hidden.
- Add **ShopManager** to this panel GameObject (or to a child that you assign as “Shop Panel”).
- In ShopManager Inspector:
  - **Parry Upgrades:** Add the 3 assets: `ParryUnlock`, `ParryTurretBuff`, `ParryReturnDamage` (from `Assets/ScriptableObjects/HubUpgrades/`).
  - **Dash Upgrades:** Add the 3 assets: `DashUnlock`, `DashTurretAttackSpeed`, `DashFiretrail`.
  - **Parry Content Parent:** Create an empty child under the panel (e.g. “ParryTree”) with a **Vertical Layout Group** so nodes stack. Assign it here.
  - **Dash Content Parent:** Same for “DashTree”. Assign it here.
  - **Node Prefab:** Your prefab that has **HubUpgradeNodeUI** and the 5 UI refs (name, description, cost, state, purchase button). Assign it here.
  - **Shop Panel:** Leave empty to use this GameObject, or assign the panel root.
  - **Resource Bank Slots:** Add 3 entries: (Resource_1, TextMeshProUGUI), (Resource_2, TextMeshProUGUI), (Resource_3, TextMeshProUGUI). Create three labels on the panel for “Wood: 0”, “Gold: 0”, “Iron: 0” and assign resource + text for each.
  - **Close Button:** Optional. Assign a Button that closes the panel (ShopManager closes and locks cursor).
  - **Close When Player Away Distance:** Optional. Set to 0 to disable; or e.g. 4 so the panel closes when the player walks away.

## 3. Node prefab (if not done)

- Create a UI row: e.g. Horizontal Layout with **Name** (TMP), **Description** (TMP), **Cost** (TMP), **State** (TMP), **Purchase** (Button).
- Add **HubUpgradeNodeUI** and assign the 5 references. Save as a prefab and assign that prefab to ShopManager’s **Node Prefab**.

## 4. One house in the scene

- Use a single “house” object (e.g. building or stand-in).
- Add **HubUpgradeHouseInteractable** to it (or to a child that has the collider).
- Assign **Upgrade Panel** to the same panel GameObject that has (or is referenced by) **ShopManager** (the one you show/hide).
- Add a **Collider** (e.g. Box or Sphere) and set it to **Trigger** if you want trigger-based detection. Ensure this object (or its layer) is in the **Interactable** layer used by **PlayerInteractor**.
- Optional: add **Outline** (QuickOutline) on the same GameObject for highlight when in range.

## 5. Player and GameManager

- Hub scene must have the **Player** (with **PlayerInteractor**, **StarterAssetsInputs**) and **GameManager** in the scene (or loaded from Bootstrap). GameManager holds the hub bank and unlock state.

## 6. Quick test

- Enter Play in the Hub scene. Give yourself resources via GameManager’s **Debug Hub Bank** list so you can afford an upgrade.
- Approach the house, press **E** → panel opens, both trees visible, resource counts at top.
- Purchase an upgrade → counts and node states refresh. Close with **E** or the Close button (and optionally by walking away if you set the distance).
