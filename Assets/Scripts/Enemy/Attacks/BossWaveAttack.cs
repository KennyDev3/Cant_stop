using UnityEngine;

public class BossWaveAttack : AttackBehaviour
{
    private RangedEnemyData _bossData; // Injected

    [Header("Wave Settings")]
    public Transform spawnPoint;
    public GameObject preAttackVFXPrefab;
    public bool aimAtPlayer = true;

    public override void Initialize(EnemyBrain brain, EnemyData data)
    {
        base.Initialize(brain, data);

        if (data is RangedEnemyData castedData)
        {
            _bossData = castedData;
        }
        else
        {
            Debug.LogError($"BossWaveAttack on {gameObject.name} requires RangedEnemyData!");
        }
    }

    public override void PerformAttack(Transform target)
    {
        IsAttacking = true;
        enemyBrain.Animator.SetTrigger("Attack");

        if (target != null && aimAtPlayer)
        {
            enemyBrain.transform.LookAt(target.position);
            Vector3 euler = enemyBrain.transform.rotation.eulerAngles;
            enemyBrain.transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }
    }

    public void AnimationEvent_PreAttackBlink()
    {
        if (preAttackVFXPrefab != null)
        {
            Instantiate(
                preAttackVFXPrefab,
                spawnPoint != null ? spawnPoint.position : transform.position,
                Quaternion.identity
            );
        }
    }

    public void AnimationEvent_SpawnWave()
    {
        if (_bossData == null || spawnPoint == null) return;

        Transform target = enemyBrain.Target;

        if (_bossData.projectilePrefab != null && target != null)
        {
            Vector3 targetDirection = target.position - spawnPoint.position;
            targetDirection.y = 0;
            targetDirection = targetDirection.normalized;

            Quaternion projectileRotation = Quaternion.LookRotation(targetDirection);
            Quaternion zOffset = Quaternion.Euler(0, 0, -90f);
            Quaternion finalRotation = projectileRotation * zOffset;

            GameObject projectileGO = Instantiate(
                _bossData.projectilePrefab,
                spawnPoint.position,
                finalRotation
            );

            ShockwaveProjectile projectile = projectileGO.GetComponent<ShockwaveProjectile>();

            if (projectile != null)
            {
                projectile.Initialize(_bossData.attackDamage, _bossData.projectileSpeed);
            }
        }
    }

    public override void AnimationEvent_EndAttack()
    {
        IsAttacking = false;
        enemyBrain.Animator.SetTrigger("AttackFinished");
        enemyBrain.ResetAttackTimer();
        enemyBrain.OnAttackFinished();
        enemyBrain.ResumeMovement();
    }

    public override void AnimationEvent_StartAttack() { }
}