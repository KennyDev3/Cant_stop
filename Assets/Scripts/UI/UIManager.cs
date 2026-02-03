using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

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

    [Header("Kill Count UI")]
    public TextMeshProUGUI killCountText;

    [Header("Run Resources UI")]
    [Tooltip("Assign one entry per resource type (e.g. 3). Wire PlayerResourceHolder.onResourceCountChanged to UpdateResourceCount.")]
    [SerializeField] private List<ResourceDisplaySlot> resourceSlots = new List<ResourceDisplaySlot>();

    [System.Serializable]
    public struct ResourceDisplaySlot
    {
        public ResourceSO resourceType;
        public TextMeshProUGUI countText;
    }

    private Dictionary<ItemSO, ItemSlotUI> _itemSlots = new Dictionary<ItemSO, ItemSlotUI>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (GameManager.Instance != null)
            GameManager.Instance.OnSceneReady += HandleSceneReady;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSceneReady -= HandleSceneReady;
            GameManager.Instance.OnKillCountChanged -= UpdateKillCountUI;
        }
        if (Instance == this)
            Instance = null;
    }

    private void HandleSceneReady()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnKillCountChanged += UpdateKillCountUI;
        UpdateKillCountUI(GameManager.Instance.KillCount);
    }

    // --- UI Update Methods ---

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

    /// <summary>Call from PlayerResourceHolder.onResourceCountChanged so run HUD updates when a resource is picked up (or state applied).</summary>
    public void UpdateResourceCount(ResourceSO type, int count)
    {
        if (type == null || resourceSlots == null) return;
        foreach (var slot in resourceSlots)
        {
            if (slot.resourceType != type || slot.countText == null) continue;
            slot.countText.text = count.ToString();
            return;
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

    // This is the listener method called by GameManager
    private void UpdateKillCountUI(int newCount)
    {
        if (killCountText != null)
        {
            killCountText.text = $"Kills: {newCount}";
        }
    }
}