using UnityEngine;

[CreateAssetMenu(fileName = "RangedEnemyData", menuName = "Enemy/New Ranged Enemy Data")]
public class RangedEnemyData : EnemyData
{
    [Header("RangedEnemy Specifics")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
}