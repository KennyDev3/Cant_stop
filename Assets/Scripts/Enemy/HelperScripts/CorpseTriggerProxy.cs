using UnityEngine;

public class CorpseTriggerProxy : MonoBehaviour
{
    [HideInInspector] public EnemyHealth parentHealth;

    private void OnTriggerEnter(Collider other)
    {
        // Pass the message to the main script
        if (parentHealth != null)
        {
            parentHealth.HandleExternalTrigger(other);
        }
    }
}