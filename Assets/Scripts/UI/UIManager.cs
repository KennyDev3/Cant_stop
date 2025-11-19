using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Health UI")]
    public Slider healthSlider;

    [Header("Stamina UI")]
    public Slider staminaSlider;

    [Header("Inventory UI")]
    public TextMeshProUGUI carryingCapacityText;
    public TextMeshProUGUI moneyText;

    [Header("PickUpCooldown UI")]
    public Slider pickupCooldownSlider;

    private void Start()
    {
       
    }

    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void UpdateStamina(float currentStamina, float maxStamina)
    {
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

     public void UpdateCarryingCapacity(int currentCapacity, int maxCapacity)
    {
        if (carryingCapacityText != null)
        {
            carryingCapacityText.text = $"{currentCapacity} / {maxCapacity}";
        }
    }

    public void UpdateMoney(float amount)
    {
        if (moneyText != null)
        {
            moneyText.text = $"$ {amount}";
        }
    }

    public void UpdatePickupCooldown(float currentTime, float maxTime)
    {
        if (pickupCooldownSlider != null)
        {
            pickupCooldownSlider.maxValue = maxTime;
            pickupCooldownSlider.value = currentTime;
        }
    }




}
