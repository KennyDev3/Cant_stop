using UnityEngine;

public class BossWaveAttack : AttackBehaviour
{
    private RangedEnemyData _bossData; // Injected

    [Header("Wave Settings")]
    public Transform spawnPoint;
    public GameObject preAttackVFXPrefab;
    public bool aimAtPlayer = true;

    [Header("Audio")]
    [SerializeField] private SoundDef shockWaveAttackSound;

    public override void Initialize(EnemyBrain brain, EnemyData data)
    {
        base.Initialize(brain, data);

        if (data is RangedEnemyData castedData)
            _bossData = castedData;
        else
            Debug.LogError($"BossWaveAttack on {gameObject.name} requires RangedEnemyData!");
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
            Vector3 targetDir = target.position - spawnPoint.position;
            targetDir.y = 0;
            targetDir.Normalize();

            // Face the player
            Quaternion faceTarget = Quaternion.LookRotation(targetDir);

            Quaternion rotationOffset = Quaternion.Euler(0, 0, -90f);

            Quaternion finalRot = faceTarget * rotationOffset;

            GameObject projectileGO = Instantiate(
                _bossData.projectilePrefab,
                spawnPoint.position,
                finalRot
            );

            ShockwaveProjectile projectile = projectileGO.GetComponent<ShockwaveProjectile>();

            if (projectile != null)
            {
                float damage = enemyBrain.GetScaledDamage(_bossData.attackDamage);
                projectile.Initialize(damage, _bossData.projectileSpeed);

                SoundManager.Instance.Play(shockWaveAttackSound, transform.position );
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