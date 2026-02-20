using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Hub upgrade panel controller: one panel showing both Parry and Dash trees.
/// Place on the upgrade panel GameObject. Assign Parry/Dash upgrade SOs, content parents, node prefab, and resource display.
/// </summary>
public class ShopManager : MonoBehaviour
{
    [Header("Tree data")]
    [Tooltip("Parry tree upgrades in order (Unlock, then upgrade 1, then upgrade 2).")]
    [SerializeField] private List<HubUpgradeSO> parryUpgrades = new List<HubUpgradeSO>();
    [Tooltip("Dash tree upgrades in order (Unlock, then upgrade 1, then upgrade 2).")]
    [SerializeField] private List<HubUpgradeSO> dashUpgrades = new List<HubUpgradeSO>();

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
    private Vector3 _openPosition;
    private readonly List<HubUpgradeNodeUI> _parryNodes = new List<HubUpgradeNodeUI>();
    private readonly List<HubUpgradeNodeUI> _dashNodes = new List<HubUpgradeNodeUI>();

    private void Awake()
    {
        if (shopPanel == null) shopPanel = gameObject;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerTransform = player.transform;

        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);
    }

    private void OnEnable()
    {
        _isPanelOpen = true;
        if (_parryNodes.Count == 0 && _dashNodes.Count == 0)
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
        if (closeWhenPlayerAwayDistance <= 0f || _playerTransform == null) return;

        float sqrDist = (_playerTransform.position - _openPosition).sqrMagnitude;
        if (sqrDist > closeWhenPlayerAwayDistance * closeWhenPlayerAwayDistance)
            CloseShop();
    }

    private void PopulateTreesOnce()
    {
        if (nodePrefab == null || parryContentParent == null && dashContentParent == null) return;

        ClearChildren(parryContentParent);
        ClearChildren(dashContentParent);
        _parryNodes.Clear();
        _dashNodes.Clear();

        PopulateTree(parryUpgrades, parryContentParent, _parryNodes);
        PopulateTree(dashUpgrades, dashContentParent, _dashNodes);
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

    /// <summary>
    /// Call when panel is shown or after a purchase to refresh all nodes and resource display.
    /// </summary>
    public void RefreshAll()
    {
        RefreshResourceDisplay();
        foreach (var n in _parryNodes) n.Refresh();
        foreach (var n in _dashNodes) n.Refresh();
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
