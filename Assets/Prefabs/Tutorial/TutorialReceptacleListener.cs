using UnityEngine;
using UnityEngine.Events;

public class TutorialReceptacleListener : MonoBehaviour
{
    // Event to tell the Manager that trash arrived
    public UnityEvent OnTrashThrownIn;

    private void OnTriggerEnter(Collider other)
    {
        // We look for the same component your main script looks for
        if (other.GetComponent<GarbageBundle>() != null)
        {
            OnTrashThrownIn.Invoke();
        }
    }
}