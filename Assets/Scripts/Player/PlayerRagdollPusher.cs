using UnityEngine;

public class PlayerRagdollPusher : MonoBehaviour
{
    [Header("Push Settings")]
    [Tooltip("Layer Mask for the Ragdoll Layer ")]
    public LayerMask ragdollLayer;
    public float pushForce = 5.0f;
    public float pushRadius = 1.0f; 

    private CharacterController _cc;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        PushRagdolls();
    }

    private void PushRagdolls()
    {
        
        Vector3 bottom = transform.position + _cc.center + Vector3.down * (_cc.height / 2f - _cc.radius);
        Vector3 top = transform.position + _cc.center + Vector3.up * (_cc.height / 2f - _cc.radius);

        Collider[] hits = Physics.OverlapCapsule(bottom, top, pushRadius, ragdollLayer);

        foreach (var hit in hits)
        {
            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                // Calculate direction from player center to the ragdoll part
                Vector3 direction = (hit.transform.position - transform.position).normalized;

                // Keep the push mostly horizontal so they don't fly upwards too much
                direction.y = 0.2f;

                // Apply velocity change for immediate snappy response, or Force for smoother weight
                rb.AddForce(direction * pushForce, ForceMode.VelocityChange);
            }
        }
    }

    // Visualize the push radius
    private void OnDrawGizmosSelected()
    {
        if (_cc == null) _cc = GetComponent<CharacterController>();
        if (_cc != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = transform.position + _cc.center;
            Gizmos.DrawWireSphere(center, pushRadius);
        }
    }
}