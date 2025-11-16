using UnityEngine;

public class EnemyTargeting : MonoBehaviour
{
    public Transform CurrentTarget { get; private set; }

    private EnemyBrain enemyBrain;
    private Transform playerTarget;
    private Transform[] truckSideTargets;
    private bool isPlayerPriority;

    public void Initialize(EnemyBrain brain)
    {
        this.enemyBrain = brain;
        this.isPlayerPriority = brain.isPlayerPriorityEnemy;

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) playerTarget = playerGO.transform;

        GameObject truckGO = GameObject.FindGameObjectWithTag("Truck");
        if (truckGO != null)
        {
            Transform left = truckGO.transform.Find("AttackTarget_Left");
            Transform right = truckGO.transform.Find("AttackTarget_Right");
            if (left != null && right != null)
            {
                truckSideTargets = new Transform[] { left, right };
            }
        }
    }

    public void FindClosestTarget()
    {
        Transform closest = null;
        float shortestDistance = enemyBrain.enemyData.visionRange;
        Vector3 myPosition = transform.position;

        if (playerTarget != null)
        {
            float distToPlayer = Vector3.Distance(myPosition, playerTarget.position);
            if (distToPlayer < shortestDistance)
            {
                shortestDistance = distToPlayer;
                closest = playerTarget;
            }
        }

        if (!isPlayerPriority && truckSideTargets != null)
        {
            foreach (Transform sideTarget in truckSideTargets)
            {
                float distToSide = Vector3.Distance(myPosition, sideTarget.position);
                if (distToSide < shortestDistance)
                {
                    shortestDistance = distToSide;
                    closest = sideTarget;
                }
            }
        }
        CurrentTarget = closest;
    }

    public void ClearTarget()
    {
        CurrentTarget = null;
    }
}