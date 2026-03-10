using UnityEngine;
using StarterAssets;

/// <summary>
/// Applies values from a PlayerConfig asset to the player components in the scene.
/// This runs on scene load so prefab defaults are overridden by the per-level config.
/// </summary>
public class PlayerConfigApplicator : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerConfig config;

    [Header("Targets (optional if on same GameObject)")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerGarbageHandler garbageHandler;
    [SerializeField] private ThirdPersonController controller;

    public PlayerConfig Config => config;

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogWarning($"[{nameof(PlayerConfigApplicator)}] No PlayerConfig assigned on {name}.", this);
            return;
        }

        // Auto-resolve components on the same GameObject if not wired explicitly.
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
        if (garbageHandler == null) garbageHandler = GetComponent<PlayerGarbageHandler>();
        if (controller == null) controller = GetComponent<ThirdPersonController>();

        ApplyConfig();
    }

    private void ApplyConfig()
    {
        if (playerHealth != null)
        {
            playerHealth.maxHealth = config.baseMaxHealth;
        }

        if (garbageHandler != null)
        {
            // Apply only the base max capacity; current capacity and run-state are handled elsewhere.
            garbageHandler.SetBaseMaxCapacity(config.baseMaxCapacity, resetCurrentCapacity: false);
        }

        if (controller != null)
        {
            // Movement speeds (treated as base values; hub upgrades still modify via StatController).
            controller.MoveSpeed = config.moveSpeed;
            controller.SprintSpeed = Mathf.Max(config.moveSpeed, config.sprintSpeed);

            controller.UseMouseRotation = config.useMouseRotation;
        }
    }
}

