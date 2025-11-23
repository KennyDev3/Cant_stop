using UnityEngine;

public class RocketLauncherBehavior : MonoBehaviour, IStatReceiver
{
    private int _stackCount;
    private float _baseDamage, _damagePerStack;
    private float _baseChance, _chancePerStack;
    private float _baseRadius, _radiusPerStack;
    private float _rocketSpeed;
    private GameObject _rocketPrefab;
    private LayerMask _enemyLayer;

    private const float BASE_CHANCE = 0.33f; // 33%

    private StatController _myStats;

    [Header("Runtime Stats")]
    [SerializeField] private float _finalDamage;
    [SerializeField] private float _finalRadius;
    [SerializeField] private float _currentProcChance;


    private void Awake()
    {
        _myStats = GetComponent<StatController>();
    }

    private void OnEnable()
    {
        TruckTurret.OnTurretShoot += TryFireRocket;
    }

    private void OnDisable()
    {
        TruckTurret.OnTurretShoot -= TryFireRocket;
    }

    public void UpdateConfiguration(
        int stacks, GameObject prefab, LayerMask layer,
        float baseChance, float stackChance, 
        float bDmg, float sDmg,
        float bRad, float sRad,
        float speed)
    {
        _stackCount = stacks;
        _rocketPrefab = prefab;
        _enemyLayer = layer;

        _baseChance = baseChance;       
        _chancePerStack = stackChance;  

        _baseDamage = bDmg; _damagePerStack = sDmg;
        _baseRadius = bRad; _radiusPerStack = sRad;
        _rocketSpeed = speed;

        OnStatsRecalculated();
    }

    public void OnStatsRecalculated()
    {
        if (_myStats == null) return;

        // Max of 100% chance
        _currentProcChance = Mathf.Clamp01(_baseChance + (_chancePerStack * (_stackCount - 1)));

        //  Damage
        float stackDamage = _baseDamage + (_damagePerStack * (_stackCount - 1));
        float globalMult = _myStats.GetStat(StatType.GlobalDamageMultiplier);
        float damageMult = _myStats.GetStat(StatType.DamageMultiplier);
        if (globalMult == 0) globalMult = 1f;
        if (damageMult == 0) damageMult = 1f;

        _finalDamage = stackDamage * globalMult * damageMult;

        // Radius
        float areaMult = _myStats.GetStat(StatType.AreaMultiplier);
        if (areaMult == 0) areaMult = 1f;
        _finalRadius = (_baseRadius + (_radiusPerStack * (_stackCount - 1))) * areaMult;
    }

    private void TryFireRocket(Vector3 origin, Vector3 targetPos)
    {
        // Use the calculated variable instead of hardcoded const
        if (UnityEngine.Random.value > _currentProcChance) return;

        if (_rocketPrefab == null) return;

        GameObject rocketObj = Instantiate(_rocketPrefab, origin, Quaternion.identity);
        rocketObj.transform.LookAt(targetPos);

        ProjectileController projectile = rocketObj.GetComponent<ProjectileController>();
        if (projectile != null)
        {
            projectile.Initialize(_finalDamage, _finalRadius, _rocketSpeed, _enemyLayer);
        }
    }

}
