using UnityEngine;

[CreateAssetMenu(fileName = "NewRangedEnemyData", menuName = "Enemy/Ranged Enemy Data")]
public class RangedEnemyData : EnemyData
{
    [Header("Ranged Combat Specifics")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 20f;
}