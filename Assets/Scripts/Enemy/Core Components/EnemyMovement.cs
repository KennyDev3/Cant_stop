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

        if (enemyBrain != null && enemyBrain.Data != null)
        {
            this.cachedSpeed = enemyBrain.Data.moveSpeed;
            if (navMeshAgent != null)
                navMeshAgent.speed = this.cachedSpeed;
        }
        else
        {
            Debug.LogError("EnemyMovement initialized but Brain has no Data!");
        }
    }

    public void MoveTo(Vector3 destination)
    {
        if (navMeshAgent == null) return;

        navMeshAgent.speed = this.cachedSpeed;

        if (navMeshAgent.isStopped)
        {
            navMeshAgent.isStopped = false;
        }
        navMeshAgent.SetDestination(destination);
    }

    public void ExecutePattern(Transform target)
    {
        if (enemyBrain == null || enemyBrain.Data == null) return;

        navMeshAgent.speed = this.cachedSpeed;
        if (navMeshAgent.isStopped) navMeshAgent.isStopped = false;

        enemyBrain.Data.movementPattern.CalculateMovement(
            navMeshAgent,
            enemyBrain,
            target,
            enemyBrain.Data
        );
    }

    public void Stop()
    {
        if (navMeshAgent == null) return;

        if (!navMeshAgent.isStopped)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.speed = 0f;
        }
    }

    public float GetCurrentSpeed()
    {
        if (navMeshAgent != null)
            return navMeshAgent.velocity.magnitude;
        return 0f;
    }
}
