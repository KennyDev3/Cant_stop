using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class EnemyDirector : MonoBehaviour, IRunStateContributor
{
    public static EnemyDirector Instance { get; private set; }

    public float GetWaveCredits() => waveCredits;
    public float GetTrickleCredits() => trickleCredits;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    [Header("Economy - Wave (Primary)")]
    [SerializeField] private float baseCreditsPerSecond = 10f;
    [SerializeField] private float startCredits = 10f;
    [SerializeField] private float refundPercentage = 0.3f;

    [Header("Wave Scaling")]
    [SerializeField] private float initialMaxExpenditure = 200f;
    [SerializeField] private float expenditureGrowthPerMinute = 100f;
    [SerializeField] private float absoluteMaxExpenditure = 10000f;

    [Header("Dynamic Wave Timing")]
    [SerializeField] private float initialWaveInterval = 60f;
    [SerializeField] private float acceleratedWaveInterval = 30f;
    [SerializeField] private float waveAccelerationThreshold = 300f;

    [Header("Wave Spawn Pattern")]
    [SerializeField] private float waveDuration = 30f;
    [SerializeField] private float minSpawnDelay = 0.3f;
    [SerializeField] private float maxSpawnDelay = 2.5f;
    [SerializeField] private AnimationCurve spawnIntensityCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1f);
    [SerializeField] private int minBatchSize = 1;
    [SerializeField] private int maxBatchSize = 5;
    [Tooltip("How much the batch size scales with wave progress (0-1). Higher = bigger batches later")]
    [SerializeField] private float batchSizeScaling = 0.6f;

    [Header("Economy - Trickle (Background)")]
    [SerializeField] private float trickleCreditsPerSecond = 2f;
    [SerializeField] private float tricklePauseAfterWave = 30f;
    [SerializeField] private float maxTricklePool = 20f;
    [SerializeField] private float trickleIntervalMin = 3f;
    [SerializeField] private float trickleIntervalMax = 7f;
    [SerializeField] private int maxTrickleGroupSize = 3;

    [Tooltip("Enemies with a selectionWeight higher than this are considered 'Trickle' enemies")]
    [SerializeField] private float trickleWeightThreshold = 0.8f;

    private float trickleTimer;
    private float currentTrickleDelay;

    [Header("Spawning Rules")]
    [SerializeField] private int maxActiveEnemies = 60;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnRadius = 6f;
    [SerializeField] private float maxSpawnRadius = 16f;

    private List<EnemyData> _affordableCache = new List<EnemyData>();

    [System.Serializable]
    public class EnemyConfig
    {
        public string label = "New Enemy";
        public EnemyData enemyData;
        public bool canSpawn = true;
    }

    [Header("Data")]
    public List<EnemyConfig> enemyList;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI creditsText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI hpModifierText;
    [SerializeField] private TextMeshProUGUI damageModifierText;
    [SerializeField] private TextMeshProUGUI creditModifierText;
    [SerializeField] private TextMeshProUGUI trickleCreditsText;
    [SerializeField] private TextMeshProUGUI livingEnemiesText;
    [SerializeField] private TextMeshProUGUI waveStatusText;

    // Internal State
    private float waveCredits;
    private float trickleCredits;
    private float tricklePauseTimer;
    private bool isTricklePaused;

    private int currentLivingEnemies;
    private float waveTimer;
    private bool isWaveActive;
    private Coroutine activeWaveCoroutine;

    private void Awake()
    {
        Instance = this;

        waveCredits = startCredits;
        trickleCredits = 0f;
        currentLivingEnemies = 0;

        if (spawnIntensityCurve == null || spawnIntensityCurve.keys.Length == 0)
        {
            spawnIntensityCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1f);
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterRunStateContributor(this);
        EnemyHealth.OnEnemyDeath += HandleEnemyDeath;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterRunStateContributor(this);
        EnemyHealth.OnEnemyDeath -= HandleEnemyDeath;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterRunStateContributor(this);

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        waveTimer = 0f;
    }

    public void ContributeToRunState(RunState state)
    {
        state.Economy.WaveCredits = waveCredits;
        state.Economy.TrickleCredits = trickleCredits;
    }

    public void ApplyRunState(RunState state)
    {
        waveCredits = state.Economy.WaveCredits;
        trickleCredits = state.Economy.TrickleCredits;
        Debug.Log($"[RunState] EnemyDirector applied: waveCredits={waveCredits} trickleCredits={trickleCredits}");
    }

    private void Update()
    {
        if (playerTransform == null) return;

        UpdateUI();
        HandleEconomyAndTimers();

        // Only increment wave timer if not actively spawning a wave
        if (!isWaveActive)
        {
            float currentRunTime = DifficultyManager.Instance.TotalRunTime;
            float currentSpawnInterval = (currentRunTime >= waveAccelerationThreshold)
                ? acceleratedWaveInterval
                : initialWaveInterval;

            waveTimer += Time.deltaTime;
            if (waveTimer >= currentSpawnInterval)
            {
                TriggerWave();
                waveTimer = 0f;
            }
        }
    }

    private void HandleEconomyAndTimers()
    {
        float multiplier = DifficultyManager.Instance != null ? DifficultyManager.Instance.CreditMultiplier : 1f;

        waveCredits += baseCreditsPerSecond * multiplier * Time.deltaTime;

        if (isTricklePaused)
        {
            tricklePauseTimer -= Time.deltaTime;
            if (tricklePauseTimer <= 0) isTricklePaused = false;
        }
        else
        {
            trickleCredits = Mathf.Min(trickleCredits + (trickleCreditsPerSecond * multiplier * Time.deltaTime), maxTricklePool);

            trickleTimer += Time.deltaTime;
            if (trickleTimer >= currentTrickleDelay)
            {
                AttemptTrickleSpawn();
                trickleTimer = 0;
                currentTrickleDelay = Random.Range(trickleIntervalMin, trickleIntervalMax);
            }
        }
    }

    private void AttemptTrickleSpawn()
    {
        if (currentLivingEnemies >= maxActiveEnemies || trickleCredits <= 0) return;

        int spawnCountGoal = Random.Range(1, maxTrickleGroupSize + 1);
        int spawnedThisBeat = 0;

        for (int i = 0; i < spawnCountGoal; i++)
        {
            if (currentLivingEnemies >= maxActiveEnemies) break;

            List<EnemyData> trickleOptions = enemyList
                .Where(x => x.canSpawn && x.enemyData != null &&
                            x.enemyData.selectionWeight >= trickleWeightThreshold &&
                            x.enemyData.spawnCost <= trickleCredits)
                .Select(x => x.enemyData)
                .ToList();

            if (trickleOptions.Count == 0) break;

            EnemyData selected = GetWeightedRandomEnemy(trickleOptions);
            Vector3 spawnPos = GetDonutSpawnPosition();

            if (spawnPos != Vector3.zero && ExecuteSpawn(selected, spawnPos))
            {
                trickleCredits -= selected.spawnCost;
                spawnedThisBeat++;
            }
        }

        if (debugMode && spawnedThisBeat > 0)
            Debug.Log($"Trickle heartbeat spawned a group of {spawnedThisBeat} enemies.");
    }

    private void TriggerWave()
    {
        if (isWaveActive) return;
        if (currentLivingEnemies >= maxActiveEnemies) return;
        if (enemyList == null || enemyList.Count == 0) return;

        if (activeWaveCoroutine != null)
            StopCoroutine(activeWaveCoroutine);

        activeWaveCoroutine = StartCoroutine(ExecuteWaveOverTime());
    }

    private IEnumerator ExecuteWaveOverTime()
    {
        isWaveActive = true;
        isTricklePaused = true;
        tricklePauseTimer = tricklePauseAfterWave;
        trickleCredits = 0;

        float runTimeMinutes = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.TotalRunTime / 60f : 0;
        float currentSpendingLimit = Mathf.Min(initialMaxExpenditure + (expenditureGrowthPerMinute * runTimeMinutes), absoluteMaxExpenditure);

        float amountSpentThisWave = 0;
        float waveElapsed = 0f;
        int totalSpawned = 0;

        if (debugMode)
            Debug.Log($"=== WAVE START === Budget: {currentSpendingLimit:F0}, Available Credits: {waveCredits:F0}");

        while (waveElapsed < waveDuration &&
               waveCredits > 0 &&
               amountSpentThisWave < currentSpendingLimit)
        {
            float waveProgress = waveElapsed / waveDuration;

            // Determine batch size based on wave progress
            float scaledBatchMax = Mathf.Lerp(minBatchSize, maxBatchSize, waveProgress * batchSizeScaling);
            int batchSize = Mathf.Max(minBatchSize, Random.Range(minBatchSize, Mathf.CeilToInt(scaledBatchMax) + 1));

            // Spawn a batch
            int spawnedInBatch = 0;
            for (int i = 0; i < batchSize; i++)
            {
                if (currentLivingEnemies >= maxActiveEnemies) break;
                if (waveCredits <= 0) break;
                if (amountSpentThisWave >= currentSpendingLimit) break;

                // Get affordable enemies
                _affordableCache.Clear();
                for (int j = 0; j < enemyList.Count; j++)
                {
                    EnemyConfig config = enemyList[j];
                    if (config.canSpawn && config.enemyData != null && config.enemyData.spawnCost <= waveCredits)
                    {
                        _affordableCache.Add(config.enemyData);
                    }
                }

                if (_affordableCache.Count == 0) break;

                EnemyData selectedEnemy = GetWeightedRandomEnemy(_affordableCache);

                // Check if we can afford this without exceeding limit (with small buffer)
                if (amountSpentThisWave + selectedEnemy.spawnCost > currentSpendingLimit + (selectedEnemy.spawnCost * 0.5f))
                    break;

                Vector3 spawnPos = GetDonutSpawnPosition();
                if (spawnPos != Vector3.zero && ExecuteSpawn(selectedEnemy, spawnPos))
                {
                    waveCredits -= selectedEnemy.spawnCost;
                    amountSpentThisWave += selectedEnemy.spawnCost;
                    spawnedInBatch++;
                    totalSpawned++;
                }
            }

            if (debugMode && spawnedInBatch > 0)
                Debug.Log($"Wave batch ({waveProgress * 100:F0}%): Spawned {spawnedInBatch} enemies | Total: {totalSpawned} | Spent: {amountSpentThisWave:F0}/{currentSpendingLimit:F0}");

            // Calculate delay until next batch based on intensity curve
            float intensityValue = spawnIntensityCurve.Evaluate(waveProgress);
            float delayMultiplier = 1f - intensityValue; // Higher intensity = lower delay
            float nextDelay = Mathf.Lerp(minSpawnDelay, maxSpawnDelay, delayMultiplier);

            // Add some randomness to prevent predictability
            nextDelay *= Random.Range(0.8f, 1.2f);

            yield return new WaitForSeconds(nextDelay);
            waveElapsed += nextDelay;
        }

        if (debugMode)
            Debug.Log($"=== WAVE END === Total Spawned: {totalSpawned}, Spent: {amountSpentThisWave:F0}, Remaining Credits: {waveCredits:F0}");

        isWaveActive = false;
        activeWaveCoroutine = null;
    }

    private bool ExecuteSpawn(EnemyData data, Vector3 pos)
    {
        // safety check
        if (EnemyPooler.Instance == null) return false;

        float hpMult = DifficultyManager.Instance != null ? DifficultyManager.Instance.HpMultiplier : 1f;
        float dmgMult = DifficultyManager.Instance != null ? DifficultyManager.Instance.DamageMultiplier : 1f;

        GameObject newEnemy = EnemyPooler.Instance.GetEnemy(
            data,
            pos,
            Quaternion.identity,
            hpMult,
            dmgMult
        );

        if (newEnemy != null)
        {
            currentLivingEnemies++;
            if (debugMode) Debug.Log($"Spawned {data.name}. Living: {currentLivingEnemies}");
            return true;
        }

        return false;
    }

    private void HandleEnemyDeath(EnemyData data)
    {
        currentLivingEnemies--;
        if (currentLivingEnemies < 0) currentLivingEnemies = 0;

        if (data != null)
        {
            float refund = data.spawnCost * refundPercentage;
            waveCredits += refund;
        }
    }

    private EnemyData GetWeightedRandomEnemy(List<EnemyData> list)
    {
        float totalWeight = list.Sum(e => e.selectionWeight);
        float rnd = Random.value * totalWeight;

        foreach (var e in list)
        {
            rnd -= e.selectionWeight;
            if (rnd <= 0f) return e;
        }

        return list[list.Count - 1];
    }

    private Vector3 GetDonutSpawnPosition()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 offset = new Vector3(randomDir.x, 0, randomDir.y) * distance;
            Vector3 candidatePos = playerTransform.position + offset;
            candidatePos.y = playerTransform.position.y;

            if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return Vector3.zero;
    }

    private void UpdateUI()
    {
        if (creditsText != null)
            creditsText.text = $"Credits: {waveCredits:F0}";

        if (trickleCreditsText != null)
        {
            if (isTricklePaused)
                trickleCreditsText.text = $"Trickle Credits: PAUSED ({tricklePauseTimer:F0}s)";
            else
                trickleCreditsText.text = $"Trickle Credits: {trickleCredits:F1}";
        }

        if (livingEnemiesText != null)
            livingEnemiesText.text = $"Enemies: {currentLivingEnemies}/{maxActiveEnemies}";

        if (waveStatusText != null)
        {
            if (isWaveActive)
                waveStatusText.text = "WAVE ACTIVE";
            else
            {
                float currentRunTime = DifficultyManager.Instance != null ? DifficultyManager.Instance.TotalRunTime : 0;
                float currentInterval = (currentRunTime >= waveAccelerationThreshold) ? acceleratedWaveInterval : initialWaveInterval;
                float timeToNextWave = currentInterval - waveTimer;
                waveStatusText.text = $"Next Wave: {timeToNextWave:F0}s";
            }
        }

        if (DifficultyManager.Instance == null) return;

        if (timeText != null)
        {
            float t = DifficultyManager.Instance.TotalRunTime;
            timeText.text = string.Format("{0:00}:{1:00}", Mathf.FloorToInt(t / 60), Mathf.FloorToInt(t % 60));
        }

        if (hpModifierText != null) hpModifierText.text = $"HP: x{DifficultyManager.Instance.HpMultiplier:F1}";
        if (damageModifierText != null) damageModifierText.text = $"DMG: x{DifficultyManager.Instance.DamageMultiplier:F1}";
        if (creditModifierText != null) creditModifierText.text = $"$$$: x{DifficultyManager.Instance.CreditMultiplier:F1}";
    }
}