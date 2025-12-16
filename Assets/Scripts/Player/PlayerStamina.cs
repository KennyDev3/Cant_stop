using UnityEngine;
using UnityEngine.Events;

public class PlayerStamina : MonoBehaviour
{
    [Header("Boost Settings")]
    [SerializeField] private float boostDuration = 3f;
    [SerializeField] private float cooldownDuration = 5f;

    [Header("Boost VFX")]
    [SerializeField] private TrailRenderer boostTrail;

    [Header("Audio")]
    [SerializeField] private SoundDef boostSound;


    private float cooldownTimer;
    private bool isBoostActive;
    private float boostTimer;

    public UnityEvent<float, float> onStaminaChanged;

    public bool CanActivateBoost => cooldownTimer >= cooldownDuration;

    void Start()
    {
        cooldownTimer = cooldownDuration;
        onStaminaChanged.Invoke(cooldownTimer, cooldownDuration);

        if (boostTrail != null)
            boostTrail.emitting = false;
    }

    void Update()
    {
        if (isBoostActive)
            UpdateBoost();
        else
            UpdateCooldown();
    }

    private void UpdateBoost()
    {
        boostTimer -= Time.deltaTime;

        if (boostTimer <= 0f)
        {
            isBoostActive = false;

            // VFX can go here (stop)
            if (boostTrail != null)
                boostTrail.emitting = false;
        }
    }

    private void UpdateCooldown()
    {
        if (cooldownTimer >= cooldownDuration)
            return;

        cooldownTimer += Time.deltaTime;
        if (cooldownTimer > cooldownDuration)
            cooldownTimer = cooldownDuration;

        onStaminaChanged.Invoke(cooldownTimer, cooldownDuration);
    }

    public bool TryActivateBoost()
    {
        if (!CanActivateBoost) return false;

        isBoostActive = true;
        boostTimer = boostDuration;
        cooldownTimer = 0f;

        // VFX can go here (play)
        if (boostTrail != null)
            boostTrail.emitting = true;

        SoundManager.Instance.Play(boostSound, transform.position);

        onStaminaChanged.Invoke(cooldownTimer, cooldownDuration);
        return true;
    }

    public bool IsBoostActive()
    {
        return isBoostActive;
    }
}
