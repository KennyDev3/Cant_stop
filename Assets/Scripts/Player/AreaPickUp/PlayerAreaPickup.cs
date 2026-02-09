using UnityEngine;
using System.Collections;
using StarterAssets;

[RequireComponent(typeof(LineRenderer))]
public class PlayerAreaPickup : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float radius = 2.0f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Cooldown (Stamina Style)")]
    [SerializeField] private float pickupCooldown = 3.0f; 
    private float _currentCooldownTimer;


    [Header("VFX Settings")]
    [SerializeField] private GameObject pickupVfxPrefab;
    [SerializeField] private float vfxScaleRatio = 0.8f;

    [Header("Visuals")]
    [SerializeField] private float visualDuration = 0.3f;
    [SerializeField] private int segments = 50;
    [SerializeField] private Color circleColor = Color.cyan;
    [SerializeField] private float lineWidth = 0.1f;

    private LineRenderer _line;
    private StarterAssetsInputs _input;
    private PlayerGarbageHandler _garbageHandler;
    private UIManager _uiManager;

    void Start()
    {
        _input = GetComponentInParent<StarterAssetsInputs>();
        _garbageHandler = GetComponentInParent<PlayerGarbageHandler>();
        _uiManager = FindFirstObjectByType<UIManager>();

        SetupLineRenderer();

        // Start FULL (Ready to use)
        _currentCooldownTimer = pickupCooldown;

        if (_uiManager != null)
        {
            // Send (3, 3) -> Full Bar
            _uiManager.UpdatePickupCooldown(_currentCooldownTimer, pickupCooldown);
        }
    }

    void Update()
    {
        HandleCooldownRegen();

        if (_input.pickUpGarbage)
        {
            _input.pickUpGarbage = false;

           
            if (_currentCooldownTimer < pickupCooldown)
            {
                Debug.Log("Ability on Cooldown");
                return;
            }

            PerformAreaPickup();
        }
    }

    void HandleCooldownRegen()
    {
        if (_currentCooldownTimer < pickupCooldown)
        {
            _currentCooldownTimer += Time.deltaTime;

            if (_currentCooldownTimer > pickupCooldown)
                _currentCooldownTimer = pickupCooldown;

            // Update UI (Current, Max)
            if (_uiManager != null)
            {
                _uiManager.UpdatePickupCooldown(_currentCooldownTimer, pickupCooldown);
            }
        }
    }

    void PerformAreaPickup()
    {
        //StartCoroutine(ShowPickupRadius()); // Turn on to show linerenderer Debugger 

        TriggerPickupVFX();

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, interactableLayer);
        int collectedCount = 0;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out GarbageItem item))
            {
                // In magnet mode, let the magnet system handle enemy loot garbage
                if (GameManager.Instance != null &&
                    GameManager.Instance.UseEnemyGarbageMagnetPickup &&
                    item.UseMagnetPickup)
                {
                    continue;
                }

                if (_garbageHandler.IsOverencumbered) break;
                if (_garbageHandler.TryInstantCollect(item)) collectedCount++;
            }
        }

        if (collectedCount > 0)
        {
            Debug.Log($"Collected {collectedCount} items.");

            _currentCooldownTimer = 0f;

            if (_uiManager != null)
            {
                _uiManager.UpdatePickupCooldown(0f, pickupCooldown);
            }
        }
    }

    void TriggerPickupVFX()
    {
        if (pickupVfxPrefab == null) return;

        GameObject vfxInstance = Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);
        float scaleFactor = radius * vfxScaleRatio;
        vfxInstance.transform.localScale = Vector3.one * scaleFactor;
 
    }

    // Visuals
    IEnumerator ShowPickupRadius()
    {
        DrawCircle();
        _line.enabled = true;
        yield return new WaitForSeconds(visualDuration);
        _line.enabled = false;
    }

    void SetupLineRenderer()
    {
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = false;
        _line.loop = true;
        _line.startWidth = lineWidth;
        _line.endWidth = lineWidth;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.startColor = circleColor;
        _line.endColor = circleColor;
        _line.positionCount = segments + 1;
        _line.enabled = false;
    }

    void DrawCircle()
    {
        float angle = 0f;
        for (int i = 0; i < (segments + 1); i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            _line.SetPosition(i, new Vector3(x, 0.5f, z));
            angle += (360f / segments);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}