/// <summary>
/// Constants for hub upgrade IDs. Must match the id field on HubUpgradeSO assets.
/// </summary>
public static class HubUpgradeKeys
{
    // Parry tree
    public const string ParryUnlock = "parry_unlock";
    public const string ParryTurretBuff = "parry_turret_buff";
    public const string ParryReturnDamage = "parry_return_damage";

    // Dash tree
    public const string DashUnlock = "dash_unlock";
    public const string DashTurretAttackSpeed = "dash_turret_attack_speed";
    public const string DashFiretrail = "dash_firetrail";

    // Passive tree – Movement Speed (3 levels)
    public const string PassiveMoveSpeed1 = "passive_movespeed_1";
    public const string PassiveMoveSpeed2 = "passive_movespeed_2";
    public const string PassiveMoveSpeed3 = "passive_movespeed_3";

    // Passive tree – Health Regeneration (3 levels)
    public const string PassiveHealthRegen1 = "passive_healthregen_1";
    public const string PassiveHealthRegen2 = "passive_healthregen_2";
    public const string PassiveHealthRegen3 = "passive_healthregen_3";

    // Passive tree – AOE Pickup Range (3 levels)
    public const string PassivePickupRange1 = "passive_pickuprange_1";
    public const string PassivePickupRange2 = "passive_pickuprange_2";
    public const string PassivePickupRange3 = "passive_pickuprange_3";

    // Meta tree – Capacity Limit (3 levels)
    public const string MetaCapacity1 = "meta_capacity_1";
    public const string MetaCapacity2 = "meta_capacity_2";
    public const string MetaCapacity3 = "meta_capacity_3";

    // Meta tree – Resource Value Increase (3 levels)
    public const string MetaResourceValue1 = "meta_resource_value_1";
    public const string MetaResourceValue2 = "meta_resource_value_2";
    public const string MetaResourceValue3 = "meta_resource_value_3";

    // Meta tree – Double Item Drop Chance (3 levels)
    public const string MetaDoubleDrop1 = "meta_double_drop_1";
    public const string MetaDoubleDrop2 = "meta_double_drop_2";
    public const string MetaDoubleDrop3 = "meta_double_drop_3";
}
