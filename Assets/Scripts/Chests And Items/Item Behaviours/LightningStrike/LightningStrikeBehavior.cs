using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;


public class LightningStrikeBehavior : MonoBehaviour, IStatReceiver
{

    private int _stackCount;
    private float _baseDamage;
    private float _damagePerStack;
    private float _range;
    private GameObject _lightningPrefab;
    private LayerMask _enemyLayer;

    // Probability Settings
    private float _baseProcChance = 0.3f; 
    private float _procChancePerStack = 0.05f; 
    private float _chainChance = 0.85f;

    private StatController _myStats;
    private TruckTurret _turret; // Reference to the turret

    [Header("Runtime Stats")]
    [SerializeField] private float _finalDamage;

    private SoundDef _hitSound;

    private void Awake()
    {
        _myStats = GetComponent<StatController>();
    }

    private void OnEnable()
    {
        // We subscribe to the CLASS "TurretController", not a specific object.
        TruckTurret.OnTurretShoot += TryTriggerLightning;
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent errors when scene changes or player dies
        TruckTurret.OnTurretShoot -= TryTriggerLightning;
    }

    public void UpdateConfiguration(
      int stacks,
      GameObject prefab,
      LayerMask layer,
      float bDmg, float sDmg, float range, SoundDef sound)
    {
        _stackCount = stacks;
        _lightningPrefab = prefab;
        _enemyLayer = layer;
        _baseDamage = bDmg;
        _damagePerStack = sDmg;
        _range = range;
        _hitSound = sound;

        OnStatsRecalculated();
    }

    private void TryTriggerLightning(Vector3 originPoint, Vector3 unusedTarget)
    {
        float currentChance = _baseProcChance + (_procChancePerStack * (_stackCount - 1));

        if (UnityEngine.Random.value > currentChance) return;

        int maxStrikes = Mathf.Clamp(3 + _stackCount, 3, 10);
        int actualStrikes = 1; //

        for (int i = 1; i < maxStrikes; i++)
        {
            float currentChainChance = _chainChance - (i * 0.02f);
            if (UnityEngine.Random.value <= currentChainChance) actualStrikes++;
            else break;
        }
        StartCoroutine(ProcessLightningStrikes(originPoint, actualStrikes));
    }

    private IEnumerator ProcessLightningStrikes(Vector3 center, int strikeCount)
    {
        Collider[] hits = Physics.OverlapSphere(center, _range, _enemyLayer);
        
        List<EnemyHealth> validEnemies = new List<EnemyHealth>();
        
        // Filter list for valid, alive enemies
        foreach(var hit in hits)
        {
            if(hit.TryGetComponent(out EnemyHealth enemy))
            {
                if(enemy.enabled && !validEnemies.Contains(enemy)) 
                    validEnemies.Add(enemy);
            }
            else 
            {
                var parentEnemy = hit.GetComponent<EnemyHealth>();
                if(parentEnemy != null && parentEnemy.enabled && !validEnemies.Contains(parentEnemy))
                    validEnemies.Add(parentEnemy);
            }
        }

        if (validEnemies.Count == 0) yield break; 

        Queue<EnemyHealth> targetQueue = new Queue<EnemyHealth>();
        int strikesQueued = 0;

        while (strikesQueued < strikeCount)
        {
            validEnemies = validEnemies.OrderBy(x => UnityEngine.Random.value).ToList();

            foreach (var enemy in validEnemies)
            {
                targetQueue.Enqueue(enemy);
                strikesQueued++;
                if (strikesQueued >= strikeCount) break;
            }
        }

        while (targetQueue.Count > 0)
        {
            EnemyHealth target = targetQueue.Dequeue();

            if (target != null && target.enabled)
            {
                SpawnLightning(target);
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.4f));
        }

    }

    private void SpawnLightning(EnemyHealth target)
    {
        if (_lightningPrefab != null)
        {
            Vector3 spawnPos = target.transform.position + new Vector3(0, 0.5f, 0);

            GameObject instance = Instantiate(_lightningPrefab, spawnPos, Quaternion.identity);

            SoundManager.Instance.Play(_hitSound, transform.position);
            Destroy(instance, 2.0f); // Clean up VFX
        }

        target.TakeDamage(_finalDamage);
    }

    public void OnStatsRecalculated()
    {
        if (_myStats == null) return;

        float stackDamage = _baseDamage + (_damagePerStack * (_stackCount - 1));
        float globalMult = _myStats.GetStat(StatType.GlobalDamageMultiplier);
        float damageMult = _myStats.GetStat(StatType.DamageMultiplier);

        if (globalMult == 0) globalMult = 1f;
        if (damageMult == 0) damageMult = 1f;

        _finalDamage = stackDamage * globalMult * damageMult;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _range);
    }





}
