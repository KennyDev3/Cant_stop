// MeleeAttack.cs
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class MeleeAttack : AttackBehaviour
{
    [Tooltip("The collider on the hand/weapon that deals damage.")]
    public Collider damageCollider;
    private float cachedAttackDamage;
    private bool hasHit;

    void Start()
    {

        if (damageCollider != null)
        {
            damageCollider.isTrigger = true;
            damageCollider.enabled = false;
        }

        HitboxForwarder hitbox = damageCollider.GetComponent<HitboxForwarder>();
        if (hitbox == null)
        {
            hitbox = damageCollider.gameObject.AddComponent<HitboxForwarder>();
        }

        hitbox.Initialize(this);

    }

    public override void Initialize(EnemyBrain brain)
    {
        base.Initialize(brain);
        cachedAttackDamage = enemyBrain.enemyData.attackDamage;
    }

    public override void PerformAttack(Transform target)
    {
        IsAttacking = true;

        enemyBrain.Animator.SetTrigger("Attack");
    }

    public override void AnimationEvent_StartAttack()
    {
        enemyBrain.StopMovement();

        hasHit = false;
        if (damageCollider != null) damageCollider.enabled = true;
        Debug.Log("MeleeAttack Event: Damage collider enabled.");
    }

    public override void AnimationEvent_EndAttack()
    {

        if (damageCollider != null) damageCollider.enabled = false;
        IsAttacking = false;

        enemyBrain.ResetAttackTimer();

        enemyBrain.OnAttackFinished();
        enemyBrain.ResumeMovement();

        Debug.Log("MeleeAttack Event: Damage collider disabled.");
    }

    public void ReportHit(Collider other)
    {
        if (hasHit || !damageCollider.enabled) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("HIT REGISTERED ON PLAYER!"); // Add a debug log to be sure
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                hasHit = true;
                playerHealth.TakeDamage(cachedAttackDamage);
            }
        }

        // Can check for collisions with other things too
    }

    private void OnDrawGizmosSelected()
    {
        if (damageCollider == null) return;

        Gizmos.color = Color.red;
        Gizmos.matrix = damageCollider.transform.localToWorldMatrix;

        if (damageCollider is BoxCollider boxCollider)
        {
            // Draw a wireframe cube matching the BoxCollider's center and size
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else if (damageCollider is SphereCollider sphereCollider)
        {
            // Draw a wireframe sphere matching the SphereCollider's center and radius
            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
        }
        else if (damageCollider is CapsuleCollider capsuleCollider)
        {
            // Use the helper function to draw a detailed capsule
            DrawCapsuleGizmo(capsuleCollider);
        }
    }

    private void DrawCapsuleGizmo(CapsuleCollider capsule)
    {
        Vector3 center = capsule.center;
        float radius = capsule.radius;
        float height = capsule.height;
        int direction = capsule.direction; // 0=X, 1=Y, 2=Z

        float halfHeight = Mathf.Max(radius, height / 2f); // Ensure halfHeight is at least the radius
        Vector3 p1, p2;

        switch (direction)
        {
            case 0: // X-axis
                p1 = center + Vector3.right * (halfHeight - radius);
                p2 = center - Vector3.right * (halfHeight - radius);
                break;
            case 2: // Z-axis
                p1 = center + Vector3.forward * (halfHeight - radius);
                p2 = center - Vector3.forward * (halfHeight - radius);
                break;
            default: // Y-axis (default)
                p1 = center + Vector3.up * (halfHeight - radius);
                p2 = center - Vector3.up * (halfHeight - radius);
                break;
        }

        // Draw the two spheres at the ends
        Gizmos.DrawWireSphere(p1, radius);
        Gizmos.DrawWireSphere(p2, radius);

        // Draw the connecting lines
        Vector3 right = (direction == 1 || direction == 0) ? Vector3.forward : Vector3.up;
        Vector3 up = (direction == 2) ? Vector3.up : Vector3.right;

        Gizmos.DrawLine(p1 + right * radius, p2 + right * radius);
        Gizmos.DrawLine(p1 - right * radius, p2 - right * radius);
        Gizmos.DrawLine(p1 + up * radius, p2 + up * radius);
        Gizmos.DrawLine(p1 - up * radius, p2 - up * radius);
    }
}