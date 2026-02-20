using UnityEngine;
using StarterAssets;

/// <summary>
/// Listens for hub upgrade unlocks and enables the corresponding abilities on the player immediately
/// (e.g. so parry works in the hub right after purchase without reloading).
/// Add to the player root. Parry/Dash controllers stay disabled until unlock; this turns them on when unlocked.
/// </summary>
public class HubUpgradeApplyToPlayer : MonoBehaviour
{
    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnHubUpgradeUnlocked += ApplyUnlock;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnHubUpgradeUnlocked -= ApplyUnlock;
    }

    private void ApplyUnlock(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return;

        if (upgradeId == HubUpgradeKeys.ParryUnlock)
        {
            var parryController = GetComponentInChildren<PlayerParryController>(true);
            if (parryController != null)
            {
                parryController.enabled = true;
                var shield = parryController.GetComponentInChildren<ParryShield>(true);
                if (shield != null) shield.enabled = true;
            }
        }
        // Dash is gated in ThirdPersonController by check each frame; no component to enable.
    }
}
