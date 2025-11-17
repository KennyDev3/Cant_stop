using UnityEngine;

public class BombThrowerAttack : AttackBehaviour
{

    [Tooltip("The Scriptable Object holding data for this bomb-throwing enemy.")]
    public BombThrowerEnemyData bombThrowerData;

    [Tooltip("The transform where the bomb will be instantiated (e.g., the enemy's hand).")]
    public Transform launchPoint;

    [Tooltip("A VFX to play before the bomb is thrown to warn the player.")]
    public GameObject preAttackVFXPrefab;

    public override void Initialize(EnemyBrain brain)
    {
        base.Initialize(brain);
    }
    public override void PerformAttack(Transform target)
    {
        IsAttacking = true;
        // This triggers the throwing animation, which will then call our animation events.
        enemyBrain.Animator.SetTrigger("Attack");
    }

    public void AnimationEvent_LobBomb()
    {
        Transform target = enemyBrain.Target;

        if (launchPoint != null && bombThrowerData.bombPrefab != null && target != null)
        {
            // --- Instantiate and get components ---
            GameObject bombInstance = Instantiate(
                bombThrowerData.bombPrefab,
                launchPoint.position,
                Quaternion.identity // Bombs typically don't need to face the target when thrown
            );

            BombController bombController = bombInstance.GetComponent<BombController>();
            Rigidbody bombRb = bombInstance.GetComponent<Rigidbody>();

            // --- Initialize the bomb with damage ---
            if (bombController != null)
            {
                bombController.Initialize(bombThrowerData.attackDamage);
            }

            // --- Calculate and apply the lob velocity ---
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
        float angleRad = bombThrowerData.launchAngle * Mathf.Deg2Rad;

        Vector3 displacementXZ = new Vector3(targetPosition.x - startPosition.x, 0, targetPosition.z - startPosition.z);
        float distance = displacementXZ.magnitude;

        float velocity = Mathf.Sqrt(Mathf.Abs((distance * gravity) / Mathf.Sin(2 * angleRad)));

        if (float.IsNaN(velocity))
        {
            velocity = 5f; 
            Debug.LogWarning("Could not calculate a valid launch velocity. Using fallback.", this);
        }

        Vector3 launchDirection = (displacementXZ.normalized + Vector3.up * Mathf.Tan(angleRad)).normalized;
        return launchDirection * velocity;
    }


}
