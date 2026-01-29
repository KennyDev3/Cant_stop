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
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string hubSceneName = "HubScene";

    public string HubSceneName => hubSceneName;

    [Header("Level Sequence")]
    [Tooltip("The exact names of your scenes in order. e.g. World_1, World_2")]
    [SerializeField] private List<string> levelOrder = new List<string>();

    private RunState _currentRun = new RunState();

    // Persistence variables
   
    public int CurrentRotation => _currentRun.Meta.CurrentRotation;
    public int KillCount => _currentRun.Meta.KillCount;

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnStateChanged;
    public event Action<int> OnKillCountChanged;

    private float _defaultFixedDeltaTime;
    private Coroutine _hitStopCoroutine;

    private List<IRunStateContributor> _contributors = new List<IRunStateContributor>();

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _defaultFixedDeltaTime = Time.fixedDeltaTime;
        Time.maximumDeltaTime = 0.15f;
    }

    private void OnEnable() => EnemyHealth.OnEnemyDeath += HandleEnemyDeath;
    private void OnDisable() => EnemyHealth.OnEnemyDeath -= HandleEnemyDeath;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
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
        }
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

    // --- Scene Management ---

    public void LoadNextLevelInSequence()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        int currentIndex = levelOrder.IndexOf(currentScene);

        // If current level is not in the list (e.g. Hub), start from the beginning
        if (currentIndex == -1)
        {
            if (levelOrder.Count > 0)
            {
                StartCoroutine(LoadSceneRoutine(levelOrder[0], true));
            }
            else
            {
                Debug.LogError("[GameManager] Level Order list is empty!");
            }
            return;
        }

        // Check if there is a next level
        if (currentIndex + 1 < levelOrder.Count)
        {
            string nextLevel = levelOrder[currentIndex + 1];
            StartCoroutine(LoadSceneRoutine(nextLevel, true));
        }
        // If no next level, we Win
        else
        {
            Debug.Log("[GameManager] End of Sequence reached. Win State.");
            SetState(GameState.Paused);
        }
    }

    public void LoadSpecificLevel(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, false)); // False usually for Hub/Menus
    }

    private IEnumerator LoadSceneRoutine(string sceneName, bool shouldIncrementRotation)
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = _defaultFixedDeltaTime;

        if (EnemyPooler.Instance != null)
            EnemyPooler.Instance.ClearPool();

        yield return SceneManager.LoadSceneAsync(sceneName);

        yield return null;

        Debug.Log($"[RunState] Scene loaded. Before apply: Health={_currentRun.Player.Health} Money={_currentRun.Economy.Money} InventoryCount={_currentRun.Inventory.Items.Count} WaveCredits={_currentRun.Economy.WaveCredits} TrickleCredits={_currentRun.Economy.TrickleCredits} DiffStage={_currentRun.Difficulty.Stage}");
        ApplyRunStateToScene();

        if (sceneName == hubSceneName) SetState(GameState.Hub);
        else if (sceneName == mainMenuSceneName) SetState(GameState.MainMenu);
        else
        {
            SetState(GameState.Playing);
            if (shouldIncrementRotation) _currentRun.Meta.CurrentRotation++;
        }
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

    public void RestartGame()
    {
        ClearPersistentData();
        StartCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name, false));
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(LoadSceneRoutine(mainMenuSceneName, false));
    }

    public void TriggerGameOver() => SetState(GameState.GameOver);
    public void SetCursorState(bool visible) { Cursor.visible = visible; Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked; }
    public void QuitGame() => Application.Quit();
}