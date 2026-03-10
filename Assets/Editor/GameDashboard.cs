using UnityEngine;
using UnityEditor;
using StarterAssets;
using System;
using System.Linq;
using UnityEngine.SceneManagement;
using System.IO;

public class GameDashboard : EditorWindow
{
    // --- 1. THE FIELDS ---
    [Header("Auto-resolved (Scene)")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private EnemyDirector enemyDirector;
    [SerializeField] private ThirdPersonController playerController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerGarbageHandler playerGarbage;
    [SerializeField] private TruckTurret truckTurret;

    [Header("Auto-resolved (Assets)")]
    [SerializeField] private PlayerConfig playerConfig;
    [SerializeField] private TurretData turretData;

    private Vector2 scrollPos;
    private GUIStyle headerStyle;
    private GUIStyle sectionBoxStyle;
    private GUIStyle fieldLabelStyle;
    private GUIStyle primaryButtonStyle;
    private int selectedTabIndex = 0;

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
        EnsureStyles();
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // =========================================================
        // SECTION 1: REFERENCES (Auto Resolve)
        // =========================================================
        GUILayout.Label("SETUP REFERENCES", headerStyle);
        EditorGUILayout.HelpBox("This dashboard focuses on player setup. Open a gameplay scene (World_1/World_2/Hub) or enter Play mode so it can auto-find the player and config.", MessageType.Info);

        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("GameManager", gameManager != null ? gameManager.gameObject.name : "(not found)", fieldLabelStyle);
            EditorGUILayout.LabelField("EnemyDirector", enemyDirector != null ? enemyDirector.gameObject.name : "(not found)", fieldLabelStyle);
            EditorGUILayout.LabelField("Player Controller", playerController != null ? playerController.gameObject.name : "(not found)", fieldLabelStyle);
            EditorGUILayout.LabelField("Player Health", playerHealth != null ? playerHealth.gameObject.name : "(not found)", fieldLabelStyle);
            EditorGUILayout.LabelField("Player Garbage", playerGarbage != null ? playerGarbage.gameObject.name : "(not found)", fieldLabelStyle);
            EditorGUILayout.LabelField("PlayerConfig (asset)", playerConfig != null ? AssetDatabase.GetAssetPath(playerConfig) : "(not found)", fieldLabelStyle);
            EditorGUILayout.LabelField("Truck Turret", truckTurret != null ? truckTurret.gameObject.name : "(not found)", fieldLabelStyle);
            EditorGUILayout.LabelField("TurretData (asset)", turretData != null ? AssetDatabase.GetAssetPath(turretData) : "(not found)", fieldLabelStyle);
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Tabs: Player / Game / Hub Upgrades
        GUILayout.Space(4);
        string[] tabs = { "Player", "Game", "Hub Upgrades" };
        selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, tabs, GUILayout.Height(24));

        GUILayout.Space(8);

        if (selectedTabIndex == 0)
        {
            // =========================================================
            // PLAYER TAB
            // =========================================================
            GUILayout.Label("PLAYER SETTINGS (via PlayerConfig)", headerStyle);
            DrawPlayerConfigSection();

            GUILayout.Space(10);
            GUILayout.Label("TURRET SETTINGS (TurretData)", headerStyle);
            DrawTurretSection();
        }
        else if (selectedTabIndex == 1)
        {
            // =========================================================
            // GAME TAB
            // =========================================================
            GUILayout.Label("GAME SETTINGS", headerStyle);
            DrawGameTab();
        }
        else
        {
            // =========================================================
            // HUB UPGRADES TAB
            // =========================================================
            GUILayout.Label("HUB UPGRADES (Debug Editor Unlocks)", headerStyle);
            DrawHubUpgradesTab();
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
        if (truckTurret == null) truckTurret = FindFirstSceneObject<TruckTurret>();

        // If we found controller, preferentially grab related components from that same GO.
        if (playerController != null)
        {
            if (playerHealth == null) playerHealth = playerController.GetComponent<PlayerHealth>();
            if (playerGarbage == null) playerGarbage = playerController.GetComponent<PlayerGarbageHandler>();
        }

        if (playerConfig == null)
        {
            // Prefer the config referenced by a PlayerConfigApplicator in the current scene.
            var applicator = FindFirstSceneObject<PlayerConfigApplicator>();
            if (applicator != null && applicator.Config != null)
            {
                playerConfig = applicator.Config;
            }
            else
            {
                playerConfig = FindFirstAssetOfType<PlayerConfig>();
            }
        }

        if (turretData == null)
        {
            if (truckTurret != null && truckTurret.turretData != null)
            {
                turretData = truckTurret.turretData;
            }
            else
            {
                turretData = FindFirstAssetOfType<TurretData>();
            }
        }

        Repaint();
    }

    private void DrawPlayerConfigSection()
    {
        // Scene name context
        string sceneName = SceneManager.GetActiveScene().name;
        EditorGUILayout.LabelField("Active Scene", string.IsNullOrEmpty(sceneName) ? "(none)" : sceneName, fieldLabelStyle);

        EditorGUILayout.Space(4);

        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            playerConfig = (PlayerConfig)EditorGUILayout.ObjectField("PlayerConfig Asset", playerConfig, typeof(PlayerConfig), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find PlayerConfig assets", primaryButtonStyle, GUILayout.Height(22)))
                {
                    playerConfig = FindFirstAssetOfType<PlayerConfig>();
                }

                if (GUILayout.Button("Create/Assign for this Scene", primaryButtonStyle, GUILayout.Height(22)))
                {
                    CreateOrAssignPlayerConfigForScene(sceneName);
                }
            }
        }

        if (playerConfig == null)
        {
            EditorGUILayout.HelpBox("No PlayerConfig assigned. Create or assign one so artists can edit player health, capacity, and input mode here.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(6);

        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            Undo.RecordObject(playerConfig, "Change PlayerConfig");

            EditorGUILayout.LabelField("Base Stats", fieldLabelStyle);
            playerConfig.baseMaxHealth = EditorGUILayout.FloatField("Base Max Health", playerConfig.baseMaxHealth);
            playerConfig.baseMaxCapacity = EditorGUILayout.IntField("Base Max Capacity", playerConfig.baseMaxCapacity);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Movement", fieldLabelStyle);
            playerConfig.moveSpeed = EditorGUILayout.FloatField("Move Speed", playerConfig.moveSpeed);
            playerConfig.sprintSpeed = EditorGUILayout.FloatField("Sprint Speed", playerConfig.sprintSpeed);
            if (playerConfig.sprintSpeed < playerConfig.moveSpeed)
            {
                playerConfig.sprintSpeed = playerConfig.moveSpeed;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Input Mode", fieldLabelStyle);
            string[] options = new[] { "Gamepad Mode", "Mouse Mode" };
            int selectedIndex = playerConfig.useMouseRotation ? 1 : 0;
            int newIndex = GUILayout.Toolbar(selectedIndex, options, GUILayout.Height(22));
            if (newIndex != selectedIndex)
            {
                playerConfig.useMouseRotation = (newIndex == 1);
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(playerConfig);
            }
        }
    }

    private void DrawTurretSection()
    {
        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            turretData = (TurretData)EditorGUILayout.ObjectField("TurretData Asset", turretData, typeof(TurretData), false);

            if (GUILayout.Button("Find TurretData assets", primaryButtonStyle, GUILayout.Height(22)))
            {
                turretData = FindFirstAssetOfType<TurretData>();
            }
        }

        if (turretData == null)
        {
            EditorGUILayout.HelpBox("No TurretData assigned. Assign one to edit turret base damage, fire rate, range and rotation speed here.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(6);

        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            Undo.RecordObject(turretData, "Change TurretData");

            EditorGUILayout.LabelField("Combat Stats", fieldLabelStyle);
            turretData.damage = EditorGUILayout.FloatField("Damage", turretData.damage);
            turretData.fireRate = EditorGUILayout.FloatField("Fire Rate (shots/sec)", turretData.fireRate);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Targeting", fieldLabelStyle);
            turretData.targetRange = EditorGUILayout.FloatField("Target Range", turretData.targetRange);
            turretData.rotationSpeed = EditorGUILayout.FloatField("Rotation Speed", turretData.rotationSpeed);

            if (GUI.changed)
            {
                EditorUtility.SetDirty(turretData);
            }
        }
    }

    private void DrawGameTab()
    {
        // Scene toggles
        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("Scene Toggles", fieldLabelStyle);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (gameManager != null)
                {
                    bool active = gameManager.gameObject.activeSelf;
                    string label = active ? "Game Manager: ON" : "Game Manager: OFF";
                    if (GUILayout.Button(label, primaryButtonStyle, GUILayout.Height(22)))
                    {
                        Undo.RecordObject(gameManager.gameObject, "Toggle GameManager");
                        gameManager.gameObject.SetActive(!active);
                    }
                }

                if (enemyDirector != null)
                {
                    bool active = enemyDirector.gameObject.activeSelf;
                    string label = active ? "Enemies: ON" : "Enemies: OFF";
                    if (GUILayout.Button(label, primaryButtonStyle, GUILayout.Height(22)))
                    {
                        Undo.RecordObject(enemyDirector.gameObject, "Toggle EnemyDirector");
                        enemyDirector.gameObject.SetActive(!active);
                    }
                }
            }
        }

        // Level goals aligned with level order
        if (gameManager == null)
        {
            EditorGUILayout.HelpBox("No GameManager found in the scene. Open a gameplay scene to edit game-level settings.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(6);

        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("Level Goals (per level order)", fieldLabelStyle);

            SerializedObject so = new SerializedObject(gameManager);
            SerializedProperty levelOrderProp = so.FindProperty("levelOrder");
            SerializedProperty levelGoalsProp = so.FindProperty("levelGoals");

            if (levelOrderProp == null || levelGoalsProp == null)
            {
                EditorGUILayout.HelpBox("Could not find 'levelOrder' or 'levelGoals' on GameManager.", MessageType.Warning);
                return;
            }

            so.Update();

            int levelCount = levelOrderProp.arraySize;
            if (levelCount == 0)
            {
                EditorGUILayout.HelpBox("GameManager has no entries in levelOrder. Add levels there first.", MessageType.Info);
                so.ApplyModifiedProperties();
                return;
            }

            // Ensure levelGoals has at least as many entries as levelOrder so we don't break transitions.
            while (levelGoalsProp.arraySize < levelCount)
            {
                int newIndex = levelGoalsProp.arraySize;
                levelGoalsProp.InsertArrayElementAtIndex(newIndex);
                SerializedProperty elem = levelGoalsProp.GetArrayElementAtIndex(newIndex);
                int defaultValue = (newIndex > 0)
                    ? levelGoalsProp.GetArrayElementAtIndex(newIndex - 1).intValue
                    : 0;
                elem.intValue = defaultValue;
            }

            for (int i = 0; i < levelCount; i++)
            {
                SerializedProperty nameProp = levelOrderProp.GetArrayElementAtIndex(i);
                SerializedProperty goalProp = levelGoalsProp.GetArrayElementAtIndex(i);

                string levelName = string.IsNullOrEmpty(nameProp.stringValue)
                    ? $"Level {i}"
                    : nameProp.stringValue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i}: {levelName}", GUILayout.Width(200));
                goalProp.intValue = EditorGUILayout.IntField("Goal", goalProp.intValue);
                EditorGUILayout.EndHorizontal();
            }

            so.ApplyModifiedProperties();
        }
    }

    private void DrawHubUpgradesTab()
    {
        if (gameManager == null)
        {
            EditorGUILayout.HelpBox("No GameManager found. Open a scene that has the GameManager singleton to edit hub upgrades.", MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("Editor Debug Unlocks", fieldLabelStyle);
            EditorGUILayout.HelpBox("These settings control which hub upgrades are force-unlocked in the Editor (via GameManager.applyDebugUnlocksInEditor). They are intended for testing, not for final save data.", MessageType.Info);
        }

        SerializedObject so = new SerializedObject(gameManager);
        SerializedProperty applyDebugProp = so.FindProperty("applyDebugUnlocksInEditor");
        SerializedProperty debugIdsProp = so.FindProperty("debugUnlockUpgradeIds");

        if (applyDebugProp == null || debugIdsProp == null)
        {
            EditorGUILayout.HelpBox("GameManager is missing debug unlock fields (applyDebugUnlocksInEditor / debugUnlockUpgradeIds).", MessageType.Warning);
            return;
        }

        so.Update();

        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            applyDebugProp.boolValue = EditorGUILayout.ToggleLeft("Enable debug hub unlocks in Editor", applyDebugProp.boolValue);
        }

        // Build a working set of ids
        var ids = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < debugIdsProp.arraySize; i++)
        {
            var elem = debugIdsProp.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(elem.stringValue))
            {
                ids.Add(elem.stringValue);
            }
        }

        EditorGUILayout.Space(6);

        // Parry tree
        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("Parry Tree", fieldLabelStyle);
            bool parryUnlock = ids.Contains(HubUpgradeKeys.ParryUnlock);
            bool parryTurretBuff = ids.Contains(HubUpgradeKeys.ParryTurretBuff);
            bool parryReturnDamage = ids.Contains(HubUpgradeKeys.ParryReturnDamage);

            parryUnlock = EditorGUILayout.ToggleLeft("Unlock Parry", parryUnlock);
            parryTurretBuff = EditorGUILayout.ToggleLeft("Parry: Turret Buff", parryTurretBuff);
            parryReturnDamage = EditorGUILayout.ToggleLeft("Parry: Return Damage", parryReturnDamage);

            // Write back
            if (parryUnlock) ids.Add(HubUpgradeKeys.ParryUnlock); else ids.Remove(HubUpgradeKeys.ParryUnlock);
            if (parryTurretBuff) ids.Add(HubUpgradeKeys.ParryTurretBuff); else ids.Remove(HubUpgradeKeys.ParryTurretBuff);
            if (parryReturnDamage) ids.Add(HubUpgradeKeys.ParryReturnDamage); else ids.Remove(HubUpgradeKeys.ParryReturnDamage);
        }

        // Dash tree
        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("Dash Tree", fieldLabelStyle);
            bool dashUnlock = ids.Contains(HubUpgradeKeys.DashUnlock);
            bool dashTurretSpeed = ids.Contains(HubUpgradeKeys.DashTurretAttackSpeed);
            bool dashFiretrail = ids.Contains(HubUpgradeKeys.DashFiretrail);

            dashUnlock = EditorGUILayout.ToggleLeft("Unlock Dash", dashUnlock);
            dashTurretSpeed = EditorGUILayout.ToggleLeft("Dash: Turret Attack Speed", dashTurretSpeed);
            dashFiretrail = EditorGUILayout.ToggleLeft("Dash: Firetrail", dashFiretrail);

            if (dashUnlock) ids.Add(HubUpgradeKeys.DashUnlock); else ids.Remove(HubUpgradeKeys.DashUnlock);
            if (dashTurretSpeed) ids.Add(HubUpgradeKeys.DashTurretAttackSpeed); else ids.Remove(HubUpgradeKeys.DashTurretAttackSpeed);
            if (dashFiretrail) ids.Add(HubUpgradeKeys.DashFiretrail); else ids.Remove(HubUpgradeKeys.DashFiretrail);
        }

        // Helper for tiered dropdowns
        int DrawTierDropdown(string label, int currentLevel)
        {
            string[] options = { "None", "Level 1", "Level 2", "Level 3" };
            int clamped = Mathf.Clamp(currentLevel, 0, 3);
            return EditorGUILayout.Popup(label, clamped, options);
        }

        // Passive Move Speed
        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("Passive: Movement Speed", fieldLabelStyle);
            int level = ids.Contains(HubUpgradeKeys.PassiveMoveSpeed3) ? 3 :
                        ids.Contains(HubUpgradeKeys.PassiveMoveSpeed2) ? 2 :
                        ids.Contains(HubUpgradeKeys.PassiveMoveSpeed1) ? 1 : 0;
            int newLevel = DrawTierDropdown("Move Speed Level", level);
            if (newLevel != level)
            {
                ids.Remove(HubUpgradeKeys.PassiveMoveSpeed1);
                ids.Remove(HubUpgradeKeys.PassiveMoveSpeed2);
                ids.Remove(HubUpgradeKeys.PassiveMoveSpeed3);
                if (newLevel == 1) ids.Add(HubUpgradeKeys.PassiveMoveSpeed1);
                else if (newLevel == 2) ids.Add(HubUpgradeKeys.PassiveMoveSpeed2);
                else if (newLevel == 3) ids.Add(HubUpgradeKeys.PassiveMoveSpeed3);
            }
        }

        // Passive Health Regen
        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("Passive: Health Regeneration", fieldLabelStyle);
            int level = ids.Contains(HubUpgradeKeys.PassiveHealthRegen3) ? 3 :
                        ids.Contains(HubUpgradeKeys.PassiveHealthRegen2) ? 2 :
                        ids.Contains(HubUpgradeKeys.PassiveHealthRegen1) ? 1 : 0;
            int newLevel = DrawTierDropdown("Health Regen Level", level);
            if (newLevel != level)
            {
                ids.Remove(HubUpgradeKeys.PassiveHealthRegen1);
                ids.Remove(HubUpgradeKeys.PassiveHealthRegen2);
                ids.Remove(HubUpgradeKeys.PassiveHealthRegen3);
                if (newLevel == 1) ids.Add(HubUpgradeKeys.PassiveHealthRegen1);
                else if (newLevel == 2) ids.Add(HubUpgradeKeys.PassiveHealthRegen2);
                else if (newLevel == 3) ids.Add(HubUpgradeKeys.PassiveHealthRegen3);
            }
        }

        // Passive Pickup Range
        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("Passive: AOE Pickup Range", fieldLabelStyle);
            int level = ids.Contains(HubUpgradeKeys.PassivePickupRange3) ? 3 :
                        ids.Contains(HubUpgradeKeys.PassivePickupRange2) ? 2 :
                        ids.Contains(HubUpgradeKeys.PassivePickupRange1) ? 1 : 0;
            int newLevel = DrawTierDropdown("Pickup Range Level", level);
            if (newLevel != level)
            {
                ids.Remove(HubUpgradeKeys.PassivePickupRange1);
                ids.Remove(HubUpgradeKeys.PassivePickupRange2);
                ids.Remove(HubUpgradeKeys.PassivePickupRange3);
                if (newLevel == 1) ids.Add(HubUpgradeKeys.PassivePickupRange1);
                else if (newLevel == 2) ids.Add(HubUpgradeKeys.PassivePickupRange2);
                else if (newLevel == 3) ids.Add(HubUpgradeKeys.PassivePickupRange3);
            }
        }

        // Meta Capacity
        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("Meta: Capacity Limit", fieldLabelStyle);
            int level = ids.Contains(HubUpgradeKeys.MetaCapacity3) ? 3 :
                        ids.Contains(HubUpgradeKeys.MetaCapacity2) ? 2 :
                        ids.Contains(HubUpgradeKeys.MetaCapacity1) ? 1 : 0;
            int newLevel = DrawTierDropdown("Capacity Level", level);
            if (newLevel != level)
            {
                ids.Remove(HubUpgradeKeys.MetaCapacity1);
                ids.Remove(HubUpgradeKeys.MetaCapacity2);
                ids.Remove(HubUpgradeKeys.MetaCapacity3);
                if (newLevel == 1) ids.Add(HubUpgradeKeys.MetaCapacity1);
                else if (newLevel == 2) ids.Add(HubUpgradeKeys.MetaCapacity2);
                else if (newLevel == 3) ids.Add(HubUpgradeKeys.MetaCapacity3);
            }
        }

        // Meta Resource Value
        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("Meta: Resource Value", fieldLabelStyle);
            int level = ids.Contains(HubUpgradeKeys.MetaResourceValue3) ? 3 :
                        ids.Contains(HubUpgradeKeys.MetaResourceValue2) ? 2 :
                        ids.Contains(HubUpgradeKeys.MetaResourceValue1) ? 1 : 0;
            int newLevel = DrawTierDropdown("Resource Value Level", level);
            if (newLevel != level)
            {
                ids.Remove(HubUpgradeKeys.MetaResourceValue1);
                ids.Remove(HubUpgradeKeys.MetaResourceValue2);
                ids.Remove(HubUpgradeKeys.MetaResourceValue3);
                if (newLevel == 1) ids.Add(HubUpgradeKeys.MetaResourceValue1);
                else if (newLevel == 2) ids.Add(HubUpgradeKeys.MetaResourceValue2);
                else if (newLevel == 3) ids.Add(HubUpgradeKeys.MetaResourceValue3);
            }
        }

        // Meta Double Drop
        using (new EditorGUILayout.VerticalScope(sectionBoxStyle))
        {
            EditorGUILayout.LabelField("Meta: Double Drop Chance", fieldLabelStyle);
            int level = ids.Contains(HubUpgradeKeys.MetaDoubleDrop3) ? 3 :
                        ids.Contains(HubUpgradeKeys.MetaDoubleDrop2) ? 2 :
                        ids.Contains(HubUpgradeKeys.MetaDoubleDrop1) ? 1 : 0;
            int newLevel = DrawTierDropdown("Double Drop Level", level);
            if (newLevel != level)
            {
                ids.Remove(HubUpgradeKeys.MetaDoubleDrop1);
                ids.Remove(HubUpgradeKeys.MetaDoubleDrop2);
                ids.Remove(HubUpgradeKeys.MetaDoubleDrop3);
                if (newLevel == 1) ids.Add(HubUpgradeKeys.MetaDoubleDrop1);
                else if (newLevel == 2) ids.Add(HubUpgradeKeys.MetaDoubleDrop2);
                else if (newLevel == 3) ids.Add(HubUpgradeKeys.MetaDoubleDrop3);
            }
        }

        // Write back ids set to the serialized list
        debugIdsProp.ClearArray();
        int index = 0;
        foreach (var id in ids)
        {
            debugIdsProp.InsertArrayElementAtIndex(index);
            debugIdsProp.GetArrayElementAtIndex(index).stringValue = id;
            index++;
        }

        so.ApplyModifiedProperties();
    }

    private void CreateOrAssignPlayerConfigForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            sceneName = "UnnamedScene";

        // Try to find an existing config first.
        if (playerConfig == null)
        {
            playerConfig = FindFirstAssetOfType<PlayerConfig>();
        }

        if (playerConfig == null)
        {
            string folder = "Assets/Config/Player";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{sceneName}_PlayerConfig.asset");
            var configInstance = ScriptableObject.CreateInstance<PlayerConfig>();
            AssetDatabase.CreateAsset(configInstance, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            playerConfig = configInstance;
        }

        // Try to assign to a PlayerConfigApplicator in the scene so runtime uses this config.
        var applicator = FindFirstSceneObject<PlayerConfigApplicator>();
        if (applicator != null)
        {
            Undo.RecordObject(applicator, "Assign PlayerConfig to Applicator");
            typeof(PlayerConfigApplicator)
                .GetField("config", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(applicator, playerConfig);
            EditorUtility.SetDirty(applicator);
        }
    }

    private void EnsureStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 10, 6)
            };
            headerStyle.normal.textColor = new Color(0.85f, 0.9f, 1f);
        }

        if (sectionBoxStyle == null)
        {
            sectionBoxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 6, 8),
                margin = new RectOffset(0, 0, 4, 6)
            };
        }

        if (fieldLabelStyle == null)
        {
            fieldLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold
            };
        }

        if (primaryButtonStyle == null)
        {
            primaryButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold
            };
        }
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
