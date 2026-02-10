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

    [Header("Area Geometry")]
    [SerializeField] private float outerHitRadius = 2.0f;
    [SerializeField] private float outerHeightTolerance = 0.3f;
    [SerializeField] private float trackingRadius = 3.0f;

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

    private int _currentAccumulatedCapacity = 0;
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
        _uiInstance.UpdateDisplay(_currentAccumulatedCapacity, rewardLogic.GetCost(), attemptsLeft, _bullseyeHitSucceeded);
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
            // Intentionally left blank: tracking is managed in MonitorMovement
            // based on distance from the disposal area's center.
        }
    }

    private IEnumerator MonitorMovement(GarbageBundle bundle)
    {
        Rigidbody rb = bundle.GetComponent<Rigidbody>();
        float stillTimer = 0f;
        while (bundle != null && _trackedBundles.Contains(bundle))
        {
            if (bullseyeRing == null)
            {
                yield break;
            }

            Vector3 center = bullseyeRing.transform.position;
            Vector3 pos = bundle.transform.position;
            float distToCenter = Vector2.Distance(
                new Vector2(pos.x, pos.z),
                new Vector2(center.x, center.z)
            );

            // Stop tracking once the bundle is clearly far away from the disposal area.
            if (distToCenter > trackingRadius)
            {
                _trackedBundles.Remove(bundle);
                yield break;
            }

            // Separate horizontal and vertical motion; be more forgiving to tiny jitters.
            Vector2 horizontalVel = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z);
            float horizontalSpeed = horizontalVel.magnitude;
            float verticalSpeed = rb.linearVelocity.y;

            bool isMovingSlowlyHorizontally = horizontalSpeed <= stoppedVelocityThreshold;
            bool isNotFallingFast = verticalSpeed > -0.2f && verticalSpeed < 0.2f;

            if (isMovingSlowlyHorizontally && isNotFallingFast)
            {
                // Fully still: accumulate time.
                stillTimer += Time.deltaTime;
            }
            else
            {
                // Soft decay instead of hard reset so small bumps don't completely restart.
                stillTimer = Mathf.Max(0f, stillTimer - Time.deltaTime * 2f);
            }

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
        float distToCenter = Vector2.Distance(
            new Vector2(bundlePos.x, bundlePos.z),
            new Vector2(bullseyeCenter.x, bullseyeCenter.z)
        );
        bool isStuckOnRim = (bundlePos.y - bullseyeCenter.y) > rimHeightThreshold;
        bool isInsideBullseyeZone = distToCenter <= bullseyeRadius;

        // General outer-area hit: inside a slightly padded radius and near the ring plane.
        bool isInsideOuterZone =
            distToCenter <= outerHitRadius &&
            Mathf.Abs(bundlePos.y - bullseyeCenter.y) <= outerHeightTolerance;

        bool validHit = false;

        if (!_bullseyeHitSucceeded && _bullseyeAttemptsUsed < maxBullseyeAttempts && isInsideBullseyeZone && !isStuckOnRim)
        {
            _bullseyeHitSucceeded = true;
            _bullseyeAttemptsUsed++;
            validHit = true;
            StartCoroutine(SinkObject(bullseyeRing));
        }
        // General hit: either inside bullseye zone or within our outer area tolerance.
        else if (isInsideOuterZone || isInsideBullseyeZone)
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
        else
        {
            StartCoroutine(MonitorMovement(bundle));
        }
    }

    private void OnDrawGizmos()
    {
        if (bullseyeRing == null) return;

        Vector3 center = bullseyeRing.transform.position;

        // Outer hit radius (where we consider normal hits valid)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, outerHitRadius);

        // Tracking radius (beyond this we stop tracking bundles)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, trackingRadius);
    }

    private IEnumerator ProcessBundlePulsed(GarbageBundle bundle)
    {
        List<GarbageData> bundleContents = new List<GarbageData>(bundle.GetContents());
        int targetCost = rewardLogic.GetCost();

        // Determine how much capacity we actually take from this bundle
        int remainingNeeded = targetCost - _currentAccumulatedCapacity;
        int totalBundleCapacity = bundle.GetTotalCapacity();
        int capacityToConsume = Mathf.Min(totalBundleCapacity, remainingNeeded);

        // Logic for splitting "Change":
        List<GarbageData> refundList = new List<GarbageData>();
        int currentTaken = 0;

        // Iterate backwards through contents to consume items until we hit capacityToConsume
        for (int i = bundleContents.Count - 1; i >= 0; i--)
        {
            if (currentTaken >= capacityToConsume)
            {
                // We've already paid the cost, return the rest of the items
                refundList.Add(bundleContents[i]);
                bundleContents.RemoveAt(i);
            }
            else
            {
                currentTaken += bundleContents[i].capacityCost;
            }
        }

        // Processing Visuals
        if (bundle.TryGetComponent<Rigidbody>(out Rigidbody rb)) rb.isKinematic = true;
        if (bundle.TryGetComponent<Collider>(out Collider col)) col.enabled = false;

        float timePerPulse = totalProcessTime / pulses;
        int capacityPerPulse = Mathf.CeilToInt((float)capacityToConsume / pulses);

        for (int i = 1; i <= pulses; i++)
        {
            yield return new WaitForSeconds(timePerPulse);
            if (bundle == null) break;

            _currentAccumulatedCapacity += capacityPerPulse;

            if (LevelObjectiveManager.Instance != null)
            {
                LevelObjectiveManager.Instance.AddProgress(capacityPerPulse);
            }
            
            bundle.ShrinkToPercentage(1f - ((float)i / pulses));

            if (processingMoneySound != null)
            {
                float currentPitch = 1.0f + ((i - 1) * pitchStep);
                SoundManager.Instance.Play(processingMoneySound, transform.position, currentPitch);
            }

            UpdateUI();

            if (_currentAccumulatedCapacity >= targetCost && !_costReached)
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