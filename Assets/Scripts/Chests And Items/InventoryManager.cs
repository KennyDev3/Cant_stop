using UnityEngine;
using System.Collections.Generic;


public class InventoryManager : MonoBehaviour
{
    private Dictionary<ItemSO, int> _inventory = new Dictionary<ItemSO, int>();

    // References to the entities that get buffed
    [SerializeField] private StatController playerStats;
    [SerializeField] private StatController turretStats;
    private UIManager _uiManager;

    private void Start()
    {
        _uiManager = FindFirstObjectByType<UIManager>();
    }

    public void AddItem(ItemSO item)
    {
        // 1. Add to dictionary
        if (_inventory.ContainsKey(item))
            _inventory[item]++;
        else
            _inventory.Add(item, 1);

        Debug.Log($"Picked up {item.itemName}. Stack: {_inventory[item]}");

        // 2. Recalculate all stats (simplest way to handle stacking without bugs)
        RecalculateAllStats();


        // DEBUGGING LOGIC

        //if (turretStats != null) ============================================> Turret Attack Speed Debug 
        //{
        //    float currentSpeed = turretStats.GetStat(StatType.FireRate);

        //    // Color formatting makes it easy to see in the console
        //    Debug.Log($"<color=green>PICKUP CONFIRMED:</color> Added {item.itemName}. " +
        //              $"<color=cyan>New Fire Rate: {currentSpeed} shots/sec</color>");
        //}

        if (_uiManager != null)
    {
        // Update Item Bar UI For more items
        _uiManager.UpdateItemDisplay(item, _inventory[item]);
    }
    }

    private void RecalculateAllStats()
    {
        // Reset to base values
        if (playerStats) playerStats.ResetStats();
        if (turretStats) turretStats.ResetStats();

        // Re-apply all items
        foreach (var entry in _inventory)
        {
            ItemSO item = entry.Key;
            int stack = entry.Value;

            // Distribute effects based on ItemTarget
            if (item.target == ItemTarget.Player || item.target == ItemTarget.Both)
            {
                if (playerStats) item.ApplyEffect(playerStats, stack);
            }

            if (item.target == ItemTarget.Turret || item.target == ItemTarget.Both)
            {
                if (turretStats) item.ApplyEffect(turretStats, stack);
            }
        }

        // UI events and updates can go here, display items for Player
    }





}
