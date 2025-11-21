using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Stat Boost Item")]

public class StatBoostItemSO : ItemSO
{
    [Header("Stat Settings")]
    public StatType statToBuff;
    [Tooltip("0.1 = 10%, 1 = +1 Flat")]
    public float amountPerStack;
    public bool isMultiplier;

    [Header("Scaling Logic")]
    [Tooltip("If true, scaling will slow down after hitting the threshold.")]
    public bool useSoftCap;

    [Tooltip("At what bonus amount does diminishing returns start? (e.g., 1.0 = 100% bonus)")]
    public float softCapThreshold = 1.0f;

    public override void ApplyEffect(StatController targetStats, int stackCount)
    {

        float finalBonus = 0f;
        float linearTotal = amountPerStack * stackCount;

        if (!useSoftCap)
        {
            // Infinite linear growth
            finalBonus = linearTotal;
        }

        else
        {
            // HYBRID: Linear up to Cap, then Hyperbolic
            if (linearTotal <= softCapThreshold)
            {
                // We haven't hit the cap yet, keep growing linearly
                finalBonus = linearTotal;
            }
            else
            {
                // We exceeded the cap!
                float excessLinear = linearTotal - softCapThreshold;

                // Diminish the excess using the Hyperbolic formula
                
                float diminishedExcess = 1.0f - (1.0f / (1.0f + excessLinear));

                //  Add the Hard Cap + The Diminished Extra
                finalBonus = softCapThreshold + diminishedExcess;
            }
        }

        // If it's a multiplier (e.g., 1.1x speed), we add 1 to the calculated bonus
        float modification = isMultiplier ? (1 + finalBonus) : finalBonus;



        Debug.Log($"Applying Item: {name} | Stack: {stackCount} | Multiplier: {modification} | Target Stat: {statToBuff}");

        targetStats.ModifyStat(statToBuff, modification, isMultiplier);
    }


}
