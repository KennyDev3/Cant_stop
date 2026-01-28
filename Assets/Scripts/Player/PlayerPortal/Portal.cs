using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Leave EMPTY to let GameManager decide the next level automatically. Only fill this if you want to force a specific level (like a secret level).")]
    [SerializeField] private string nextSceneNameOverride;

    [SerializeField] private bool returnToHub = false;
    [SerializeField] private bool increaseDifficulty = true;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        if (other.CompareTag("Player"))
        {
            _triggered = true;
            ActivatePortal(other.gameObject);
        }
    }

    // Call this if you need to trigger the portal via code/event instead of collision
    public void HandleExternalTrigger(Collider other)
    {
        OnTriggerEnter(other);
    }

    private void ActivatePortal(GameObject playerObj)
    {
        Debug.Log("<color=cyan>[Portal]</color> Activating... Saving run data.");

        float hp = 0;
        Dictionary<ItemSO, int> inventory = new Dictionary<ItemSO, int>();
        float money = 0;
        float waveCredits = 0;
        float trickleCredits = 0;

        if (playerObj.TryGetComponent(out PlayerHealth health))
        {
            hp = health.GetCurrentHealth();
        }

        if (playerObj.TryGetComponent(out InventoryManager inv))
        {
            inventory = inv.GetInventory();
            Debug.Log($"<color=orange>[PORTAL]</color> Saving {inventory.Count} item types from Inventory.");
        }

        if (playerObj.TryGetComponent(out PlayerGarbageHandler garbageHandler))
        {
            money = garbageHandler.GetMoney();
        }

        if (EnemyDirector.Instance != null)
        {
            waveCredits = EnemyDirector.Instance.GetWaveCredits();
            trickleCredits = EnemyDirector.Instance.GetTrickleCredits();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveRunData(hp, inventory, money, waveCredits, trickleCredits);

            // To force difficulty jump between levels
            if (increaseDifficulty && DifficultyManager.Instance != null)
            {
                // DifficultyManager.Instance.IncreaseDifficulty(); // Uncomment if you have this method
            }

            // Determine Destination
            if (returnToHub)
            {
                GameManager.Instance.LoadSpecificLevel("HubScene");
            }
            else if (!string.IsNullOrEmpty(nextSceneNameOverride))
            {
                // Manual Override (e.g. Secret Exit)
                GameManager.Instance.LoadSpecificLevel(nextSceneNameOverride);
            }
            else
            {
                // Standard Logic: Check the list in GameManager and go to next
                GameManager.Instance.LoadNextLevelInSequence();
            }
        }
        else
        {
            // Fallback for testing (if no GameManager is present)
            Debug.LogWarning("[Portal] No GameManager found. Reloading current scene as fallback.");
            SceneManager.LoadScene(string.IsNullOrEmpty(nextSceneNameOverride) ? SceneManager.GetActiveScene().name : nextSceneNameOverride);
        }
    }
}