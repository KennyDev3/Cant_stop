using UnityEngine;

// Define all possible upgrade types here
public enum UpgradeType
{
    None,
    MaxStamina,
    PlayerCapacity,
    TruckSpeed
}

[CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "Upgrade/Upgrade Definition", order = 1)]
public class UpgradeDefinition : ScriptableObject
{
    [Header("Core Definition")]
    public UpgradeType type;
    public string displayName = "New Upgrade";
    [TextArea(3, 5)]
    public string baseDescription = "Increases something. Current level: {0}. Next level: {1}";

    [Header("Leveling")]
    public int maxLevel = 5;
    [Tooltip("The initial cost for Level 1.")]
    public float baseCost = 100f;
    [Tooltip("The multiplier for cost per level (e.g., 100, 200, 300...).")]
    public float costScaleFactor = 100f;

    [Header("Effect")]
    [Tooltip("The amount of increase applied per level.")]
    public float valuePerLevel = 50f; // e.g., +50 Max Stamina

    /// <summary>
    /// Calculates the cost for the given target level.
    /// </summary>
    public float GetCostForLevel(int targetLevel)
    {
        // Cost: baseCost * targetLevel (100 * 1, 100 * 2, 100 * 3, etc.)
        // Using costScaleFactor * targetLevel as requested: 100, 200, 300, 400...
        return costScaleFactor * targetLevel;
    }
}
