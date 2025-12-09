using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    private EnemyData _data;
    private EnemyHealth _health;
    public EnemyData Data => _data;

    public float playerPriorityChance = 0.666f;
    public bool isPlayerPriorityEnemy;

    private EnemyTargeting targeting;
    public Transform Target => targeting.CurrentTarget;

    private EnemyMovement movement;
    private AttackBehaviour attackBehaviour;
    public Animator Animator { get; private set; }

    private enum State { Idle, Chasing, Attacking }
    private State currentState;

    private float attackTimer;
    public bool IsAttackReady => attackTimer >= _data.attackCooldown;

    private float visionCheckTimer;
    private const float VISION_CHECK_COOLDOWN = 0.5f;

    void Awake()
    {
        targeting = GetComponent<EnemyTargeting>();
        movement = GetComponent<EnemyMovement>();
        attackBehaviour = GetComponent<AttackBehaviour>();
        Animator = GetComponentInChildren<Animator>();
        _health = GetComponent<EnemyHealth>();

    }

    // Called by EnemyHealth -> Which is called by Pooler
    public void Initialize(EnemyData data)
    {
        _data = data;

        if (targeting != null) targeting.Initialize(this);
        if (movement != null) movement.Initialize(this);
        if (attackBehaviour != null) attackBehaviour.Initialize(this, _data);

        // Reset Logic
        isPlayerPriorityEnemy = Random.value < playerPriorityChance;
        currentState = State.Idle;
        attackTimer = _data.attackCooldown;
    }

    public float GetScaledDamage(float baseDamage)
    {
        if (_data == null) return 0f;
        float multiplier = (_health != null) ? _health.RuntimeDamageMultiplier : 1f;

        return baseDamage * multiplier;
    }

    void Update()
    {
        // SAFETY CHECK: If Data hasn't been injected yet, do nothing.
        if (_data == null) return;

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

        if (distance > _data.visionRange)
        {
            targeting.ClearTarget();
            currentState = State.Idle;
            movement.Stop();
            return;
        }

        
        bool isCooldownReady = attackTimer >= _data.attackCooldown;

        if (distance <= _data.attackRange && isCooldownReady)
        {
            currentState = State.Attacking;
            movement.Stop(); 
            return;
        }

        if (_data.movementPattern != null)
        {
            movement.ExecutePattern(targeting.CurrentTarget);
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
            if (attackTimer >= _data.attackCooldown)
            {
                attackBehaviour.PerformAttack(targeting.CurrentTarget);
                attackTimer = 0f; 
            }
            else
            {
                currentState = State.Chasing;
            }
        }
    }

    public void OnAttackFinished()
    {
        currentState = State.Chasing;

    }

    public void PlayHitAnimation()
    {
        if (Animator != null) Animator.SetTrigger("GetHit");
    }

    public void HandleDeath()
    {
        this.enabled = false;
        if (Animator != null) Animator.enabled = false;
    }

    public void StopMovement()
    {
        if (movement != null) movement.Stop();
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