using UnityEngine;

[CreateAssetMenu(fileName = "New GarbageData", menuName = "Game/Garbage Data")]
public class GarbageData : ScriptableObject
{
    [Tooltip("The name of the garbage item, e.g., 'Old Newspaper'")]
    public string itemName;

    [Tooltip("Weight/capacity. Used for carry limit and disposal value (how much it weighs = how much it's worth).")]
    public int capacityCost = 1;

    [Tooltip("If true, enemy loot using this data can be auto-collected by a magnet radius around the player (Vampire Survivors style).")]
    public bool useMagnetPickup = false;

    [Tooltip("The tier of the garbage item (1-3). Determines the minimum Player Strength needed to pick it up.")]
    [Range(1, 3)] // Add a range for clarity in the inspector
    public int garbageTier = 1;



}