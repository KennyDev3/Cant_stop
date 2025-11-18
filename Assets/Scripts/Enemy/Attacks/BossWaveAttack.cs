using UnityEngine;

public class BossWaveAttack : AttackBehaviour
{
    public RangedEnemyData bossEnemyData;

    [Header("Wave Settings")]
    public Transform spawnPoint; // Assign a Transform in front of the boss
    public GameObject preAttackVFXPrefab;


    [Tooltip("If true, wave fires at player. If false, fires boss forward.")]
    public bool aimAtPlayer = true;

    public override void Initialize(EnemyBrain brain)
    {
        base.Initialize(brain);
    }

    public override void PerformAttack(Transform target)
    {
        IsAttacking = true;
        enemyBrain.Animator.SetTrigger("Attack");

        if (target != null)
        {
            enemyBrain.transform.LookAt(target.position);
            Vector3 euler = enemyBrain.transform.rotation.eulerAngles;
            enemyBrain.transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }
    }

    public void AnimationEvent_PreAttackBlink()
    {
        Debug.Log("Enemy is about to fire!");

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
        Transform target = enemyBrain.Target;

        if (spawnPoint != null && bossEnemyData.projectilePrefab != null && target != null)
        {
            Vector3 targetDirection = target.position - spawnPoint.position;
            targetDirection.y = 0; // No Y rotation to keep projectile perallel to the ground 
            targetDirection = targetDirection.normalized;

            Quaternion projectileRotation = Quaternion.LookRotation(targetDirection);
            Quaternion zOffset = Quaternion.Euler(0, 0, -90f);
            Quaternion finalRotation = projectileRotation * zOffset;

            GameObject projectileGO = Instantiate(
                bossEnemyData.projectilePrefab,
                spawnPoint.position,
                finalRotation
            );

            ShockwaveProjectile projectile = projectileGO.GetComponent<ShockwaveProjectile>();

            if (projectile != null)
            {
                projectile.Initialize(bossEnemyData.attackDamage, bossEnemyData.projectileSpeed);
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
