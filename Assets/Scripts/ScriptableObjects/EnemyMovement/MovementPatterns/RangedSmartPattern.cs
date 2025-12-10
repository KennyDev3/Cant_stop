using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Enemy/Movement/Ranged Smart")]
public class RangedSmartPattern : EnemyMovementPattern
{
    public float idealDistance = 10f;
    public float strafeFrequency = 2f;
    public float strafeAmplitude = 3f;

    private float lastFlipTime = 0f;
    private float flipIntervalMin = 3f;
    private float flipIntervalMax = 8f;
    private bool clockwise = true;

    public override void CalculateMovement(NavMeshAgent agent, EnemyBrain brain, Transform target, EnemyData data)
    {
        if (agent == null || brain == null || target == null || data == null) return;

        
        if (lastFlipTime == 0f)
        {
            clockwise = (brain.GetInstanceID() % 2 == 0);
            lastFlipTime = Time.time + Random.Range(0f, 1f);
        }

        if (Time.time - lastFlipTime > Random.Range(flipIntervalMin, flipIntervalMax))
        {
            clockwise = !clockwise;
            lastFlipTime = Time.time;
        }

        float distanceToTarget = Vector3.Distance(agent.transform.position, target.position);
        Vector3 dirToTarget = (target.position - agent.transform.position).normalized;

        Vector3 desiredPos;

        if (distanceToTarget < idealDistance * 0.5f)
        {
            desiredPos = agent.transform.position - dirToTarget * 2f;
        }
        else if (distanceToTarget > idealDistance * 1.1f)
        {
            desiredPos = target.position;
        }
        else
        {
            Vector3 sideDir = Vector3.Cross(dirToTarget, Vector3.up).normalized;
            float sign = clockwise ? 1f : -1f;

            float strafeVal = Mathf.Sin(Time.time * strafeFrequency) * strafeAmplitude * sign;

            desiredPos = agent.transform.position + (sideDir * strafeVal) + (dirToTarget * 0.5f);
        }

        float flankSign = ((brain.GetInstanceID() % 2) == 0) ? 1f : -1f;
        desiredPos += Vector3.Cross((target.position - agent.transform.position).normalized, Vector3.up) * 0.6f * flankSign;

        desiredPos += AvoidOtherEnemies(agent.transform.position, radius: 1.8f, strength: 1.6f);

        desiredPos += MicroJitter(brain.GetInstanceID(), magnitude: 0.12f, speed: 2.0f);

        agent.SetDestination(GetValidNavMeshPosition(desiredPos));
    }
}
