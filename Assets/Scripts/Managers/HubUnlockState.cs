using System.Collections.Generic;

/// <summary>
/// Holds which hub upgrades have been purchased. In-memory only; no save/load yet.
/// GameManager owns the current instance. Never cleared on scene change or when entering Hub.
/// </summary>
public class HubUnlockState
{
    private readonly HashSet<string> _unlockedIds = new HashSet<string>();

    public bool IsUnlocked(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return false;
        return _unlockedIds.Contains(upgradeId);
    }

    public void Unlock(string upgradeId)
    {
        if (!string.IsNullOrEmpty(upgradeId))
            _unlockedIds.Add(upgradeId);
    }

    /// <summary>
    /// Clear all unlocks (e.g. debug reset or new game). Call explicitly; not used on scene load.
    /// </summary>
    public void Clear()
    {
        _unlockedIds.Clear();
    }
}
