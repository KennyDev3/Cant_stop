using UnityEngine;
using UnityEngine.Events;


public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    [Tooltip("The maximum amount of stamina the player can have.")]
    public float maxStamina = 100f;
    [Tooltip("The current amount of stamina.")]
    [SerializeField] private float currentStamina;
    [Tooltip("The rate at which stamina drains per second when sprinting.")]
    public float staminaDrainRate = 10f;
    [Tooltip("The rate at which stamina regenerates per second when not sprinting.")]
    public float staminaRegenRate = 5f;
    [Tooltip("The delay in seconds before stamina starts regenerating after sprinting.")]
    public float staminaRegenDelay = 1f;

     private float staminaRegenTimer;
    private bool isDraining;

    [System.Serializable]
    public class StaminaChangeEvent : UnityEvent<float, float> { }
    public StaminaChangeEvent onStaminaChanged;

    void Start()
    {
        currentStamina = maxStamina;
    }

    void Update()
    {
        if (!isDraining)
        {
            if (currentStamina < maxStamina)
            {
                if (staminaRegenTimer > 0)
                {
                    staminaRegenTimer -= Time.deltaTime;
                }
                else
                {
                    currentStamina += staminaRegenRate * Time.deltaTime;
                    currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
                    onStaminaChanged.Invoke(currentStamina, maxStamina);
                }
            }
        }
        isDraining = false;
    }

    public bool CanSprint()
    {
        return currentStamina > 0;
    }

     public void DrainStamina()
    {
        if (CanSprint())
        {
            isDraining = true;
            staminaRegenTimer = staminaRegenDelay;
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
            onStaminaChanged.Invoke(currentStamina, maxStamina);
        }
    }






    
}
