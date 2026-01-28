using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.AI;

public class LevelObjectiveManager : MonoBehaviour
{
    public static LevelObjectiveManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private int targetGarbageValue = 500;

    [Header("Portal Spawning")]
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private float portalSpawnDistance = 6f;

    [Header("UI References")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private RectTransform meterContainer;
    [SerializeField] private float popScaleAmount = 1.2f;

    private int _currentDepositedValue = 0;
    private bool _isObjectiveComplete = false;
    private Coroutine _popCoroutine;

    private Vector3 _baseScale;


    private void Awake()
    {
        
        Instance = this;

        if (meterContainer != null)
        {
            _baseScale = meterContainer.localScale;
        }
    }

    private void Start()
    {
        UpdateUI();

        if (GameManager.Instance != null)
        {
            // We'll add this method to GameManager in a moment
            targetGarbageValue = GameManager.Instance.GetTargetGoalForCurrentLevel();
        }
    }

    private void UpdateUI()
    {
        float progress = Mathf.Clamp01((float)_currentDepositedValue / targetGarbageValue);

        if (progressSlider != null) progressSlider.value = progress;
        if (progressText != null) progressText.text = $"{_currentDepositedValue} / {targetGarbageValue}";
    }

    public void AddProgress(int value)
    {
        if (_isObjectiveComplete) return;

        _currentDepositedValue += value;

        if (_popCoroutine != null) StopCoroutine(_popCoroutine);
        _popCoroutine = StartCoroutine(AnimatePop());

        UpdateUI();

        if (_currentDepositedValue >= targetGarbageValue)
        {
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        _isObjectiveComplete = true;
        _currentDepositedValue = targetGarbageValue;
        UpdateUI();

        Debug.Log("Objective Complete! Spawning Portal...");
        SpawnPortalNearPlayer();
    }


    private void SpawnPortalNearPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector3 spawnPos = player.transform.position + (player.transform.forward * portalSpawnDistance);

        // Ensure portal spawns on the NavMesh so players can walk to it
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPos, out hit, 5.0f, NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }
        else
        {
            // Fallback: spawn on player's y level
            spawnPos.y = player.transform.position.y;
        }

        Instantiate(portalPrefab, spawnPos, Quaternion.LookRotation(-player.transform.forward));
    }

    private IEnumerator AnimatePop()
    {
        if (meterContainer == null) yield break;

        Vector3 originalScale = _baseScale;
        Vector3 targetScale = _baseScale * popScaleAmount;

        float duration = 0.1f;

        // Scale Up
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            meterContainer.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // Scale Down
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            meterContainer.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        // Ensure we end exactly on the base scale
        meterContainer.localScale = originalScale;
    }



}