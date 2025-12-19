using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ProjectileController : MonoBehaviour
{
    // --- Config ---
    public float speed = 100;

    private LayerMask _damageLayer;

    // --- Explosion VFX ---
    public GameObject rocketExplosion;

    // --- Projectile Mesh ---
    public MeshRenderer projectileMesh;

    // --- Script Variables ---
    private bool targetHit;
    private float _damage;
    private float _radius;

    private SoundDef _explosionSound;

    

    // --- VFX ---
    public ParticleSystem disableOnHit;

    private void Start()
    {
        if (disableOnHit != null)
        {
            disableOnHit.Play(true); 
        }
    }

    public void Initialize(float damage, float radius, float speedVal, LayerMask layer, SoundDef sound)
    {
        _damage = damage;
        _radius = radius;
        speed = speedVal; 
        _damageLayer = layer;
        _explosionSound = sound;
    }

    private void Update()
    {
        if (targetHit) return;
        transform.position += transform.forward * (speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;
        if (targetHit) return;

        
        int otherLayerMask = (1 << other.gameObject.layer);

        
        if ((_damageLayer.value & otherLayerMask) == 0)
        {
            return;
        }

        
        if (other.CompareTag("Player") || other.CompareTag("Turret")) return;

        Explode();

        projectileMesh.enabled = false;
        targetHit = true;

        foreach (Collider col in GetComponents<Collider>())
        {
            col.enabled = false;
        }

        if (disableOnHit != null) disableOnHit.Stop();

        Destroy(gameObject, 5f);
    }

    private void Explode()
    {
        if (rocketExplosion != null)
        {
            Instantiate(rocketExplosion, transform.position, rocketExplosion.transform.rotation, null);
        }

        
        DealAreaDamage();
        SoundManager.Instance.Play(_explosionSound, transform.position);
    }

    private void DealAreaDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _damageLayer);
        HashSet<GameObject> hitObjects = new HashSet<GameObject>(); 

        foreach (var hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy == null) enemy = hit.GetComponent<EnemyHealth>();

            if (enemy != null && enemy.enabled && !hitObjects.Contains(enemy.gameObject))
            {
                hitObjects.Add(enemy.gameObject);
                enemy.TakeDamage(_damage);
            }
        }
    }



}
