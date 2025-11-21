using UnityEngine;


public abstract class ItemSO : ScriptableObject
{
    [Header("Display")]
    public string itemName;
    public Sprite icon;
    public GameObject pickupPrefab; // The physical visual to spawn from chest

    [Header("Settings")]
    public ItemRarity rarity;
    public ItemTarget target; // Who gets the buff?

    // The core logic method
    
    public abstract void ApplyEffect(StatController targetStats, int stackCount);
}

