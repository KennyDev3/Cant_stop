using UnityEngine;
using UnityEditor;
using StarterAssets;
using System;
using System.Linq;

public class GameDashboard : EditorWindow
{
    // --- 1. THE FIELDS ---
    [Header("Auto-resolved (Scene)")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private EnemyDirector enemyDirector;
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerGarbageHandler playerGarbage;

    [Header("Auto-resolved (Assets)")]
    [SerializeField] private TurretData turretData;

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
        TryAutoResolveAll();
    }

    private void OnFocus() => TryAutoResolveAll();
    private void OnHierarchyChange() => TryAutoResolveAll();
    private void OnProjectChange() => TryAutoResolveAll();

    void OnGUI()
    {
        headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, margin = new RectOffset(0, 0, 10, 5) };
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // =========================================================
        // SECTION 1: REFERENCES (Auto Resolve)
        // =========================================================
        GUILayout.Label("SETUP REFERENCES", headerStyle);
        EditorGUILayout.HelpBox("This dashboard auto-finds objects in loaded scenes / play mode. If something is missing, open a gameplay scene (World_1/World_2/Hub) or enter Play mode.", MessageType.Info);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("GameManager", gameManager != null ? gameManager.gameObject.name : "(not found)");
            EditorGUILayout.LabelField("EnemyDirector", enemyDirector != null ? enemyDirector.gameObject.name : "(not found)");
            EditorGUILayout.LabelField("Player Controller", playerController != null ? playerController.gameObject.name : "(not found)");
            EditorGUILayout.LabelField("Player Health", playerHealth != null ? playerHealth.gameObject.name : "(not found)");
            EditorGUILayout.LabelField("Player Garbage", playerGarbage != null ? playerGarbage.gameObject.name : "(not found)");
        }

        // TurretData is an asset; if multiple exist, allow manual pick.
        turretData = (TurretData)EditorGUILayout.ObjectField("Turret Data (SO)", turretData, typeof(TurretData), false);
        if (GUILayout.Button("Re-scan TurretData assets"))
            turretData = FindFirstAssetOfType<TurretData>();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // =========================================================
        // SECTION 2: SCENE CONTROLS
        // =========================================================
        if (gameManager != null || enemyDirector != null)
        {
            GUILayout.Label("SCENE TOGGLES", headerStyle);

            GUILayout.BeginHorizontal();

            if (gameManager != null)
            {
                bool active = gameManager.gameObject.activeSelf;
                if (GUILayout.Button(active ? "Game Manager: ON" : "Game Manager: OFF"))
                {
                    Undo.RecordObject(gameManager.gameObject, "Toggle GameManager");
                    gameManager.gameObject.SetActive(!active);
                }
            }

            if (enemyDirector != null)
            {
                bool active = enemyDirector.gameObject.activeSelf;
                if (GUILayout.Button(active ? "ENEMIES: ON" : "ENEMIES: OFF"))
                {
                    Undo.RecordObject(enemyDirector.gameObject, "Toggle EnemyDirector");
                    enemyDirector.gameObject.SetActive(!active);
                }
            }

            GUILayout.EndHorizontal();
        }


        // =========================================================
        // SECTION 3: PLAYER STATS
        // =========================================================
        if (playerController != null || playerHealth != null || playerGarbage != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("PLAYER SETTINGS", headerStyle);

            if (playerController != null)
            {
                Undo.RecordObject(playerController, "Change Player Movement");
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
            }

            if (playerHealth != null)
            {
                Undo.RecordObject(playerHealth, "Change Player Health");
                playerHealth.maxHealth = EditorGUILayout.FloatField("Max HP", playerHealth.maxHealth);
                if (GUI.changed) EditorUtility.SetDirty(playerHealth);
            }

            if (playerGarbage != null)
            {
                // maxCapacity is private; edit it via SerializedObject so we don't need to change runtime code.
                SerializedObject so = new SerializedObject(playerGarbage);
                SerializedProperty maxCapProp = so.FindProperty("maxCapacity");
                if (maxCapProp != null)
                {
                    so.Update();
                    EditorGUILayout.PropertyField(maxCapProp, new GUIContent("Max Capacity"));
                    so.ApplyModifiedProperties();
                }
                else
                {
                    EditorGUILayout.HelpBox("Could not find serialized field 'maxCapacity' on PlayerGarbageHandler.", MessageType.Warning);
                }
            }
        }

        // =========================================================
        // SECTION 4: WORLD / LEVEL GOALS
        // =========================================================
        if (gameManager != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("WORLD SETTINGS", headerStyle);

            SerializedObject so = new SerializedObject(gameManager);
            SerializedProperty goalsProp = so.FindProperty("levelGoals");
            if (goalsProp != null)
            {
                so.Update();
                EditorGUILayout.PropertyField(goalsProp, new GUIContent("Level Goals (by rotation)"), true);
                so.ApplyModifiedProperties();
            }
            else
            {
                EditorGUILayout.HelpBox("Could not find serialized field 'levelGoals' on GameManager.", MessageType.Warning);
            }
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

    private void TryAutoResolveAll()
    {
        if (gameManager == null) gameManager = FindFirstSceneObject<GameManager>();
        if (enemyDirector == null) enemyDirector = FindFirstSceneObject<EnemyDirector>();

        if (playerController == null) playerController = FindFirstSceneObject<ThirdPersonController>();
        if (playerHealth == null) playerHealth = FindFirstSceneObject<PlayerHealth>();
        if (playerGarbage == null) playerGarbage = FindFirstSceneObject<PlayerGarbageHandler>();

        // If we found controller, preferentially grab related components from that same GO.
        if (playerController != null)
        {
            if (playerHealth == null) playerHealth = playerController.GetComponent<PlayerHealth>();
            if (playerGarbage == null) playerGarbage = playerController.GetComponent<PlayerGarbageHandler>();
        }

        if (turretData == null) turretData = FindFirstAssetOfType<TurretData>();

        Repaint();
    }

    private static T FindFirstAssetOfType<T>() where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        if (guids == null || guids.Length == 0) return null;
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    /// <summary>
    /// Finds the first instance of T that exists in a loaded scene (not a prefab/asset).
    /// Works in Edit mode and Play mode, and includes inactive objects.
    /// </summary>
    private static T FindFirstSceneObject<T>() where T : UnityEngine.Object
    {
        // Resources.FindObjectsOfTypeAll works in Edit mode and includes inactive objects,
        // but also returns prefab assets. Filter those out.
        T[] all = Resources.FindObjectsOfTypeAll<T>();
        if (all == null || all.Length == 0) return null;

        for (int i = 0; i < all.Length; i++)
        {
            T obj = all[i];
            if (obj == null) continue;
            if (EditorUtility.IsPersistent(obj)) continue; // prefab/asset on disk

            if (obj is Component c)
            {
                if (!c.gameObject.scene.IsValid()) continue;
                return obj;
            }

            if (obj is GameObject go)
            {
                if (!go.scene.IsValid()) continue;
                return obj;
            }

            // Fallback (should be rare for our types)
            return obj;
        }

        return null;
    }
}