using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Linq;


public class EnemyController : MonoBehaviour
{
    public EnemyData enemyData;
    private NavMeshAgent agent;
    private Transform target;

    private Transform playerTarget;
    private Transform[] truckSideTargets; //  array for truck targets
    // State Machine
    private enum State { Idle, Chasing, Attacking }
    private State currentState;
    private float attackTimer;

    private float visionCheckTimer;
    private const float VISION_CHECK_COOLDOWN = 1f;

  void Start()
{
    agent = GetComponent<NavMeshAgent>();
    agent.speed = enemyData.moveSpeed;

    // --- Find Targets once in Start ---
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

        // You could add simple random wandering here if desired
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
    if (truckSideTargets != null)
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

        // Stop moving to perform the attack
        agent.SetDestination(transform.position);

        // Face the target (only Y-axis rotation)
        Vector3 lookPos = target.position;
        lookPos.y = transform.position.y; // Keep vertical alignment
        transform.LookAt(lookPos);

        float distance = Vector3.Distance(transform.position, target.position);

        if (attackTimer >= enemyData.attackCooldown)
        {
            attackTimer = 0f;
            // This starts the attack "animation" process
            StartCoroutine(PerformAttack());
        }

        // If the target moves out of attack range, go back to chasing
        if (distance > enemyData.attackRange)
        {
            currentState = State.Chasing;
        }
    }
    
   private IEnumerator PerformAttack()
    {
        // --- This is where the attack animation would start ---
        Debug.Log(gameObject.name + " starts attack animation!");

        // Wait for the animation to reach the damage point
        yield return new WaitForSeconds(enemyData.attackAnimationDelay);

        // Re-check: is the target still in attack range after the animation delay?
        if (target != null && Vector3.Distance(transform.position, target.position) <= enemyData.attackRange)
        {
            Debug.Log(gameObject.name + " successfully hit " + target.name + " for " + enemyData.attackDamage + " damage!");

            // *** Dynamic Damage Application ***
            // Use the tag to determine which health script to call
            if (target.CompareTag("Player"))
            {
                PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(enemyData.attackDamage);
                }
            }
            else if (target.CompareTag("Truck"))
            {
                TruckHealth truckHealth = target.parent.GetComponent<TruckHealth>();
                if (truckHealth != null)
                {
                    truckHealth.TakeDamage(enemyData.attackDamage);
                }
            }
            // **********************************
        }
        else
        {
            Debug.Log(gameObject.name + " attacked, but the target moved out of range or was destroyed!");
        }
        // --- Animation would end here ---
    }
    


    
}
