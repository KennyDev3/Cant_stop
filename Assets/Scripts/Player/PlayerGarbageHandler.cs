using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using StarterAssets;

public class PlayerGarbageHandler : MonoBehaviour
{
    [Header("Capacity")]
    [SerializeField] private int maxCapacity = 10;
    private int _currentCapacity = 0;

    [Header("Inventory")]
    private List<GarbageData> _carriedGarbage = new List<GarbageData>();
    [SerializeField] private float _money = 200f;

    [Header("Strength")]
    [Tooltip("Player's current strength level (1-3). Determines which garbage tiers they can pick up.")]
    [SerializeField] public int playerStrength = 1;

    private GarbageItem _currentItemToCollect;
    private ThirdPersonController _playerController;

    public bool IsOverencumbered
    {
        get { return _currentCapacity > maxCapacity; }
    }

    public int GetBaseMaxCapacity() => maxCapacity;
    public int GetPlayerStrength() => playerStrength;
    public float GetMoney() => _money;
    public int GetCurrentCapacity() => _currentCapacity;

    [System.Serializable]
    public class CapacityChangeEvent : UnityEvent<int, int> { }
    public CapacityChangeEvent onCapacityChanged;

    [System.Serializable]
    public class MoneyChangeEvent : UnityEvent<float> { }
    public MoneyChangeEvent onMoneyChanged;

    void Awake()
    {
        _playerController = GetComponent<ThirdPersonController>();

        if (_playerController == null)
        {
            Debug.LogError("Player is missing ThirdPersonController!");
        }
        else
        {
            // Subscribe to the animation event
            _playerController.OnPickupAnimationComplete += FinalizePickup;
        }
    }

    private void Start()
    {
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
        onMoneyChanged.Invoke(_money);
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (_playerController != null)
        {
            _playerController.OnPickupAnimationComplete -= FinalizePickup;
        }
    }

    // ===================================
    // GARBAGE HANDLING LOGIC
    // ===================================

    /// <summary>
    /// This is the main collection logic. It should ONLY be called by FinalizePickup.
    /// </summary>
    private bool AddGarbageToInventory(GarbageData data)
    {
        _currentCapacity += data.capacityCost;
        _carriedGarbage.Add(data);
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);

        Debug.Log($"Collected {data.itemName}. Current capacity: {_currentCapacity}/{maxCapacity}");

        if (IsOverencumbered)
        {
            Debug.Log("Player is now overencumbered! Movement penalties should apply.");
        }
        return true;
    }

    /// <summary>
    /// Called by GarbageItem.Interact() to start the entire animation and collection process.
    /// </summary>
    public bool StartPickupProcess(GarbageItem garbageItem)
    {
        GarbageData data = garbageItem.GetGarbageData();

        // 1. Check if player is strong enough
        if (playerStrength < data.garbageTier)
        {
            Debug.LogWarning($"Not strong enough to pick up {data.itemName} (Tier {data.garbageTier}).");
            return false;
        }

        
        if (_playerController.StartPickUp(false))
        {
            
            _currentItemToCollect = garbageItem;

            Debug.Log($"Initiated pickup sequence for {data.itemName}. Waiting for animation...");
            return true;
        }
        else
        {
            return false;
        }
    }


    private void FinalizePickup()
    {
        if (_currentItemToCollect == null)
        {
            // This can happen if the event fires but no item was being collected
            // (e.g., if another system triggers the animation)
            return;
        }

        GarbageData data = _currentItemToCollect.GetGarbageData();

        // 1. Add item to inventory
        AddGarbageToInventory(data);

        // 2. Tell the item it has been collected (so it can destroy itself)
        _currentItemToCollect.NotifyCollected();

        // 3. Clear the reference
        _currentItemToCollect = null;
    }


    public bool TryInstantCollect(GarbageItem item) // Instant Area Pick up Logic
    {
        GarbageData data = item.GetGarbageData();

        // 1. Check Strength
        if (playerStrength < data.garbageTier)
        {
            Debug.Log("You are too weak to pick this item up");
            return false;
        }
        AddGarbageToInventory(data);

        // 3. Notify item to destroy itself
        item.NotifyCollected();

        return true;
    }

    public int DropOffGarbage()
    {
        int totalValue = 0;
        foreach (var garbage in _carriedGarbage)
        {
            totalValue += garbage.value;
        }

        Debug.Log($"Dropped off {_carriedGarbage.Count} items for ${totalValue}");
        _money += totalValue;

        _carriedGarbage.Clear();
        _currentCapacity = 0;

        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
        onMoneyChanged.Invoke(_money);

        return totalValue;
    }

    // ===================================
    // ECONOMY & UPGRADES
    // ===================================

    public bool CanAfford(float amount)
    {
        return _money >= amount;
    }

    public bool Spend(float amount)
    {
        if (amount <= 0) return false;

        if (CanAfford(amount))
        {
            _money -= amount;
            onMoneyChanged.Invoke(_money);
            Debug.Log($"Spent ${amount:F2}. New balance: ${_money:F2}");
            return true;
        }
        else
        {
            float needed = amount - _money;
            Debug.LogWarning($"Not enough funds! Need ${needed:F2} more.");
            return false;
        }
    }

    public void UpgradeMaxCapacity(int increaseAmount)
    {
        if (increaseAmount <= 0) return;
        maxCapacity += increaseAmount;
        Debug.Log($"Max Capacity upgraded to {maxCapacity}.");
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
    }

    public void UpgradePlayerStrength(int increaseAmount = 1)
    {
        if (increaseAmount <= 0) return;
        playerStrength += increaseAmount;
        Debug.Log($"Player Strength upgraded to Tier {playerStrength}.");
    }
}