using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

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

    public event Action<GameState> OnStateChanged;

    public GameState CurrentState { get; private set; }
    public int CurrentRotation { get; private set; }

    public int KillCount { get; private set; }
    public event Action<int> OnKillCountChanged;

    private Coroutine _hitStopCoroutine;
    private float _defaultFixedDeltaTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        Time.maximumDeltaTime = 0.15f;
    }

    private void OnEnable()
    {
        EnemyHealth.OnEnemyDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        EnemyHealth.OnEnemyDeath -= HandleEnemyDeath;
    }

    private void Start()
    {
        _defaultFixedDeltaTime = Time.fixedDeltaTime;

        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            SetState(GameState.MainMenu);
        }
        else
        {
            StartRun();
        }
    }

    // --- Scene Management ---

    public void LoadNextLevel(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, true));
    }

    public void GoToHub()
    {
        StartCoroutine(LoadSceneRoutine(hubSceneName, false));
    }

    public void RestartGame()
    {
        // Reset difficulty if it exists
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.ResetDifficulty();

        // Restart without incrementing rotation
        StartCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name, false));
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(LoadSceneRoutine(mainMenuSceneName, false));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, bool shouldIncrementRotation)
    {
        // Reset time settings immediately
        Time.timeScale = 1f;
        Time.fixedDeltaTime = _defaultFixedDeltaTime;

        // Stop any active hit-stop effect
        if (_hitStopCoroutine != null)
        {
            StopCoroutine(_hitStopCoroutine);
            _hitStopCoroutine = null;
        }

        yield return SceneManager.LoadSceneAsync(sceneName);

        // Set appropriate state based on scene
        if (sceneName == hubSceneName)
        {
            SetState(GameState.Hub);
        }
        else if (sceneName == mainMenuSceneName)
        {
            SetState(GameState.MainMenu);
        }
        else
        {
            StartRun();

            // Only increment rotation when progressing to next level
            if (shouldIncrementRotation)
            {
                IncrementRotation();
            }
        }
    }

    // --- Game Logic ---

    public void StartRun()
    {
        KillCount = 0;
        OnKillCountChanged?.Invoke(KillCount);
        CurrentRotation = 0;

        // Ensure no lingering hit-stop effects
        if (_hitStopCoroutine != null)
        {
            StopCoroutine(_hitStopCoroutine);
            _hitStopCoroutine = null;
        }

        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.ResetDifficulty();

        SetState(GameState.Playing);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        // Stop any active hit-stop and reset to default timing
        if (_hitStopCoroutine != null)
        {
            StopCoroutine(_hitStopCoroutine);
            _hitStopCoroutine = null;
        }

        // Always reset to default first
        Time.fixedDeltaTime = _defaultFixedDeltaTime;

        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 1f;
                SetCursorState(true);
                break;

            case GameState.Playing:
                Time.timeScale = 1f;
                SetCursorState(false);
                break;

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
                // Scale physics with timeScale for smooth slow-mo death
                Time.fixedDeltaTime = _defaultFixedDeltaTime * 0.5f;
                SetCursorState(true);
                break;
        }

        OnStateChanged?.Invoke(newState);
    }

    public void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
            SetState(GameState.Paused);
        else if (CurrentState == GameState.Paused)
            SetState(GameState.Playing);
    }

    public void TriggerGameOver()
    {
        if (CurrentState != GameState.GameOver)
        {
            SetState(GameState.GameOver);
        }
    }

    public void IncrementRotation()
    {
        CurrentRotation++;
        Debug.Log("Rotation completed: " + CurrentRotation);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // --- Hit Stop ---

    public void TriggerHitStop(float duration, float targetScale)
    {
        if (CurrentState != GameState.Playing) return;

        if (_hitStopCoroutine != null)
        {
            StopCoroutine(_hitStopCoroutine);
        }

        _hitStopCoroutine = StartCoroutine(DoHitStop(duration, targetScale));
    }

    private IEnumerator DoHitStop(float duration, float targetScale)
    {
        // Safety: Ensure we never set timescale/fixedDeltaTime to absolute 0
        // or a negative value, which breaks physics.
        targetScale = Mathf.Max(0.001f, targetScale);

        Time.timeScale = targetScale;
        Time.fixedDeltaTime = _defaultFixedDeltaTime * targetScale;

        yield return new WaitForSecondsRealtime(duration);

        // Only reset if still in Playing state
        if (CurrentState == GameState.Playing)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _defaultFixedDeltaTime;
        }

        _hitStopCoroutine = null;
    }

    private void HandleEnemyDeath(EnemyData data)
    {
        if (CurrentState == GameState.GameOver) return;

        KillCount++;
        OnKillCountChanged?.Invoke(KillCount);
    }
}