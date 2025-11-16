using UnityEngine;

public class RangeAttack : AttackBehaviour
{
    [Tooltip("The Scriptable Object holding data for this ranged enemy.")]
    public RangedEnemyData rangedEnemyData;

    [Tooltip("The transform where the projectile will be instantiated.")]
    public Transform projectileSpawnPoint;

    public override void Initialize(EnemyBrain brain)
    {
        base.Initialize(brain);
    }

    public override void PerformAttack(Transform target)
    {
        IsAttacking = true;
        enemyBrain.Animator.SetTrigger("Attack");
    }

    public void AnimationEvent_FireProjectile()
    {
        Transform target = enemyBrain.Target;

        if (projectileSpawnPoint != null && rangedEnemyData.projectilePrefab != null && target != null)
        {
            Vector3 targetDirection = target.position - projectileSpawnPoint.position;
            targetDirection.y = 0; // No Y rotation to keep projectile perallel to the ground 
            targetDirection = targetDirection.normalized;

            Quaternion projectileRotation = Quaternion.LookRotation(targetDirection);

            GameObject projectileGO = Instantiate(
                rangedEnemyData.projectilePrefab,
                projectileSpawnPoint.position,
                projectileRotation
            );

            Projectile projectile = projectileGO.GetComponent<Projectile>();

            if (projectile != null)
            {
                projectile.Initialize(rangedEnemyData.attackDamage, rangedEnemyData.projectileSpeed);
            }
        }
    }

    // This can be called by an Animation Event to signal a pre-attack warning
    public void AnimationEvent_PreAttackBlink()
    {
        // Implement your red blink logic here.
        // For example, you could have a script on the enemy to flash a material color.
        Debug.Log("Enemy is about to fire!");
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