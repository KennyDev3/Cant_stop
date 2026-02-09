using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using StarterAssets;
using UnityEngine.InputSystem;

public class PlayerGarbageHandler : MonoBehaviour, IRunStateContributor
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
    [SerializeField] private float minThrowForce = 5f;
    [SerializeField] private float maxThrowForce = 15f;
    [SerializeField] private float maxChargeTime = 2.0f;
    [SerializeField] private float throwUpwardModifier = 1.0f;

    private bool _isCharging = false;
    private float _chargeStartTime = 0f;

    [Header("Audio")]
    [SerializeField] SoundDef singleGarbagePickUpSound;
    [SerializeField] SoundDef garbageAddedToIventorySound;
    [SerializeField] SoundDef areaOfEffectPickupSound;
    [SerializeField] SoundDef throwGarbageSound;


    [Header("--- DEBUGGING ---")]
    [SerializeField] private bool enableDebugCheats = true;
    [SerializeField] private Key debugAddKey = Key.T;
    [SerializeField] private bool startWithFullInventory = false;
    [SerializeField] private GarbageData debugTestItem;

    private GarbageItem _currentItemToCollect;
    private ThirdPersonController _playerController;

    public bool IsOverencumbered => _currentCapacity > maxCapacity;
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

        if (_playerController == null) Debug.LogError("Player is missing ThirdPersonController!");
        else _playerController.OnPickupAnimationComplete += FinalizePickup;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterRunStateContributor(this);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterRunStateContributor(this);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterRunStateContributor(this);

        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
        onMoneyChanged.Invoke(_money);

        if (enableDebugCheats && startWithFullInventory) FillInventoryDebug();
    }

    public void ContributeToRunState(RunState state)
    {
        state.Economy.Money = _money;
        state.Player.MaxCapacity = maxCapacity;
    }

    public void ApplyRunState(RunState state)
    {
        if (state.Player.Health > 0f)
        {
            _money = state.Economy.Money;
            maxCapacity = state.Player.MaxCapacity;
            Debug.Log($"[RunState] PlayerGarbageHandler applied: money={_money} maxCapacity={maxCapacity}");
        }
        else
            Debug.Log($"[RunState] PlayerGarbageHandler skipped (Health <= 0: {state.Player.Health})");
    }

    private void Update()
    {
        HandleThrowLogic();

        if (enableDebugCheats && Keyboard.current != null)
        {
            if (Keyboard.current[debugAddKey].wasPressedThisFrame) AddDebugGarbage();
        }
    }

    void OnDestroy()
    {
        if (_playerController != null) _playerController.OnPickupAnimationComplete -= FinalizePickup;
    }

    // ===================================
    // THROW LOGIC
    // ===================================

    private void HandleThrowLogic()
    {
        if (_input.chuck && !_isCharging)
        {
            if (_carriedGarbage.Count > 0)
            {
                _isCharging = true;
                _chargeStartTime = Time.time;
            }
            else
            {
                _input.chuck = false;
            }
        }

        if (_isCharging)
        {
            float currentHoldDuration = Time.time - _chargeStartTime;

            if (currentHoldDuration >= maxChargeTime)
            {
                PerformThrow(maxThrowForce);
                ResetChargeState();
            }
            else if (!_input.chuck)
            {
                float chargePercent = Mathf.Clamp01(currentHoldDuration / maxChargeTime);
                float finalForce = Mathf.Lerp(minThrowForce, maxThrowForce, chargePercent);
                PerformThrow(finalForce);
                ResetChargeState();
            }
        }
    }

    private void ResetChargeState()
    {
        _isCharging = false;
    }

    public void PerformThrow(float calculatedForce)
    {
        if (_carriedGarbage.Count == 0) return;

        if (garbageBundlePrefab == null || throwOrigin == null)
        {
            Debug.LogError("GarbageHandler: Missing Prefab or ThrowOrigin.");
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

            SoundManager.Instance.Play(throwGarbageSound, transform.position);
            bundleScript.InitializeBundle(_carriedGarbage, finalDir, calculatedForce, fullnessRatio);
        }

        _carriedGarbage.Clear();
        _currentCapacity = 0;
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
    }

    // ===================================
    // GARBAGE COLLECTION
    // ===================================

    private bool AddGarbageToInventory(GarbageData data)
    {
        _currentCapacity += data.capacityCost;
        _carriedGarbage.Add(data);
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
        SoundManager.Instance.Play(garbageAddedToIventorySound, transform.position);
        return true;
    }

    public bool StartPickupProcess(GarbageItem garbageItem)
    {
        GarbageData data = garbageItem.GetGarbageData();

        if (playerStrength < data.garbageTier) return false;

        if (_playerController.StartPickUp(false))
        {
            _currentItemToCollect = garbageItem;
            SoundManager.Instance.Play(singleGarbagePickUpSound, transform.position);
            return true;
        }
        return false;
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
        SoundManager.Instance.Play(areaOfEffectPickupSound, transform.position);

        if (playerStrength < data.garbageTier) return false;

        AddGarbageToInventory(data);
        item.NotifyCollected();
        return true;
    }

    public bool TryCollectBundle(GarbageBundle bundle)
    {
        List<GarbageData> bundleContents = bundle.GetContents();
        int bundleWeight = 0;
        foreach (var item in bundleContents) bundleWeight += item.capacityCost;
        foreach (var item in bundleContents) _carriedGarbage.Add(item);

        _currentCapacity += bundleWeight;
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
        SoundManager.Instance.Play(singleGarbagePickUpSound, transform.position);
        return true;
    }

   
    // ===================================
    // ECONOMY & UPGRADES
    // ===================================

    public bool CanAfford(float amount) => _money >= amount;

    public bool Spend(float amount)
    {
        if (amount <= 0) return false;

        if (CanAfford(amount))
        {
            _money -= amount;
            onMoneyChanged.Invoke(_money);
            return true;
        }
        return false;
    }

    public void AddMoney(float amount)
    {
        _money += amount;
        onMoneyChanged.Invoke(_money);
    }

    public void UpgradeMaxCapacity(int increaseAmount)
    {
        if (increaseAmount <= 0) return;
        maxCapacity += increaseAmount;
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
    }

    public void UpgradePlayerStrength(int increaseAmount = 1)
    {
        if (increaseAmount <= 0) return;
        playerStrength += increaseAmount;
    }

    // ===================================
    // DEBUG / HELPERS
    // ===================================

    private void AddDebugGarbage()
    {
        GarbageData dataToAdd = debugTestItem;
        if (dataToAdd == null)
        {
            dataToAdd = ScriptableObject.CreateInstance<GarbageData>();
            dataToAdd.itemName = "Debug Trash";
            dataToAdd.capacityCost = 2;
            dataToAdd.garbageTier = 1;
        }
        AddGarbageToInventory(dataToAdd);
    }

    private void FillInventoryDebug()
    {
        int safeGuard = 0;
        while (!IsOverencumbered && safeGuard < 100)
        {
            AddDebugGarbage();
            safeGuard++;
        }
    }

    public void AddRefundedGarbage(List<GarbageData> data)
    {
        if (data == null || data.Count == 0) return;
        foreach (var item in data)
        {
            _carriedGarbage.Add(item);
            _currentCapacity += item.capacityCost;
        }
        onCapacityChanged.Invoke(_currentCapacity, maxCapacity);
        SoundManager.Instance.Play(garbageAddedToIventorySound, transform.position);
    }
}