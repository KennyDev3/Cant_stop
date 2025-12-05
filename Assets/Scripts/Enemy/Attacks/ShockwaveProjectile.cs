using StarterAssets;
using System.Collections.Generic;
using UnityEngine;

public class ShockwaveProjectile : MonoBehaviour, IParriable
{

    private Vector3 moveDirection;

    private bool _isParried = false;


    private float speed;
    private float damage;
    public float lifeTime = 5f;

    private HashSet<GameObject> _alreadyHitEnemies = new HashSet<GameObject>();


    public void Initialize(float attackDamage, float projectileSpeed)
    {
        this.damage = attackDamage;
        this.speed = projectileSpeed;

        // Auto-destroy after lifetime 
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {

        // Using Space.World ensures we move along the global axes defined at launch
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_isParried)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                
                Destroy(gameObject);
            }
        }

        else if (_isParried && other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            
            var enemyRoot = other.transform.root.gameObject;

            if (_alreadyHitEnemies.Contains(enemyRoot))
            {
                return; 
            }

            
            var enemyHealth = other.GetComponent<EnemyHealth>(); 
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage); // Damage is already doubled in OnParried
            }

            _alreadyHitEnemies.Add(enemyRoot);

            Debug.Log($"Pierced through {other.name}!");

            //// Consider Playing VFX

            //if (enemyHitEffect != null)
            //{
            //    Instantiate(enemyHitEffect, transform.position, Quaternion.identity);
            //}

            
        }
    }

    public void OnParried(Vector3 parrySourcePosition)
    {
        if (_isParried) return;

        _isParried = true;

        Quaternion backwardRotation = Quaternion.LookRotation(-transform.forward);
        Quaternion zOffset = Quaternion.Euler(0, 0, -90f);
        transform.rotation = backwardRotation * zOffset;

        speed *= 1.5f;
        damage *= 1.5f;

        
        CancelInvoke();
        Destroy(gameObject, 5f);

        _alreadyHitEnemies.Clear();
    }



}
