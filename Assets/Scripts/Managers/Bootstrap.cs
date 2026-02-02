using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a GameObject in the Bootstrap scene (build index 0).
/// When the game runs, this scene loads first; GameManager (in the same scene) runs Awake and persists.
/// This script then loads the first "real" scene (e.g. Main Menu) via RequestScene so all transitions stay in one place.
/// MainMenu and Hub do not need a bootstrap—they are the scenes that get loaded by this or by RequestScene.
/// </summary>
public class Bootstrap : MonoBehaviour
{
    [Header("First scene after bootstrap")]
    [Tooltip("Scene to load when the game starts (e.g. MainMenu). Must match the name in Build Settings.")]
    [SerializeField] private string firstSceneName = "MainMenu";

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[Bootstrap] GameManager not found in Bootstrap scene. Add a GameManager GameObject to the Bootstrap scene.");
            return;
        }

        GameManager.Instance.RequestScene(SceneRequest.ToScene(firstSceneName, false));
    }
}
