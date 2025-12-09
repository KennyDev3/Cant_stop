
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data")]

public class EnemyData : ScriptableObject
{
    public GameObject prefab;
    public enum TargetType { Player, Truck }

    [Header("AI Behavior")]
    public EnemyMovementPattern movementPattern;

    [Header("Director Settings")]
    [Tooltip("Credit cost to spawn this unit")]
    public float spawnCost = 10f;

    [Tooltip("Chance to pick this unit if we can afford it")]
    public float selectionWeight = 1f;


    [Header("Behavior")]
    public bool onlyAttackPlayer = false;
    public float visionRange = 20f;
    
     [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 1.5f; 

    [Header("Combat")]
    public float attackDamage = 10f;
    public float attackRange = 1.5f; // Melee range
    public float attackCooldown = 2f; // Time between 

    [Header("Blood Splatter Particle FX")]
    public GameObject bloodVFX;

    [Header("Loot")]
    public GarbageData garbageDataOnDeath; 





}

