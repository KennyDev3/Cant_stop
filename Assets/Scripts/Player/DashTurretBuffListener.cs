using UnityEngine;
using StarterAssets;

/// <summary>
/// Listens for dash start and applies the hub upgrade "dash turret attack speed" buff to the turret.
/// Place on the player (same GameObject as ThirdPersonController or ensure controller is assigned).
/// </summary>
public class DashTurretBuffListener : MonoBehaviour
{
    [SerializeField] private ThirdPersonController _controller;
    [Tooltip("Duration in seconds for the turret fire rate buff after dash.")]
    [SerializeField] private float _buffDuration = 10f;
    [Tooltip("Fire rate multiplier (2 = 100% more, i.e. double fire rate).")]
    [SerializeField] private float _buffMultiplier = 2f;

    private void OnEnable()
    {
        if (_controller == null) _controller = GetComponent<ThirdPersonController>();
        if (_controller != null)
            _controller.OnDashStart += OnDashStart;
    }

    private void OnDisable()
    {
        if (_controller != null)
            _controller.OnDashStart -= OnDashStart;
    }

    private void OnDashStart(Vector3 dashDirection)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsHubUpgradeUnlocked(HubUpgradeKeys.DashTurretAttackSpeed))
            return;

        var turret = FindFirstObjectByType<TruckTurret>();
        if (turret != null)
        {
            turret.ApplyTempFireRateBuff(_buffMultiplier, _buffDuration);
            Debug.Log("[Dash] Turret attack speed buff active.");
        }
    }
}
