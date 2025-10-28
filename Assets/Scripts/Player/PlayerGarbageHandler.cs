using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;


public class PlayerGarbageHandler : MonoBehaviour
{
    [Header("Capacity")]
    [SerializeField] private int maxCapacity = 10;
    private int _currentCapacity = 0;

    [Header("Inventory")]
    private List<GarbageData> _carriedGarbage = new List<GarbageData>();
    [SerializeField] float _money = 200;

    [Header("Strength")]
    [Tooltip("Player's current strength level (1-3). Determines which garbage tiers they can pick up.")]
    [SerializeField] public int playerStrength = 1; // Default to 1


    public bool IsOverencumbered
    {
        get { return _currentCapacity > maxCapacity; }
    }

    public int GetBaseMaxCapacity() => maxCapacity;
    public int GetPlayerStrength() => playerStrength;



    [System.Serializable]
    public class CapacityChangeEvent : UnityEvent<int, int> { }
    public CapacityChangeEvent onCapacityChanged;

    [System.Serializable]
    public class MoneyChangeEvent : UnityEvent<float> { }
    public MoneyChangeEvent onMoneyChanged;

     private void Start()
    {
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
        onMoneyChanged.Invoke(_money);
    }

    public bool PickupGarbage(GarbageItem garbageItem)
    {
        GarbageData data = garbageItem.GetGarbageData();

        if (playerStrength < data.garbageTier)
        {
            // Player's strength is NOT sufficient to pick up this tier of garbage
            Debug.LogWarning($"Not strong enough to pick up {data.itemName} (Tier {data.garbageTier}). " +
                             $"Requires Strength {data.garbageTier}, Player has Strength {playerStrength}.");

            return false;
        }

        // Add item and update capacity
        _currentCapacity += data.capacityCost;
        _carriedGarbage.Add(data);
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);

        Debug.Log($"Picked up {data.itemName}. Current capacity: {_currentCapacity}/{maxCapacity}");

        if (IsOverencumbered)
        {
            Debug.Log("Player is now overencumbered!");
            // You could fire an event here to show a "Overencumbered" message on the UI
        }

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

        
        // Clear inventory
        _carriedGarbage.Clear();
        _currentCapacity = 0;

        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
        onMoneyChanged.Invoke(_money);
        // Here you can fire an event to update the UI (capacity, cash)
        
        return totalValue;
    }


    public float GetMoney()
    {
        return _money;
    }

    

    public bool CanAfford(float amount)
    {
        return _money >= amount;
    }

    public bool Spend(float amount)
    {
        if (amount <= 0)
        {
            Debug.LogError("Attempted to spend a non-positive amount.");
            return false;
        }

        if (CanAfford(amount))
        {
            _money -= amount;
            onMoneyChanged.Invoke(_money);
            Debug.Log($"Spent ${amount:F2}. New balance: ${_money:F2}");
            return true;
        }
        else
        {
            // Now, the failure logic is executed whenever the Spend method is called
            float needed = amount - _money;
            Debug.LogWarning($"Not enough funds! You need ${needed:F2} more to buy this item (Cost: ${amount:F2}, Current: ${_money:F2}).");
            return false;
        }
    }

    public void UpgradeMaxCapacity(int increaseAmount)
    {
        maxCapacity += increaseAmount;
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
    }

    public void UpgradePlayerStrength(int increaseAmount = 1)
    {
        playerStrength += increaseAmount;
        // Optionally clamp or add an event here
    }








}
