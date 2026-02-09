using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarbageReceptacle : MonoBehaviour
{
    [Header("Processing Settings")]
    [Tooltip("Total time to consume the garbage.")]
    [SerializeField] private float totalProcessTime = 2.0f;

    [Tooltip("How many pulses/payments to split the process into.")]
    [SerializeField] private int pulses = 4;

    [Header("Visuals")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private Transform textSpawnPoint;

    [Header("Audio")]
    [SerializeField] private SoundDef processingGarbageSound;
    [SerializeField] private SoundDef processingMoneySound; 
    [Tooltip("How much the pitch increases per pulse (e.g., 0.15 or 0.2)")]
    [SerializeField] private float pitchStep = 0.2f;

    [Header("References")]
    [SerializeField] private string playerTag = "Player";

    private Dictionary<GarbageBundle, Coroutine> _pendingBundles = new Dictionary<GarbageBundle, Coroutine>();

    private void OnTriggerEnter(Collider other)
    {
        GarbageBundle bundle = other.GetComponent<GarbageBundle>();
        if (bundle != null && !_pendingBundles.ContainsKey(bundle))
        {

            Coroutine routine = StartCoroutine(ProcessBundleRoutine(bundle));
            _pendingBundles.Add(bundle, routine);

            SoundManager.Instance.Play(processingGarbageSound, transform.position);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GarbageBundle bundle = other.GetComponent<GarbageBundle>();
        if (bundle != null && _pendingBundles.ContainsKey(bundle))
        {
            if (_pendingBundles[bundle] != null) StopCoroutine(_pendingBundles[bundle]);
            _pendingBundles.Remove(bundle);
        }
    }

    private IEnumerator ProcessBundleRoutine(GarbageBundle bundle)
    {
        int totalCapacity = bundle.GetTotalCapacity();
        int capacityPerPulse = Mathf.CeilToInt((float)totalCapacity / pulses);
        float timePerPulse = totalProcessTime / pulses;

        for (int i = 1; i <= pulses; i++)
        {
            yield return new WaitForSeconds(timePerPulse);

            if (bundle == null) break;

            // Visuals & Math
            float progress = (float)i / pulses;
            float targetSizeParams = 1f - progress;

            bundle.ShrinkToPercentage(targetSizeParams);
            GivePlayerMoney(capacityPerPulse);
            SpawnFloatingText(capacityPerPulse);

            // Audio 
            if (processingMoneySound != null)
            {
                
                float currentPitch = 1.0f + ((i - 1) * pitchStep);

                SoundManager.Instance.Play(processingMoneySound, transform.position, currentPitch);
            }
        }

        // Cleanup
        if (bundle != null)
        {
            _pendingBundles.Remove(bundle);
            Destroy(bundle.gameObject);
        }
    }

    private void GivePlayerMoney(int amount)
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            var handler = player.GetComponent<PlayerGarbageHandler>();
            if (handler != null) handler.AddMoney(amount);
        }
    }

    private void SpawnFloatingText(int amount)
    {
        if (floatingTextPrefab == null) return;

        Vector3 spawnPos = textSpawnPoint != null ? textSpawnPoint.position : transform.position + Vector3.up;

        GameObject textObj = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
        FloatingMoneyText textScript = textObj.GetComponent<FloatingMoneyText>();

        if (textScript != null)
        {
            textScript.Initialize($"+{amount}");
        }
    }
}