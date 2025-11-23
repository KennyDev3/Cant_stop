using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Pulsing Aura Item")]
public class PulsingAuraItemSO : ItemSO
{
    [Header("Aura Visuals")]
    public GameObject auraVisualPrefab;
    public LayerMask enemyLayer;
    public float activeDuration = 0.2f; // How long the visual stays ON per pulse

    [Header("Stats & Scaling")]
    public float baseDamage = 10f;
    public float damagePerStack = 5f;

    public float baseRadius = 5f;
    public float radiusPerStack = 1f;

    public float baseInterval = 2.0f;
    public float intervalReductionPerStack = 0.1f;
    public float minInterval = 0.5f;

    public override void ApplyEffect(StatController targetStats, int stackCount)
    {
        PulsingAuraBehavior behavior = targetStats.GetComponent<PulsingAuraBehavior>();

        if (behavior == null)
        {
            behavior = targetStats.gameObject.AddComponent<PulsingAuraBehavior>();
        }

        behavior.UpdateConfiguration(
            stackCount,
            auraVisualPrefab,
            enemyLayer,
            activeDuration,
            baseDamage, damagePerStack,
            baseRadius, radiusPerStack,
            baseInterval, intervalReductionPerStack, minInterval
        );
    }
}