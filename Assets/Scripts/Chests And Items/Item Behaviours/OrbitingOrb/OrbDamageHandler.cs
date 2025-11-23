using System.Collections.Generic;
using UnityEngine;

public class OrbDamageHandler : MonoBehaviour
{
    private float _damage;
    private LayerMask _targetLayer;

    // How many times can an enemy be hit while in the orb
    private Dictionary<GameObject, float> _damageTimers = new Dictionary<GameObject, float>();
    private const float DAMAGE_INTERVAL = 0.1f;

    public void Initialize(float damage, LayerMask targetLayer)
    {
        _damage = damage;
        _targetLayer = targetLayer;
        _damageTimers.Clear();
    }

    private void OnTriggerStay(Collider other)
    {
        // Check layer
        if (((1 << other.gameObject.layer) & _targetLayer) == 0) return;

       
        var enemy = other.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
        {
            HandleDamageTick(enemy);
        }

        //Debug.Log(_damage);
    }

    private void HandleDamageTick(EnemyHealth enemy)
    {
        GameObject enemyObj = enemy.gameObject;
        float currentTime = Time.time;

        if (!_damageTimers.ContainsKey(enemyObj) || currentTime >= _damageTimers[enemyObj] + DAMAGE_INTERVAL)
        {
            enemy.TakeDamage(_damage); 

            _damageTimers[enemyObj] = currentTime;
        }
    }

    private void OnDisable()
    {
        // Clear cache when orb is disabled/pooled to prevent memory leaks
        _damageTimers.Clear();
    }


}
