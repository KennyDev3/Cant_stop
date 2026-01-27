using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Name of the scene to load. Leave empty to reload current level.")]
    [SerializeField] private string nextSceneName;

    [SerializeField] private bool returnToHub = false;
    [SerializeField] private bool increaseDifficulty = true;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        if (other.CompareTag("Player"))
        {
            _triggered = true;
            ActivatePortal();
            Debug.Log(other);
        }
    }

    public void HandleExternalTrigger(Collider other)
    {
        OnTriggerEnter(other);
    }

    private void ActivatePortal()
    {
        Debug.Log("Entering Portal...");

        if (increaseDifficulty && DifficultyManager.Instance != null)
        {
            // Logic to increase difficulty immediately if desired
            // Currently DifficultyManager scales by time, but you could force a stage jump here
        }

        if (GameManager.Instance != null)
        {
            if (returnToHub)
            {
                GameManager.Instance.GoToHub();
            }
            else
            {
                // Reload current scene if name is empty, otherwise load next
                string target = string.IsNullOrEmpty(nextSceneName) ? SceneManager.GetActiveScene().name : nextSceneName;
                GameManager.Instance.LoadNextLevel(target);
            }
        }
        else
        {
            
            SceneManager.LoadScene(string.IsNullOrEmpty(nextSceneName) ? SceneManager.GetActiveScene().name : nextSceneName);
        }
    }
}