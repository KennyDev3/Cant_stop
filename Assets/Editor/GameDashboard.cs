using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using StarterAssets;

public class GameDashboard : EditorWindow
{
    // --- 1. THE FIELDS ---
    [Header("Scene Objects")]
    public GameObject gameManagerObj;
    public GameObject levelManagersObj;

    [Header("Player Scripts")]
    public ThirdPersonController playerController;
    public PlayerHealth playerHealth;

    [Header("Data Assets")]
    public TurretData turretData;

    // --- KEYS FOR SAVING ---
    // We use these keys to store the names of your objects in the Editor's memory
    private const string KEY_GM_NAME = "DASH_GM_NAME";
    private const string KEY_LVL_NAME = "DASH_LVL_NAME";
    private const string KEY_PLAYER_NAME = "DASH_PLAYER_NAME";

    private Vector2 scrollPos;
    private GUIStyle headerStyle;

    [MenuItem("Tools/Game Dashboard")]
    public static void ShowWindow()
    {
        GetWindow<GameDashboard>("Game Dashboard");
    }

    // This runs when you open the window OR when you hit Play
    private void OnEnable()
    {
        // 1. Recover Turret Data (Easy, it's an asset)
        if (turretData == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:TurretData");
            if (guids.Length > 0)
                turretData = AssetDatabase.LoadAssetAtPath<TurretData>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        // 2. Recover Scene Objects using the Names we saved
        RestoreReferences();
    }

    void OnGUI()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, margin = new RectOffset(0, 0, 10, 5) };
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // =========================================================
        // SECTION 1: REFERENCES (Smart Persistence)
        // =========================================================
        GUILayout.Label("SETUP REFERENCES", headerStyle);
        EditorGUILayout.HelpBox("Drag objects here once. The tool will remember them by Name.", MessageType.None);

        // --- GAME MANAGER ---
        EditorGUI.BeginChangeCheck();
        gameManagerObj = (GameObject)EditorGUILayout.ObjectField("Game Manager", gameManagerObj, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck() && gameManagerObj != null)
        {
            // If user dragged something new, save its NAME
            EditorPrefs.SetString(KEY_GM_NAME, gameManagerObj.name);
        }

        // --- LEVEL MANAGER ---
        EditorGUI.BeginChangeCheck();
        levelManagersObj = (GameObject)EditorGUILayout.ObjectField("Level Managers", levelManagersObj, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck() && levelManagersObj != null)
        {
            EditorPrefs.SetString(KEY_LVL_NAME, levelManagersObj.name);
        }

        // --- PLAYER ---
        // We use the Controller to determine the Player Object Name
        EditorGUI.BeginChangeCheck();
        playerController = (ThirdPersonController)EditorGUILayout.ObjectField("Player Controller", playerController, typeof(ThirdPersonController), true);
        if (EditorGUI.EndChangeCheck() && playerController != null)
        {
            EditorPrefs.SetString(KEY_PLAYER_NAME, playerController.gameObject.name);
            // Auto-grab health if controller is assigned
            if (playerHealth == null) playerHealth = playerController.GetComponent<PlayerHealth>();
        }

        playerHealth = (PlayerHealth)EditorGUILayout.ObjectField("Player Health", playerHealth, typeof(PlayerHealth), true);
        turretData = (TurretData)EditorGUILayout.ObjectField("Turret Data (SO)", turretData, typeof(TurretData), false);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // =========================================================
        // SECTION 2: SCENE CONTROLS
        // =========================================================
        if (gameManagerObj != null || levelManagersObj != null)
        {
            GUILayout.Label("SCENE TOGGLES", headerStyle);

            GUILayout.BeginHorizontal();

            if (gameManagerObj != null)
            {
                bool active = gameManagerObj.activeSelf;
                if (GUILayout.Button(active ? "Game Manager: ON" : "Game Manager: OFF"))
                {
                    Undo.RecordObject(gameManagerObj, "Toggle GM");
                    gameManagerObj.SetActive(!active);
                }
            }

            if (levelManagersObj != null)
            {
                bool active = levelManagersObj.activeSelf;
                if (GUILayout.Button(active ? "ENEMIES: ON" : "ENEMIES: OFF"))
                {
                    Undo.RecordObject(levelManagersObj, "Toggle Enemies");
                    levelManagersObj.SetActive(!active);
                }
            }

            GUILayout.EndHorizontal();
        }


        // =========================================================
        // SECTION 3: PLAYER STATS
        // =========================================================
        if (playerController != null && playerHealth != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("PLAYER SETTINGS", headerStyle);

            playerController.MoveSpeed = EditorGUILayout.Slider("Walk Speed", playerController.MoveSpeed, 0f, 20f);
            playerController.SprintSpeed = EditorGUILayout.Slider("Sprint Speed", playerController.SprintSpeed, playerController.MoveSpeed, 30f);

            GUILayout.Space(5);
            string[] options = new string[] { "Gamepad Mode", "Mouse Mode" };
            int selectedIndex = playerController.UseMouseRotation ? 1 : 0;
            int newIndex = GUILayout.Toolbar(selectedIndex, options, GUILayout.Height(25));
            if (newIndex != selectedIndex)
            {
                playerController.UseMouseRotation = (newIndex == 1);
                EditorUtility.SetDirty(playerController);
            }

            GUILayout.Space(5);
            playerHealth.maxHealth = EditorGUILayout.FloatField("Max HP", playerHealth.maxHealth);
        }

        // =========================================================
        // SECTION 4: TURRET STATS
        // =========================================================
        if (turretData != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("TURRET DATA", headerStyle);

            // ScriptableObjects persist naturally, no tricks needed here
            Undo.RecordObject(turretData, "Change Turret Data");
            turretData.damage = EditorGUILayout.FloatField("Damage", turretData.damage);
            turretData.fireRate = EditorGUILayout.FloatField("Fire Rate", turretData.fireRate);
            turretData.targetRange = EditorGUILayout.FloatField("Range", turretData.targetRange);

            if (GUI.changed) EditorUtility.SetDirty(turretData);
        }

        EditorGUILayout.EndScrollView();
    }

    // --- HELPER TO RESTORE REFERENCES ---
    void RestoreReferences()
    {
        // 1. Load Game Manager
        string gmName = EditorPrefs.GetString(KEY_GM_NAME, "");
        if (!string.IsNullOrEmpty(gmName) && gameManagerObj == null)
        {
            gameManagerObj = FindObjectEvenIfDisabled(gmName);
        }

        // 2. Load Level Manager
        string lvlName = EditorPrefs.GetString(KEY_LVL_NAME, "");
        if (!string.IsNullOrEmpty(lvlName) && levelManagersObj == null)
        {
            levelManagersObj = FindObjectEvenIfDisabled(lvlName);
        }

        // 3. Load Player
        string playerName = EditorPrefs.GetString(KEY_PLAYER_NAME, "");
        if (!string.IsNullOrEmpty(playerName) && playerController == null)
        {
            GameObject playerObj = FindObjectEvenIfDisabled(playerName);
            if (playerObj != null)
            {
                playerController = playerObj.GetComponent<ThirdPersonController>();
                playerHealth = playerObj.GetComponent<PlayerHealth>();
            }
        }
    }

    // Needed because GameObject.Find() fails on disabled objects
    GameObject FindObjectEvenIfDisabled(string name)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = currentScene.GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            if (obj.name == name) return obj;

            // Check immediate children too (Common for Managers)
            Transform result = obj.transform.Find(name);
            if (result != null) return result.gameObject;
        }
        return null;
    }
}