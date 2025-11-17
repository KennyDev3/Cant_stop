using UnityEngine;

[CreateAssetMenu(fileName = "NewBombThrowerEnemyData", menuName = "Enemy/Bomb Thrower Enemy Data")]

public class BombThrowerEnemyData : EnemyData
{
    [Header("Bomb Prefab")]
    public GameObject bombPrefab;

    [Header("Lob Physics")]
    [Tooltip("The angle in degrees to launch the bomb. 45 is optimal for distance.")]
    [Range(10f, 85f)]
    public float launchAngle = 45f;
}
