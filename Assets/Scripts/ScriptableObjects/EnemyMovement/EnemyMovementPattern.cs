using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyMovementPattern : ScriptableObject
{
    [TextArea] public string description;

    protected static int EnemyLayer;

    protected virtual void OnEnable()
    {
        EnemyLayer = LayerMask.NameToLayer("Enemy");
    }

    public abstract void CalculateMovement(
        NavMeshAgent agent,
        EnemyBrain brain,
        Transform target,
        EnemyData data
    );

    protected Vector3 GetValidNavMeshPosition(Vector3 targetPos, float maxDistance = 2f)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, maxDistance, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return targetPos;
    }

   // Anti Clamping
    protected Vector3 AvoidOtherEnemies(Vector3 agentPos, float radius = 1.5f, float strength = 1.5f)
    {
        Collider[] cols = Physics.OverlapSphere(agentPos, radius);
        Vector3 push = Vector3.zero;
        int count = 0;

        foreach (var c in cols)
        {
            if (c.transform == null) continue;
            if (c.gameObject.layer != EnemyLayer) continue;


            if (Vector3.Distance(c.transform.position, agentPos) < 0.01f) continue;

            Vector3 away = (agentPos - c.transform.position);
            if (away.sqrMagnitude > 0.0001f)
            {
                push += away.normalized / Mathf.Max(0.1f, away.magnitude); 
                count++;
            }
        }

        if (count > 0)
        {
            push /= count;
            return push.normalized * strength;
        }

        return Vector3.zero;
    }

   
    protected Vector3 MicroJitter(int seed, float magnitude = 0.25f, float speed = 1.8f)
    {
        float t = Time.time * speed + (seed % 1000);
        return new Vector3(Mathf.Sin(t * 0.9f), 0f, Mathf.Cos(t * 1.1f)) * magnitude;
    }
}
