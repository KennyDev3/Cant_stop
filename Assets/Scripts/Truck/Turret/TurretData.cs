using UnityEngine;

[CreateAssetMenu(fileName = "NewTurretData", menuName = "Combat/Turret Data")]
public class TurretData : ScriptableObject
{
    [Header("Targeting and Range")]
    public float targetRange = 50f;     // Large range for detection
    public float rotationSpeed = 10f;    // Speed for turret "lock-on" rotation

    [Header("Combat Stats")]
    public float damage = 100f;          // Damage per shot
    public float fireRate = 0.5f;       // Time between shots (e.g., 0.5s = 2 shots/sec)
    public LayerMask enemyLayer;         // Layer to check for enemies (Crucial for performance)

    [Header("Visual/Audio Delays (Placeholder)")]
    // Used to align bullet effects/sound with hitscan timing
    public float muzzleFlashDuration = 0.1f; 
}
