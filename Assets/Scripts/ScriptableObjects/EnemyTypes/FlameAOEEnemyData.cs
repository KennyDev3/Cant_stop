using UnityEngine;

[CreateAssetMenu(fileName = "FlameAOEEnemyData", menuName = "Enemy/New FlameAOE Enemy Data")]

public class FlameAOEEnemyData : EnemyData
{

    [Header("Flame AOE Specifics")]

    public GameObject flamePrefab;
    public float tickRate = 0.5f;
    public float lifeTime = 3f;
}
