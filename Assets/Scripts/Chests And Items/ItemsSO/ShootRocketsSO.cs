using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/ShootsRockets")]
public class ShootRocketsSO : ItemSO
{
    [Header("Prefabs")]
    [Tooltip("The Rocket Prefab containing ProjectileController")]
    public GameObject rocketPrefab;
    public LayerMask enemyLayer;

    [Header("Proc Chance")]
    [Range(0f, 1f)] public float baseProcChance = 0.45f; // Default 33%
    [Range(0f, 1f)] public float procChancePerStack = 0.1f; // Set to 0 if you don't want scaling

    [Header("Stats")]
    public float baseDamage = 200f;
    public float damagePerStack = 200;

    [Header("Explosion Area")]
    public float baseRadius = 4.0f;
    public float radiusPerStack = 1.0f;

    [Header("Flight Settings")]
    public float rocketSpeed = 40f;

    public override void ApplyEffect(StatController targetStats, int stackCount)
    {
        RocketLauncherBehavior behavior = targetStats.GetComponent<RocketLauncherBehavior>();

        if (behavior == null)
        {
            behavior = targetStats.gameObject.AddComponent<RocketLauncherBehavior>();
        }

        behavior.UpdateConfiguration(
            stackCount,
            rocketPrefab,
            enemyLayer,
            baseProcChance, procChancePerStack, 
            baseDamage, damagePerStack,         
            baseRadius, radiusPerStack,         
            rocketSpeed                        
        );
    }
}
