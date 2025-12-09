using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Enemy/Movement/Ranged Smart")]
public class RangedSmartPattern : EnemyMovementPattern
{
    public float idealDistance = 10f;
    public float strafeFrequency = 2f;
    public float strafeAmplitude = 3f;

    public override void CalculateMovement(NavMeshAgent agent, EnemyBrain brain, Transform target, EnemyData data)
    {
        float distanceToTarget = Vector3.Distance(agent.transform.position, target.position);
        Vector3 dirToTarget = (target.position - agent.transform.position).normalized;

        Vector3 desiredPos;

        if (distanceToTarget < idealDistance * 0.5f)
        {
            desiredPos = agent.transform.position - dirToTarget * 2f;
        }
        else if (distanceToTarget > idealDistance)
        {
            desiredPos = target.position;
        }
        else
        {
            Vector3 sideDir = Vector3.Cross(dirToTarget, Vector3.up);
            float strafeVal = Mathf.Sin(Time.time * strafeFrequency) * strafeAmplitude;

            desiredPos = agent.transform.position + (sideDir * strafeVal) + (dirToTarget * 0.5f); 
        }

        agent.SetDestination(GetValidNavMeshPosition(desiredPos));
    }
}