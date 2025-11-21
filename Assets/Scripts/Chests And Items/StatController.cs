using UnityEngine;
using System.Collections.Generic;

public class StatController : MonoBehaviour
{
    [System.Serializable]

    public class Stat
    {
        public StatType type;
        public float baseValue;
        [HideInInspector] public float currentValue;
    }

    [SerializeField] private List<Stat> stats = new List<Stat>();
    private Dictionary<StatType, Stat> _statDict = new Dictionary<StatType, Stat>();

    private void Awake()
    {
        InitializeStats();
    }

    private void InitializeStats()
    {
        _statDict.Clear();
        foreach (var stat in stats)
        {
            stat.currentValue = stat.baseValue;
            _statDict.Add(stat.type, stat);
        }
    }

    public void InitializeStat(StatType type, float baseValue)
    {
        // Check if exists, if not add it
        var statMatch = stats.Find(s => s.type == type);
        if (statMatch == null)
        {
            Stat newStat = new Stat { type = type, baseValue = baseValue, currentValue = baseValue };
            stats.Add(newStat);
            _statDict[type] = newStat;
        }
        else
        {
            statMatch.baseValue = baseValue;
            statMatch.currentValue = baseValue;
        }
    }


    public float GetStat(StatType type)
    {
        return _statDict.ContainsKey(type) ? _statDict[type].currentValue : 0f;
    }

    public void ResetStats()
    {
        foreach (var stat in stats)
        {
            stat.currentValue = stat.baseValue;
        }
    }

    public void ModifyStat(StatType type, float amount, bool isMultiplier)
    {
        if (_statDict.ContainsKey(type))
        {
            if (isMultiplier)
                _statDict[type].currentValue *= amount;
            else
                _statDict[type].currentValue += amount;
        }
    }






}
