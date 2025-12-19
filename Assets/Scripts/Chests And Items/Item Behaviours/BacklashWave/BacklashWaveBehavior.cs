using UnityEngine;
using System.Collections;
using StarterAssets;

public class BacklashWaveBehavior : MonoBehaviour, IStatReceiver
{
    private int _stackCount;
    private float _baseDmg, _dmgPerStack;
    private float _waveSpeed;
    private float _waveDuration;
    private float _timeGap; 
    private LayerMask _enemyLayer;
    private GameObject _projectilePrefab;

    private StatController _myStats;
    private ThirdPersonController _controller;

    [Header("Runtime Stats")]
    [SerializeField] private float _finalDamage;

    private SoundDef _hitSound;


    private const int MAX_LAYERS = 4;

    private void Awake()
    {
        _myStats = GetComponent<StatController>();
        _controller = GetComponent<ThirdPersonController>();
    }

    private void OnEnable()
    {
        if (_controller != null)
            _controller.OnDashStart += StartWaveRoutine;
    }

    private void OnDisable()
    {
        if (_controller != null)
            _controller.OnDashStart -= StartWaveRoutine;
    }

    public void UpdateConfiguration(
        int stacks,
        GameObject prefab,
        LayerMask layer,
        float bDmg, float sDmg,
        float speed, float duration,
        float timeGap, SoundDef sound)
    {
        _stackCount = stacks;
        _projectilePrefab = prefab;
        _enemyLayer = layer;
        _baseDmg = bDmg;
        _dmgPerStack = sDmg;
        _waveSpeed = speed;
        _waveDuration = duration;
        _timeGap = timeGap;
        _hitSound = sound;

        OnStatsRecalculated();
    }

    public void OnStatsRecalculated()
    {
        if (_myStats == null) return;

        float stackDamage = _baseDmg + (_dmgPerStack * (_stackCount - 1));
        float globalMult = _myStats.GetStat(StatType.GlobalDamageMultiplier);
        float damageMult = _myStats.GetStat(StatType.DamageMultiplier);

        if (globalMult == 0) globalMult = 1f;
        if (damageMult == 0) damageMult = 1f;

        _finalDamage = stackDamage * globalMult * damageMult;
    }

    private void StartWaveRoutine(Vector3 dashDirection)
    {
        if (_projectilePrefab == null) return;
        StartCoroutine(FireRipple(dashDirection));

        SoundManager.Instance.Play(_hitSound, transform.position);
    }

    private IEnumerator FireRipple(Vector3 dashDirection)
    {
        Vector3 forward = dashDirection;
        forward.y = 0;
        forward.Normalize();
        if (forward == Vector3.zero) forward = transform.forward;

        Vector3 backward = -forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 left = -right;

        
        int layersNS = Mathf.Clamp((_stackCount + 1) / 2, 1, MAX_LAYERS);
        int layersEW = Mathf.Clamp(_stackCount / 2, 0, MAX_LAYERS);

        int maxLoops = Mathf.Max(layersNS, layersEW);

        
        for (int i = 0; i < maxLoops; i++)
        {
            if (i < layersNS)
            {
                SpawnWave(backward); 
                SpawnWave(forward);  
            }

            if (i < layersEW)
            {
                SpawnWave(right);
                SpawnWave(left);
            }

            
            if (i < maxLoops - 1)
            {
                yield return new WaitForSeconds(_timeGap);
            }
        }
    }

    private void SpawnWave(Vector3 direction)
    {
        Quaternion rotation = Quaternion.LookRotation(direction);

        Vector3 spawnPos = transform.position + (Vector3.up * 1.0f);

        GameObject waveObj = Instantiate(_projectilePrefab, spawnPos, rotation);

        BacklashWaveProjectile projectile = waveObj.GetComponent<BacklashWaveProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(_finalDamage, _waveSpeed, _waveDuration, _enemyLayer);
        }
    }
}