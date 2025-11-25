using System.Collections.Generic;
using UnityEngine;

public class EnemyPooler : MonoBehaviour
{
    public static EnemyPooler Instance { get; private set; }

    // Dictionary maps Data (SO) -> Queue of Objects
    private Dictionary<EnemyData, Queue<GameObject>> poolDictionary = new Dictionary<EnemyData, Queue<GameObject>>();

    void Awake()
    {
        Instance = this;
    }


    public GameObject GetEnemy(EnemyData data, Vector3 position, Quaternion rotation)
    {
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
                Debug.LogError($"Enemy Data {data.name} has no Prefab assigned!");
                return null;
            }
            enemyObj = Instantiate(data.prefab, transform);
        }

        enemyObj.transform.position = position;
        enemyObj.transform.rotation = rotation;

        EnemyHealth health = enemyObj.GetComponentInChildren<EnemyHealth>();
        if (health != null)
        {
            health.Initialize(data); // <-- Inject the Data
        }

        enemyObj.SetActive(true);
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