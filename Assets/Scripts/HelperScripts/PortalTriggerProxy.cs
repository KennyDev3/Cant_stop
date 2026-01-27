using UnityEngine;

public class PortalTriggerProxy : MonoBehaviour
{
    [HideInInspector] public Portal portal;

    private void OnTriggerEnter(Collider other)
    {
        // Pass the message to the main script
        if (portal != null)
        {
            portal.HandleExternalTrigger(other);
        }
    }
}