using UnityEngine;

[CreateAssetMenu(fileName = "New GarbageData", menuName = "Game/Garbage Data")]
public class GarbageData : ScriptableObject
{
    [Tooltip("The name of the garbage item, e.g., 'Old Newspaper'")]
    public string itemName;

    [Tooltip("How much this item 'weighs'. Contributes to player's total capacity.")]
    public int capacityCost = 1;

    [Tooltip("How much cash this item is worth when dropped off.")]
    public int value = 5;

    [Tooltip("The tier of the garbage item (1-3). Determines the minimum Player Strength needed to pick it up.")]
    [Range(1, 3)] // Add a range for clarity in the inspector
    public int garbageTier = 1;



}