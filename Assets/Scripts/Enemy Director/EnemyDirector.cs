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

    [Header("Economy")]
    [SerializeField] private float baseCreditsPerSecond = 10f;
    [SerializeField] private float startCredits = 10f;
    [SerializeField] private float refundPercentage = 0.3f;

    [Header("Spawning Rules")]
    [SerializeField] private float spawnCheckInterval = 4.5f;
    [SerializeField] private int maxActiveEnemies = 60;

    [Header("Positioning")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnRadius = 6f;
    [SerializeField] private float maxSpawnRadius = 16f;

    // --- NEW WRAPPER CLASS ---
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

    [Header("UI - Modifiers")]
    [SerializeField] private TextMeshProUGUI hpModifierText;
    [SerializeField] private TextMeshProUGUI damageModifierText;
    [SerializeField] private TextMeshProUGUI creditModifierText;

    private float currentCredits;
    private int currentLivingEnemies;
    private float waveTimer;

    private void Awake()
    {
        Instance = this;
        currentCredits = startCredits;
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

        // Safety check if DifficultyManager is missing
        float multiplier = DifficultyManager.Instance != null ? DifficultyManager.Instance.CreditMultiplier : 1f;
        float currentRate = baseCreditsPerSecond * multiplier;

        currentCredits += currentRate * Time.deltaTime;
        waveTimer += Time.deltaTime;

        if (waveTimer >= spawnCheckInterval)
        {
            AttemptSpawnWave();
            waveTimer = 0f;
        }
    }

    private void HandleEnemyDeath(EnemyData data)
    {
        currentLivingEnemies--;
        if (currentLivingEnemies < 0) currentLivingEnemies = 0;

        if (data != null)
        {
            float refund = data.spawnCost * refundPercentage;
            currentCredits += refund;
        }
    }

    private void AttemptSpawnWave()
    {
        if (currentLivingEnemies >= maxActiveEnemies) return;
        if (enemyList == null || enemyList.Count == 0) return;

        int attempts = 0;

        while (currentCredits > 0 && currentLivingEnemies < maxActiveEnemies && attempts < 20)
        {
            attempts++;

            // Filter list by Enabled Checkbox AND Cost
            List<EnemyData> affordable = enemyList
                .Where(x => x.canSpawn && x.enemyData != null && x.enemyData.spawnCost <= currentCredits)
                .Select(x => x.enemyData)
                .ToList();

            if (affordable.Count == 0) break;

            EnemyData selectedEnemy = GetWeightedRandomEnemy(affordable);
            Vector3 spawnPos = GetDonutSpawnPosition();

            if (spawnPos != Vector3.zero)
            {
                currentCredits -= selectedEnemy.spawnCost;

                float hpMult = 1f;
                float dmgMult = 1f;

                if (DifficultyManager.Instance != null)
                {
                    hpMult = DifficultyManager.Instance.HpMultiplier;
                    dmgMult = DifficultyManager.Instance.DamageMultiplier;
                }

                GameObject newEnemy = EnemyPooler.Instance.GetEnemy(
                    selectedEnemy,
                    spawnPos,
                    Quaternion.identity,
                    hpMult,
                    dmgMult
                );

                if (newEnemy != null)
                {
                    currentLivingEnemies++;
                }
            }
        }
    }

    private EnemyData GetWeightedRandomEnemy(List<EnemyData> list)
    {
        float totalWeight = 0f;

        foreach (var e in list)
            totalWeight += e.selectionWeight;

        float rnd = Random.value * totalWeight;

        foreach (var e in list)
        {
            rnd -= e.selectionWeight;
            if (rnd <= 0f)
                return e;
        }

        return list[list.Count - 1]; // fallback
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
            creditsText.text = $"Credits: {currentCredits:F0}";

        if (DifficultyManager.Instance == null) return;

        if (timeText != null)
        {
            float t = DifficultyManager.Instance.TotalRunTime;
            int minutes = Mathf.FloorToInt(t / 60);
            int seconds = Mathf.FloorToInt(t % 60);
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        if (hpModifierText != null)
            hpModifierText.text = $"HP: x{DifficultyManager.Instance.HpMultiplier:F1}";

        if (damageModifierText != null)
            damageModifierText.text = $"DMG: x{DifficultyManager.Instance.DamageMultiplier:F1}";

        if (creditModifierText != null)
            creditModifierText.text = $"$$$: x{DifficultyManager.Instance.CreditMultiplier:F1}";
    }
}