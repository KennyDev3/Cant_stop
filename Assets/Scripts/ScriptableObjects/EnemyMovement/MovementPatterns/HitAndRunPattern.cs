using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu(menuName = "Enemy/Movement/Hit And Run")]
public class HitAndRunPattern : EnemyMovementPattern
{
    [Header("Distances")]
    [Tooltip("If player gets closer than this (even if we are attacking), back up immediately.")]
    public float panicDistance = 4f;

    [Tooltip("When fleeing (on cooldown), run until we reach this distance.")]
    public float safeDistance = 12f;

    [Header("Speed Settings")]
    [Tooltip("Multiplier applied ONLY when fleeing or backing up.")]
    public float fleeSpeedMultiplier = 1.5f;

    public override void CalculateMovement(NavMeshAgent agent, EnemyBrain brain, Transform target, EnemyData data)
    {
        if (target == null) return;

        float distanceToTarget = Vector3.Distance(agent.transform.position, target.position);

        bool isAttackReady = brain.IsAttackReady;

        
        if (distanceToTarget < panicDistance)
        {
            agent.updateRotation = true; 
            SetSpeed(agent, data.moveSpeed * fleeSpeedMultiplier);
            MoveAwayFromTarget(agent, target, safeDistance);
        }
        
        else if (!isAttackReady)
        {
            if (distanceToTarget < safeDistance)
            {
                agent.updateRotation = true; 
                SetSpeed(agent, data.moveSpeed * fleeSpeedMultiplier);
                MoveAwayFromTarget(agent, target, safeDistance);
            }
            
            else
            {
                SetSpeed(agent, data.moveSpeed);
                agent.ResetPath(); 

                agent.updateRotation = false;
                Vector3 lookPos = target.position;
                lookPos.y = agent.transform.position.y; 
                agent.transform.LookAt(lookPos);
            }
        }
       
        else
        {
            agent.updateRotation = true; 
            SetSpeed(agent, data.moveSpeed);

            if (distanceToTarget > data.attackRange)
            {
                agent.SetDestination(target.position);
            }
            else
            {
                
                agent.ResetPath();
            }
        }
    }

    private void MoveAwayFromTarget(NavMeshAgent agent, Transform target, float desiredDist)
    {
        Vector3 directionAway = (agent.transform.position - target.position).normalized;

        Vector3 fleePos = target.position + (directionAway * desiredDist);

        agent.SetDestination(GetValidNavMeshPosition(fleePos));
    }

    private void SetSpeed(NavMeshAgent agent, float speed)
    {
        if (Mathf.Abs(agent.speed - speed) > 0.1f)
        {
            agent.speed = speed;
        }
    }
}