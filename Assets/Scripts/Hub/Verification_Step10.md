# Step 10: Verification Checklist

Use this to confirm the hub upgrade backbone is working end-to-end.

## 1. Hub scene

- [ ] Open Hub scene. GameManager and Player present (or loaded from Bootstrap).
- [ ] One house with **HubUpgradeHouseInteractable**; **Upgrade Panel** assigned to the panel with **ShopManager**.
- [ ] Approach house, press **E** → upgrade panel opens with Parry and Dash trees and resource counts.
- [ ] **Debug Hub Bank** on GameManager has some resources (e.g. 50/50/50 for each) so you can afford an upgrade.
- [ ] Purchase one upgrade (e.g. Parry Unlock) → resource counts decrease, node shows "Owned", panel still open.
- [ ] Close panel (E or Close button) → cursor locked, panel hidden.

## 2. Level with no debug unlocks

- [ ] Disable **Apply Debug Unlocks In Editor** or clear **Debug Unlock Upgrade Ids**.
- [ ] Start a level (from Hub or open level scene and Play). Parry and Dash should be **locked** (no parry, no dash).
- [ ] Confirm: no "[GameManager] Debug: unlocked..." log when entering Play without debug IDs.

## 3. Level with debug unlocks

- [ ] Enable **Apply Debug Unlocks In Editor** and add e.g. `parry_unlock`, `dash_unlock`, `parry_turret_buff`, `dash_turret_attack_speed`.
- [ ] Start a level. You should see the debug log about unlocked upgrades.
- [ ] Parry works (input triggers parry). Dash works (jump/dash input triggers dash).
- [ ] After a successful parry, turret damage is higher for ~10s (log or damage numbers). After a dash, turret fires faster for ~10s.
- [ ] Parry a projectile so it hits an enemy: with `parry_return_damage` in the list, enemy takes double damage from the returned projectile.

## 4. Persistence (Editor)

- [ ] In Hub, buy an upgrade (e.g. Parry Unlock). Start a run (go to level). Parry should work in the level.
- [ ] Return to Hub (portal or load Hub scene). Open panel → that upgrade still shows "Owned". Unlocks are not cleared when entering Hub.

## 5. Optional: DashTurretBuffListener

- [ ] Player has **DashTurretBuffListener**. After dashing with `dash_turret_attack_speed` unlocked, turret fire rate increases (and you see the "[Dash] Turret attack speed buff active." log if enabled).

If all items above pass, the hub upgrade backbone is verified. For save/load persistence to disk, a later system can be added; current state is session-only in memory.
