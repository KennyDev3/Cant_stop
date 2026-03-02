using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Controls the generic in-level run summary panel used for extraction and full completion.
/// Shows time, kills, garbage, and resources collected vs retrieved.
/// </summary>
public class RunSummaryPanelController : MonoBehaviour
{
    [Header("Panel Root")]
    [Tooltip("Optional explicit root GameObject for the summary panel. Can be inactive at startup. If null, this component's GameObject is used.")]
    [SerializeField] private GameObject panelRoot;

    [Header("Text Fields")]
    [SerializeField] private TextMeshProUGUI titleText;
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

    private RunSummaryData _currentSummary;

    private GameObject PanelRoot => panelRoot != null ? panelRoot : gameObject;

    /// <summary>Populate the panel from a run summary and show it. Also pauses gameplay.</summary>
    public void Show(RunSummaryData summary)
    {
        if (summary == null)
        {
            Debug.LogWarning("[RunSummaryPanelController] Show called with null summary.");
            return;
        }

        _currentSummary = summary;

        PanelRoot.SetActive(true);

        if (GameManager.Instance != null)
            GameManager.Instance.SetCursorState(true);

        Time.timeScale = 0f;

        BindSummary(summary);
    }

    private void BindSummary(RunSummaryData summary)
    {
        if (titleText != null)
            titleText.text = GetTitleFor(summary.EndType);

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

    private string GetTitleFor(RunEndType endType)
    {
        switch (endType)
        {
            case RunEndType.Completed:
                return "You completed the run";
            case RunEndType.Extracted:
                return "Extraction completed";
            case RunEndType.Death:
                return "Run failed";
            default:
                return "Run summary";
        }
    }

    private string FormatTime(float seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        if (t.TotalHours >= 1.0)
            return $"{(int)t.TotalHours:0}:{t.Minutes:00}:{t.Seconds:00}";
        return $"{t.Minutes:00}:{t.Seconds:00}";
    }

    /// <summary>Called from the panel's Return to Hub button.</summary>
    public void OnReturnToHubClicked()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestScene(SceneRequest.ToHub(true));
        }
        else
        {
            Debug.LogWarning("[RunSummaryPanelController] No GameManager found, cannot return to Hub.");
        }
    }

    /// <summary>Optional hook for a Cancel/Close button if you add one.</summary>
    public void OnCloseWithoutLeaving()
    {
        Time.timeScale = 1f;
        PanelRoot.SetActive(false);
        if (GameManager.Instance != null)
            GameManager.Instance.SetCursorState(false);
    }
}

