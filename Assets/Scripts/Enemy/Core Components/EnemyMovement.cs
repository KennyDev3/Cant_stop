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

        // SAFETY: Check if Data exists before accessing properties
        if (enemyBrain.Data != null)
        {
            this.cachedSpeed = enemyBrain.Data.moveSpeed;
            navMeshAgent.speed = this.cachedSpeed;
        }
        else
        {
            Debug.LogError("EnemyMovement initialized but Brain has no Data!");
        }
    }

    public void MoveTo(Vector3 destination)
    {
        // Safety check to prevent NREs if init failed
        if (navMeshAgent == null) return;

        navMeshAgent.speed = this.cachedSpeed;

        if (navMeshAgent.isStopped)
        {
            navMeshAgent.isStopped = false;
        }
        navMeshAgent.SetDestination(destination);
    }

    public void Stop()
    {
        if (navMeshAgent == null) return;

        if (!navMeshAgent.isStopped)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.speed = 0;
        }
    }

    public float GetCurrentSpeed()
    {
        if (navMeshAgent != null)
            return navMeshAgent.velocity.magnitude;
        return 0f;
    }
}