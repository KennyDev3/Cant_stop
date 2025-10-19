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
    private int _money = 0;



    [System.Serializable]
    public class CapacityChangeEvent : UnityEvent<int, int> { }
    public CapacityChangeEvent onCapacityChanged;

    [System.Serializable]
    public class MoneyChangeEvent : UnityEvent<int> { }
    public MoneyChangeEvent onMoneyChanged;

     private void Start()
    {
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
    }

    public bool TryPickupGarbage(GarbageItem garbageItem)
    {
        GarbageData data = garbageItem.GetGarbageData();

        // Check if the player has enough capacity
        if (_currentCapacity + data.capacityCost <= maxCapacity)
        {
            _currentCapacity += data.capacityCost;
            _carriedGarbage.Add(data);
            onCapacityChanged.Invoke(_currentCapacity, maxCapacity);


            Debug.Log($"Picked up {data.itemName}. Current capacity: {_currentCapacity}/{maxCapacity}");
            // Here you can fire an event to update the UI

            return true;
        }
        else
        {
            Debug.Log($"Not enough capacity to pick up {data.itemName}.");
            // Here you can fire an event to show a "Capacity Full" message

            return false;
        }
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
    




    
}
