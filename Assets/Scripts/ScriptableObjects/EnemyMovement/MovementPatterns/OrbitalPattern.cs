using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Enemy/Movement/Orbital Circle")]
public class OrbitalPattern : EnemyMovementPattern
{
    public float orbitRadius = 5f;
    public float rotationSpeed = 30f; // Degrees per second


    public override void CalculateMovement(NavMeshAgent agent, EnemyBrain brain, Transform target, EnemyData data)
    {
        Vector3 offset = agent.transform.position - target.position;

        float currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        float targetAngle = currentAngle + (rotationSpeed * Time.deltaTime);

        float rad = targetAngle * Mathf.Deg2Rad;
        Vector3 newPos = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * orbitRadius;

        Vector3 finalPos = target.position + newPos;

        agent.SetDestination(GetValidNavMeshPosition(finalPos));
    }
}