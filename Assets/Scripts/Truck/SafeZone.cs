using UnityEngine;

public class SafeZone : MonoBehaviour
{
    public static event System.Action OnTruckEnteredSafeZone;
    public static event System.Action OnTruckExitedSafeZone;

    private bool isTruckInside = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Truck") && !isTruckInside)
        {
            isTruckInside = true;
            OnTruckEnteredSafeZone?.Invoke();
            if (GameManager.Instance.CurrentRotation > 0)
            {
                // Logic to enable shop interaction can be triggered here
            }
            GameManager.Instance.IncrementRotation();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Truck"))
        {
            isTruckInside = false;
            OnTruckExitedSafeZone?.Invoke();
        }
    }
}