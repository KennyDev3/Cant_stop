using UnityEngine;
using StarterAssets;
using System.Collections.Generic; // Added for list management

public class ShopManager : MonoBehaviour
{
    [Header("Upgrade System Setup")]
    [Tooltip("Array of all available ScriptableObject upgrade definitions.")]
    [SerializeField] private UpgradeDefinition[] availableUpgrades;
    [Tooltip("The parent transform where upgrade items will be instantiated.")]
    [SerializeField] private Transform upgradeContentParent;
    [Tooltip("The template prefab for a single upgrade item (must have ShopItemUI.cs).")]
    [SerializeField] private GameObject upgradeTemplatePrefab;

    [Header("UI References")]
    [Tooltip("The root Panel of the Shop UI.")]
    [SerializeField] private GameObject shopPanel;

    [Header("Auto-Close Settings")]
    [Tooltip("The player's UpgradeHandler for purchase logic.")]
    [SerializeField] private UpgradeHandler upgradeHandler;

    // Public property for other scripts to check the shop state
    public bool IsShopOpen => _isShopOpen;

    private Transform _playerTransform;
    private bool _isShopOpen = false;

    private PlayerInteractor _playerInteractor;
    private StarterAssetsInputs _input;

    // Keep track of instantiated UI items to manage refresh
    private List<ShopItemUI> _instantiatedItems = new List<ShopItemUI>();

    void Awake()
    {
        // ... (Player finding logic remains) ...
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            _playerInteractor = player.GetComponent<PlayerInteractor>();
            _input = player.GetComponent<StarterAssetsInputs>();
            // Get UpgradeHandler from Player
            upgradeHandler = player.GetComponent<UpgradeHandler>();
        }

        if (shopPanel == null)
        {
            Debug.LogError("ShopManager::shopPanel is unassigned. Please assign the UI Panel in the Inspector.");
        }
        if (upgradeContentParent == null)
        {
            Debug.LogError("ShopManager::upgradeContentParent is unassigned. Cannot populate upgrades.");
        }
        if (upgradeTemplatePrefab == null)
        {
            Debug.LogError("ShopManager::upgradeTemplatePrefab is unassigned. Cannot populate upgrades.");
        }

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Only check for input closing if the shop is open. Distance is checked by PlayerInteractor.
        if (_isShopOpen)
        {
            // Handle closing with 'E'
            if (_input != null && _input.interact)
            {
                CloseShop();
                _input.interact = false;
                return;
            }
        }
    }

    public void OpenShop(Vector3 shopPosition)
    {
        if (_isShopOpen) return;

        _isShopOpen = true;

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }

        // Populate the shop items when opening
        PopulateUpgrades();

        // CRITICAL CONSUMPTION
        if (_input != null)
        {
            _input.interact = false;
        }

        // 1. FREE THE MOUSE CURSOR
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseShop()
    {
        if (!_isShopOpen) return;

        _isShopOpen = false;

        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        // 1. LOCK THE MOUSE CURSOR
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void PopulateUpgrades()
    {
        // Clear old items
        foreach (Transform child in upgradeContentParent)
        {
            Destroy(child.gameObject);
        }
        _instantiatedItems.Clear();

        if (upgradeHandler == null || upgradeTemplatePrefab == null || upgradeContentParent == null)
        {
            Debug.LogError("Shop components missing. Cannot populate shop.");
            return;
        }

        // Instantiate new items for each definition
        foreach (var definition in availableUpgrades)
        {
            GameObject itemObject = Instantiate(upgradeTemplatePrefab, upgradeContentParent);
            if (itemObject.TryGetComponent(out ShopItemUI itemUI))
            {
                itemUI.Setup(definition, upgradeHandler);
                _instantiatedItems.Add(itemUI);
            }
        }
    }

    public void RefreshUpgradeUI()
    {
        // Called by UpgradeHandler after a successful purchase
        foreach (var itemUI in _instantiatedItems)
        {
            itemUI.RefreshUI();
        }
        // Future: Could also refresh currency display here
    }
}
