using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.AI;

public class LevelObjectiveManager : MonoBehaviour
{
    public static LevelObjectiveManager Instance { get; private set; }

    [Header("Configuration")]
    [Tooltip("Default only. Overwritten at runtime by GameManager.GetTargetGoalForCurrentLevel() when the scene loads. Set level goals on GameManager (Level Goals list).")]
    [SerializeField] private int targetGarbageValue = 500;

    [Header("Portal Spawning")]
    [Tooltip("Distance in front of player to spawn portal(s).")]
    [SerializeField] private float portalSpawnDistance = 6f;
    [Tooltip("Horizontal (X) distance between the two portals. Continue Run = right, Return to Hub = left. Same Y and Z for both.")]
    [SerializeField] private float portalSpacingX = 20f;

    [Header("Portal Destinations")]
    [Tooltip("Each SO defines destination (where we go) and prefab (visual). Assign the three destination assets.")]
    [SerializeField] private PortalDestinationSO continueRunDestination;
    [SerializeField] private PortalDestinationSO returnToHubDestination;
    [SerializeField] private PortalDestinationSO endRunDestination;

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
            _baseScale = meterContainer.localScale;

        if (GameManager.Instance != null)
            GameManager.Instance.OnSceneReady += HandleSceneReady;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnSceneReady -= HandleSceneReady;
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        UpdateUI();
    }

    private void HandleSceneReady()
    {
        if (GameManager.Instance != null)
            targetGarbageValue = GameManager.Instance.GetTargetGoalForCurrentLevel();
        UpdateUI();
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGarbageDeposited(value);
        }

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

        Debug.Log("Objective Complete! Spawning Portal(s)...");
        SpawnPortalsNearPlayer();
    }

    private void SpawnPortalsNearPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;


        Vector3 basePos = player.transform.position + (player.transform.forward * portalSpawnDistance);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(basePos, out hit, 5.0f, NavMesh.AllAreas))
        {
            basePos = hit.position;
        }
        else
        {
            basePos.y = player.transform.position.y;
        }

        bool isLastLevel = GameManager.Instance != null && GameManager.Instance.IsCurrentLevelLastInSequence();

        if (isLastLevel)
        {
            SpawnSinglePortal(basePos, endRunDestination);
        }
        else
        {
            float halfSpacing = portalSpacingX * 0.5f;
            Vector3 leftPos = basePos + Vector3.left * halfSpacing;
            Vector3 rightPos = basePos + Vector3.right * halfSpacing;
            SpawnSinglePortal(leftPos, returnToHubDestination);
            SpawnSinglePortal(rightPos, continueRunDestination);
        }
    }

    private void SpawnSinglePortal(Vector3 position, PortalDestinationSO destinationSO)
    {
        if (destinationSO == null)
        {
            Debug.LogWarning("[LevelObjectiveManager] Portal destination SO missing. Skipping spawn.");
            return;
        }

        GameObject prefab = destinationSO.Prefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[LevelObjectiveManager] Portal destination '{destinationSO.name}' has no prefab assigned. Skipping spawn.");
            return;
        }

        GameObject go = Instantiate(prefab, position, Quaternion.identity);
        Portal portal = go.GetComponent<Portal>();
        if (portal != null)
        {
            portal.SetDestination(destinationSO);
        }
        else
        {
            Debug.LogWarning("[LevelObjectiveManager] Portal prefab has no Portal component.");
        }
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