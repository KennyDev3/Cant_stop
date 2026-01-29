using UnityEngine;
using System.Collections.Generic;
using System;

public class InventoryManager : MonoBehaviour, IRunStateContributor
{
    public event Action OnItemPickedUp;

    [System.Serializable]
    public struct DebugItemEntry
    {
        public ItemSO item;
        [Min(1)] public int count;
    }

    public Dictionary<ItemSO, int> GetInventory() => _inventory;

    public void ContributeToRunState(RunState state)
    {
        state.Inventory.Items.Clear();
        foreach (var kvp in _inventory)
            state.Inventory.Items[kvp.Key] = kvp.Value;
    }

    public void ApplyRunState(RunState state)
    {
        if (state.Inventory.Items.Count == 0)
        {
            Debug.Log($"[RunState] InventoryManager skipped (0 items in state)");
            return;
        }

        _inventory.Clear();
        foreach (var kvp in state.Inventory.Items)
            _inventory[kvp.Key] = kvp.Value;

        RecalculateAllStats();
        UpdateDebugList();

        UIManager ui = FindFirstObjectByType<UIManager>();
        if (ui != null)
        {
            foreach (var entry in _inventory)
                ui.UpdateItemDisplay(entry.Key, entry.Value);
        }

        Debug.Log($"[RunState] InventoryManager applied: {_inventory.Count} item types");
    }

    [Header("Debugging")]
    [SerializeField] private List<DebugItemEntry> startingItems;

    // ---  VISUAL DEBUG LIST ---
    [Header("Live Inventory View (Read Only)")]
    [SerializeField] private List<string> liveInventoryDebug = new List<string>();
    // ------------------------------

    private Dictionary<ItemSO, int> _inventory = new Dictionary<ItemSO, int>();

    [SerializeField] private StatController playerStats;
    [SerializeField] private StatController turretStats;
    private UIManager _uiManager;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterRunStateContributor(this);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterRunStateContributor(this);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterRunStateContributor(this);

        _uiManager = FindFirstObjectByType<UIManager>();

        if (_uiManager == null)
            Debug.LogError("<color=red>[INVENTORY CRITICAL]</color> UIManager not found in Start!");

        if (startingItems != null && startingItems.Count > 0)
        {
            ProcessStartingItems();
        }

        RecalculateAllStats();
        UpdateDebugList();

        if (_uiManager != null)
        {
            foreach (var entry in _inventory)
                _uiManager.UpdateItemDisplay(entry.Key, entry.Value);
        }
    }

    private void ProcessStartingItems()
    {
        foreach (var entry in startingItems)
        {
            if (entry.item == null) continue;

            if (_inventory.ContainsKey(entry.item))
                _inventory[entry.item] += entry.count;
            else
                _inventory.Add(entry.item, entry.count);

            if (_uiManager != null)
                _uiManager.UpdateItemDisplay(entry.item, _inventory[entry.item]);
        }
        UpdateDebugList();
    }

    public void AddItem(ItemSO item)
    {
        OnItemPickedUp?.Invoke();

        if (_inventory.ContainsKey(item))
            _inventory[item]++;
        else
            _inventory.Add(item, 1);

        Debug.Log($"Picked up {item.itemName}. Stack: {_inventory[item]}");

        RecalculateAllStats();
        UpdateDebugList(); // Update the visual list on Pickup

        if (_uiManager != null)
        {
            _uiManager.UpdateItemDisplay(item, _inventory[item]);
            _uiManager.ShowItemPopup(item);
        }
    }

    // --- HELPER FOR DEBUGGING ---
    private void UpdateDebugList()
    {
        liveInventoryDebug.Clear();
        foreach (var kvp in _inventory)
        {
            string itemName = kvp.Key != null ? kvp.Key.itemName : "Unknown Item";
            liveInventoryDebug.Add($"{itemName} [x{kvp.Value}]");
        }
    }
    // --------------------------------

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
            var receivers = playerStats.GetComponents<IStatReceiver>();
            foreach (var receiver in receivers) receiver.OnStatsRecalculated();
        }

        if (turretStats)
        {
            var receivers = turretStats.GetComponents<IStatReceiver>();
            foreach (var receiver in receivers) receiver.OnStatsRecalculated();
        }
    }
}