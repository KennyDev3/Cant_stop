using UnityEngine;

public class PlayerMagnetPickupZone : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Radius around the player where enemy loot garbage starts homing toward the player.")]
    [SerializeField] private float radius = 4f;
    [Tooltip("Layers to check for magnetizable garbage (e.g. Interactable). Uses OverlapSphere so trigger collision matrix is not required.")]
    [SerializeField] private LayerMask magnetLayers = -1;

    [Header("References")]
    [Tooltip("Optional. If unset, uses GetComponentInParent at runtime.")]
    [SerializeField] private PlayerGarbageHandler garbageHandler;

    [Header("Polling")]
    [Tooltip("How often to scan for garbage in range (seconds).")]
    [SerializeField] private float pollInterval = 0.1f;

    private float _nextPollTime;

    private void Awake()
    {
        if (garbageHandler == null)
            garbageHandler = GetComponentInParent<PlayerGarbageHandler>();
    }

    private void FixedUpdate()
    {
        if (!IsMagnetModeEnabled()) return;
        if (garbageHandler == null) return;
        if (garbageHandler.IsOverencumbered) return;
        if (Time.time < _nextPollTime) return;

        _nextPollTime = Time.time + pollInterval;

        Vector3 center = transform.position;
        Collider[] hits = Physics.OverlapSphere(center, radius, magnetLayers);

        foreach (Collider col in hits)
        {
            if (!col.TryGetComponent(out GarbageItem item)) continue;
            if (!item.UseMagnetPickup) continue;

            item.StartMagnet(garbageHandler.transform, garbageHandler);
        }
    }

    private bool IsMagnetModeEnabled()
    {
        if (GameManager.Instance == null) return true;
        return GameManager.Instance.UseEnemyGarbageMagnetPickup;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}

