using UnityEngine;

public class GroundPoundAttack : AttackBehaviour
{
    [Header("Ground Pound Settings")]
    [Tooltip("The radius of the ground pound attack.")]
    public float attackRadius = 3f;

    [Tooltip("The visual indicator for the attack area.")]
    public GameObject attackIndicatorPrefab;

   

    private GameObject activeIndicator;
    private bool hasDealtDamage;
    private float attackDamage;


    public override void Initialize(EnemyBrain brain)
    {
        base.Initialize(brain);
        attackDamage = enemyBrain.enemyData.attackDamage;
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
            // Instantiate the indicator and scale it to the attack radius
            activeIndicator = Instantiate(attackIndicatorPrefab, enemyBrain.transform.position, Quaternion.identity);
            activeIndicator.transform.localScale = new Vector3(attackRadius * 2, 0.1f, attackRadius * 2); // Assuming a flat cylinder/quad
        }

        // Find all colliders within the attack radius on the "Player" layer
        Collider[] colliders = Physics.OverlapSphere(transform.position, attackRadius);

        foreach (Collider hit in colliders)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                }

                break;
            }
        }


        

        hasDealtDamage = true;
    }

    public void AnimationEvent_DestroyDamageIndicator()
    {
        // Clean up the indicator

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
