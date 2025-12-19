using System.Collections.Generic;
using UnityEngine;


public class BacklashWaveProjectile : MonoBehaviour
{
    private float _damage;
    private float _speed;
    private float _duration;
    private LayerMask _hitLayer;



    private HashSet<GameObject> _hitTargets = new HashSet<GameObject>(); // Who I already hit list

    public void Initialize(float damage, float speed, float duration, LayerMask hitLayer)
    {
        _damage = damage;
        _speed = speed;
        _duration = duration;
        _hitLayer = hitLayer;

        // Destroy self automatically after duration
        Destroy(gameObject, _duration);
    }

    private void Update()
    {
        // Move forward relative to self
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((_hitLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            
            var enemy = other.GetComponent<EnemyHealth>();

            if (enemy != null && enemy.enabled)
            {
                if (!_hitTargets.Contains(enemy.gameObject))
                {
                    _hitTargets.Add(enemy.gameObject);
                    enemy.TakeDamage(_damage);

                    // Potentially spawn VFX here
                }
            }
        }
    }

}
