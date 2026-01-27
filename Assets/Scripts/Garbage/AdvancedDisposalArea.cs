using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedDisposalArea : MonoBehaviour
{
    [Header("Disposal Rules")]
    [SerializeField] private int maxBullseyeAttempts = 3;
    [SerializeField] private float stoppedVelocityThreshold = 0.05f;
    [SerializeField] private float requiredStillTime = 0.5f;
    [SerializeField] private float sinkSpeed = 2f;
    [SerializeField] private float sinkDepth = 2f;

    [Header("Edge Case Detection")]
    [SerializeField] private float rimHeightThreshold = 0.3f;
    [SerializeField] private float bullseyeRadius = 1.0f;

    [Header("Pulse Processing")]
    [SerializeField] private float totalProcessTime = 2.0f;
    [SerializeField] private int pulses = 4;

    [Header("Refund / Change System")]
    [SerializeField] private GameObject refundProjectilePrefab;

    [Header("UI Setup")]
    [SerializeField] private GameObject uiPrefab;
    [SerializeField] private Transform uiAnchor;
    [SerializeField] private Vector3 uiRotation = new Vector3(80f, 0f, 0f);
    private DisposalUI _uiInstance;

    [Header("References")]
    [SerializeField] private GameObject outerRing;
    [SerializeField] private GameObject bullseyeRing;
    [SerializeField] private DisposableRewardLogic rewardLogic;

    [Header("Visuals/Audio")]
    [SerializeField] private SoundDef garbageAcceptedSound;
    [SerializeField] private SoundDef processingMoneySound;
    [SerializeField] private float pitchStep = 0.2f;

    private int _currentAccumulatedValue = 0;
    private int _bullseyeAttemptsUsed = 0;
    private bool _bullseyeHitSucceeded = false;
    private bool _costReached = false;
    private bool _isAreaActive = true;

    private List<GarbageBundle> _trackedBundles = new List<GarbageBundle>();
    private HashSet<GarbageBundle> _processedBundles = new HashSet<GarbageBundle>();

    private void Start()
    {
        if (uiPrefab != null && uiAnchor != null)
        {
            GameObject go = Instantiate(uiPrefab, uiAnchor.position, Quaternion.Euler(uiRotation), uiAnchor);
            _uiInstance = go.GetComponent<DisposalUI>();
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (_uiInstance == null) return;
        int attemptsLeft = maxBullseyeAttempts - _bullseyeAttemptsUsed;
        _uiInstance.UpdateDisplay(_currentAccumulatedValue, rewardLogic.GetCost(), attemptsLeft, _bullseyeHitSucceeded);
    }

    public void NotifyTriggerEnter(Collider other, GameObject sender)
    {
        if (!_isAreaActive) return;
        GarbageBundle bundle = other.GetComponent<GarbageBundle>();
        if (bundle != null && !_processedBundles.Contains(bundle))
        {
            if (!_trackedBundles.Contains(bundle))
            {
                _trackedBundles.Add(bundle);
                StartCoroutine(MonitorMovement(bundle));
            }
        }
    }

    public void NotifyTriggerExit(Collider other, GameObject sender)
    {
        GarbageBundle bundle = other.GetComponent<GarbageBundle>();
        if (bundle != null)
        {
            Rigidbody rb = bundle.GetComponent<Rigidbody>();
            if (rb != null && rb.linearVelocity.y > 0.1f) _trackedBundles.Remove(bundle);
        }
    }

    private IEnumerator MonitorMovement(GarbageBundle bundle)
    {
        Rigidbody rb = bundle.GetComponent<Rigidbody>();
        float stillTimer = 0f;
        while (bundle != null && _trackedBundles.Contains(bundle))
        {
            bool isMovingSlowly = rb.linearVelocity.magnitude <= stoppedVelocityThreshold;
            bool isNotFalling = Mathf.Abs(rb.linearVelocity.y) < 0.02f;
            if (isMovingSlowly && isNotFalling) stillTimer += Time.deltaTime;
            else stillTimer = 0f;

            if (stillTimer >= requiredStillTime)
            {
                ProcessLandedBundle(bundle);
                yield break;
            }
            yield return null;
        }
    }

    private void ProcessLandedBundle(GarbageBundle bundle)
    {
        if (bundle == null || _processedBundles.Contains(bundle)) return;

        Vector3 bundlePos = bundle.transform.position;
        Vector3 bullseyeCenter = bullseyeRing.transform.position;
        float distToCenter = Vector2.Distance(new Vector2(bundlePos.x, bundlePos.z), new Vector2(bullseyeCenter.x, bullseyeCenter.z));
        bool isStuckOnRim = (bundlePos.y - bullseyeCenter.y) > rimHeightThreshold;
        bool isInsideBullseyeZone = distToCenter <= bullseyeRadius;

        bool validHit = false;

        if (!_bullseyeHitSucceeded && _bullseyeAttemptsUsed < maxBullseyeAttempts && isInsideBullseyeZone && !isStuckOnRim)
        {
            _bullseyeHitSucceeded = true;
            _bullseyeAttemptsUsed++;
            validHit = true;
            StartCoroutine(SinkObject(bullseyeRing));
        }
        else if (outerRing.GetComponent<Collider>().bounds.Contains(bundlePos) || isInsideBullseyeZone)
        {
            validHit = true;
            if (!_bullseyeHitSucceeded)
            {
                _bullseyeAttemptsUsed++;
                if (_bullseyeAttemptsUsed >= maxBullseyeAttempts) StartCoroutine(SinkObject(bullseyeRing));
            }
        }

        if (validHit)
        {
            _processedBundles.Add(bundle);
            SoundManager.Instance.Play(garbageAcceptedSound, transform.position);
            StartCoroutine(ProcessBundlePulsed(bundle));
        }
        else { StartCoroutine(MonitorMovement(bundle)); }
    }

    private IEnumerator ProcessBundlePulsed(GarbageBundle bundle)
    {
        List<GarbageData> bundleContents = new List<GarbageData>(bundle.GetContents());
        int targetCost = rewardLogic.GetCost();

        // Determine how much value we actually take from this bundle
        int remainingNeeded = targetCost - _currentAccumulatedValue;
        int totalBundleValue = bundle.GetTotalValue();
        int valueToConsume = Mathf.Min(totalBundleValue, remainingNeeded);

        // Logic for splitting "Change":
        List<GarbageData> refundList = new List<GarbageData>();
        int currentTaken = 0;

        // Iterate backwards through contents to consume items until we hit valueToConsume
        for (int i = bundleContents.Count - 1; i >= 0; i--)
        {
            if (currentTaken >= valueToConsume)
            {
                // We've already paid the cost, return the rest of the items
                refundList.Add(bundleContents[i]);
                bundleContents.RemoveAt(i);
            }
            else
            {
                currentTaken += bundleContents[i].value;
                // If this item made us go OVER, we should technically return the overflow 
                // but since these are distinct items, we just consume the item.
            }
        }

        // Processing Visuals
        if (bundle.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = true;
        if (bundle.TryGetComponent<Collider>(out Collider col)) col.enabled = false;

        float timePerPulse = totalProcessTime / pulses;
        int valPerPulse = Mathf.CeilToInt((float)valueToConsume / pulses);

        for (int i = 1; i <= pulses; i++)
        {
            yield return new WaitForSeconds(timePerPulse);
            if (bundle == null) break;

            _currentAccumulatedValue += valPerPulse;

            // Notify LevelObjectiveManager garbage is accumulated towards spawning a prot 

            if (LevelObjectiveManager.Instance != null)
            {
                LevelObjectiveManager.Instance.AddProgress(valPerPulse);
            }
            //////////////
            
            bundle.ShrinkToPercentage(1f - ((float)i / pulses));

            if (processingMoneySound != null)
            {
                float currentPitch = 1.0f + ((i - 1) * pitchStep);
                SoundManager.Instance.Play(processingMoneySound, transform.position, currentPitch);
            }

            UpdateUI();

            if (_currentAccumulatedValue >= targetCost && !_costReached)
            {
                _costReached = true;
                _isAreaActive = false;
                if (_uiInstance != null) _uiInstance.Hide();
                rewardLogic.TriggerReward(_bullseyeHitSucceeded);
                StartCoroutine(SinkObject(outerRing));
                if (bullseyeRing.activeSelf) StartCoroutine(SinkObject(bullseyeRing));
            }
        }

        // --- SPAWN REFUND SQUIGGLIES ---
        if (refundList.Count > 0)
        {
            SpawnRefunds(refundList, bundle.transform.position);
        }

        if (bundle != null)
        {
            _trackedBundles.Remove(bundle);
            _processedBundles.Remove(bundle);
            Destroy(bundle.gameObject);
        }
    }

    private void SpawnRefunds(List<GarbageData> itemsToReturn, Vector3 position)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || refundProjectilePrefab == null) return;

        // We can spawn one projectile per item for a "swarm" effect, 
        // or one projectile for the whole list. Let's do one per item for the visual.
        foreach (var data in itemsToReturn)
        {
            GameObject proj = Instantiate(refundProjectilePrefab, position, Quaternion.identity);
            if (proj.TryGetComponent(out GarbageRefundProjectile script))
            {
                script.Setup(player.transform, new List<GarbageData> { data });
            }
        }
    }

    private IEnumerator SinkObject(GameObject target)
    {
        if (target == null || !target.activeInHierarchy) yield break;
        if (target.TryGetComponent(out Collider mainCol)) mainCol.enabled = false;
        foreach (Collider c in target.GetComponentsInChildren<Collider>()) c.enabled = false;

        Vector3 startPos = target.transform.position;
        Vector3 endPos = startPos + (Vector3.down * sinkDepth);
        float elapsed = 0;
        float duration = sinkDepth / sinkSpeed;
        while (elapsed < duration)
        {
            if (target == null) yield break;
            target.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.SetActive(false);
    }
}