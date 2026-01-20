using UnityEngine;

public class DroppedLootBox : MonoBehaviour
{
    [SerializeField] private GarbageItem garbageItem;

    public void Initialize(GarbageData data)
    {
        if (garbageItem != null)
        {
            garbageItem.ActivatePooledInteractable(data);

            garbageItem.isPooledObject = false;
        }
    }
}