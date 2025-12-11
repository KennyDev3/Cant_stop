using UnityEngine;
using UnityEngine.Events;

public class PlayerStamina : MonoBehaviour
{
    [Header("Boost Settings")]
    [SerializeField] private float boostDuration = 3f;
    [SerializeField] private float cooldownDuration = 5f;

    private float cooldownTimer;
    private bool isBoostActive;
    private float boostTimer;

    public UnityEvent<float, float> onStaminaChanged;

    public bool CanActivateBoost => cooldownTimer >= cooldownDuration;

    void Start()
    {
        cooldownTimer = cooldownDuration;
        onStaminaChanged.Invoke(cooldownTimer, cooldownDuration);
    }

    void Update()
    {
        if (isBoostActive)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0f)
            {
                isBoostActive = false;

                // VFX can go here (stop)
            }
        }
        else
        {
            if (cooldownTimer < cooldownDuration)
            {
                cooldownTimer += Time.deltaTime;
                if (cooldownTimer > cooldownDuration)
                    cooldownTimer = cooldownDuration;

                onStaminaChanged.Invoke(cooldownTimer, cooldownDuration);
            }
        }
    }

    public bool TryActivateBoost()
    {
        if (!CanActivateBoost) return false;

        isBoostActive = true;
        boostTimer = boostDuration;
        cooldownTimer = 0f;

        // VFX can go here (play)

        onStaminaChanged.Invoke(cooldownTimer, cooldownDuration);
        return true;
    }

    public bool IsBoostActive()
    {
        return isBoostActive;
    }
}
