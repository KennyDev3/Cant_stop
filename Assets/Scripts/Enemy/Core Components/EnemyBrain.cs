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

    private float loseTargetTimer = 0f;
    private const float LOSE_TARGET_DELAY = 1.2f;

    private float reactionTimer;
    private float nextReactionTime;

    private float idOffset;

    private bool shouldReact;

    public int InstanceID => GetInstanceID();

    void Awake()
    {
        targeting = GetComponent<EnemyTargeting>();
        movement = GetComponent<EnemyMovement>();
        attackBehaviour = GetComponent<AttackBehaviour>();
        Animator = GetComponentInChildren<Animator>();
        _health = GetComponent<EnemyHealth>();
    }

    public void Initialize(EnemyData data)
    {
        _data = data;

        targeting.Initialize(this);
        movement.Initialize(this);
        attackBehaviour.Initialize(this, _data);

        isPlayerPriorityEnemy = Random.value < playerPriorityChance;
        currentState = State.Idle;

        attackTimer = Random.Range(_data.attackCooldown * 0.5f, _data.attackCooldown * 1.2f);

        visionCheckTimer = 0f;
        loseTargetTimer = 0f;

        idOffset = Random.Range(0f, 999f);

        nextReactionTime = Random.Range(_data.reactionIntervalMin, _data.reactionIntervalMax);
        reactionTimer = 0f;
    }

    void Update()
    {
        if (_data == null) return;

        if (Animator != null)
            Animator.SetFloat("Speed", movement.GetCurrentSpeed());

        attackTimer += Time.deltaTime;
        visionCheckTimer += Time.deltaTime;

        reactionTimer += Time.deltaTime;
        shouldReact = reactionTimer >= nextReactionTime;

        if (shouldReact)
        {
            reactionTimer = 0f;
            nextReactionTime = Random.Range(_data.reactionIntervalMin, _data.reactionIntervalMax);

            if (targeting.CurrentTarget == null)
                targeting.FindClosestTarget();
        }

        if (visionCheckTimer >= VISION_CHECK_COOLDOWN)
        {
            visionCheckTimer = 0f;
            if (targeting.CurrentTarget == null)
                targeting.FindClosestTarget();
        }

        switch (currentState)
        {
            case State.Idle:
                if (targeting.CurrentTarget != null)
                    currentState = State.Chasing;
                break;

            case State.Chasing:
                HandleChasing();
                break;

            case State.Attacking:
                HandleAttacking();
                break;
        }
    }

    void HandleChasing()
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
            loseTargetTimer += Time.deltaTime;
            if (loseTargetTimer >= LOSE_TARGET_DELAY)
            {
                targeting.ClearTarget();
                currentState = State.Idle;
                movement.Stop();
                loseTargetTimer = 0f;
                return;
            }
        }
        else loseTargetTimer = 0f;

        bool ready = attackTimer >= _data.attackCooldown;

        if (distance <= _data.attackRange && ready)
        {
            currentState = State.Attacking;
            movement.Stop();
            return;
        }

        if (distance <= _data.attackRange)
        {
            Vector3 retreat = (transform.position - targeting.CurrentTarget.position).normalized;
            float noise = Mathf.PerlinNoise(Time.time * _data.erraticFrequency + idOffset, idOffset) - 0.5f;
            Vector3 perp = Vector3.Cross(Vector3.up, retreat);
            retreat += perp * noise * _data.erraticIntensity;
            retreat.Normalize();
            movement.MoveTo(transform.position + retreat * 2.5f);
            return;
        }

        if (_data.movementPattern != null)
        {
            if (shouldReact)
                movement.ExecutePattern(targeting.CurrentTarget);
        }
        else
        {
            movement.MoveTo(targeting.CurrentTarget.position);
        }
    }

    void HandleAttacking()
    {
        if (targeting.CurrentTarget == null)
        {
            currentState = State.Idle;
            return;
        }

        Vector3 look = targeting.CurrentTarget.position;
        look.y = transform.position.y;
        transform.LookAt(look);

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
        movement.Stop();
    }

    public void ResumeMovement()
    {
        if (targeting.CurrentTarget != null)
            movement.MoveTo(targeting.CurrentTarget.position);
    }

    public float GetScaledDamage(float baseDamage)
    {
        if (_data == null) return 0f;
        float mult = _health != null ? _health.RuntimeDamageMultiplier : 1f;
        return baseDamage * mult;
    }

    public void ResetAttackTimer()
    {
        attackTimer = 0f;
    }
}
