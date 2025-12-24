using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class EnemyDirector : MonoBehaviour
{
    public static EnemyDirector Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    [Header("Economy - Wave (Primary)")]
    [SerializeField] private float baseCreditsPerSecond = 10f;
    [SerializeField] private float startCredits = 10f;
    [SerializeField] private float refundPercentage = 0.3f;
    [SerializeField] private float spawnCheckInterval = 4.5f;

    [Header("Wave Scaling")]
    [SerializeField] private float initialMaxExpenditure = 200f; 
    [SerializeField] private float expenditureGrowthPerMinute = 100f; 
    [SerializeField] private float absoluteMaxExpenditure = 10000f; 

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

    // Internal State
    private float waveCredits;
    private float trickleCredits;
    private float tricklePauseTimer;
    private bool isTricklePaused;

    private int currentLivingEnemies;
    private float waveTimer;

    private void Awake()
    {
        Instance = this;
        waveCredits = startCredits;
        trickleCredits = 0f;
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    private void OnEnable() => EnemyHealth.OnEnemyDeath += HandleEnemyDeath;
    private void OnDisable() => EnemyHealth.OnEnemyDeath -= HandleEnemyDeath;

    private void Update()
    {
        if (playerTransform == null) return;

        UpdateUI();
        HandleEconomyAndTimers();

        // Wave Interval Check
        waveTimer += Time.deltaTime;
        if (waveTimer >= spawnCheckInterval)
        {
            AttemptSpawnWave();
            waveTimer = 0f;
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

    private void AttemptSpawnWave()
    {
        if (currentLivingEnemies >= maxActiveEnemies) return;
        if (enemyList == null || enemyList.Count == 0) return;

        float runTimeMinutes = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.TotalRunTime / 60f : 0;
        float currentSpendingLimit = Mathf.Min(initialMaxExpenditure + (expenditureGrowthPerMinute * runTimeMinutes), absoluteMaxExpenditure);

        float amountSpentThisWave = 0;
        bool spawnedAnything = false;
        int attempts = 0;

        while (waveCredits > 0 &&
               amountSpentThisWave < currentSpendingLimit &&
               currentLivingEnemies < maxActiveEnemies &&
               attempts < 20)
        {
            attempts++;

            // Caching Enemy List
            _affordableCache.Clear();
            for (int i = 0; i < enemyList.Count; i++)
            {
                EnemyConfig config = enemyList[i];
                if (config.canSpawn && config.enemyData != null && config.enemyData.spawnCost <= waveCredits)
                {
                    _affordableCache.Add(config.enemyData);
                }
            }

            if (_affordableCache.Count == 0) break;

            EnemyData selectedEnemy = GetWeightedRandomEnemy(_affordableCache);

            if (amountSpentThisWave + selectedEnemy.spawnCost > currentSpendingLimit + (selectedEnemy.spawnCost * 0.5f))
                break;

            Vector3 spawnPos = GetDonutSpawnPosition();
            if (spawnPos != Vector3.zero && ExecuteSpawn(selectedEnemy, spawnPos))
            {
                waveCredits -= selectedEnemy.spawnCost;
                amountSpentThisWave += selectedEnemy.spawnCost;
                spawnedAnything = true;
            }
        }

        if (spawnedAnything)
        {
            isTricklePaused = true;
            tricklePauseTimer = tricklePauseAfterWave;
            trickleCredits = 0;
        }
    }

    private bool ExecuteSpawn(EnemyData data, Vector3 pos)
    {
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