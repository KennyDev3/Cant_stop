using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    private ItemSO _itemData;
    private bool _canPickup = false;

    public void Initialize(ItemSO itemData)
    {
        _itemData = itemData;
        // Delay pickup so you don't instantly grab it when it spawns
        Invoke(nameof(EnablePickup), 0.5f);
    }
    private void EnablePickup() => _canPickup = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!_canPickup) return;

        // Check if Player touched it
        if (other.CompareTag("Player"))
        {
            // Find Inventory Manager
            var inventory = other.GetComponent<InventoryManager>();
            // Or FindFirstObjectByType<InventoryManager>() if it's a singleton

            if (inventory != null)
            {
                inventory.AddItem(_itemData);
                Destroy(gameObject);
            }
        }
    }


}
