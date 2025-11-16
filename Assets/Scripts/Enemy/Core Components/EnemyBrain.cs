using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    public EnemyData enemyData;
    public bool isPlayerPriorityEnemy;
    public float playerPriorityChance = 0.666f;

    private EnemyTargeting targeting;
    public Transform Target => targeting.CurrentTarget;

    private EnemyMovement movement;
    private AttackBehaviour attackBehaviour;
    public Animator Animator { get; private set; }

    //State Machine
    private enum State { Idle, Chasing, Attacking }
    private State currentState;

    //  Timers 
    private float attackTimer;
    private float visionCheckTimer;
    private const float VISION_CHECK_COOLDOWN = 0.5f;

    void Awake()
    {
        targeting = GetComponent<EnemyTargeting>();
        movement = GetComponent<EnemyMovement>();
        attackBehaviour = GetComponent<AttackBehaviour>();
        Animator = GetComponentInChildren<Animator>();

        targeting.Initialize(this);
        movement.Initialize(this);
        attackBehaviour.Initialize(this);
    }

    void Start()
    {
        isPlayerPriorityEnemy = Random.value < playerPriorityChance;
        currentState = State.Idle;
        attackTimer = enemyData.attackCooldown;

    }

    void Update()
    {
        Animator.SetFloat("Speed", movement.GetCurrentSpeed());

        
        attackTimer += Time.deltaTime;

        visionCheckTimer += Time.deltaTime;
        if (visionCheckTimer >= VISION_CHECK_COOLDOWN)
        {
            visionCheckTimer = 0f;
            if (targeting.CurrentTarget == null)
            {
                targeting.FindClosestTarget();
            }
        }

        switch (currentState)
        {
            case State.Idle:
                HandleIdleState();
                break;
            case State.Chasing:
                HandleChasingState();
                break;
            case State.Attacking:
                HandleAttackingState();
                break;
        }
    }

    private void HandleIdleState()
    {
        if (targeting.CurrentTarget != null)
        {
            currentState = State.Chasing;
        }
    }

    private void HandleChasingState()
    {
        if (targeting.CurrentTarget == null)
        {
            currentState = State.Idle;
            movement.Stop();
            return;
        }

        float distance = Vector3.Distance(transform.position, targeting.CurrentTarget.position);

        if (distance > enemyData.visionRange)
        {
            targeting.ClearTarget();
            currentState = State.Idle;
            movement.Stop();
            return;
        }

        if (distance <= enemyData.attackRange)
        {
            currentState = State.Attacking;
        }
        else
        {
            movement.MoveTo(targeting.CurrentTarget.position);
        }
    }

    private void HandleAttackingState()
    {
        if (targeting.CurrentTarget == null)
        {
            currentState = State.Idle;
            return;
        }

        Vector3 lookPos = targeting.CurrentTarget.position;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);


        if (!attackBehaviour.IsAttacking)
        {
            if (attackTimer >= enemyData.attackCooldown)
            {
                attackBehaviour.PerformAttack(targeting.CurrentTarget);
            }

            float distance = Vector3.Distance(transform.position, targeting.CurrentTarget.position);
            float disengageRange = enemyData.attackRange;
            if (distance > disengageRange)
            {
                currentState = State.Chasing;
            }
        }
    }

    public void OnAttackFinished()
    {
        if (targeting.CurrentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, targeting.CurrentTarget.position);
            if (distance > enemyData.attackRange)
            {
                currentState = State.Chasing;
            }
        }
    }
    public void PlayHitAnimation()
    {
        if (Animator != null)
            Animator.SetTrigger("GetHit");
    }

    public void HandleDeath()
    {
        this.enabled = false;

        if (Animator != null)
        {
            Animator.enabled = false;
        }
    }

    public void StopMovement()
    {
        if (movement != null)
        {
            movement.Stop();
        }
    }

    public void ResumeMovement()
    {
        if (movement != null && targeting.CurrentTarget != null)
        {
            movement.MoveTo(targeting.CurrentTarget.position);
        }
    }

    public void ResetAttackTimer()
    {
        attackTimer = 0f;
    }
}