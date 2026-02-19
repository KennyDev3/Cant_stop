using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Hub upgrade tree type. One panel can show multiple trees (e.g. Parry and Dash).
/// </summary>
public enum HubUpgradeTree
{
    Parry,
    Dash,
    // Passive,
    // Meta,
}

/// <summary>
/// Defines a single hub upgrade: id, cost in resources, tree, order, and prerequisite.
/// Create assets via Assets > Create > Game > Hub Upgrade.
/// </summary>
[CreateAssetMenu(fileName = "NewHubUpgrade", menuName = "Game/Hub Upgrade", order = 2)]
public class HubUpgradeSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique id used in code and for prerequisites. Must match HubUpgradeKeys constants.")]
    public string id;

    [Header("Display")]
    public string displayName = "New Upgrade";
    [TextArea(2, 4)]
    public string description = "";

    [Header("Tree")]
    public HubUpgradeTree upgradeTree = HubUpgradeTree.Parry;
    [Tooltip("Order in the tree: 0 = Unlock, 1 = first upgrade, 2 = second.")]
    [Min(0)]
    public int orderInTree = 0;
    [Tooltip("Id of the upgrade that must be purchased first. Leave empty for the first node (Unlock).")]
    public string prerequisiteUpgradeId = "";

    [Header("Cost")]
    [Tooltip("Cost per resource. Default 50/50/50 for Wood/Gold/Iron; fully configurable per upgrade.")]
    public List<ResourceCostEntry> cost = new List<ResourceCostEntry>();

    [System.Serializable]
    public struct ResourceCostEntry
    {
        public ResourceSO resource;
        [Min(0)]
        public int amount;
    }

    /// <summary>
    /// True if this upgrade has no prerequisite (e.g. the Unlock node).
    /// </summary>
    public bool HasNoPrerequisite => string.IsNullOrEmpty(prerequisiteUpgradeId);
}
