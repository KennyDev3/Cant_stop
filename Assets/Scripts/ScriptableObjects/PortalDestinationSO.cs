using UnityEngine;

/// <summary>
/// Defines where a portal sends the player. Used by Portal to get the SceneRequest without hardcoding destination logic.
/// Create assets via Assets > Create > Portal > Portal Destination.
/// </summary>
[CreateAssetMenu(fileName = "NewPortalDestination", menuName = "Portal/Portal Destination")]
public class PortalDestinationSO : ScriptableObject
{
    public enum DestinationType
    {
        NextLevelInSequence,
        Hub,
        SpecificScene
    }

    [Header("Destination")]
    [Tooltip("What scene transition this portal triggers.")]
    [SerializeField] private DestinationType destinationType = DestinationType.Hub;

    [Tooltip("Only used when Destination Type is Specific Scene (e.g. secret level).")]
    [SerializeField] private string sceneName = "";

    [Header("Prefab")]
    [Tooltip("Portal prefab to spawn for this destination (must have Portal component).")]
    [SerializeField] private GameObject prefab;

    [Header("Display (optional)")]
    [Tooltip("Label text shown on the portal (e.g. 'Continue Run', 'Return to Hub'). Leave empty to keep prefab default.")]
    [SerializeField] private string labelText = "";

    [Tooltip("Whether to preserve run state (health, inventory, etc.) when transitioning. Usually true for Hub and Next Level.")]
    [SerializeField] private bool preserveRunState = true;

    /// <summary>Portal prefab for this destination. LevelObjectiveManager spawns this when using this SO.</summary>
    public GameObject Prefab => prefab;

    /// <summary>Label text for the portal UI. Empty means use prefab default.</summary>
    public string LabelText => labelText;

    /// <summary>Returns the scene request for GameManager.RequestScene.</summary>
    public SceneRequest GetRequest()
    {
        switch (destinationType)
        {
            case DestinationType.NextLevelInSequence:
                return SceneRequest.ToNextLevelInSequence();
            case DestinationType.Hub:
                return SceneRequest.ToHub(preserveRunState);
            case DestinationType.SpecificScene:
                return SceneRequest.ToScene(sceneName, preserveRunState);
            default:
                return SceneRequest.ToHub(preserveRunState);
        }
    }
}
