using UnityEngine;

public class GroundPoundAttack : AttackBehaviour
{
    [Header("Ground Pound Settings")]
    public float attackRadius = 3f;
    public GameObject attackIndicatorPrefab;

    private GameObject activeIndicator;
    private bool hasDealtDamage;
    private float _damage;

    public override void Initialize(EnemyBrain brain, EnemyData data)
    {
        base.Initialize(brain, data);
        _damage = data.attackDamage; // Generic damage
    }

    public override void PerformAttack(Transform target)
    {
        IsAttacking = true;
        enemyBrain.Animator.SetTrigger("Attack");
    }

    public override void AnimationEvent_StartAttack()
    {
        enemyBrain.StopMovement();
        hasDealtDamage = false;
    }

    public void AnimationEvent_DealDamage()
    {
        if (hasDealtDamage) return;

        if (attackIndicatorPrefab != null)
        {
            activeIndicator = Instantiate(attackIndicatorPrefab, enemyBrain.transform.position, Quaternion.identity);
            activeIndicator.transform.localScale = new Vector3(attackRadius * 2, 0.1f, attackRadius * 2);
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, attackRadius);

        foreach (Collider hit in colliders)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(_damage);
                }
                break;
            }
        }

        hasDealtDamage = true;
    }

    public void AnimationEvent_DestroyDamageIndicator()
    {
        if (activeIndicator != null)
        {
            Destroy(activeIndicator);
        }
    }

    public override void AnimationEvent_EndAttack()
    {
        enemyBrain.Animator.SetTrigger("AttackFinished");
        IsAttacking = false;
        enemyBrain.ResetAttackTimer();
        enemyBrain.OnAttackFinished();
        enemyBrain.ResumeMovement();
    }
}