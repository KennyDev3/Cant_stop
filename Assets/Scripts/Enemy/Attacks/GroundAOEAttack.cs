using System.Collections;
using UnityEngine;

public class GroundAOEAttack : AttackBehaviour
{
    [Tooltip("The Scriptable Object holding data for this Flame AOE enemy.")]
    public FlameAOEEnemyData flameAOEEnemyData;

    [Header("AOE Settings")]
    public int burstCount = 3;      
    public float timeBetweenBursts = 0.5f;

    [Header("Targeting (The Donut)")]
    public float minRadiusFromPlayer = 2.0f; // Inner circle 
    public float maxRadiusFromPlayer = 5.0f; // Outer circle 


    public override void Initialize(EnemyBrain brain)
    {
        base.Initialize(brain);
    }

    public override void PerformAttack(Transform target)
    {
        IsAttacking = true;
        enemyBrain.Animator.SetTrigger("Attack");
    }

    public void AnimationEvent_SpawnFlames()
    {
        StartCoroutine(SpawnFlameRoutine());
    }

    private IEnumerator SpawnFlameRoutine()
    {
        Transform target = enemyBrain.Target;

        if (target == null) yield break;

        for (int i = 0; i < burstCount; i++)
        {
            SpawnSingleFlame(target.position);
            yield return new WaitForSeconds(timeBetweenBursts);
        }
    }

    private void SpawnSingleFlame(Vector3 centerPoint)
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minRadiusFromPlayer, maxRadiusFromPlayer);
        Vector3 spawnOffset = new Vector3(randomDirection.x, 0, randomDirection.y) * randomDistance;
        Vector3 spawnPos = centerPoint + spawnOffset;



        if (flameAOEEnemyData.flamePrefab != null)
        {
            GameObject flamesGO = Instantiate(flameAOEEnemyData.flamePrefab, spawnPos, Quaternion.identity);
            FlameArea flameArea = flamesGO.GetComponent<FlameArea>();

            if (flameArea != null)
            {
                flameArea.Initialize(flameAOEEnemyData.attackDamage, flameAOEEnemyData.tickRate, flameAOEEnemyData.lifeTime);
            }
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
