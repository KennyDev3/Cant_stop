using UnityEngine;
using System.Collections.Generic;
using TMPro; // Assuming TextMeshPro for UI components

public class UpgradeHandler : MonoBehaviour
{
    // Dictionary to track the current level of each upgrade type
    private Dictionary<UpgradeType, int> _upgradeLevels = new Dictionary<UpgradeType, int>();

    [Header("Component References (Set in Inspector)")]
    [SerializeField] private PlayerStamina playerStamina;
    [SerializeField] public PlayerGarbageHandler playerGarbageHandler;

    void Start()
    {
        // Initialize all known upgrade types to Level 0
        foreach (UpgradeType type in System.Enum.GetValues(typeof(UpgradeType)))
        {
            if (type != UpgradeType.None)
            {
                // In a real game, this is where you'd load saved data.
                if (!_upgradeLevels.ContainsKey(type))
                {
                    _upgradeLevels.Add(type, 0);
                }
            }
        }

        // Initialize Stamina component if not assigned
        if (playerStamina == null)
        {
            playerStamina = GetComponent<PlayerStamina>();
        }
        if (playerGarbageHandler == null)
        {
            playerGarbageHandler = FindFirstObjectByType<PlayerGarbageHandler>();
        }
    }

    public int GetCurrentLevel(UpgradeType type)
    {
        return _upgradeLevels.GetValueOrDefault(type, 0);
    }

    public bool TryPurchaseUpgrade(UpgradeDefinition definition)
    {
        int currentLevel = GetCurrentLevel(definition.type);
        int nextLevel = currentLevel + 1;

        if (currentLevel >= definition.maxLevel)
        {
            Debug.Log($"[Upgrade] Max level reached for {definition.displayName}.");
            return false;
        }

        float cost = definition.GetCostForLevel(nextLevel);

        if (playerGarbageHandler == null || !playerGarbageHandler.CanAfford(cost))
        {
            Debug.Log($"[Upgrade] Cannot afford {definition.displayName}. Requires ${cost}.");
            return false;
        }

        // 1. Deduct cost
        playerGarbageHandler.Spend(cost);

        // 2. Apply the effect
        ApplyUpgradeEffect(definition, nextLevel);

        // 3. Update level tracking
        _upgradeLevels[definition.type] = nextLevel;
        Debug.Log($"[Upgrade] Purchased {definition.displayName} (Level {nextLevel}) for ${cost}.");

        // Notify the ShopManager to refresh the UI
        ShopManager shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager != null)
        {
            shopManager.RefreshUpgradeUI();
        }

        return true;
    }

    private void ApplyUpgradeEffect(UpgradeDefinition definition, int nextLevel)
    {
        // Total value is calculated based on the cumulative level
        float totalIncrease = definition.valuePerLevel * nextLevel;

        switch (definition.type)
        {
            case UpgradeType.MaxStamina:
                if (playerStamina != null)
                {
                    // For stamina, we add the value per level (50) to the base.
                    // To handle cumulative upgrades, we'll reset and reapply the total effect.

                    // NOTE: This assumes the base MaxStamina is set in the inspector (e.g. 100) 
                    // and we calculate the total from that base + (valuePerLevel * level).
                    // Since the current PlayerStamina doesn't track a 'base' vs 'upgraded' value, 
                    // a simpler approach is to let PlayerStamina handle the additive change.

                    // Additive change:
                    playerStamina.UpgradeMaxStamina(definition.valuePerLevel);
                }
                break;
            case UpgradeType.PlayerCapacity:
                // Future: Get player capacity component and call UpgradeCapacity(definition.valuePerLevel);
                break;
                // Add more cases for future upgrade types
        }
    }

}
