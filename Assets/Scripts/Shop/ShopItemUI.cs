using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI Component References")]
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI levelText;

    private UpgradeDefinition _definition;
    private UpgradeHandler _upgradeHandler;

    /// Initializes the UI element with the upgrade data.
   
    public void Setup(UpgradeDefinition definition, UpgradeHandler handler)
    {
        _definition = definition;
        _upgradeHandler = handler;

        // Set up button listener once
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(OnPurchaseClicked);

        RefreshUI();
    }

    /// Updates the text fields based on the current player level for this upgrade.

    public void RefreshUI()
    {
        if (_upgradeHandler == null || _definition == null) return;

        int currentLevel = _upgradeHandler.GetCurrentLevel(_definition.type);
        int nextLevel = currentLevel + 1;

        nameText.text = $"{_definition.displayName}";
        levelText.text = $"{currentLevel} / {_definition.maxLevel}";

        PlayerGarbageHandler currencyHandler = _upgradeHandler.playerGarbageHandler;
        bool canAfford = false;

        if (currentLevel < _definition.maxLevel)
        {
            float cost = _definition.GetCostForLevel(nextLevel);

            // Calculate current and next effect values (cumulative increase)
            float currentEffect = _definition.valuePerLevel * currentLevel;
            float nextEffect = _definition.valuePerLevel * nextLevel;

            // Update description text with current/next stats
            descriptionText.text = string.Format(_definition.baseDescription,
                                                Mathf.RoundToInt(currentEffect),
                                                Mathf.RoundToInt(nextEffect));

            // Display cost as a clean integer
            costText.text = $"${Mathf.RoundToInt(cost)}";

            // Check affordability using the PlayerGarbageHandler reference
            if (currencyHandler != null)
            {
                canAfford = currencyHandler.CanAfford(cost);
            }
        }
        else
        {
            // Max level reached
            float maxEffect = _definition.valuePerLevel * _definition.maxLevel;
            descriptionText.text = string.Format(_definition.baseDescription,
                                                Mathf.RoundToInt(maxEffect),
                                                Mathf.RoundToInt(maxEffect))
                                                + "\n(MAX LEVEL)";
            costText.text = "SOLD OUT";
        }

        // Update button interactability
        purchaseButton.interactable = true;
    }

    private void OnPurchaseClicked()
    {
        // Delegate purchase attempt to the handler
        _upgradeHandler.TryPurchaseUpgrade(_definition);
    }
}

