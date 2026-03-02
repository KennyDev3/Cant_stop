using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Describes how a run ended and the summary stats to show on end-of-run screens.
/// Built from the current RunState by GameManager and consumed by UI panels.
/// </summary>
public enum RunEndType
{
    Completed,
    Extracted,
    Death
}

public class RunSummaryData
{
    public RunEndType EndType;

    public float TotalRunTimeSeconds;
    public int TotalKills;
    public int TotalGarbageDeposited;

    /// <summary>Total resources collected during the run, per type.</summary>
    public Dictionary<ResourceSO, int> CollectedResources = new Dictionary<ResourceSO, int>();

    /// <summary>Resources the player will actually bring back to the hub, after applying the outcome multiplier.</summary>
    public Dictionary<ResourceSO, int> RetrievedResources = new Dictionary<ResourceSO, int>();

    /// <summary>Retrieval percentage (e.g. 100, 75, 25) for display.</summary>
    public float RetrievalPercentage;
}

