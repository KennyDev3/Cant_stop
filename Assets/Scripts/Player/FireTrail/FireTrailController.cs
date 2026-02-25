using UnityEngine;
using StarterAssets;

/// <summary>
/// Spawns fire trail nodes at the player's feet while grounded and moving.
/// Only active when hub upgrade DashFiretrail is unlocked, and only for a duration after each dash (resets on new dash).
/// </summary>
public class FireTrailController : MonoBehaviour
{

    private Transform _nodesParent;

    [Header("Prefab")]
    [SerializeField] private GameObject _nodePrefab;

    [Header("Spawn")]
    [SerializeField] private float _spawnInterval = 0.15f;
    [SerializeField] private float _minMoveSpeed = 0.5f;
    [SerializeField] private float _groundOffset = 0.1f;

    [Header("Active window")]
    [Tooltip("Trail only spawns for this many seconds after the last dash. Each new dash resets the timer.")]
    [SerializeField] private float _activeDurationAfterDash = 6f;

    [Header("Hub Upgrade Data (optional)")]
    [Tooltip("Hub upgrade definition for Dash Firetrail. primaryAmount/durationSeconds can be used later for damage or active window tuning.")]
    [SerializeField] private HubUpgradeSO _dashFiretrailUpgrade;

    private float _lastSpawnTime;
    private float _trailActiveUntil; // Time.time value; spawn only when Time.time < this
    private CharacterController _characterController;
    private ThirdPersonController _thirdPersonController;

    private void Start()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsHubUpgradeUnlocked(HubUpgradeKeys.DashFiretrail))
        {
            enabled = false;
            return;
        }

        _characterController = GetComponent<CharacterController>();
        _thirdPersonController = GetComponent<ThirdPersonController>();
        if (_thirdPersonController != null)
            _thirdPersonController.OnDashStart += OnPlayerDashed;
        if (_nodePrefab == null)
            Debug.LogWarning("[FireTrailController] Node prefab not assigned.", this);

        if (_nodePrefab == null)
            Debug.LogWarning("[FireTrailController] Node prefab not assigned.", this);

        var parentGO = new GameObject("FireTrailNodes");
        _nodesParent = parentGO.transform;
        
    }

    private void OnDisable()
    {
        if (_thirdPersonController != null)
            _thirdPersonController.OnDashStart -= OnPlayerDashed;
    }

    private void OnPlayerDashed(Vector3 _)
    {
        _trailActiveUntil = Time.time + _activeDurationAfterDash;
    }

    private void Update()
    {
        if (_nodePrefab == null) return;
        if (Time.time >= _trailActiveUntil) return;
        if (_thirdPersonController != null && !_thirdPersonController.Grounded) return;

        float horizontalSpeed = 0f;
        if (_characterController != null)
        {
            Vector3 vel = _characterController.velocity;
            horizontalSpeed = new Vector3(vel.x, 0f, vel.z).magnitude;
        }
        if (horizontalSpeed < _minMoveSpeed) return;

        if (Time.time - _lastSpawnTime < _spawnInterval) return;

        _lastSpawnTime = Time.time;
        Vector3 groundPos = transform.position - new Vector3(0f, _groundOffset, 0f);
        Instantiate(_nodePrefab, groundPos, Quaternion.identity, _nodesParent);
    }
}
