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
}
