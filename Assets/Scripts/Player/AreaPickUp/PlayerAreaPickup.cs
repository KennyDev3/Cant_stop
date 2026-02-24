using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

    // Passive hub upgrades – pickup radius
    private float _baseRadius;
    private int _passivePickupLevelApplied = 0;

    [Header("Passive Hub Upgrades")]
    [Tooltip("Pickup Range passive upgrades in order (Level 1, 2, 3). primaryAmount = radius bonus fraction (0.10, 0.20, 0.30).")]
    [SerializeField] private List<HubUpgradeSO> passivePickupRangeUpgrades = new List<HubUpgradeSO>();

    void Start()
    {
        _input = GetComponentInParent<StarterAssetsInputs>();
        _garbageHandler = GetComponentInParent<PlayerGarbageHandler>();
        _uiManager = FindFirstObjectByType<UIManager>();

        SetupLineRenderer();

        _baseRadius = radius;
        ApplyPassivePickupRangeFromHubUpgrades();

        // Start FULL (Ready to use)
        _currentCooldownTimer = pickupCooldown;

        if (_uiManager != null)
        {
            // Send (3, 3) -> Full Bar
            _uiManager.UpdatePickupCooldown(_currentCooldownTimer, pickupCooldown);
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnHubUpgradeUnlocked += HandleHubUpgradeUnlocked;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnHubUpgradeUnlocked -= HandleHubUpgradeUnlocked;
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

    private void HandleHubUpgradeUnlocked(string upgradeId)
    {
        if (upgradeId == HubUpgradeKeys.PassivePickupRange1 ||
            upgradeId == HubUpgradeKeys.PassivePickupRange2 ||
            upgradeId == HubUpgradeKeys.PassivePickupRange3)
        {
            ApplyPassivePickupRangeFromHubUpgrades();
        }
    }

    private void ApplyPassivePickupRangeFromHubUpgrades()
    {
        if (GameManager.Instance == null) return;

        HubUpgradeSO selected = null;
        int selectedLevel = 0;

        if (passivePickupRangeUpgrades != null)
        {
            for (int i = 0; i < passivePickupRangeUpgrades.Count; i++)
            {
                var upgrade = passivePickupRangeUpgrades[i];
                if (upgrade == null || string.IsNullOrEmpty(upgrade.id)) continue;
                if (GameManager.Instance.IsHubUpgradeUnlocked(upgrade.id))
                {
                    selected = upgrade;
                    selectedLevel = i + 1;
                }
            }
        }

        if (selected == null && _passivePickupLevelApplied == 0)
            return;

        _passivePickupLevelApplied = selectedLevel;

        float multiplier = 1f;
        if (selected != null)
        {
            // primaryAmount is radius bonus fraction (e.g. 0.10, 0.20, 0.30)
            multiplier = 1f + selected.primaryAmount;
        }

        radius = _baseRadius * multiplier;

        // Keep debug/line-renderer visual in sync
        if (_line != null)
        {
            DrawCircle();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    [ContextMenu("Debug/Passive Pickup Radius")]
    private void DebugPassivePickupRadius()
    {
        Debug.Log($"[Passive Debug] BaseRadius={_baseRadius}, CurrentRadius={radius}, PassivePickupLevel={_passivePickupLevelApplied}", this);
    }
}