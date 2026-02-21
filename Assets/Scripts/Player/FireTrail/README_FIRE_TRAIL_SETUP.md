# Fire Trail – Setup Guide

Scripts are in place: **FireTrailNode.cs**, **FireTrailController.cs**, and **HubUpgradeApplyToPlayer** (updated for DashFiretrail). Follow these steps in Unity.

---

## 1. Create the "Fire Trailer Node Parent" prefab

1. In the Hierarchy, create an **empty GameObject** and name it **"Fire Trailer Node Parent"**.
2. Add the **FireTrailNode** script to it (Add Component → Fire Trail Node).
3. Add a **Box Collider** (Add Component → Box Collider):
   - Check **Is Trigger**.
   - Set **Size** to cover the area where enemies should take damage (e.g. X: 1.5, Y: 0.5, Z: 1.5). Adjust in play mode if needed.
4. As **children** of this object, add your existing **fire particle effect** (the empty GO that has your particle system(s) and fire effect as children). Drag your prefab or hierarchy into "Fire Trailer Node Parent" so the particles are children.
5. In the **FireTrailNode** component, set:
   - **Damage Per Tick** (e.g. 10).
   - **Tick Interval** (0.25).
   - **Lifetime** (4).
   - **Scale Down Duration** (0.5).
6. Drag "Fire Trailer Node Parent" from the Hierarchy into your **Project** (e.g. under `Assets/Prefabs/Effects/`) to create a **prefab**, then delete the instance from the scene if you don’t want it in the current scene.

---

## 2. Add FireTrailController to the player

1. Select your **player** GameObject (the one that has **ThirdPersonController** and **CharacterController**).
2. Add the **FireTrailController** script (Add Component → Fire Trail Controller).
3. In the inspector:
   - **Node Prefab**: Assign the **"Fire Trailer Node Parent"** prefab you created.
   - **Spawn Interval**: e.g. 0.15–0.2.
   - **Min Move Speed**: e.g. 0.5 (trail only spawns when moving faster than this).
   - **Ground Offset**: e.g. 0.1 (how far below the player’s origin to place nodes; tweak so nodes sit on the ground).
4. Leave **FireTrailController** enabled. It turns itself off in `Start()` if the Dash Firetrail upgrade is not unlocked; **HubUpgradeApplyToPlayer** will turn it back on when the player buys the upgrade in the hub.

---

## 3. Verify upgrade wiring

- **HubUpgradeApplyToPlayer** is already on the player and now enables **FireTrailController** when the upgrade id `dash_firetrail` is unlocked.
- Your **HubUpgradeSO** asset for Dash Firetrail and the upgrade route are already set up; no change needed there.
- Ensure the **Dash Firetrail** upgrade is in your hub’s Dash upgrade list (e.g. on ShopManager) so it appears in the UI.

---

## 4. Quick test

1. In the hub or a run scene, **unlock the Dash Firetrail upgrade** (e.g. via debug unlock on GameManager or by purchasing it).
2. Enter play mode and **move the player** on the ground.
3. You should see fire trail nodes spawning; they last 4 seconds and scale down over the last 0.5s.
4. Spawn an enemy and stand or walk so the enemy is over the trail; the enemy should take damage every 0.25 seconds (check **Damage Per Tick** on the node prefab).

---

## Optional

- **Layers**: If your node prefab is on **Default**, triggers should already overlap enemies. If you use a custom layer for nodes, ensure it overlaps the **Enemy** layer in Edit → Project Settings → Physics (Layer Collision Matrix).
- **Particles**: If your particle effect is set to “Play On Awake”, it will also play when the node spawns; **FireTrailNode** also calls `Play(true)` on all child particle systems in `OnEnable`, so it will start even if Play On Awake is off.
