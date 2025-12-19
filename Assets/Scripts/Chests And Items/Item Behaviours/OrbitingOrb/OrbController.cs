using UnityEngine;
using System.Collections.Generic;

public class OrbController : MonoBehaviour, IStatReceiver
{
    [Header("Internal State")]
    private StatController _myStats; 
    private float _baseItemDamage;   
    private int _stackCount;         
    private float _finalDamage; // Using IstatReceiver and OnStatsRecalculated to calcuate final damage after all additions
    private float _damageScaling; // Increase of damage per stack


    [Header("Settings")]
    private float _damage;
    private float _rotationSpeed;
    private float _radius;
    private LayerMask _enemyLayer;

    [Header("State")]
    private List<GameObject> _activeOrbs = new List<GameObject>();
    private GameObject _orbPrefab;
    private Transform _centerPoint;

    [Header("Audio")]
    private SoundDef _orbHitSound;

    // Save the angles of where orbs will go 
    private readonly float[] _angleFillOrder = new float[] { 0f, 180f, 90f, 270f, 45f, 225f, 135f, 315f };

    private void Awake()
    {
        _myStats = GetComponent<StatController>();

        _centerPoint = transform;
    }

    public void UpdateConfiguration(int stackCount, GameObject prefab, float baseDmg, float scaling, float radius, float rotSpeed, LayerMask layer, SoundDef sound)
    {
        _stackCount = stackCount;
        _baseItemDamage = baseDmg;
        _damageScaling = scaling;
        _orbPrefab = prefab;
        _radius = radius;
        _rotationSpeed = rotSpeed;
        _enemyLayer = layer;

        _orbHitSound = sound; 

        UpdateOrbCount(Mathf.Clamp(stackCount + 1, 0, 8));
    }

    public void OnStatsRecalculated()
    {
        float stackedDamage = _baseItemDamage + (_baseItemDamage * _damageScaling * (_stackCount - 1));


        float globalMult = _myStats.GetStat(StatType.GlobalDamageMultiplier);

        _finalDamage = stackedDamage * globalMult;
        UpdateActiveOrbsDamage();

        Debug.Log($"Orb Damage Updated: Base({stackedDamage}) * Global({globalMult}) = {_finalDamage}");
    }


    private void UpdateOrbCount(int targetCount)
    {
        // 1. Add missing orbs
        while (_activeOrbs.Count < targetCount)
        {
            GameObject newOrb = Instantiate(_orbPrefab, _centerPoint.position, Quaternion.identity);

            
            newOrb.transform.SetParent(null);

            _activeOrbs.Add(newOrb);
        }

        while (_activeOrbs.Count > targetCount)
        {
            GameObject orbToRemove = _activeOrbs[_activeOrbs.Count - 1];
            _activeOrbs.RemoveAt(_activeOrbs.Count - 1);
            Destroy(orbToRemove);
        }
    }

    private void UpdateActiveOrbsDamage()
    {
        foreach (var orb in _activeOrbs)
        {
            if (orb != null)
                orb.GetComponent<OrbDamageHandler>().Initialize(_finalDamage, _enemyLayer, _orbHitSound);
        }
    }


    private void LateUpdate()
    {
        if (_activeOrbs.Count == 0) return;

        float currentBaseAngle = Time.time * _rotationSpeed;

        for (int i = 0; i < _activeOrbs.Count; i++)
        {
            if (_activeOrbs[i] == null) continue;

            float fixedOffset = _angleFillOrder[i];
            float totalAngleRad = (currentBaseAngle + fixedOffset) * Mathf.Deg2Rad;

            
            Vector3 offset = new Vector3(Mathf.Cos(totalAngleRad), 0, Mathf.Sin(totalAngleRad)) * _radius;

            _activeOrbs[i].transform.position = _centerPoint.position + offset;
        }
    }

    
    private void OnDestroy()
    {
        foreach (var orb in _activeOrbs)
        {
            if (orb != null) Destroy(orb);
        }
        _activeOrbs.Clear();
    }
}