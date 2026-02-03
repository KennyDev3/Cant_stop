using UnityEngine;

/// <summary>
/// Defines a resource type. Used for run resources (pickups in Worlds) and hub bank.
/// Create assets via Assets > Create > Game > Resource.
/// </summary>
[CreateAssetMenu(fileName = "NewResource", menuName = "Game/Resource")]
public class ResourceSO : ScriptableObject
{
    [Header("Display")]
    [Tooltip("Name shown in UI (e.g. hub bench, run HUD).")]
    public string displayName;

    [Tooltip("Icon shown next to the count in run UI and hub.")]
    public Sprite icon;

    [Header("Pickup")]
    [Tooltip("Default amount granted per pickup. Can be overridden on the resource prefab.")]
    [Min(1)]
    public int amountPerPickup = 1;
}
