using System.Collections.Generic;
using System.Linq; 
using UnityEngine;
using TMPro;

public class EnemyDirector : MonoBehaviour
{
    public static EnemyDirector Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool debugMode = false; 

    [Header("Economy")]
    [SerializeField] private float creditsPerSecond = 10f;
    [SerializeField] private float startCredits = 10f;
    [SerializeField] private float refundPercentage = 0.3f;

    [Header("Spawning Rules")]
    [SerializeField] private float spawnCheckInterval = 4.5f;
    [SerializeField] private int maxActiveEnemies = 60;

    [Header("Positioning")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float minSpawnRadius = 6f;
    [SerializeField] private float maxSpawnRadius = 16f;

    [Header("Data")]
    public List<EnemyData> availableEnemies;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI creditsText;

    private float currentCredits;
    private int currentLivingEnemies;
    private float timer;

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
            else if (debugMode) Debug.LogError("? [Director] No Player found! Ensure the player is tagged 'Player' or assigned in Inspector.");
        }

        if (debugMode && (availableEnemies == null || availableEnemies.Count == 0))
        {
            Debug.LogError("? [Director] 'Available Enemies' list is empty!");
        }
    }

    private void OnEnable() => EnemyHealth.OnEnemyDeath += HandleEnemyDeath;
    private void OnDisable() => EnemyHealth.OnEnemyDeath -= HandleEnemyDeath;

    private void Update()
    {
        if (playerTransform == null) return;

        UpdateUI();

        currentCredits += creditsPerSecond * Time.deltaTime;
        timer += Time.deltaTime;

        if (timer >= spawnCheckInterval)
        {
            AttemptSpawnWave();
            timer = 0f;
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
            if (debugMode) Debug.Log($"?? [Director] Enemy Died. Refunded {refund}. Current Credits: {currentCredits}");
        }
    }

    private void AttemptSpawnWave()
    {
        if (currentLivingEnemies >= maxActiveEnemies) return;

        int attempts = 0;
        int spawnedCount = 0;

        // Failsafe: Don't run if we have no enemy data to work with
        if (availableEnemies == null || availableEnemies.Count == 0) return;

        while (currentCredits > 0 && currentLivingEnemies < maxActiveEnemies && attempts < 20)
        {
            attempts++;

            // 1. Find Affordable Enemies
            List<EnemyData> affordable = availableEnemies.Where(x => x.spawnCost <= currentCredits).ToList();

            if (affordable.Count == 0)
            {
                if (debugMode) Debug.Log($"?? [Director] Too poor to buy wave. Credits: {currentCredits:F1}. Cheapest Enemy: {availableEnemies.Min(e => e.spawnCost)}");
                break; // Exit loop, wait for more money
            }

            // 2. Select Enemy
            EnemyData selectedEnemy = affordable[Random.Range(0, affordable.Count)];

            // 3. Find Position
            Vector3 spawnPos = GetDonutSpawnPosition();

            if (spawnPos != Vector3.zero)
            {
                // 4. Pay & Spawn
                currentCredits -= selectedEnemy.spawnCost;

                GameObject newEnemy = EnemyPooler.Instance.GetEnemy(selectedEnemy, spawnPos, Quaternion.identity);

                if (newEnemy != null)
                {
                    currentLivingEnemies++;
                    spawnedCount++;
                    if (debugMode) Debug.Log($"? [Director] Spawned {selectedEnemy.name}. Remaining Credits: {currentCredits:F1}");
                }
            }
            else
            {
                if (debugMode) Debug.LogWarning("?? [Director] Could not find valid spawn position (Hit wall or invalid area).");
            }
        }
    }

    private Vector3 GetDonutSpawnPosition()
    {
        // Try 10 times to find a spot
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnRadius, maxSpawnRadius);
            Vector3 offset = new Vector3(randomDir.x, 0, randomDir.y) * distance;

            Vector3 candidatePos = playerTransform.position + offset;

            candidatePos.y = playerTransform.position.y;

            

            return candidatePos;
        }

        return Vector3.zero;
    }


    private void UpdateUI()
    {
        if (creditsText != null)
        {
            creditsText.text = $"Credits: {currentCredits:F0}";
        }
    }
}