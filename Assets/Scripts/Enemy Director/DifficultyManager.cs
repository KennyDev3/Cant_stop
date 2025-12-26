using UnityEngine;
using System;

[System.Serializable]
public class DifficultyProfile
{
    [Header("Timers")]
    public float initialSafeTime = 60f;
    public float standardInterval = 60f;
    public float accelerationThreshold = 600f;
    public float acceleratedInterval = 30f;

    [Header("Multipliers")]
    public float hpMultiplierPerStep = 1.1f;
    public float damageMultiplierPerStep = 1.2f;
    public float creditMultiplierPerStep = 1.15f;
}

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [SerializeField] private DifficultyProfile profile;
    [SerializeField] private bool debugMode = false;

    public int DifficultyStage { get; private set; } = 0;
    public float TotalRunTime { get; private set; } = 0f;
    public float HpMultiplier { get; private set; } = 1f;
    public float DamageMultiplier { get; private set; } = 1f;
    public float CreditMultiplier { get; private set; } = 1f;

    private float _timer;
    private float _currentInterval;

    public event Action<int> OnDifficultyIncreased;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _currentInterval = profile.initialSafeTime;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        TotalRunTime += dt; 
        _timer += dt;

        if (_timer >= _currentInterval)
        {
            IncreaseDifficulty();
            _timer = 0f;
            _currentInterval = (TotalRunTime >= profile.accelerationThreshold)
                ? profile.acceleratedInterval : profile.standardInterval;
        }
    }


    private void IncreaseDifficulty()
    {
        DifficultyStage++;

        // Subtracting 1 converts "1.3" into "0.3" (the 30% increase)
        float hpStep = profile.hpMultiplierPerStep - 1f;
        float dmgStep = profile.damageMultiplierPerStep - 1f;
        float creditStep = profile.creditMultiplierPerStep - 1f;

        // Linear formula: Base (1) + (Stage * 30%)
        HpMultiplier = 1f + (DifficultyStage * hpStep);
        DamageMultiplier = 1f + (DifficultyStage * dmgStep);
        CreditMultiplier = 1f + (DifficultyStage * creditStep);

        if (debugMode) Debug.Log($"Difficulty Increased: Stage {DifficultyStage} | HP: x{HpMultiplier}");
        OnDifficultyIncreased?.Invoke(DifficultyStage);
    }

    public void ResetDifficulty()
    {
        DifficultyStage = 0;
        TotalRunTime = 0f;
        _timer = 0f;
        _currentInterval = profile.initialSafeTime;

        HpMultiplier = 1f;
        DamageMultiplier = 1f;
        CreditMultiplier = 1f;

        Debug.Log("[Difficulty] Stats reset.");
    }
}



