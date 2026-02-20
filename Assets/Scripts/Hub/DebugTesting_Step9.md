# Step 9: Debug / Testing Mode for Hub Upgrades

Use this to test levels with specific upgrades unlocked without buying them in the Hub.

## How it works

- On **GameManager**, in the **Hub upgrade debug** section:
  - **Apply Debug Unlocks In Editor** – when enabled, every time you enter Play (in the Editor), the upgrade IDs in the list below are unlocked for free.
  - **Debug Unlock Upgrade Ids** – list of strings. Add the same IDs you use in code (see `HubUpgradeKeys`).

## Upgrade IDs (use these exact strings)

| ID | Upgrade |
|----|--------|
| `parry_unlock` | Parry ability |
| `parry_turret_buff` | Parry → turret +100% damage 10s |
| `parry_return_damage` | Parry → returned projectile +100% damage |
| `dash_unlock` | Dash ability |
| `dash_turret_attack_speed` | Dash → turret +100% fire rate |
| `dash_firetrail` | Dash firetrail (placeholder) |

## Setup in Unity

1. Select the **GameManager** (e.g. in Bootstrap or your persistent manager object).
2. Enable **Apply Debug Unlocks In Editor**.
3. In **Debug Unlock Upgrade Ids**, add elements and type the IDs above (e.g. `parry_unlock`, `dash_unlock`).

## What to test

- **Start a level scene directly** (e.g. open World_1 in the Editor and press Play). With debug unlocks, parry/dash and their upgrades should work as if you had bought them in the Hub.
- **No debug list / disabled** – parry and dash should be locked; no turret buffs from parry/dash.
- **Only `parry_unlock`** – parry works; turret buff and return damage do not until you add those IDs.
- **Only `dash_unlock`** – dash works; turret haste does not until you add `dash_turret_attack_speed`.

## Note

- Debug unlocks apply only in the **Unity Editor** (`#if UNITY_EDITOR`). Builds do not get them unless you change that.
- Unlocks are applied in **Awake** on GameManager, so they are active before any level logic runs.
