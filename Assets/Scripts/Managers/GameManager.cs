using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Hub
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private string bootstrapSceneName = "Bootstrap";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string hubSceneName = "HubScene";

    public string HubSceneName => hubSceneName;

    [Header("Garbage Pickup")]
    [Tooltip("If true, enemy loot garbage that has useMagnetPickup enabled on its GarbageData will be auto-collected by a magnet radius around the player instead of only via the Q area-pickup.")]
    [SerializeField] private bool useEnemyGarbageMagnetPickup = true;
    public bool UseEnemyGarbageMagnetPickup => useEnemyGarbageMagnetPickup;

    [Header("Scene transition")]
    [Tooltip("Duration of fade out and fade in (seconds) for scene transitions.")]
    [SerializeField] private float sceneTransitionFadeDuration = 1f;

    [Header("Level Sequence")]
    [Tooltip("The exact names of your scenes in order. e.g. World_1, World_2")]
    [SerializeField] private List<string> levelOrder = new List<string>();

    private RunState _currentRun = new RunState();
    private SceneFadeOverlay _sceneFadeOverlay;

    [Header("Hub resource bank")]
    [Tooltip("Session-only. Run resources are flushed here when entering Hub. Not saved (no save file yet).")]
    [SerializeField] private List<HubBankDebugEntry> debugHubBank = new List<HubBankDebugEntry>();

    [System.Serializable]
    public struct HubBankDebugEntry
    {
        public ResourceSO resource;
        public int count;
    }

    private Dictionary<ResourceSO, int> _hubBank = new Dictionary<ResourceSO, int>();

    [Header("Hub upgrade unlock state")]
    [Tooltip("Persistent across scenes. Never cleared when entering Hub or loading levels. Cleared only explicitly (e.g. new game).")]
    private HubUnlockState _hubUnlocks = new HubUnlockState();

    [Header("Hub upgrade debug")]
    [Tooltip("When true, in Editor these upgrade IDs are unlocked on Awake (no cost). Use to test levels with specific upgrades.")]
    [SerializeField] private bool applyDebugUnlocksInEditor = false;
    [Tooltip("Upgrade IDs to force-unlock when applyDebugUnlocksInEditor is true. Use HubUpgradeKeys constants.")]
    [SerializeField] private List<string> debugUnlockUpgradeIds = new List<string>();

    // Persistence variables
   
    public int CurrentRotation => _currentRun.Meta.CurrentRotation;
    public int KillCount => _currentRun.Meta.KillCount;

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnStateChanged;
    public event Action<int> OnKillCountChanged;

    /// <summary>Fired when a hub upgrade is unlocked (purchase or debug). Use to enable parry/dash in the current scene (e.g. hub) immediately.</summary>
    public event Action<string> OnHubUpgradeUnlocked;

    /// <summary>Fired once after a scene is loaded and run state (if any) is applied. Per-scene systems subscribe in Awake and refresh/resolve refs here instead of relying on Start order.</summary>
    public event Action OnSceneReady;

    private float _defaultFixedDeltaTime;
    private Coroutine _hitStopCoroutine;

    private List<IRunStateContributor> _contributors = new List<IRunStateContributor>();

    private bool _isLoading;

    public void RegisterRunStateContributor(IRunStateContributor contributor)
    {
        if (contributor != null && !_contributors.Contains(contributor))
            _contributors.Add(contributor);
    }

    public void UnregisterRunStateContributor(IRunStateContributor contributor)
    {
        _contributors.Remove(contributor);
    }

    public void CollectRunState()
    {
        Debug.Log($"[RunState] CollectRunState: _contributors.Count={_contributors.Count}");
        for (int i = _contributors.Count - 1; i >= 0; i--)
        {
            if (_contributors[i] is UnityEngine.Object obj && obj == null)
            {
                _contributors.RemoveAt(i);
                continue;
            }
            _contributors[i].ContributeToRunState(_currentRun);
        }
        Debug.Log($"[RunState] After collect: Health={_currentRun.Player.Health} Money={_currentRun.Economy.Money} InventoryCount={_currentRun.Inventory.Items.Count} WaveCredits={_currentRun.Economy.WaveCredits} TrickleCredits={_currentRun.Economy.TrickleCredits} DiffStage={_currentRun.Difficulty.Stage} TotalRunTime={_currentRun.Difficulty.TotalRunTime:F1} KillCount={_currentRun.Meta.KillCount}");
    }

    [Header("Level Objectives")]
    [Tooltip("Index 0 = World 1, Index 1 = World 2, etc.")]
    [SerializeField] private List<int> levelGoals = new List<int> { 50, 200 };

    [Header("Debug")]
    [Tooltip("Show current world/scene and rotation in Inspector at runtime.")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField, TextArea(0, 2)] private string _debugCurrentWorld = "(runtime)";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadDebugHubBankIntoBank();
        ApplyDebugHubUnlocksIfNeeded();

        var overlayGo = new GameObject("SceneFadeOverlay");
        overlayGo.transform.SetParent(transform);
        _sceneFadeOverlay = overlayGo.AddComponent<SceneFadeOverlay>();

        _defaultFixedDeltaTime = Time.fixedDeltaTime;
        Time.maximumDeltaTime = 0.15f;
    }

    private void OnEnable() => EnemyHealth.OnEnemyDeath += HandleEnemyDeath;
    private void OnDisable() => EnemyHealth.OnEnemyDeath -= HandleEnemyDeath;

    private void Start()
    {
        string activeScene = SceneManager.GetActiveScene().name;

        if (activeScene == bootstrapSceneName)
        {
            UpdateDebugCurrentWorld();
            return;
        }

        if (activeScene == mainMenuSceneName)
        {
            SetState(GameState.MainMenu);
        }
        else
        {
            if (_currentRun.Player.Health <= 0)
            {
                StartRun();
            }
            else
            {
                SetState(GameState.Playing);
            }
            OnSceneReady?.Invoke();
        }

        UpdateDebugCurrentWorld();
    }

    private void Update()
    {
        if (showDebugInfo)
            UpdateDebugCurrentWorld();
    }

    private void UpdateDebugCurrentWorld()
    {
        if (!showDebugInfo) return;

        string scene = SceneManager.GetActiveScene().name;
        _debugCurrentWorld = $"Scene: {scene}\nRotation: {CurrentRotation} | State: {CurrentState}";
    }

    public void SaveRunData(float hp, Dictionary<ItemSO, int> inventory, float money, float wCredits, float tCredits)
    {
        _currentRun.Player.Health = hp;
        _currentRun.Inventory.Items.Clear();
        foreach (var kvp in inventory)
            _currentRun.Inventory.Items[kvp.Key] = kvp.Value;
        _currentRun.Economy.Money = money;
        _currentRun.Economy.WaveCredits = wCredits;
        _currentRun.Economy.TrickleCredits = tCredits;
        Debug.Log("[GameManager] Run Saved.");
    }

    private void ClearPersistentData()
    {
        _currentRun.Clear();
    }

    // --- Scene Management (single entry point) ---

    /// <summary>
    /// Single entry point for all scene transitions. Resolves the request, collects or clears state as needed,
    /// then runs the load coroutine. Ignores calls while a load is already in progress.
    /// </summary>
    public void RequestScene(SceneRequest request)
    {
        if (_isLoading)
        {
            Debug.LogWarning("[GameManager] RequestScene ignored: load already in progress.");
            return;
        }

        if (!ResolveSceneRequest(request, out string sceneName, out bool shouldIncrementRotation))
        {
            return;
        }

        if (request.Type == SceneRequest.RequestType.RestartCurrentLevel)
        {
            ClearPersistentData();
            if (DifficultyManager.Instance != null)
                DifficultyManager.Instance.ResetDifficulty();
        }

        if (request.PreserveRunState)
            CollectRunState();

        StartCoroutine(LoadSceneRoutine(sceneName, request.PreserveRunState, shouldIncrementRotation));
    }

    /// <summary>Resolves a SceneRequest to concrete scene name and flags. Returns false if no load should occur (e.g. win state).</summary>
    private bool ResolveSceneRequest(SceneRequest request, out string sceneName, out bool shouldIncrementRotation)
    {
        sceneName = null;
        shouldIncrementRotation = false;

        switch (request.Type)
        {
            case SceneRequest.RequestType.MainMenu:
                sceneName = mainMenuSceneName;
                return true;

            case SceneRequest.RequestType.Hub:
                sceneName = hubSceneName;
                return true;

            case SceneRequest.RequestType.SpecificScene:
                if (string.IsNullOrEmpty(request.SceneName))
                {
                    Debug.LogError("[GameManager] SceneRequest.SpecificScene has no SceneName.");
                    return false;
                }
                sceneName = request.SceneName;
                return true;

            case SceneRequest.RequestType.RestartCurrentLevel:
                sceneName = SceneManager.GetActiveScene().name;
                return true;

            case SceneRequest.RequestType.NextLevelInSequence:
                string currentScene = SceneManager.GetActiveScene().name;
                int currentIndex = levelOrder.IndexOf(currentScene);

                if (currentIndex == -1)
                {
                    if (levelOrder.Count > 0)
                    {
                        sceneName = levelOrder[0];
                        shouldIncrementRotation = true;
                        return true;
                    }
                    Debug.LogError("[GameManager] Level Order list is empty!");
                    return false;
                }

                if (currentIndex + 1 < levelOrder.Count)
                {
                    sceneName = levelOrder[currentIndex + 1];
                    shouldIncrementRotation = true;
                    return true;
                }

                Debug.Log("[GameManager] End of Sequence reached. Win State.");
                SetState(GameState.Paused);
                return false;

            default:
                return false;
        }
    }

    /// <summary>True when the current scene is the last in the level sequence (e.g. World_2). Used to spawn one End Run portal instead of Continue + Return to Hub.</summary>
    public bool IsCurrentLevelLastInSequence()
    {
        if (levelOrder == null || levelOrder.Count == 0) return false;
        string currentScene = SceneManager.GetActiveScene().name;
        int index = levelOrder.IndexOf(currentScene);
        return index >= 0 && index == levelOrder.Count - 1;
    }

    /// <summary>Returns the index of the current scene in levelOrder, or -1 if not in list. Used by EnemyDirector for per-level intensity caps.</summary>
    public int GetCurrentLevelIndex()
    {
        if (levelOrder == null || levelOrder.Count == 0) return -1;
        return levelOrder.IndexOf(SceneManager.GetActiveScene().name);
    }

    public void LoadNextLevelInSequence()
    {
        RequestScene(SceneRequest.ToNextLevelInSequence());
    }

    public void LoadSpecificLevel(string sceneName)
    {
        RequestScene(SceneRequest.ToScene(sceneName, false));
    }

    public void RestartGame()
    {
        RequestScene(SceneRequest.RestartCurrentLevel());
    }

    public void ReturnToMainMenu()
    {
        RequestScene(SceneRequest.ToMainMenu());
    }

    private IEnumerator LoadSceneRoutine(string sceneName, bool preserveRunState, bool shouldIncrementRotation)
    {
        _isLoading = true;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = _defaultFixedDeltaTime;

        if (EnemyPooler.Instance != null)
            EnemyPooler.Instance.ClearPool();

        // Fade out before loading
        if (_sceneFadeOverlay != null && sceneTransitionFadeDuration > 0f)
            yield return _sceneFadeOverlay.FadeOut(sceneTransitionFadeDuration);

        yield return SceneManager.LoadSceneAsync(sceneName);

        yield return null;

        // Fade in after scene is loaded
        if (_sceneFadeOverlay != null && sceneTransitionFadeDuration > 0f)
            yield return _sceneFadeOverlay.FadeIn(sceneTransitionFadeDuration);

        if (preserveRunState)
        {
            if (sceneName == hubSceneName)
            {
                FlushRunResourcesToHubBank();
                _currentRun.Clear(); // Run ended: reset health, inventory, rotation, kill count, difficulty etc.; hub bank already updated
            }
            Debug.Log($"[RunState] Scene loaded. Before apply: Health={_currentRun.Player.Health} Money={_currentRun.Economy.Money} InventoryCount={_currentRun.Inventory.Items.Count} WaveCredits={_currentRun.Economy.WaveCredits} TrickleCredits={_currentRun.Economy.TrickleCredits} DiffStage={_currentRun.Difficulty.Stage}");
            ApplyRunStateToScene();
        }

        if (sceneName == hubSceneName) SetState(GameState.Hub);
        else if (sceneName == mainMenuSceneName) SetState(GameState.MainMenu);
        else
        {
            SetState(GameState.Playing);
            if (shouldIncrementRotation) _currentRun.Meta.CurrentRotation++;
        }

        _isLoading = false;
        OnSceneReady?.Invoke();
        UpdateDebugCurrentWorld();
    }

    private void ApplyRunStateToScene()
    {
        MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        int applied = 0;
        foreach (MonoBehaviour mb in all)
        {
            if (mb is IRunStateContributor contributor)
            {
                applied++;
                Debug.Log($"[RunState] ApplyRunState to: {mb.GetType().Name} on {mb.gameObject.name}");
                contributor.ApplyRunState(_currentRun);
            }
        }
        Debug.Log($"[RunState] ApplyRunStateToScene done. Contributors applied to: {applied}");
    }

    // --- Game Logic ---

    public void StartRun()
    {
        ClearPersistentData();

        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.ResetDifficulty();

        SetState(GameState.Playing);
        OnKillCountChanged?.Invoke(KillCount);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        Time.fixedDeltaTime = _defaultFixedDeltaTime;

        switch (newState)
        {
            case GameState.Playing:
            case GameState.Hub:
                Time.timeScale = 1f;
                SetCursorState(false);
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                SetCursorState(true);
                break;
            case GameState.GameOver:
                Time.timeScale = 0.5f;
                Time.fixedDeltaTime = _defaultFixedDeltaTime * 0.5f;
                SetCursorState(true);
                break;
            case GameState.MainMenu:
                Time.timeScale = 1f;
                SetCursorState(true);
                break;
        }
        OnStateChanged?.Invoke(newState);
    }

    public void TriggerHitStop(float duration, float targetScale)
    {
        if (CurrentState != GameState.Playing) return;
        if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);
        _hitStopCoroutine = StartCoroutine(DoHitStop(duration, targetScale));
    }

    private IEnumerator DoHitStop(float duration, float targetScale)
    {
        Time.timeScale = Mathf.Max(0.001f, targetScale);
        Time.fixedDeltaTime = _defaultFixedDeltaTime * Time.timeScale;
        yield return new WaitForSecondsRealtime(duration);

        if (CurrentState == GameState.Playing)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _defaultFixedDeltaTime;
        }
    }

    private void HandleEnemyDeath(EnemyData data)
    {
        if (CurrentState == GameState.GameOver) return;
        _currentRun.Meta.KillCount++;
        OnKillCountChanged?.Invoke(_currentRun.Meta.KillCount);
    }

    public int GetTargetGoalForCurrentLevel()
    {
        if (_currentRun.Meta.CurrentRotation < levelGoals.Count)
        {
            return levelGoals[_currentRun.Meta.CurrentRotation];
        }
        // Fallback scaling if we run out of defined goals
        return levelGoals[levelGoals.Count - 1] + (_currentRun.Meta.CurrentRotation * 100);
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
            SetState(GameState.Paused);
        else if (CurrentState == GameState.Paused)
            SetState(GameState.Playing);
    }

    public void TriggerGameOver() => SetState(GameState.GameOver);
    public void SetCursorState(bool visible) { Cursor.visible = visible; Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked; }
    public void QuitGame() => Application.Quit();

    /// <summary>Session-only hub bank. Returns 0 if type not in bank. Used by hub bench UI.</summary>
    public int GetHubBankCount(ResourceSO type)
    {
        if (type == null) return 0;
        return _hubBank.TryGetValue(type, out int c) ? c : 0;
    }

    /// <summary>Deduct amount from hub bank for the given resource. Call only when CanAfford has been checked. Updates debug list.</summary>
    private void SpendFromHubBank(ResourceSO type, int amount)
    {
        if (type == null || amount <= 0) return;
        if (!_hubBank.ContainsKey(type)) return;
        _hubBank[type] = Mathf.Max(0, _hubBank[type] - amount);
        UpdateDebugHubBank();
    }

    // --- Hub upgrade unlock API (persistent; not cleared on scene change) ---

    public bool IsHubUpgradeUnlocked(string upgradeId) => _hubUnlocks.IsUnlocked(upgradeId);

    /// <summary>Mark an upgrade as purchased (no cost deduction). Use for debug or after TryPurchaseHubUpgrade has deducted cost.</summary>
    public void UnlockHubUpgrade(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return;
        _hubUnlocks.Unlock(upgradeId);
        OnHubUpgradeUnlocked?.Invoke(upgradeId);
    }

    public bool CanAffordHubUpgrade(HubUpgradeSO upgrade)
    {
        if (upgrade == null || upgrade.cost == null) return false;
        foreach (var entry in upgrade.cost)
        {
            if (entry.resource == null) continue;
            if (GetHubBankCount(entry.resource) < entry.amount) return false;
        }
        return true;
    }

    /// <summary>If prerequisite met, can afford, and not already owned: deducts cost from hub bank and unlocks. Returns true if purchased.</summary>
    public bool TryPurchaseHubUpgrade(HubUpgradeSO upgrade)
    {
        if (upgrade == null) return false;
        if (_hubUnlocks.IsUnlocked(upgrade.id)) return false;
        if (!string.IsNullOrEmpty(upgrade.prerequisiteUpgradeId) && !_hubUnlocks.IsUnlocked(upgrade.prerequisiteUpgradeId))
            return false;
        if (!CanAffordHubUpgrade(upgrade)) return false;

        foreach (var entry in upgrade.cost)
        {
            if (entry.resource == null || entry.amount <= 0) continue;
            SpendFromHubBank(entry.resource, entry.amount);
        }
        _hubUnlocks.Unlock(upgrade.id);
        OnHubUpgradeUnlocked?.Invoke(upgrade.id);
        return true;
    }

    private void ApplyDebugHubUnlocksIfNeeded()
    {
#if UNITY_EDITOR
        if (!applyDebugUnlocksInEditor || debugUnlockUpgradeIds == null) return;
        foreach (string id in debugUnlockUpgradeIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            _hubUnlocks.Unlock(id);
            OnHubUpgradeUnlocked?.Invoke(id);
        }
        if (debugUnlockUpgradeIds.Count > 0)
            Debug.Log($"[GameManager] Debug: unlocked {debugUnlockUpgradeIds.Count} hub upgrade(s) in Editor.");
#endif
    }

    /// <summary>When entering Hub with run state, add run resources to hub bank and clear run resources so ApplyRunState gives player 0/0/0.</summary>
    private void FlushRunResourcesToHubBank()
    {
        foreach (var kvp in _currentRun.Resources.Counts)
        {
            if (kvp.Key == null) continue;
            if (!_hubBank.ContainsKey(kvp.Key))
                _hubBank[kvp.Key] = 0;
            _hubBank[kvp.Key] += kvp.Value;
        }
        _currentRun.Resources.Clear();
        UpdateDebugHubBank();
        Debug.Log("[GameManager] Run resources flushed to hub bank.");
    }

    private void UpdateDebugHubBank()
    {
        debugHubBank.Clear();
        foreach (var kvp in _hubBank)
        {
            if (kvp.Key == null) continue;
            debugHubBank.Add(new HubBankDebugEntry { resource = kvp.Key, count = kvp.Value });
        }
    }

    /// <summary>At startup, load Inspector-set debug hub bank into the real bank so starting in Hub shows correct counts.</summary>
    private void LoadDebugHubBankIntoBank()
    {
        if (debugHubBank == null) return;
        foreach (var entry in debugHubBank)
        {
            if (entry.resource == null) continue;
            _hubBank[entry.resource] = Mathf.Max(0, entry.count);
        }
    }
}