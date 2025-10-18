using UnityEngine;
using System.Collections.Generic;


public class PlayerGarbageHandler : MonoBehaviour
{
     [Header("Capacity")]
    [SerializeField] private int maxCapacity = 10;
    private int _currentCapacity = 0;

    [Header("Inventory")]
    private List<GarbageData> _carriedGarbage = new List<GarbageData>();

    public bool TryPickupGarbage(GarbageItem garbageItem)
    {
        GarbageData data = garbageItem.GetGarbageData();

        // Check if the player has enough capacity
        if (_currentCapacity + data.capacityCost <= maxCapacity)
        {
            _currentCapacity += data.capacityCost;
            _carriedGarbage.Add(data);

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
        
        // Clear inventory
        _carriedGarbage.Clear();
        _currentCapacity = 0;
        
        // Here you can fire an event to update the UI (capacity, cash)
        
        return totalValue;
    }
    




    
}
