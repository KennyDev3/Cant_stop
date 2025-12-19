using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Lightning Strike Item")]

public class LightningStrikeItemSO : ItemSO
{
    [Header("Visuals")]
    public GameObject lightningVFXPrefab;
    public LayerMask enemyLayer;

    [Header("Stats")]
    public float baseDamage = 30f;
    public float damagePerStack = 15f;

    [Tooltip("Radius around the Turret/Impact to search for targets")]
    public float searchRadius = 15.0f;

    [Header("Audio")]
    [SerializeField] SoundDef hitSound;

    public override void ApplyEffect(StatController targetStats, int stackCount)
    {
        LightningStrikeBehavior behavior = targetStats.GetComponent<LightningStrikeBehavior>();

        if (behavior == null)
        {
            behavior = targetStats.gameObject.AddComponent<LightningStrikeBehavior>();
        }

        behavior.UpdateConfiguration(
            stackCount,
            lightningVFXPrefab,
            enemyLayer,
            baseDamage, damagePerStack,
            searchRadius,
            hitSound
        );
    }

}
