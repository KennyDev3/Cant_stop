using System.Collections;
using UnityEngine;

public class GroundAOEAttack : AttackBehaviour
{
    private FlameAOEEnemyData _flameData; // Injected

    [Header("Audio")]
    [SerializeField] private SoundDef fireAOESpellCastChantSound;


    [Header("AOE Settings")]
    public int burstCount = 3;
    public float timeBetweenBursts = 0.5f;

    [Header("Targeting")]
    public float minRadiusFromPlayer = 2.0f;
    public float maxRadiusFromPlayer = 5.0f;


    public override void Initialize(EnemyBrain brain, EnemyData data)
    {
        base.Initialize(brain, data);

        if (data is FlameAOEEnemyData castedData)
        {
            _flameData = castedData;
        }
        else
        {
            Debug.LogError($"GroundAOEAttack on {gameObject.name} requires FlameAOEEnemyData!");
        }
    }

    public override void PerformAttack(Transform target)
    {
        IsAttacking = true;
        enemyBrain.Animator.SetTrigger("Attack");

        SoundManager.Instance.Play(fireAOESpellCastChantSound, transform.position);
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
        if (_flameData == null) return;

        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(minRadiusFromPlayer, maxRadiusFromPlayer);
        Vector3 spawnOffset = new Vector3(randomDirection.x, 0, randomDirection.y) * randomDistance;
        Vector3 spawnPos = centerPoint + spawnOffset;

        if (_flameData.flamePrefab != null)
        {
            GameObject flamesGO = Instantiate(_flameData.flamePrefab, spawnPos, Quaternion.identity);
            FlameArea flameArea = flamesGO.GetComponent<FlameArea>();

            if (flameArea != null)
            {
                float damageToDeal = enemyBrain.GetScaledDamage(_flameData.attackDamage);
                flameArea.Initialize(damageToDeal, _flameData.tickRate, _flameData.lifeTime);
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