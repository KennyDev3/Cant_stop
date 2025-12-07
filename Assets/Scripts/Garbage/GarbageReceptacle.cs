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
        int totalValue = bundle.GetTotalValue();

        int valuePerPulse = Mathf.CeilToInt((float)totalValue / pulses);

        float timePerPulse = totalProcessTime / pulses;

        for (int i = 1; i <= pulses; i++)
        {
            yield return new WaitForSeconds(timePerPulse);

            if (bundle == null) break;

            float progress = (float)i / pulses;
            float targetSizeParams = 1f - progress;

            bundle.ShrinkToPercentage(targetSizeParams);

            // 2. Award Partial Money & Show Text
            GivePlayerMoney(valuePerPulse);
            SpawnFloatingText(valuePerPulse);

            // Optional: Play Sound Pulse Here
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

        // Use spawn point if assigned, otherwise use own position + up
        Vector3 spawnPos = textSpawnPoint != null ? textSpawnPoint.position : transform.position + Vector3.up;

        GameObject textObj = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
        FloatingMoneyText textScript = textObj.GetComponent<FloatingMoneyText>();

        if (textScript != null)
        {
            textScript.Initialize($"+{amount}");
        }

    }
}