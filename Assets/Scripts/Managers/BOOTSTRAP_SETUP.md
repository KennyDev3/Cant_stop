# Bootstrap Setup

The **Bootstrap** is a small scene (build index 0) that runs first when the game starts. It creates the persistent core (GameManager) and then loads the first "real" scene (e.g. Main Menu). **MainMenu and Hub do not need a bootstrap**—they are the scenes that get **loaded by** the Bootstrap or by `RequestScene`; they are normal scenes.

---

## Step 1: Create the Bootstrap scene

1. In Unity: **File → New Scene** (or Ctrl+N).
2. Save it as **Bootstrap** in your scenes folder (e.g. `Assets/Scenes/Shahar Scenes/HubAndWorldScenes/Bootstrap.unity`).
3. Leave the scene empty (no gameplay objects needed).

---

## Step 2: Add GameManager to the Bootstrap scene

1. **Option A:** Copy the **GameManager** GameObject from World_1 (or MainMenu):  
   - Open World_1, select the GameManager object in the hierarchy, Ctrl+C, then open Bootstrap and Ctrl+V.
2. **Option B:** Create a new empty GameObject, name it **GameManager**, and add the **GameManager** component. Then configure it in the Inspector (Main Menu scene name, Hub scene name, Level Order, Level Goals) the same as in your other scenes.

The Bootstrap scene must contain GameManager so that when the game runs, GameManager exists and persists (DontDestroyOnLoad) before any other scene loads.

---

## Step 3: Add the Bootstrap GameObject

1. In the Bootstrap scene hierarchy: **Right-click → Create Empty**. Name it **Bootstrap**.
2. With **Bootstrap** selected, click **Add Component** and add the **Bootstrap** script.
3. In the Inspector, set **First Scene Name** to the scene you want to load after bootstrap (e.g. **MainMenu**). Use the **exact scene name** as in Build Settings (e.g. `MainMenuWithHub` if that’s your main menu scene name).

---

## Step 4: Add Bootstrap to Build Settings as index 0

1. **File → Build Settings** (Ctrl+Shift+B).
2. Drag **Bootstrap** into the **Scenes In Build** list (or click **Add Open Scenes** while Bootstrap is open).
3. Drag **Bootstrap** to the **top** of the list so it is **index 0** (the first scene that loads when you run the game or build).
4. Ensure your other scenes (MainMenu, World_1, World_2, Hub, etc.) are also in the list in the order you want.

---

## Step 5: Keep GameManager in other scenes (for Editor play-from-any-scene)

Leave **GameManager** in **MainMenu**, **World_1**, **World_2**, and **Hub** as you have now. When you run from the **build**, Bootstrap loads first and creates GameManager; then MainMenu loads and its GameManager destroys itself (duplicate). When you **Play from World_1** in the Editor (without going through Bootstrap), World_1’s GameManager becomes the instance. So you don’t need to remove GameManager from other scenes.

---

## Step 6 (Editor only): Script Execution Order when not using Bootstrap

When you **Play from World_1** (or any scene) in the Editor, the Bootstrap scene does **not** run—Unity loads that scene directly. So GameManager and LevelObjectiveManager run Awake in an undefined order. To ensure GameManager exists first so that LevelObjectiveManager can subscribe to OnSceneReady in Awake:

1. **Edit → Project Settings → Script Execution Order**
2. Add **GameManager** (and optionally **Bootstrap**) and set to **-100** (or any value before Default **0**).

Then when you run from World_1 in the Editor, GameManager.Awake runs first, so `GameManager.Instance` is set when LevelObjectiveManager.Awake runs and it can subscribe. When you run from the **build**, Bootstrap runs first so GameManager already exists before any scene loads—no execution order needed for that case.

---

## Summary

| Scene       | Contains GameManager? | Contains Bootstrap script? | Role                                      |
|------------|------------------------|----------------------------|-------------------------------------------|
| **Bootstrap** | Yes                    | Yes (on "Bootstrap" GameObject) | Runs first (build index 0); loads First Scene |
| **MainMenu**  | Yes (for Editor)      | No                         | Loaded by Bootstrap or by RequestScene    |
| **Hub**       | Yes (for Editor)      | No                         | Loaded by RequestScene                    |
| **World_1, World_2** | Yes (for Editor) | No                         | Loaded by RequestScene                    |

---

## Debug: Current world in Inspector

On **GameManager**, in the Inspector under **Debug**:

- **Show Debug Info**: When checked, the debug section updates at runtime.
- **_Debug Current World**: Shows the current **Scene** name, **Rotation** (level index), and **State** (MainMenu, Playing, Hub, etc.).

Use this to confirm which world you’re on (World_1, World_2, Hub, MainMenu) and the current rotation while play-testing.
