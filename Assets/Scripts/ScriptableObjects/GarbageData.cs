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

    
}