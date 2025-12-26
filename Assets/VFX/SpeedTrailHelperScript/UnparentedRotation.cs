using UnityEngine;

public class UnparentedRotation : MonoBehaviour
{
   
    private Rigidbody _parentRb;

    void Start()
    {
        _parentRb = GetComponentInParent<Rigidbody>();
    }

    void LateUpdate()
    {
        
        if (_parentRb != null && _parentRb.linearVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(_parentRb.linearVelocity);
        }
    }
}