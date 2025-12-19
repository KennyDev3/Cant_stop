using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PulsingAuraBehavior : MonoBehaviour, IStatReceiver
{
    private int _stackCount;
    private float _baseDmg, _dmgPerStack;
    private float _baseRad, _radPerStack;
    private float _baseInt, _intRedPerStack, _minInt;
    private float _activeDuration;
    private LayerMask _enemyLayer;

    private StatController _myStats;
    private GameObject _visualInstance;
    private Coroutine _pulseRoutine;

    [Header("Runtime Stats (Read Only)")]
    [SerializeField] private float _finalDamage;
    [SerializeField] private float _finalRadius;
    [SerializeField] private float _finalInterval;

    [Header("Audio")]
    private SoundDef pulsingHitSound;

    // CONSTANTS
    private const float VISUAL_THICKNESS = 0.02f;
    private const float DAMAGE_HEIGHT = 10.0f;

    private void Awake()
    {
        _myStats = GetComponent<StatController>();
    }

    public void UpdateConfiguration(
        int stacks,
        GameObject prefab,
        LayerMask layer,
        float duration,
        float bDmg, float sDmg,
        float bRad, float sRad,
        float bInt, float sInt, float minInt, SoundDef sound)
    {
        // 1. Store Config
        _stackCount = stacks;
        _visualInstance = SetupVisuals(prefab); // Helper method to handle instantiation
        _enemyLayer = layer;
        _activeDuration = duration;

        _baseDmg = bDmg; _dmgPerStack = sDmg;
        _baseRad = bRad; _radPerStack = sRad;
        _baseInt = bInt; _intRedPerStack = sInt; _minInt = minInt;

        pulsingHitSound = sound;


        // 2. Start Coroutine if not running
        if (_pulseRoutine == null)
        {
            _pulseRoutine = StartCoroutine(PulseLoop());
        }
    }

    private GameObject SetupVisuals(GameObject prefab)
    {
        if (_visualInstance == null && prefab != null)
        {
            GameObject instance = Instantiate(prefab, transform.position, Quaternion.identity, transform);
            instance.SetActive(false);
            return instance;
        }
        return _visualInstance;
    }

    
    public void OnStatsRecalculated()
    {
        if (_myStats == null) return;

        float stackDamage = _baseDmg + (_dmgPerStack * (_stackCount - 1));
        float stackRadius = _baseRad + (_radPerStack * (_stackCount - 1));
        float stackInterval = Mathf.Max(_minInt, _baseInt - (_intRedPerStack * (_stackCount - 1)));

        
        float globalMult = _myStats.GetStat(StatType.GlobalDamageMultiplier);
        float damageMult = _myStats.GetStat(StatType.DamageMultiplier); 
        float areaMult = _myStats.GetStat(StatType.AreaMultiplier);

        if (globalMult == 0) globalMult = 1f;
        if (damageMult == 0) damageMult = 1f;
        if (areaMult == 0) areaMult = 1f;

        _finalDamage = stackDamage * globalMult * damageMult;
        _finalRadius = stackRadius * areaMult;
        _finalInterval = stackInterval;

        if (_visualInstance != null)
        {
            float diameter = _finalRadius * 2f;
            _visualInstance.transform.localScale = new Vector3(diameter, VISUAL_THICKNESS, diameter);
            _visualInstance.transform.localPosition = Vector3.zero;
        }

        Debug.Log($"Aura Recalculated: Dmg {_finalDamage} (Global x{globalMult}), Rad {_finalRadius}, Int {_finalInterval}");
    }

    private IEnumerator PulseLoop()
    {
        yield return new WaitForSeconds(0.1f); // Brief start delay

        while (true)
        {
            // ON
            if (_visualInstance != null) _visualInstance.SetActive(true);
            DealDamage();

            yield return new WaitForSeconds(_activeDuration);

            // OFF
            if (_visualInstance != null) _visualInstance.SetActive(false);

            float waitTime = Mathf.Max(0, _finalInterval - _activeDuration);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private void DealDamage()
    {
        Vector3 bottom = transform.position;
        Vector3 top = transform.position + (Vector3.up * DAMAGE_HEIGHT);

        SoundManager.Instance.Play(pulsingHitSound, transform.position);

        Collider[] hitColliders = Physics.OverlapCapsule(bottom, top, _finalRadius, _enemyLayer);
        HashSet<GameObject> hitTracker = new HashSet<GameObject>();

        foreach (var hit in hitColliders)
        {
            var enemy = hit.GetComponentInParent<EnemyHealth>();
            // Add check for enabled/dead to prevent hitting dead things
            if (enemy != null && enemy.enabled && !hitTracker.Contains(enemy.gameObject))
            {
                hitTracker.Add(enemy.gameObject);
                enemy.TakeDamage(_finalDamage);
            }

            Debug.Log(_finalDamage);

        }
    }

    private void OnDisable()
    {
        if (_visualInstance != null) _visualInstance.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _finalRadius > 0 ? _finalRadius : _baseRad);
    }
}