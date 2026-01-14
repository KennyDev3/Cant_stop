using UnityEngine;

public class DisposalTriggerProxy : MonoBehaviour
{
    public AdvancedDisposalArea parentScript;

    private void OnTriggerEnter(Collider other)
    {
        if (parentScript != null)
            parentScript.NotifyTriggerEnter(other, this.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (parentScript != null)
            parentScript.NotifyTriggerExit(other, this.gameObject);
    }
}