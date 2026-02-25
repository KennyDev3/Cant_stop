using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using StarterAssets;

/// <summary>
/// Hub upgrade panel controller: can show Parry, Dash, and multiple Passive trees.
/// Place on the upgrade panel GameObject. Assign upgrade SO lists, content parents, node prefab, and resource display.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [System.Serializable]
    private class PassiveColumn
    {
        [Tooltip("Optional label for editor clarity (e.g. Move Speed, Health Regen, Pickup Range).")]
        public string label;

        [Tooltip("Parent transform for this passive branch (column) nodes.")]
        public Transform contentParent;

        [Tooltip("Upgrades in this passive branch, in order (Unlock -> Level 1 -> Level 2 ...).")]
        public List<HubUpgradeSO> upgrades = new List<HubUpgradeSO>();
    }

    [Header("Tree data")]
    [Tooltip("Parry tree upgrades in order (Unlock, then upgrade 1, then upgrade 2).")]
    [SerializeField] private List<HubUpgradeSO> parryUpgrades = new List<HubUpgradeSO>();
    [Tooltip("Dash tree upgrades in order (Unlock, then upgrade 1, then upgrade 2).")]
    [SerializeField] private List<HubUpgradeSO> dashUpgrades = new List<HubUpgradeSO>();
    [Tooltip("Passive upgrade branches (e.g. Move Speed, Health Regen, Pickup Range), each with its own content parent.")]
    [SerializeField] private List<PassiveColumn> passiveColumns = new List<PassiveColumn>();

    [Header("Layout")]
    [Tooltip("Parent transform for Parry tree nodes (instantiated here).")]
    [SerializeField] private Transform parryContentParent;
    [Tooltip("Parent transform for Dash tree nodes (instantiated here).")]
    [SerializeField] private Transform dashContentParent;
    [Tooltip("Prefab with HubUpgradeNodeUI for one upgrade row.")]
    [SerializeField] private GameObject nodePrefab;

    [Header("Panel")]
    [Tooltip("Root panel GameObject to show/hide. If null, uses this GameObject.")]
    [SerializeField] private GameObject shopPanel;

    [Header("Resource display")]
    [Tooltip("Hub bank counts (e.g. Wood, Gold, Iron).")]
    [SerializeField] private List<ResourceBankSlot> resourceBankSlots = new List<ResourceBankSlot>();

    [System.Serializable]
    public struct ResourceBankSlot
    {
        public ResourceSO resourceType;
        public TextMeshProUGUI countText;
    }

    [Header("Close")]
    [Tooltip("Optional button that closes the panel when clicked.")]
    [SerializeField] private Button closeButton;
    [Tooltip("If player moves farther than this from where they opened the shop, the shop closes. 0 = disabled.")]
    [SerializeField] private float closeWhenPlayerAwayDistance = 4f;

    public bool IsShopOpen => _isPanelOpen;

    private bool _isPanelOpen;
    private Transform _playerTransform;
    private StarterAssetsInputs _input;
    private Vector3 _openPosition;
    private readonly List<HubUpgradeNodeUI> _parryNodes = new List<HubUpgradeNodeUI>();
    private readonly List<HubUpgradeNodeUI> _dashNodes = new List<HubUpgradeNodeUI>();
    private readonly List<HubUpgradeNodeUI> _passiveNodes = new List<HubUpgradeNodeUI>();

    private void Awake()
    {
        if (shopPanel == null) shopPanel = gameObject;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            _input = player.GetComponent<StarterAssetsInputs>();
        }

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);
    }

    private void OnEnable()
    {
        _isPanelOpen = true;
        if (_parryNodes.Count == 0 && _dashNodes.Count == 0 && _passiveNodes.Count == 0)
            PopulateTreesOnce();
        RefreshAll();
    }

    private void OnDisable()
    {
        _isPanelOpen = false;
    }

    private void Update()
    {
        if (!_isPanelOpen) return;

        if (_input != null && _input.interact)
        {
            CloseShop();
            _input.interact = false;
            return;
        }

        if (closeWhenPlayerAwayDistance > 0f && _playerTransform != null)
        {
            float sqrDist = (_playerTransform.position - _openPosition).sqrMagnitude;
            if (sqrDist > closeWhenPlayerAwayDistance * closeWhenPlayerAwayDistance)
                CloseShop();
        }
    }

    private void PopulateTreesOnce()
    {
        bool hasAnyParent = parryContentParent != null || dashContentParent != null;
        if (passiveColumns != null)
        {
            foreach (var col in passiveColumns)
            {
                if (col != null && col.contentParent != null)
                {
                    hasAnyParent = true;
                    break;
                }
            }
        }

        if (nodePrefab == null || !hasAnyParent) return;

        ClearChildren(parryContentParent);
        ClearChildren(dashContentParent);
        if (passiveColumns != null)
        {
            foreach (var col in passiveColumns)
            {
                if (col != null)
                    ClearChildren(col.contentParent);
            }
        }
        _parryNodes.Clear();
        _dashNodes.Clear();
        _passiveNodes.Clear();

        PopulateTree(parryUpgrades, parryContentParent, _parryNodes);
        PopulateTree(dashUpgrades, dashContentParent, _dashNodes);
        if (passiveColumns != null)
        {
            foreach (var col in passiveColumns)
            {
                if (col == null) continue;
                PopulateTree(col.upgrades, col.contentParent, _passiveNodes);
            }
        }
    }

    private void PopulateTree(List<HubUpgradeSO> upgrades, Transform parent, List<HubUpgradeNodeUI> outNodes)
    {
        if (parent == null || nodePrefab == null || upgrades == null) return;
        foreach (var upgrade in upgrades)
        {
            if (upgrade == null) continue;
            GameObject go = Instantiate(nodePrefab, parent);
            if (go.TryGetComponent(out HubUpgradeNodeUI node))
            {
                node.Setup(upgrade);
                outNodes.Add(node);
            }
        }
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    /// <summary>Legacy name for RefreshAll. Keeps UpgradeHandler and other callers compiling.</summary>
    public void RefreshUpgradeUI() => RefreshAll();

    /// <summary>
    /// Call when panel is shown or after a purchase to refresh all nodes and resource display.
    /// </summary>
    public void RefreshAll()
    {
        RefreshResourceDisplay();
        foreach (var n in _parryNodes) n.Refresh();
        foreach (var n in _dashNodes) n.Refresh();
        foreach (var n in _passiveNodes) n.Refresh();
    }

    private void RefreshResourceDisplay()
    {
        if (GameManager.Instance == null || resourceBankSlots == null) return;
        foreach (var slot in resourceBankSlots)
        {
            if (slot.resourceType == null || slot.countText == null) continue;
            int count = GameManager.Instance.GetHubBankCount(slot.resourceType);
            string label = string.IsNullOrEmpty(slot.resourceType.displayName) ? slot.resourceType.name : slot.resourceType.displayName;
            slot.countText.text = $"{label}: {count}";
        }
    }

    /// <summary>Show the panel and free cursor. Called by ShopTerminal, HubUpgradeHouseInteractable, or from code. shopPosition is used for move-away close.</summary>
    public void OpenShop(Vector3 shopPosition)
    {
        if (shopPanel == null) return;
        _openPosition = shopPosition;
        shopPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (_input != null) _input.interact = false;
    }

    public void CloseShop()
    {
        if (!_isPanelOpen) return;
        _isPanelOpen = false;
        if (shopPanel != null)
            shopPanel.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
