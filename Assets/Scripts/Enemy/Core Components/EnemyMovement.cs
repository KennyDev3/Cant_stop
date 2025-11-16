using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    private EnemyBrain enemyBrain;

    private float cachedSpeed;


    public void Initialize(EnemyBrain brain)
    {
        this.enemyBrain = brain;
        navMeshAgent = GetComponent<NavMeshAgent>();

        this.cachedSpeed = enemyBrain.enemyData.moveSpeed;
        navMeshAgent.speed = this.cachedSpeed;
    }

    public void MoveTo(Vector3 destination)
    {
        navMeshAgent.speed = this.cachedSpeed;


        if (navMeshAgent.isStopped)
        {
            navMeshAgent.isStopped = false;
        }
        navMeshAgent.SetDestination(destination);
    }

    public void Stop()
    {
        if (!navMeshAgent.isStopped)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.speed = 0;
        }
    }

    public float GetCurrentSpeed()
    {
        return navMeshAgent.velocity.magnitude;
    }
}