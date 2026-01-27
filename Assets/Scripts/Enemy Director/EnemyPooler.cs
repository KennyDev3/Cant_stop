using System.Collections.Generic;
using UnityEngine;

public class EnemyPooler : MonoBehaviour
{
    public static EnemyPooler Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private Dictionary<EnemyData, Queue<GameObject>> poolDictionary = new Dictionary<EnemyData, Queue<GameObject>>();

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public GameObject GetEnemy(EnemyData data, Vector3 position, Quaternion rotation, float hpMult, float dmgMult)
    {
        if (data == null)
        {
            if (debugMode) Debug.LogError("? [Pooler] Requested spawn with NULL EnemyData!");
            return null;
        }

        if (!poolDictionary.ContainsKey(data))
        {
            poolDictionary[data] = new Queue<GameObject>();
        }

        GameObject enemyObj;

        if (poolDictionary[data].Count > 0)
        {
            enemyObj = poolDictionary[data].Dequeue();
        }
        else
        {
            if (data.prefab == null)
            {
                if (debugMode) Debug.LogError($"? [Pooler] EnemyData '{data.name}' has no Prefab assigned in the Inspector!");
                return null;
            }
            enemyObj = Instantiate(data.prefab, transform);
        }

        enemyObj.transform.position = position;
        enemyObj.transform.rotation = rotation;

        // Initialize Data
        EnemyHealth health = enemyObj.GetComponentInChildren<EnemyHealth>();
        if (health != null)
        {
            // passing the multipliers
            health.Initialize(data, hpMult, dmgMult);
        }
        else if (debugMode)
        {
            Debug.LogWarning($"?? [Pooler] Spawned {enemyObj.name} but it has no EnemyHealth component!");
        }

        enemyObj.SetActive(true);

        EnemySpawnRise spawn = enemyObj.GetComponentInChildren<EnemySpawnRise>();
        if (spawn != null)
        {
            spawn.PlaySpawn(position);
        }

        return enemyObj;

    }

    public void ReturnEnemyToPool(EnemyData data, GameObject enemy)
    {
        enemy.SetActive(false);
        enemy.transform.SetParent(transform);

        if (!poolDictionary.ContainsKey(data))
        {
            poolDictionary[data] = new Queue<GameObject>();
        }

        poolDictionary[data].Enqueue(enemy);
    }
}