using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "ShaharCombatTest";
    [SerializeField] private string tutorialSceneName = "Tutorial";

    public void OnTutorialClicked()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.RequestScene(SceneRequest.ToScene(tutorialSceneName, false));
        else
            SceneManager.LoadScene(tutorialSceneName);
    }

    public void OnPlayClicked()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.RequestScene(SceneRequest.ToScene(gameSceneName, false));
        else
            SceneManager.LoadScene(gameSceneName);
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}
