using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemySpawnRise : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject portalPrefab;
    public float riseDuration = 2f;
    public float riseHeight = 1.6f;

    private EnemyBrain brain;
    private EnemyHealth health;
    private NavMeshAgent agent;
    private Collider col;

    private bool isSpawning = false;

    void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        health = GetComponent<EnemyHealth>();
        agent = GetComponent<NavMeshAgent>();
        col = GetComponent<Collider>();
    }

    public void PlaySpawn(Vector3 pos)
    {
        StartCoroutine(SpawnRoutine(pos));
    }

    private IEnumerator SpawnRoutine(Vector3 pos)
    {
        isSpawning = true;

        GameObject portal = Instantiate(
            portalPrefab,
            pos,
            Quaternion.Euler(-90, 0, 0)   
        );

        if (brain) brain.enabled = false;
        if (health) health.enabled = false;
        if (agent)
        {
            agent.enabled = false;
        }
        if (col) col.enabled = false;

        Vector3 startPos = pos + Vector3.down * riseHeight;
        Vector3 endPos = pos;

        transform.position = startPos;

        float t = 0;
        while (t < riseDuration)
        {
            t += Time.deltaTime;
            float pct = t / riseDuration;

            transform.position = Vector3.Lerp(startPos, endPos, pct);

            yield return null;
        }

        transform.position = endPos;

        if (agent) agent.enabled = true;
        if (col) col.enabled = true;
        if (brain) brain.enabled = true;
        if (health) health.enabled = true;

        isSpawning = false;

    }
}
