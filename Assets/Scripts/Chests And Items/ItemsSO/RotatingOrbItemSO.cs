using UnityEngine;

[CreateAssetMenu(fileName = "New Rotating Orb Item", menuName = "Game/Items/Rotating Orb")]

public class RotatingOrbItemSO : ItemSO
{
    [Header("Orb Specifics")]
    [Tooltip("The actual sphere/fireball that rotates around the player")]
    public GameObject orbProjectilePrefab;

    [Header("Orb Specifics")]
    public float baseDamage = 10f;
    public float damageScaling = 0.2f; // Damage Increase Per Stack 20% 
    public float orbitRadius = 3f;
    public float rotationSpeed = 100f; // Degrees per second
    public LayerMask enemyLayer;

    public override void ApplyEffect(StatController targetStats, int stackCount)
    {
        OrbController controller = targetStats.GetComponent<OrbController>();
        if (controller == null) controller = targetStats.gameObject.AddComponent<OrbController>();

        controller.UpdateConfiguration(
            stackCount,
            orbProjectilePrefab,
            baseDamage,
            damageScaling,
            orbitRadius,
            rotationSpeed,
            enemyLayer
        );
    }

}
