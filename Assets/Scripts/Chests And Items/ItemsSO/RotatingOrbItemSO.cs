using UnityEngine;

[CreateAssetMenu(fileName = "New Rotating Orb Item", menuName = "Game/Items/Rotating Orb")]
public class RotatingOrbItemSO : ItemSO
{
    [Header("Orb Specifics")]
    public GameObject orbProjectilePrefab;
    public float baseDamage = 10f;
    public float damageScaling = 0.2f;
    public float orbitRadius = 3f;
    public float rotationSpeed = 100f;
    public LayerMask enemyLayer;

    [Header("Audio")]
    public SoundDef hitSound;

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
            enemyLayer,
            hitSound 
        );
    }
}