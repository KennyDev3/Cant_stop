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

        HpMultiplier = Mathf.Pow(profile.hpMultiplierPerStep, DifficultyStage);
        DamageMultiplier = Mathf.Pow(profile.damageMultiplierPerStep, DifficultyStage);
        CreditMultiplier = Mathf.Pow(profile.creditMultiplierPerStep, DifficultyStage);

        if (debugMode) Debug.Log($"Difficulty Increased: Stage {DifficultyStage}");
        OnDifficultyIncreased?.Invoke(DifficultyStage);
    }
}