using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(menuName = "Game/Loot Table")]

public class LootTableSO : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
    {
        public ItemSO item;
        public int weight;
    }

    public List<LootEntry> commonItems;
    public List<LootEntry> rareItems;
    public List<LootEntry> legendaryItems;

    public ItemSO GetRandomItem(ItemRarity forcedRarity) // Or weighted rarity logic
    {
        List<LootEntry> table = forcedRarity switch
        {
            ItemRarity.Rare => rareItems,
            ItemRarity.Legendary => legendaryItems,
            _ => commonItems
        };

        // Standard Weighted Random Algorithm
        int totalWeight = table.Sum(x => x.weight);
        int rng = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var entry in table)
        {
            currentWeight += entry.weight;
            if (rng < currentWeight) return entry.item;
        }

        return table[0].item; // Fallback
    }



}
