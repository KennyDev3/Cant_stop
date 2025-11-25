using UnityEngine;

public class RangeAttack : AttackBehaviour
{
    private RangedEnemyData _rangedData; // Injected

    [Tooltip("The transform where the projectile will be instantiated.")]
    public Transform projectileSpawnPoint;
    public GameObject preAttackVFXPrefab;

    public override void Initialize(EnemyBrain brain, EnemyData data)
    {
        base.Initialize(brain, data);

        if (data is RangedEnemyData castedData)
        {
            _rangedData = castedData;
        }
        else
        {
            Debug.LogError($"RangeAttack on {gameObject.name} requires RangedEnemyData!");
        }
    }

    public override void PerformAttack(Transform target)
    {
        IsAttacking = true;
        enemyBrain.Animator.SetTrigger("Attack");
    }

    public void AnimationEvent_FireProjectile()
    {
        if (_rangedData == null || projectileSpawnPoint == null) return;

        Transform target = enemyBrain.Target;

        if (_rangedData.projectilePrefab != null && target != null)
        {
            Vector3 targetDirection = target.position - projectileSpawnPoint.position;
            targetDirection.y = 0;
            targetDirection = targetDirection.normalized;

            Quaternion projectileRotation = Quaternion.LookRotation(targetDirection);

            GameObject projectileGO = Instantiate(
                _rangedData.projectilePrefab,
                projectileSpawnPoint.position,
                projectileRotation
            );

            Projectile projectile = projectileGO.GetComponent<Projectile>();

            if (projectile != null)
            {
                projectile.Initialize(_rangedData.attackDamage, _rangedData.projectileSpeed);
            }
        }
    }

    public void AnimationEvent_PreAttackBlink()
    {
        if (preAttackVFXPrefab != null)
        {
            Instantiate(
                preAttackVFXPrefab,
                projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position,
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
}