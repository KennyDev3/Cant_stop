using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyMovementPattern : ScriptableObject
{
    [TextArea] public string description;

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
}