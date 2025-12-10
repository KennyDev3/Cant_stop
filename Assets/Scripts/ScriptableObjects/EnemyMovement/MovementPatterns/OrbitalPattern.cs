using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Enemy/Movement/Orbital Circle")]


public class OrbitalPattern : EnemyMovementPattern
{
    public float orbitRadius = 5f;
    public float rotationSpeed = 30f; // Degrees per second

    private static Vector3 sharedGroupCenter;
    private static float lastGroupUpdateTime = 0f;

    public override void CalculateMovement(
     NavMeshAgent agent,
     EnemyBrain brain,
     Transform target,
     EnemyData data)
    {
        Vector3 myPos = agent.transform.position;

        
        if (Time.time - lastGroupUpdateTime > 0.2f)
        {
            sharedGroupCenter = Vector3.Lerp(sharedGroupCenter, myPos, 0.05f);
            lastGroupUpdateTime = Time.time;
        }

        
        Vector3 offset = myPos - target.position;

        float currentAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        float targetAngle = currentAngle + (rotationSpeed * Time.deltaTime);

        float rad = targetAngle * Mathf.Deg2Rad;
        Vector3 orbitPosition = new Vector3(Mathf.Cos(rad), 0, Mathf.Sin(rad)) * orbitRadius;
        orbitPosition += target.position;

        Vector3 cohesion = (sharedGroupCenter - myPos) * 0.6f;

        
        Vector3 separation = AvoidOtherEnemies(myPos, 1.2f, 0.8f);

        Vector3 finalTarget = orbitPosition + cohesion + separation;

        agent.SetDestination(GetValidNavMeshPosition(finalTarget));
    }

}