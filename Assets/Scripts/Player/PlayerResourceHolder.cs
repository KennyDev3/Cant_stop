using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Holds run resource counts on the player. Persists across World_1 → World_2 via RunState.
/// Invokes onResourceCountChanged(ResourceSO, newCount) when a count changes so UI can update (e.g. wire to UIManager.UpdateResourceCount).
/// </summary>
public class PlayerResourceHolder : MonoBehaviour, IRunStateContributor
{
    [System.Serializable]
    public class ResourceCountChangedEvent : UnityEvent<ResourceSO, int> { }

    [Header("Events")]
    [Tooltip("Invoked when a resource count changes. Wire to UIManager.UpdateResourceCount so run HUD updates.")]
    public ResourceCountChangedEvent onResourceCountChanged;

    [Header("Debug")]
    [Tooltip("Runtime view of run resource counts. Edit in Inspector for testing (only affects current session).")]
    [SerializeField] private List<ResourceCountEntry> debugRunCounts = new List<ResourceCountEntry>();

    [System.Serializable]
    public struct ResourceCountEntry
    {
        public ResourceSO resource;
        public int count;
    }

    private Dictionary<ResourceSO, int> _counts = new Dictionary<ResourceSO, int>();

    public void Add(ResourceSO type, int amount)
    {
        if (type == null || amount <= 0) return;

        if (!_counts.ContainsKey(type))
            _counts[type] = 0;

        _counts[type] += amount;
        int newCount = _counts[type];

        UpdateDebugList();
        onResourceCountChanged?.Invoke(type, newCount);
    }

    public int GetCount(ResourceSO type)
    {
        if (type == null) return 0;
        return _counts.TryGetValue(type, out int c) ? c : 0;
    }

    public void ContributeToRunState(RunState state)
    {
        state.Resources.Counts.Clear();
        foreach (var kvp in _counts)
            state.Resources.Counts[kvp.Key] = kvp.Value;
    }

    public void ApplyRunState(RunState state)
    {
        _counts.Clear();
        foreach (var kvp in state.Resources.Counts)
            _counts[kvp.Key] = kvp.Value;

        UpdateDebugList();

        foreach (var kvp in _counts)
            onResourceCountChanged?.Invoke(kvp.Key, kvp.Value);

        // After a scene load the event may point at the previous scene's UIManager (destroyed). Push to current scene's UI so run resource HUD is correct in World_2 etc.
        var ui = FindFirstObjectByType<UIManager>();
        if (ui != null)
        {
            foreach (var kvp in _counts)
                ui.UpdateResourceCount(kvp.Key, kvp.Value);
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterRunStateContributor(this);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterRunStateContributor(this);
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterRunStateContributor(this);

        foreach (var kvp in _counts)
            onResourceCountChanged?.Invoke(kvp.Key, kvp.Value);
    }

    private void UpdateDebugList()
    {
        debugRunCounts.Clear();
        foreach (var kvp in _counts)
        {
            if (kvp.Key == null) continue;
            debugRunCounts.Add(new ResourceCountEntry { resource = kvp.Key, count = kvp.Value });
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (debugRunCounts == null) return;

        _counts.Clear();
        foreach (var entry in debugRunCounts)
        {
            if (entry.resource == null) continue;
            _counts[entry.resource] = Mathf.Max(0, entry.count);
        }
        UpdateDebugList();
    }
#endif
}
