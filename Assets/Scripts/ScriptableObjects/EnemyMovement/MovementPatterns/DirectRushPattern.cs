using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Enemy/Movement/Direct Rush")]
public class DirectRushPattern : EnemyMovementPattern
{
    public float stopDistance = 1.0f;

    public override void CalculateMovement(NavMeshAgent agent, EnemyBrain brain, Transform target, EnemyData data)
    {
        if (agent == null || target == null) return;

        agent.stoppingDistance = stopDistance;

        Vector3 desired = target.position;

        float side = ((brain.GetInstanceID() % 2) == 0) ? 1f : -1f;
        Vector3 dirToTarget = (target.position - agent.transform.position).normalized;
        Vector3 sideDir = Vector3.Cross(dirToTarget, Vector3.up).normalized;
        desired += sideDir * 0.5f * side;

        desired += AvoidOtherEnemies(agent.transform.position, radius: 1.3f, strength: 1.8f);

        desired += MicroJitter(brain.GetInstanceID(), magnitude: 0.08f, speed: 2.2f);

        agent.SetDestination(GetValidNavMeshPosition(desired));
    }
}
