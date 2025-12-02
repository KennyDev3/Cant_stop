using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Item Display Config")]
    [SerializeField] private GameObject itemSlotPrefab; 
    [SerializeField] private Transform itemContainer;

    [Header("Item Popup")]
    [SerializeField] private ItemPopupUI popupPrefab;
    [SerializeField] private Transform popupParent;
    private ItemPopupUI _currentPopup;


    // Dictionary to map Data (ItemSO) to Visuals (ItemSlotUI)

    private Dictionary<ItemSO, ItemSlotUI> _itemSlots = new Dictionary<ItemSO, ItemSlotUI>();


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

    public void UpdateItemDisplay(ItemSO item, int totalCount)
    {
        // Check if Item already exists in UI
        if (_itemSlots.ContainsKey(item))
        {
            // UPDATE EXISTING
            _itemSlots[item].UpdateCount(totalCount);
        }
        else
        {
            // CREATE NEW
            GameObject newSlotObj = Instantiate(itemSlotPrefab, itemContainer);
            ItemSlotUI newSlotScript = newSlotObj.GetComponent<ItemSlotUI>();

            if (newSlotScript != null)
            {
                newSlotScript.Initialize(item, totalCount);
                _itemSlots.Add(item, newSlotScript);
            }
        }
    }

    public void ShowItemPopup(ItemSO item)
    {
        if (_currentPopup != null)
        {
            Destroy(_currentPopup.gameObject);
        }

        _currentPopup = Instantiate(popupPrefab, popupParent);
        _currentPopup.Initialize(item.itemName, item.description);
    }




}
