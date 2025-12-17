using System.Collections;
using UnityEngine;

public class FlameArea : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private SoundDef flameAOEFireSound;
    [SerializeField] private SoundDef playerHurtFromAOEFireSound;

    [Header("Settings")]
    private float damagePerTick;
    private float tickRate;
    private float lifeTime;


    public void Initialize(float damageAmount, float ticks,float duration)
    {
        this.damagePerTick = damageAmount;
        this.tickRate = ticks;
        this.lifeTime = duration;

    }

    private void Start()
    {
        SoundManager.Instance.Play(flameAOEFireSound, transform.position);
        // Automatically destroy the fire after its lifetime
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                StartCoroutine(BurnTarget(health));
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            
            StopAllCoroutines();
        }
    }
    private IEnumerator BurnTarget(PlayerHealth targetHealth)
    {
        // While the player is standing here, keep damaging
        while (targetHealth != null && targetHealth.gameObject.activeSelf)
        {
            targetHealth.TakeDamage(damagePerTick);
            SoundManager.Instance.Play(playerHurtFromAOEFireSound, transform.position);

            // Optional: Apply visual feedback (Camera shake, red flash) here

            yield return new WaitForSeconds(tickRate);
        }
    }

}
