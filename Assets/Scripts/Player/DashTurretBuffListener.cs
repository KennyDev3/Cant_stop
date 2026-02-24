using UnityEngine;
using StarterAssets;

/// <summary>
/// Listens for dash start and applies the hub upgrade "dash turret attack speed" buff to the turret.
/// Place on the player (same GameObject as ThirdPersonController or ensure controller is assigned).
/// </summary>
public class DashTurretBuffListener : MonoBehaviour
{
    [SerializeField] private ThirdPersonController _controller;
    [Header("Hub Upgrade Data")]
    [Tooltip("Hub upgrade definition for Dash Turret Attack Speed. primaryAmount = fire-rate multiplier, durationSeconds = buff duration.")]
    [SerializeField] private HubUpgradeSO _dashTurretAttackSpeedUpgrade;

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
            float multiplier = 2f;
            float duration = 10f;

            if (_dashTurretAttackSpeedUpgrade != null)
            {
                if (_dashTurretAttackSpeedUpgrade.primaryAmount > 0f)
                    multiplier = _dashTurretAttackSpeedUpgrade.primaryAmount;
                if (_dashTurretAttackSpeedUpgrade.durationSeconds > 0f)
                    duration = _dashTurretAttackSpeedUpgrade.durationSeconds;
            }

            turret.ApplyTempFireRateBuff(multiplier, duration);
            Debug.Log($"[Dash] Turret attack speed buff active for {duration} seconds (x{multiplier}).");
        }
    }
}
