# Portal System – Setup & Overview

## Logic and implementation overview

### Architecture

- **Portal (MonoBehaviour)**  
  Handles visuals (spawn height, label animation, trigger). When the player enters, it asks its **destination** for a `SceneRequest` and calls `GameManager.RequestScene(request)`. It does **not** decide where to go; that lives in the destination.

- **PortalDestinationSO (ScriptableObject)**  
  Defines *where* a portal sends the player (Next Level, Hub, or Specific Scene) and *which prefab* to spawn. Each asset has **Prefab**, optional **Label Text** (e.g. "Continue Run", "Return to Hub") that the Portal applies to its 3D label at runtime, and preserve-run-state. One SO = one destination + one visual.

- **LevelObjectiveManager**  
  When the objective is complete, it decides *how many* portals to spawn and *which* destinations to use:
  - **Not last level (e.g. World_1):** Spawns **two** portals: **Return to Hub** (left, lower X) and **Continue Run** (right, higher X), same Y and Z, with `portalSpacingX` between them.
  - **Last level (e.g. World_2):** Spawns **one** portal (End Run prefab or same prefab with End Run destination) that goes to Hub.

- **GameManager**  
  Exposes `IsCurrentLevelLastInSequence()` (based on `levelOrder`). Single entry point for scene loads: `RequestScene(SceneRequest)`.

### Spawn positions

- **Two portals (non‑last level):** Same Y and Z; only X differs.
  - **Return to Hub** = base position + **left** (`-portalSpacingX/2`).
  - **Continue Run** = base position + **right** (`+portalSpacingX/2`).
- **One portal (last level):** Single spawn at base position (X doesn’t matter).
- Each portal’s **Y** is then overridden in `Portal.Start()` with the prefab’s `spawnPositionY` so all portals share the same height.

---

## Creating the ScriptableObjects (SOs)

### 1. Create the three destination assets

1. In the **Project** window, right‑click in the folder where you keep portal/data assets (e.g. `Assets/Data/Portal` or `Assets/ScriptableObjects`).
2. **Create → Portal → Portal Destination**.
3. Create **three** assets and name them clearly, e.g.:
   - `Destination_ContinueRun`
   - `Destination_ReturnToHub`
   - `Destination_EndRun`

### 2. Configure each asset (Inspector)

On **each** destination SO, set **Prefab** (the portal visual for this destination) plus destination and display:

**Destination_ContinueRun**

- **Prefab:** your Continue Run portal prefab (with Portal component)
- **Destination Type:** `Next Level In Sequence`
- **Label Text:** `Continue Run` (or leave empty to keep prefab default)
- **Preserve Run State:** ✓ (checked)

**Destination_ReturnToHub**

- **Prefab:** your Return to Hub portal prefab
- **Destination Type:** `Hub`
- **Label Text:** `Return to Hub`
- **Preserve Run State:** ✓

**Destination_EndRun**

- **Prefab:** your End Run portal prefab
- **Destination Type:** `Hub`
- **Label Text:** `End Run` (or whatever you want on the last-level portal)
- **Preserve Run State:** ✓

**Future: specific scene (e.g. secret level)**

- **Prefab:** your secret-area portal prefab
- **Destination Type:** `Specific Scene`
- **Scene Name:** exact scene name in Build Settings (e.g. `Secret_Level_3`)
- **Label Text:** e.g. `Secret Area`
- **Preserve Run State:** as needed

Each SO is the single source of truth: it defines both **where** the portal goes and **which prefab** to spawn.

### 3. Assign SOs on LevelObjectiveManager

On the **LevelObjectiveManager** (in the scene or prefab) you only wire the three destination SOs and spawn settings. No prefab fields:

- **Continue Run Destination:** drag `Destination_ContinueRun`
- **Return To Hub Destination:** drag `Destination_ReturnToHub`
- **End Run Destination:** drag `Destination_EndRun`
- **Portal Spawn Distance:** e.g. `6`
- **Portal Spacing X:** e.g. `4` – horizontal distance between the two portals (non–last level only)

Prefabs are taken from the SOs; LevelObjectiveManager just chooses which SO to use per spawn.

### 4. Portal prefab(s)

- Every portal prefab must have the **Portal** component.
- Assign each prefab on its **PortalDestinationSO** (Prefab field). You can leave **Destination** empty on the prefab; `LevelObjectiveManager` sets it at spawn via `SetDestination(...)`.
- If you want a default label in the editor, set it on the prefab’s TextMeshPro; it will be overridden at runtime when the SO has **Label Text** set.

---

## What you need to know for the future

1. **New destination type (e.g. another hub or special level)**  
   Create a new **Portal Destination** asset: set **Destination Type** (Hub / Next Level / Specific Scene) and **Scene Name** if Specific Scene. No code change.

2. **Different visual per portal type**  
   Each **PortalDestinationSO** has its own **Prefab** field. Assign a different prefab per SO (Continue Run, Return to Hub, End Run) so each portal type has its own look. One SO = one destination + one visual.

3. **Level order**  
   **GameManager → Level Order** must list scene names in order (e.g. `World_1`, `World_2`). Last entry = last level → one End Run portal; all others → two portals (Continue + Return to Hub).

4. **Persistence**  
   Hub and Next Level use `PreserveRunState = true` in the SOs so run state is kept. When you add resources/persistence, `GameManager`’s existing `CollectRunState` / `ApplyRunState` flow will apply; no change needed in the portal system.

5. **One script for all portals**  
   Continue Run, Return to Hub, and End Run all use the same **Portal** script; behaviour is driven by the **PortalDestinationSO** reference (set in prefab or at spawn).

---

## File reference

| File | Role |
|------|------|
| `Portal.cs` | Presentation + trigger; gets `SceneRequest` from destination and calls `GameManager.RequestScene`. |
| `PortalDestinationSO.cs` | ScriptableObject: destination type, optional scene name and label text, `GetRequest()`. |
| `LevelObjectiveManager.cs` | Spawns one or two portals with correct positions and destinations. |
| `GameManager.cs` | `IsCurrentLevelLastInSequence()`, `RequestScene(SceneRequest)`, level order. |
| `SceneRequest.cs` | Struct and factory methods for scene transition requests. |
