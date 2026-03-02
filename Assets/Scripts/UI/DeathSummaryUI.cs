using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Extends the existing death panel to show full run summary stats and to route a Return to Hub button.
/// </summary>
public class DeathSummaryUI : MonoBehaviour
{
    [Header("Text Fields")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI garbageText;
    [SerializeField] private TextMeshProUGUI retrievalPercentageText;

    [Header("Resources (fixed 3 types)")]
    [SerializeField] private ResourceSO resource1;
    [SerializeField] private ResourceSO resource2;
    [SerializeField] private ResourceSO resource3;
    [SerializeField] private TextMeshProUGUI collectedLineText;
    [SerializeField] private TextMeshProUGUI retrievedLineText;

    private RunSummaryData _cachedSummary;

    private void OnEnable()
    {
        if (GameManager.Instance == null) return;

        // Snapshot current run state (including PlayerResourceHolder) before building the summary.
        GameManager.Instance.CollectRunState();
        _cachedSummary = GameManager.Instance.BuildRunSummary(RunEndType.Death);
        GameManager.Instance.SetPendingRunEnd(RunEndType.Death, _cachedSummary);
        BindSummary(_cachedSummary);
    }

    private void BindSummary(RunSummaryData summary)
    {
        if (summary == null) return;

        if (timeText != null)
            timeText.text = FormatTime(summary.TotalRunTimeSeconds);

        if (killsText != null)
            killsText.text = $"Enemies killed: {summary.TotalKills}";

        if (garbageText != null)
            garbageText.text = $"Garbage collected: {summary.TotalGarbageDeposited}";

        if (retrievalPercentageText != null)
            retrievalPercentageText.text = $"Resources retrieved: {summary.RetrievalPercentage:0}%";

        if (collectedLineText != null)
            collectedLineText.text = BuildResourceLine(summary.CollectedResources);

        if (retrievedLineText != null)
            retrievedLineText.text = BuildResourceLine(summary.RetrievedResources);
    }

    private string BuildResourceLine(System.Collections.Generic.Dictionary<ResourceSO, int> data)
    {
        if (data == null) return string.Empty;

        int c1 = data.TryGetValue(resource1, out var v1) ? v1 : 0;
        int c2 = data.TryGetValue(resource2, out var v2) ? v2 : 0;
        int c3 = data.TryGetValue(resource3, out var v3) ? v3 : 0;

        string n1 = resource1 != null ? resource1.displayName : "R1";
        string n2 = resource2 != null ? resource2.displayName : "R2";
        string n3 = resource3 != null ? resource3.displayName : "R3";

        return $"{n1}: {c1}    {n2}: {c2}    {n3}: {c3}";
    }

    private string FormatTime(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        if (t.TotalHours >= 1.0)
            return $"{(int)t.TotalHours:0}:{t.Minutes:00}:{t.Seconds:00}";
        return $"{t.Minutes:00}:{t.Seconds:00}";
    }

    /// <summary>Hook this up to the death panel's Return to Hub button.</summary>
    public void OnReturnToHubClicked()
    {
        if (GameManager.Instance != null)
        {
            // Ensure the pending run end summary is set right before transitioning,
            // in case something cleared it after this panel was shown.
            if (_cachedSummary == null)
            {
                GameManager.Instance.CollectRunState();
                _cachedSummary = GameManager.Instance.BuildRunSummary(RunEndType.Death);
            }
            GameManager.Instance.SetPendingRunEnd(RunEndType.Death, _cachedSummary);
            GameManager.Instance.RequestScene(SceneRequest.ToHub(true));
        }
        else
        {
            Debug.LogWarning("[DeathSummaryUI] No GameManager found, cannot return to Hub.");
        }
    }
}

