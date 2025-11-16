using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;


public class EnemyController : MonoBehaviour
{
    public HandDamageDealer damageDealer;
    public EnemyData enemyData;
    private NavMeshAgent agent;
    private Transform target;
    public bool isPlayerPriorityEnemy;
    public float playerPriorityChance = 0.666f;

    private Transform playerTarget;
    private Transform[] truckSideTargets; //  array for truck targets

    private Animator animator;

    // State Machine
    private enum State { Idle, Chasing, Attacking }
    private State currentState;
    private float attackTimer;

    private float visionCheckTimer;
    private const float VISION_CHECK_COOLDOWN = 1f;

  void Start()
{
    isPlayerPriorityEnemy = Random.value < playerPriorityChance;

    agent = GetComponent<NavMeshAgent>();
    agent.speed = enemyData.moveSpeed;

    animator = GetComponentInChildren<Animator>();

    if (damageDealer != null)
    {
        damageDealer.InitializeDamage(enemyData.attackDamage);
    }


    GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
    if (playerGO != null) playerTarget = playerGO.transform;
    
    // Find the parent truck object
    GameObject truckGO = GameObject.FindGameObjectWithTag("Truck");
    if (truckGO != null)
    {
        // Find the specific child targets (created in the editor)
        Transform left = truckGO.transform.Find("AttackTarget_Left");
        Transform right = truckGO.transform.Find("AttackTarget_Right");

        // Store them in the array if found
        if (left != null && right != null)
        {
            truckSideTargets = new Transform[] { left, right };
        }
    }

    currentState = State.Idle;
}

    void Update()
    {
        // We use magnitude (length) of the velocity vector.
        if (animator != null)
        {
            float speed = agent.velocity.magnitude;
            animator.SetFloat("Speed", speed);
        }

        // Don't check timers or states if no potential targets exist
        if (playerTarget == null && truckSideTargets == null) return;

        // Timer for attack cooldown
        attackTimer += Time.deltaTime;
        // Timer for vision checks
        visionCheckTimer += Time.deltaTime;
        if (visionCheckTimer >= VISION_CHECK_COOLDOWN)
        {
            visionCheckTimer = 0f;
            target = FindClosestTargetInVision();
            
            // If we found a target but are Idle, start Chasing
            if (target != null && currentState == State.Idle)
            {
                currentState = State.Chasing;
            }
        }

        switch (currentState)
        {
            case State.Idle:
                HandleIdle();
                break;
            case State.Chasing:
                HandleChasing();
                break;
            case State.Attacking:
                HandleAttacking();
                break;
        }
    }

    private void HandleIdle()
    {
        // Only perform the expensive vision check periodically
        if (visionCheckTimer >= VISION_CHECK_COOLDOWN)
        {
            visionCheckTimer = 0f;
            target = FindClosestTargetInVision();

            if (target != null)
            {
                // Target found, transition to Chasing
                currentState = State.Chasing;
            }
        }
    }

    private Transform FindClosestTargetInVision()
{
    Transform closest = null;
    float shortestDistance = enemyData.visionRange;
    Vector3 myPosition = transform.position;

    // 1. Check Player (Player is a single point)
    if (playerTarget != null)
    {
        float distToPlayer = Vector3.Distance(myPosition, playerTarget.position);
        if (distToPlayer < shortestDistance)
        {
            shortestDistance = distToPlayer;
            closest = playerTarget;
        }
    }

        // 2. Check Truck Side Targets (We check the closest of the two sides)
        if (!isPlayerPriorityEnemy && truckSideTargets != null)
        {
        foreach (Transform sideTarget in truckSideTargets)
        {
            float distToSide = Vector3.Distance(myPosition, sideTarget.position);
            
            // Note: We use the existing 'shortestDistance' to compare against the closest target found so far
            if (distToSide < shortestDistance) 
            {
                shortestDistance = distToSide;
                closest = sideTarget;
            }
        }
    }

    return closest;
}




    private void HandleChasing()
    {
        if (target == null)
        {
            // Target was destroyed or nullified, return to Idle
            currentState = State.Idle;
            return;
        }

        if (agent.speed == 0f)
        {
            agent.isStopped = false; 
            agent.speed = enemyData.moveSpeed; 
        }

        // Tell the NavMeshAgent to find a path to the target
        agent.SetDestination(target.position);

        float distance = Vector3.Distance(transform.position, target.position);

        // Check if target has moved out of vision, return to Idle if so
        if (distance > enemyData.visionRange)
        {
            target = null;
            currentState = State.Idle;
            return;
        }

        // Check if we are in range to attack
        if (distance <= enemyData.attackRange)
        {
            currentState = State.Attacking;
        }
    }

    private void HandleAttacking()
    {
        if (target == null)
        {
            currentState = State.Idle;
            return;
        }

        Vector3 lookPos = target.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        if (attackTimer >= enemyData.attackCooldown)
        {
            attackTimer = 0f;

            StartCoroutine(PerformAttack());
        }

       
    }

    private IEnumerator PerformAttack()
    {
        if (animator != null)
        {
           
            animator.SetTrigger("Attack");
        }

        yield break; 
    }

    public void PlayHitAnimation()
    {
        if (animator != null)
            animator.SetTrigger("GetHit");
    }

    public void HandleDeath()
    {
        this.enabled = false;

        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    public void AnimationEvent_StartAttack()
    {
        // 1. Freeze the agent at the start of the attack animation
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.speed = 0f;
        }

        if (damageDealer != null)
        {
            damageDealer.EnableCollider();
        }

        Debug.Log("Event: Attack Started and movement frozen.");
    }

    public void AnimationEvent_EndAttack()
    {
        if (damageDealer != null)
        {
            damageDealer.DisableCollider();
        }

        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = enemyData.moveSpeed;
        }

        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > enemyData.attackRange)
            {
                currentState = State.Chasing;
            }
        }
    }




}
