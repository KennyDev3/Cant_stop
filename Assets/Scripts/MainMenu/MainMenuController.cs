using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "ShaharCombatTest";
    [SerializeField] private string tutorialSceneName = "Tutorial";

    public void OnTutorialClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(tutorialSceneName);
    }
    public void OnPlayClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnQuitClicked()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}
