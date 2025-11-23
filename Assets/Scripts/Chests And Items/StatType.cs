using UnityEngine;

public enum StatType
{
    MaxHealth,
    MoveSpeed,
    SprintSpeed,
    DamageMultiplier,
    Damage,
    FireRate,
    AreaMultiplier,
    GlobalDamageMultiplier, // Apllies extra damage to ALL items
    DashDuration,


    // Add more here later
}

public enum ItemRarity
{
    Common,
    Rare,
    Legendary
}

public enum ItemTarget
{
    Player,
    Turret,
    Both
}

