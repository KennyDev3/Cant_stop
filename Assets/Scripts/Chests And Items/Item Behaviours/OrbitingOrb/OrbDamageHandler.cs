using System.Collections.Generic;
using UnityEngine;

public class OrbDamageHandler : MonoBehaviour
{
    private float _damage;
    private LayerMask _targetLayer;

    private SoundDef _hitSound;

    private Dictionary<GameObject, float> _damageTimers = new Dictionary<GameObject, float>();
    private const float DAMAGE_INTERVAL = 0.1f;

    // 2. Add SoundDef to your Initialize arguments
    public void Initialize(float damage, LayerMask targetLayer, SoundDef soundToPlay)
    {
        _damage = damage;
        _targetLayer = targetLayer;
        _hitSound = soundToPlay; 

        _damageTimers.Clear();
    }

    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & _targetLayer) == 0) return;

        var enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy != null)
        {
            HandleDamageTick(enemy);
        }
    }

    private void HandleDamageTick(EnemyHealth enemy)
    {
        GameObject enemyObj = enemy.gameObject;
        float currentTime = Time.time;

        if (!_damageTimers.ContainsKey(enemyObj) || currentTime >= _damageTimers[enemyObj] + DAMAGE_INTERVAL)
        {
            enemy.TakeDamage(_damage);

            if (_hitSound != null)
            {
                SoundManager.Instance.Play(_hitSound, transform.position);
            }

            _damageTimers[enemyObj] = currentTime;
        }
    }

    private void OnDisable()
    {
        _damageTimers.Clear();
    }
}