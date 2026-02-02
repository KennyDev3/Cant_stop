/// <summary>
/// Describes a scene transition request. All scene loads go through GameManager.RequestScene(SceneRequest).
/// Use the static factory methods to build requests; GameManager resolves the intent (scene name, collect/clear state).
/// </summary>
public struct SceneRequest
{
    public enum RequestType
    {
        MainMenu,
        Hub,
        NextLevelInSequence,
        SpecificScene,
        RestartCurrentLevel
    }

    public RequestType Type;
    public string SceneName;  // Used when Type == SpecificScene
    public bool PreserveRunState;

    public static SceneRequest ToMainMenu()
    {
        return new SceneRequest { Type = RequestType.MainMenu, PreserveRunState = false };
    }

    public static SceneRequest ToHub(bool preserveRunState = true)
    {
        return new SceneRequest { Type = RequestType.Hub, PreserveRunState = preserveRunState };
    }

    /// <summary>Load the next level in the configured sequence; preserves run state and increments rotation.</summary>
    public static SceneRequest ToNextLevelInSequence()
    {
        return new SceneRequest { Type = RequestType.NextLevelInSequence, PreserveRunState = true };
    }

    public static SceneRequest ToScene(string sceneName, bool preserveRunState)
    {
        return new SceneRequest { Type = RequestType.SpecificScene, SceneName = sceneName, PreserveRunState = preserveRunState };
    }

    /// <summary>Reload current scene and clear run state (new run).</summary>
    public static SceneRequest RestartCurrentLevel()
    {
        return new SceneRequest { Type = RequestType.RestartCurrentLevel, PreserveRunState = false };
    }
}
