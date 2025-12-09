using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Enemy/Movement/Direct Rush")]
public class DirectRushPattern : EnemyMovementPattern
{
    public float stopDistance = 1.0f;

    public override void CalculateMovement(NavMeshAgent agent, EnemyBrain brain, Transform target, EnemyData data)
    {
        if (target == null) return;

        agent.stoppingDistance = stopDistance;
        agent.SetDestination(target.position);
    }
}