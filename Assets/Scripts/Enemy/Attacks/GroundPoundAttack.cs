using UnityEngine;
using UnityEngine.AI;

public class GroundPoundAttack : AttackBehaviour
{
    [Header("Ground Pound Settings")]
    public float attackRadius = 3f;
    public float jumpForwardDistance = 2f; 
    public float jumpForwardDuration = 1f;

    [Header("Audio")]
    [SerializeField] private SoundDef groundPoundEnemyJumpSound;
    [SerializeField] private SoundDef groundPoundEnemySmashSound;

    [Header("Visuals")]
    public GameObject attackIndicatorPrefab;

    private GameObject activeIndicator;
    private bool hasDealtDamage;

    // Movement Logic Variables
    private bool isLunging;
    private Vector3 lungeStartPosition;
    private Vector3 lungeTargetPosition;
    private float lungeTimer;

    // References
    private NavMeshAgent agent;

    public override void Initialize(EnemyBrain brain, EnemyData data)
    {
        base.Initialize(brain, data);
        agent = brain.GetComponent<NavMeshAgent>();
    }

    public override void PerformAttack(Transform target)
    {
        IsAttacking = true;

        Vector3 lookPos = target.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        enemyBrain.Animator.SetTrigger("Attack");
    }

    private void Update()
    {
        if (isLunging)
        {
            lungeTimer += Time.deltaTime;
            float t = lungeTimer / jumpForwardDuration;

            
            Vector3 nextPos = Vector3.Lerp(lungeStartPosition, lungeTargetPosition, t);

            transform.position = new Vector3(nextPos.x, transform.position.y, nextPos.z);

            if (lungeTimer >= jumpForwardDuration)
            {
                StopLunge();
            }
        }
    }

   
    public void AnimationEvent_StartJumpMovement()
    {
        enemyBrain.StopMovement(); 

        if (agent != null)
        {
            agent.updatePosition = false; 
            agent.updateRotation = false;
        }

        Vector3 forwardDir = transform.forward;
        Vector3 potentialTarget = transform.position + (forwardDir * jumpForwardDistance);

       
        NavMeshHit hit;
        if (NavMesh.Raycast(transform.position, potentialTarget, out hit, NavMesh.AllAreas))
        {
            lungeTargetPosition = hit.position;
        }
        else
        {
            lungeTargetPosition = potentialTarget;
        }

        lungeStartPosition = transform.position;
        lungeTimer = 0f;
        isLunging = true;
        hasDealtDamage = false;
        SoundManager.Instance.Play(groundPoundEnemyJumpSound, transform.position);
    }

    
    
    public void AnimationEvent_StopJumpMovement()
    {
        StopLunge();
    }

    public void AnimationEvent_DealDamage()
    {
        SoundManager.Instance.Play(groundPoundEnemySmashSound, transform.position);

        if (hasDealtDamage) return;

        if (attackIndicatorPrefab != null)
        {
            activeIndicator = Instantiate(attackIndicatorPrefab, enemyBrain.transform.position, Quaternion.identity);
            activeIndicator.transform.localScale = new Vector3(attackRadius * 2, 0.1f, attackRadius * 2);
        }

        // Damage Logic
        Collider[] colliders = Physics.OverlapSphere(transform.position, attackRadius);
        foreach (Collider hit in colliders)
        {
            if (hit.CompareTag("Player"))
            {
                var health = hit.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    float damageToDeal = enemyBrain.GetScaledDamage(baseData.attackDamage);
                    health.TakeDamage(damageToDeal);

                }
            }
        }
        hasDealtDamage = true;
    }

    public override void AnimationEvent_EndAttack()
    {
        AnimationEvent_DestroyDamageIndicator();

        if (agent != null)
        {
            agent.Warp(transform.position);
            agent.updatePosition = true;
            agent.updateRotation = true;
        }

        enemyBrain.Animator.SetTrigger("AttackFinished");
        IsAttacking = false;
        enemyBrain.ResetAttackTimer();
        enemyBrain.OnAttackFinished();
        enemyBrain.ResumeMovement();
    }

    private void StopLunge()
    {
        isLunging = false;
    }

    public void AnimationEvent_DestroyDamageIndicator()
    {
        if (activeIndicator != null) Destroy(activeIndicator);
    }

    // Keep this empty or basic, as we handle start logic in the specific Jump event
    public override void AnimationEvent_StartAttack() { }
}