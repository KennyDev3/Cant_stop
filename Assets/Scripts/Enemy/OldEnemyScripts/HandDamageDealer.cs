using UnityEngine;

public class HandDamageDealer : MonoBehaviour
{
    private float cachedAttackDamage; 
    private bool hasHit = false;
    private Collider handCollider;

    void Start()
    {
        handCollider = GetComponent<Collider>();
        if (handCollider != null)
        {
            handCollider.isTrigger = true;
            handCollider.enabled = false; // Initially inactive
        }
    }

    public void InitializeDamage(float damageFromSO)
    {
        cachedAttackDamage = damageFromSO;
    }

    public void EnableCollider()
    {
        hasHit = false; // Reset hit status for the new swing
        if (handCollider != null) handCollider.enabled = true;
    }

    public void DisableCollider()
    {
        if (handCollider != null) handCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Check if the collided object is the Player
        if (other.CompareTag("Player"))
        {
            hasHit = true;

            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                // Apply the cached damage from the SO
                playerHealth.TakeDamage(cachedAttackDamage);
            }
        }
    }
    private void OnDrawGizmos()
    {
        CapsuleCollider capsule = handCollider != null ? handCollider as CapsuleCollider : GetComponent<CapsuleCollider>();
        if (capsule == null) return;

        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;

        DrawCapsuleGizmo(capsule.center, capsule.radius, capsule.height, capsule.direction);
    }

    private void DrawCapsuleGizmo(Vector3 center, float radius, float height, int direction)
    {
        float halfHeight = height * 0.5f;
        float cylinderHeight = Mathf.Max(0, height - radius * 2);

        Vector3 offset = Vector3.zero;
        if (direction == 0) offset = Vector3.right * cylinderHeight * 0.5f; // X-axis
        else if (direction == 1) offset = Vector3.up * cylinderHeight * 0.5f; // Y-axis
        else offset = Vector3.forward * cylinderHeight * 0.5f; // Z-axis

        // Draw top and bottom spheres
        Gizmos.DrawWireSphere(center + offset, radius);
        Gizmos.DrawWireSphere(center - offset, radius);

        // Draw connecting lines
        Vector3 perp1 = Vector3.zero, perp2 = Vector3.zero;
        if (direction == 0) { perp1 = Vector3.up; perp2 = Vector3.forward; }
        else if (direction == 1) { perp1 = Vector3.right; perp2 = Vector3.forward; }
        else { perp1 = Vector3.right; perp2 = Vector3.up; }

        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 point = perp1 * Mathf.Cos(angle) * radius + perp2 * Mathf.Sin(angle) * radius;
            Gizmos.DrawLine(center + offset + point, center - offset + point);
        }
    }




}