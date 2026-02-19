using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

/// <summary>
/// One row in the hub upgrade tree: name, description, cost, state (locked / purchasable / owned), purchase button.
/// </summary>
public class HubUpgradeNodeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private Button purchaseButton;

    private HubUpgradeSO _upgrade;

    public void Setup(HubUpgradeSO upgrade)
    {
        _upgrade = upgrade;
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }
        Refresh();
    }

    public void Refresh()
    {
        if (_upgrade == null) return;

        if (nameText != null) nameText.text = _upgrade.displayName;
        if (descriptionText != null) descriptionText.text = _upgrade.description;

        bool isUnlocked = GameManager.Instance != null && GameManager.Instance.IsHubUpgradeUnlocked(_upgrade.id);
        bool prerequisiteMet = string.IsNullOrEmpty(_upgrade.prerequisiteUpgradeId) ||
            (GameManager.Instance != null && GameManager.Instance.IsHubUpgradeUnlocked(_upgrade.prerequisiteUpgradeId));
        bool canAfford = GameManager.Instance != null && GameManager.Instance.CanAffordHubUpgrade(_upgrade);

        if (costText != null)
            costText.text = FormatCost(_upgrade);

        if (stateText != null)
        {
            if (isUnlocked)
                stateText.text = "Owned";
            else if (!prerequisiteMet)
                stateText.text = "Locked";
            else if (canAfford)
                stateText.text = "Purchasable";
            else
                stateText.text = "Can't afford";
        }

        if (purchaseButton != null)
        {
            purchaseButton.gameObject.SetActive(!isUnlocked);
            purchaseButton.interactable = prerequisiteMet && canAfford && !isUnlocked;
        }
    }

    private static string FormatCost(HubUpgradeSO upgrade)
    {
        if (upgrade == null || upgrade.cost == null || upgrade.cost.Count == 0)
            return "—";
        var sb = new StringBuilder();
        foreach (var entry in upgrade.cost)
        {
            if (entry.resource == null) continue;
            string label = string.IsNullOrEmpty(entry.resource.displayName) ? entry.resource.name : entry.resource.displayName;
            if (sb.Length > 0) sb.Append("  ");
            sb.Append(label).Append(": ").Append(entry.amount);
        }
        return sb.Length > 0 ? sb.ToString() : "—";
    }

    private void OnPurchaseClicked()
    {
        if (_upgrade == null || GameManager.Instance == null) return;
        if (GameManager.Instance.TryPurchaseHubUpgrade(_upgrade))
        {
            Refresh();
            var panel = GetComponentInParent<ShopManager>();
            if (panel != null) panel.RefreshAll();
        }
    }
}
