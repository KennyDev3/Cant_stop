using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections; 

public enum GameState
{
    Playing,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("The name of the menu scene to return to.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

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

        StartRun();
    }

    public void StartRun()
    {
        KillCount = 0;
        OnKillCountChanged?.Invoke(KillCount);

        CurrentRotation = 0;

        
        if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);

        SetState(GameState.Playing);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        if (newState != GameState.Playing)
        {
            if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);
            Time.fixedDeltaTime = _defaultFixedDeltaTime; 
        }

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case GameState.GameOver:
                Time.timeScale = 0.5f;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }

        OnStateChanged?.Invoke(newState);
    }

  
   
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
        Time.timeScale = targetScale;

        Time.fixedDeltaTime = _defaultFixedDeltaTime * targetScale;
        yield return new WaitForSecondsRealtime(duration);

       
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


    public void TogglePause()
    {
        if (CurrentState == GameState.Playing) SetState(GameState.Paused);
        else if (CurrentState == GameState.Paused) SetState(GameState.Playing);
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

    public void RestartGame()
    {
        if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);
        Time.fixedDeltaTime = _defaultFixedDeltaTime;

        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.ResetDifficulty();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        StartRun();
    }

    public void ReturnToMainMenu()
    {
        if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);
        Time.fixedDeltaTime = _defaultFixedDeltaTime;
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}