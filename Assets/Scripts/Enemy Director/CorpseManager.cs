using System.Collections.Generic;
using UnityEngine;

public class CorpseManager : MonoBehaviour
{
    public static CorpseManager Instance { get; private set; }

    [Header("Settings")]
    public int maxCorpses = 50;

    private List<EnemyHealth> activeCorpses = new List<EnemyHealth>();

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RegisterCorpse(EnemyHealth corpse)
    {
        activeCorpses.Add(corpse);

        if (activeCorpses.Count > maxCorpses)
        {
            RemoveOldestCorpse();
        }
    }

    public void UnregisterCorpse(EnemyHealth corpse)
    {
        if (activeCorpses.Contains(corpse))
        {
            activeCorpses.Remove(corpse);
        }
    }

    private void RemoveOldestCorpse()
    {
        if (activeCorpses.Count > 0)
        {
            EnemyHealth oldest = activeCorpses[0];
            activeCorpses.RemoveAt(0);

            // Force it back to pool immediately
            oldest.ReturnToPool();
        }
    }
}