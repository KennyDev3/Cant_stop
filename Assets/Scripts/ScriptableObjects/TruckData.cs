using UnityEngine;

[CreateAssetMenu(fileName = "NewTruckData", menuName = "Truck/Truck Data")]
public class TruckData : ScriptableObject
{
    public float maxHealth;
    public float moveSpeed;
    // We will add damage and other stats here later
}
