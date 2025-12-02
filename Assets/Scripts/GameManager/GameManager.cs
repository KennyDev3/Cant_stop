using UnityEngine;
using UnityEngine.SceneManagement;
using System;

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

    private void Start()
    {
        StartRun();
    }


    public void StartRun()
    {
        CurrentRotation = 0;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

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
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.ResetDifficulty();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        StartRun();
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}