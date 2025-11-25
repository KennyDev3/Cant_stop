using System.Collections.Generic;
using UnityEngine;

public class EnemyDirector : MonoBehaviour
{
    public float spawnInterval = 4f;
    public Transform[] spawnPoints;
    public List<EnemyData> availableEnemies;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            AttemptSpawn();
            timer = 0f;
        }
    }

    private void AttemptSpawn()
    {
        if (availableEnemies.Count == 0 || spawnPoints.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        EnemyData selectedEnemy = availableEnemies[Random.Range(0, availableEnemies.Count)];

        EnemyPooler.Instance.GetEnemy(selectedEnemy, spawnPoint.position, spawnPoint.rotation);
    }
}