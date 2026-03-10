using UnityEngine;

[CreateAssetMenu(menuName = "Game/PlayerConfig", fileName = "PlayerConfig")]
public class PlayerConfig : ScriptableObject
{
    [Header("Player Base Stats")]
    [Tooltip("Base maximum health for the player in this level (before upgrades / run-state).")]
    public float baseMaxHealth = 1000f;

    [Tooltip("Base maximum garbage capacity for the player in this level (before meta upgrades).")]
    public int baseMaxCapacity = 10;

    [Tooltip("Base walking move speed for the player in this level (before upgrades / stamina boost).")]
    public float moveSpeed = 4.5f;

    [Tooltip("Base sprint speed for the player in this level (before upgrades / stamina boost).")]
    public float sprintSpeed = 7.5f;

    [Header("Input")]
    [Tooltip("If true, player uses mouse rotation. If false, gamepad / stick rotation is used.")]
    public bool useMouseRotation = true;
}

