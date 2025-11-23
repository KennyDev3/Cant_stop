using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Backlash Wave Item")]

public class BacklashWaveItemSO : ItemSO
{
    [Header("Wave Visuals")]
    public GameObject waveProjectilePrefab;
    public LayerMask enemyLayer;

    [Header("Stats & Scaling")]
    public float baseDamage = 20f;
    public float damagePerStack = 10f;

    public float waveSpeed = 15f;
    public float waveDuration = 5f; // Distance = Speed * Duration


    [Tooltip("Time in seconds between each wave layer spawning (e.g. 0.2s)")]
    public float timeBetweenWaves = 0.05f;

    public override void ApplyEffect(StatController targetStats, int stackCount)
    {
        BacklashWaveBehavior behavior = targetStats.GetComponent<BacklashWaveBehavior>();

        if (behavior == null)
        {
            behavior = targetStats.gameObject.AddComponent<BacklashWaveBehavior>();
        }

        behavior.UpdateConfiguration(
            stackCount,
            waveProjectilePrefab,
            enemyLayer,
            baseDamage, damagePerStack,
            waveSpeed, waveDuration,
            timeBetweenWaves
        );
    }



}
