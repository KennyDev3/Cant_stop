using UnityEngine;

public class GameUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject deathPanel;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;

            HandleStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(GameState state)
    {
        if (hudPanel) hudPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (deathPanel) deathPanel.SetActive(false);

        switch (state)
        {
            case GameState.Playing:
                if (hudPanel) hudPanel.SetActive(true);
                break;
            case GameState.Paused:
                if (pausePanel) pausePanel.SetActive(true);
                break;
            case GameState.GameOver:
                if (deathPanel) deathPanel.SetActive(true);
                break;
        }
    }

    public void OnResumeClicked() => GameManager.Instance.TogglePause();
    public void OnRestartClicked() => GameManager.Instance.RestartGame();
    public void OnMenuClicked() => GameManager.Instance.ReturnToMainMenu();
    public void OnQuitClicked() => GameManager.Instance.QuitGame();
}
