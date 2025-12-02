using UnityEngine;

public class EnemyDebugBootstrap : MonoBehaviour
{
    [Header("Debug Configuration")]
    [Tooltip("If true, this enemy will self-initialize on Start if not already initialized by the Pooler.")]
    [SerializeField] private bool enableDebug = true;

    [Header("Data Injection")]
    [Tooltip("Drag the specific EnemyData (e.g., BombThrowerData) here.")]
    [SerializeField] private EnemyData debugData;

    [Header("Stat Simulation")]
    [SerializeField] private float debugHpMult = 1f;
    [SerializeField] private float debugDmgMult = 1f;

    private EnemyHealth _health;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        if (!enableDebug) return;

        if (_health == null)
        {
            Debug.LogError($"[EnemyDebug] No EnemyHealth found on {gameObject.name}");
            return;
        }
        
        if (_health.Data != null)
        {
            
            return;
        }

        if (debugData == null)
        {
            Debug.LogError($"[EnemyDebug] Debug Mode enabled on {gameObject.name} but 'Debug Data' is empty!");
            return;
        }

        // 5. Inject dependencies manually
        Debug.LogWarning($"[EnemyDebug] Force Initializing {gameObject.name} with {debugData.name}");
        _health.Initialize(debugData, debugHpMult, debugDmgMult);
    }
}
