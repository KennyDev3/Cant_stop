using UnityEngine;

public class BombThrowerAttack : AttackBehaviour
{
    private BombThrowerEnemyData _bombData; // Injected

    [Tooltip("The transform where the bomb will be instantiated.")]
    public Transform launchPoint;
    public GameObject preAttackVFXPrefab;

    public override void Initialize(EnemyBrain brain, EnemyData data)
    {
        base.Initialize(brain, data);

        if (data is BombThrowerEnemyData castedData)
        {
            _bombData = castedData;
        }
        else
        {
            Debug.LogError($"BombThrowerAttack on {gameObject.name} requires BombThrowerEnemyData!");
        }
    }

    public override void PerformAttack(Transform target)
    {
        IsAttacking = true;
        enemyBrain.Animator.SetTrigger("Attack");
    }

    public void AnimationEvent_LobBomb()
    {
        if (_bombData == null || launchPoint == null) return;

        Transform target = enemyBrain.Target;
        if (target == null) return;

        if (_bombData.bombPrefab != null)
        {
            GameObject bombInstance = Instantiate(
                _bombData.bombPrefab,
                launchPoint.position,
                Quaternion.identity
            );

            BombController bombController = bombInstance.GetComponent<BombController>();
            Rigidbody bombRb = bombInstance.GetComponent<Rigidbody>();

            if (bombController != null)
            {
                bombController.Initialize(_bombData.attackDamage);
            }

            if (bombRb != null)
            {
                Vector3 launchVelocity = CalculateLaunchVelocity(target);
                bombRb.linearVelocity = launchVelocity;
            }
            else
            {
                Debug.LogError("The Bomb Prefab is missing a Rigidbody component!", this);
            }
        }
    }

    public void AnimationEvent_PreAttackBlink()
    {
        if (preAttackVFXPrefab != null)
        {
            Instantiate(
                preAttackVFXPrefab,
                launchPoint != null ? launchPoint.position : transform.position,
                Quaternion.identity
            );
        }
    }

    public override void AnimationEvent_StartAttack()
    {
        enemyBrain.StopMovement();
    }

    public override void AnimationEvent_EndAttack()
    {
        IsAttacking = false;
        enemyBrain.Animator.SetTrigger("AttackFinished");
        enemyBrain.ResetAttackTimer();
        enemyBrain.OnAttackFinished();
        enemyBrain.ResumeMovement();
    }

    private Vector3 CalculateLaunchVelocity(Transform target)
    {
        Vector3 startPosition = launchPoint.position;
        Vector3 targetPosition = target.position;
        float gravity = Physics.gravity.y;
        float angleRad = _bombData.launchAngle * Mathf.Deg2Rad;

        Vector3 displacementXZ = new Vector3(targetPosition.x - startPosition.x, 0, targetPosition.z - startPosition.z);
        float distance = displacementXZ.magnitude;

        float velocity = Mathf.Sqrt(Mathf.Abs((distance * gravity) / Mathf.Sin(2 * angleRad)));

        if (float.IsNaN(velocity))
        {
            velocity = 5f;
        }

        Vector3 launchDirection = (displacementXZ.normalized + Vector3.up * Mathf.Tan(angleRad)).normalized;
        return launchDirection * velocity;
    }
}