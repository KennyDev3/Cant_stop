using UnityEngine;
using System.Collections.Generic;
using System;


public class InventoryManager : MonoBehaviour
{
    public event Action OnItemPickedUp;

    // Debugger to help spawn with items
    [System.Serializable]
    public struct DebugItemEntry
    {
        public ItemSO item;
        [Min(1)] public int count;
    }

    [Header("Debugging")]
    [SerializeField] private List<DebugItemEntry> startingItems;


    //


    private Dictionary<ItemSO, int> _inventory = new Dictionary<ItemSO, int>();

    // References to the entities that get buffed
    [SerializeField] private StatController playerStats;
    [SerializeField] private StatController turretStats;
    private UIManager _uiManager;

    private void Start()
    {
        _uiManager = FindFirstObjectByType<UIManager>();

        // Spawn with added Items
        if (startingItems != null && startingItems.Count > 0)
        {
            ProcessStartingItems();
        }


    }

    // ================================================================ DEBUGGING
    // Process the list from the inspector
    private void ProcessStartingItems()
    {
        foreach (var entry in startingItems)
        {
            if (entry.item == null) continue;

            // Add directly to the internal dictionary
            if (_inventory.ContainsKey(entry.item))
                _inventory[entry.item] += entry.count;
            else
                _inventory.Add(entry.item, entry.count);

            Debug.Log($"<color=yellow>DEBUG LOAD:</color> Added {entry.count}x {entry.item.itemName}");

            // Sync UI
            if (_uiManager != null)
                _uiManager.UpdateItemDisplay(entry.item, _inventory[entry.item]);
        }

       
        RecalculateAllStats();

    }
    // ================================================================ DEBUGGING

    public void AddItem(ItemSO item)
    {

        OnItemPickedUp?.Invoke();

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

        // Show Fading Item description Panel
        _uiManager.ShowItemPopup(item);

        }
    }

    private void RecalculateAllStats()
    {
        if (playerStats) playerStats.ResetStats();
        if (turretStats) turretStats.ResetStats();

        foreach (var entry in _inventory)
        {
            ItemSO item = entry.Key;
            int stack = entry.Value;

            if (item.target == ItemTarget.Player || item.target == ItemTarget.Both)
                if (playerStats) item.ApplyEffect(playerStats, stack);

            if (item.target == ItemTarget.Turret || item.target == ItemTarget.Both)
                if (turretStats) item.ApplyEffect(turretStats, stack);
        }

        

        if (playerStats)
        {
            // Find all scripts on the player that care about stats (OrbController, Aura, etc)
            var receivers = playerStats.GetComponents<IStatReceiver>();
            foreach (var receiver in receivers) receiver.OnStatsRecalculated();
        }

        if (turretStats)
        {
            // Find all scripts on the turret (TruckTurret)
            var receivers = turretStats.GetComponents<IStatReceiver>();
            foreach (var receiver in receivers) receiver.OnStatsRecalculated();
        }
    }





}
