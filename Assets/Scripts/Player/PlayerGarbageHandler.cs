using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using StarterAssets;
using UnityEngine.InputSystem;

public class PlayerGarbageHandler : MonoBehaviour
{
    private StarterAssetsInputs _input;

    [Header("Capacity")]
    [SerializeField] private int maxCapacity = 10;
    private int _currentCapacity = 0;

    [Header("Inventory")]
    private List<GarbageData> _carriedGarbage = new List<GarbageData>();
    [SerializeField] private float _money = 200f;

    [Header("Strength")]
    [Tooltip("Player's current strength level (1-3). Determines which garbage tiers they can pick up.")]
    [SerializeField] public int playerStrength = 1;

    [Header("Chuck Mechanics")]
    [SerializeField] private GameObject garbageBundlePrefab;
    [SerializeField] private Transform throwOrigin;

    [Header("Throw Charge Settings")]
    [SerializeField] private float minThrowForce = 5f;    // Force if tapped
    [SerializeField] private float maxThrowForce = 15f;   // Force if held fully
    [SerializeField] private float maxChargeTime = 2.0f;  // Time in seconds to reach max force
    [Tooltip("1.0 = 45 degree throw. Higher = Higher arc.")]
    [SerializeField] private float throwUpwardModifier = 1.0f;

    // Internal state for charging
    private bool _isCharging = false;
    private float _chargeStartTime = 0f;

    [Header("--- DEBUGGING ---")]
    [Tooltip("Enable to use debug keys and features below.")]
    [SerializeField] private bool enableDebugCheats = true;
    [SerializeField] private Key debugAddKey = Key.T;
    [SerializeField] private bool startWithFullInventory = false;
    [SerializeField] private GarbageData debugTestItem;

    private GarbageItem _currentItemToCollect;
    private ThirdPersonController _playerController;

    public bool IsOverencumbered
    {
        get { return _currentCapacity > maxCapacity; }
    }

    public int GetBaseMaxCapacity() => maxCapacity;
    public int GetPlayerStrength() => playerStrength;
    public float GetMoney() => _money;
    public int GetCurrentCapacity() => _currentCapacity;

    [System.Serializable]
    public class CapacityChangeEvent : UnityEvent<int, int> { }
    public CapacityChangeEvent onCapacityChanged;

    [System.Serializable]
    public class MoneyChangeEvent : UnityEvent<float> { }
    public MoneyChangeEvent onMoneyChanged;

    void Awake()
    {
        _playerController = GetComponent<ThirdPersonController>();
        _input = GetComponent<StarterAssetsInputs>();

        if (_playerController == null)
        {
            Debug.LogError("Player is missing ThirdPersonController!");
        }
        else
        {
            _playerController.OnPickupAnimationComplete += FinalizePickup;
        }
    }

    private void Start()
    {
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
        onMoneyChanged.Invoke(_money);

        if (enableDebugCheats && startWithFullInventory)
        {
            FillInventoryDebug();
        }
    }

    private void Update()
    {
        HandleThrowLogic();

        if (enableDebugCheats && Keyboard.current != null)
        {
            if (Keyboard.current[debugAddKey].wasPressedThisFrame)
            {
                AddDebugGarbage();
            }
        }
    }

    void OnDestroy()
    {
        if (_playerController != null)
        {
            _playerController.OnPickupAnimationComplete -= FinalizePickup;
        }
    }

    // ===================================
    // THROW CHARGING LOGIC
    // ===================================

    private void HandleThrowLogic()
    {
        // 1. Start Charging
        if (_input.chuck && !_isCharging)
        {
            if (_carriedGarbage.Count > 0)
            {
                _isCharging = true;
                _chargeStartTime = Time.time;
            }
            else
            {
                // Reset input so it doesn't spam logs if empty
                if (_input.chuck) Debug.LogWarning("[Throw] Cannot throw: Inventory Empty.");
                _input.chuck = false;
            }
        }

        // 2. Process Charge while button is held
        if (_isCharging)
        {
            float currentHoldDuration = Time.time - _chargeStartTime;

            // Scenario A: Max Charge Reached (Auto Release)
            if (currentHoldDuration >= maxChargeTime)
            {
                PerformThrow(maxThrowForce);
                ResetChargeState();
            }
            // Scenario B: Player Released Button (Manual Release)
            else if (!_input.chuck)
            {
                // Calculate percentage (0.0 to 1.0)
                float chargePercent = Mathf.Clamp01(currentHoldDuration / maxChargeTime);

                // Lerp force
                float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargePercent);


                PerformThrow(finalForce);
                ResetChargeState();
            }
        }
    }

    private void ResetChargeState()
    {
        _isCharging = false;
        // We do NOT set _input.chuck to false here, to prevent input fighting 
        // if the player is still physically holding the key after an auto-throw.
    }

    public void PerformThrow(float calculatedForce)
    {
        if (_carriedGarbage.Count == 0) return;

        if (garbageBundlePrefab == null || throwOrigin == null)
        {
            Debug.LogError("GarbageHandler: Missing Prefab or ThrowOrigin assignment.");
            return;
        }

        GameObject bundleObj = Instantiate(garbageBundlePrefab, throwOrigin.position, throwOrigin.rotation);
        GarbageBundle bundleScript = bundleObj.GetComponent<GarbageBundle>();

        if (bundleScript != null)
        {
            Vector3 upForce = Vector3.up * throwUpwardModifier;
            Vector3 forwardForce = transform.forward;
            Vector3 finalDir = (forwardForce + upForce).normalized;

            float fullnessRatio = (float)_currentCapacity / maxCapacity;

            Debug.Log($"<color=yellow>[Physics] Launching Bundle! Force: {calculatedForce} | Dir: {finalDir}</color>");

            bundleScript.InitializeBundle(_carriedGarbage, finalDir, calculatedForce, fullnessRatio);
        }

        _carriedGarbage.Clear();
        _currentCapacity = 0;
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
    }

    // ===================================
    // GARBAGE COLLECTION LOGIC
    // ===================================

    private bool AddGarbageToInventory(GarbageData data)
    {
        _currentCapacity += data.capacityCost;
        _carriedGarbage.Add(data);
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);

        Debug.Log($"Collected {data.itemName}. Current capacity: {_currentCapacity}/{maxCapacity}");

        if (IsOverencumbered)
        {
            Debug.Log("Player is now overencumbered! Movement penalties should apply.");
        }
        return true;
    }

    public bool StartPickupProcess(GarbageItem garbageItem)
    {
        GarbageData data = garbageItem.GetGarbageData();

        if (playerStrength < data.garbageTier)
        {
            Debug.LogWarning($"Not strong enough to pick up {data.itemName} (Tier {data.garbageTier}).");
            return false;
        }

        if (_playerController.StartPickUp(false))
        {
            _currentItemToCollect = garbageItem;
            // Debug.Log($"Initiated pickup sequence for {data.itemName}.");
            return true;
        }
        else
        {
            return false;
        }
    }

    private void FinalizePickup()
    {
        if (_currentItemToCollect == null) return;

        GarbageData data = _currentItemToCollect.GetGarbageData();
        AddGarbageToInventory(data);
        _currentItemToCollect.NotifyCollected();
        _currentItemToCollect = null;
    }

    public bool TryInstantCollect(GarbageItem item)
    {
        GarbageData data = item.GetGarbageData();

        if (playerStrength < data.garbageTier)
        {
            Debug.Log("You are too weak to pick this item up");
            return false;
        }
        AddGarbageToInventory(data);
        item.NotifyCollected();
        return true;
    }

    public bool TryCollectBundle(GarbageBundle bundle)
    {
        List<GarbageData> bundleContents = bundle.GetContents();
        int bundleWeight = 0;

        foreach (var item in bundleContents) bundleWeight += item.capacityCost;

        foreach (var item in bundleContents)
        {
            _carriedGarbage.Add(item);
        }

        _currentCapacity += bundleWeight;

        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
        Debug.Log($"Picked up bundle worth {bundleWeight} weight.");

        return true;
    }

    public int DropOffGarbage()
    {
        int totalValue = 0;
        foreach (var garbage in _carriedGarbage)
        {
            totalValue += garbage.value;
        }

        Debug.Log($"Dropped off {_carriedGarbage.Count} items for ${totalValue}");
        _money += totalValue;

        _carriedGarbage.Clear();
        _currentCapacity = 0;

        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
        onMoneyChanged.Invoke(_money);

        return totalValue;
    }

    // ===================================
    // ECONOMY & UPGRADES
    // ===================================

    public bool CanAfford(float amount)
    {
        return _money >= amount;
    }

    public bool Spend(float amount)
    {
        if (amount <= 0) return false;

        if (CanAfford(amount))
        {
            _money -= amount;
            onMoneyChanged.Invoke(_money);
            Debug.Log($"Spent ${amount:F2}. New balance: ${_money:F2}");
            return true;
        }
        else
        {
            float needed = amount - _money;
            Debug.LogWarning($"Not enough funds! Need ${needed:F2} more.");
            return false;
        }
    }

    public void AddMoney(float amount)
    {
        _money += amount;
        onMoneyChanged.Invoke(_money);
        Debug.Log($"Received payment: ${amount}. New Balance: ${_money}");
    }

    public void UpgradeMaxCapacity(int increaseAmount)
    {
        if (increaseAmount <= 0) return;
        maxCapacity += increaseAmount;
        Debug.Log($"Max Capacity upgraded to {maxCapacity}.");
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
    }

    public void UpgradePlayerStrength(int increaseAmount = 1)
    {
        if (increaseAmount <= 0) return;
        playerStrength += increaseAmount;
        Debug.Log($"Player Strength upgraded to Tier {playerStrength}.");
    }

    // ===================================
    // DEBUG HELPERS
    // ===================================

    private void AddDebugGarbage()
    {
        GarbageData dataToAdd = debugTestItem;

        if (dataToAdd == null)
        {
            dataToAdd = ScriptableObject.CreateInstance<GarbageData>();
            dataToAdd.itemName = "Debug Trash";
            dataToAdd.value = 10;
            dataToAdd.capacityCost = 2;
            dataToAdd.garbageTier = 1;
        }

        AddGarbageToInventory(dataToAdd);
    }

    private void FillInventoryDebug()
    {
        Debug.Log("Debug: Filling Inventory...");
        int safeGuard = 0;

        while (!IsOverencumbered && safeGuard < 100)
        {
            AddDebugGarbage();
            safeGuard++;
        }
    }
}