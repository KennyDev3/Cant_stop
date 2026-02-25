using UnityEngine;

/// <summary>
/// Applies meta capacity upgrades from GameManager to the player's garbage handler once per run.
/// Attach to the player root or a child with access to PlayerGarbageHandler.
/// </summary>
public class PlayerMetaCapacityApply : MonoBehaviour
{
    [SerializeField] private PlayerGarbageHandler garbageHandler;

    private void Awake()
    {
        if (garbageHandler == null)
        {
            garbageHandler = GetComponentInParent<PlayerGarbageHandler>();
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSceneReady += HandleSceneReady;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSceneReady -= HandleSceneReady;
        }
    }

    private void Start()
    {
        TryApplyMetaCapacity();
    }

    private void HandleSceneReady()
    {
        TryApplyMetaCapacity();
    }

    private void TryApplyMetaCapacity()
    {
        if (GameManager.Instance == null || garbageHandler == null) return;
        GameManager.Instance.ApplyMetaCapacityTo(garbageHandler);
    }
}

