using System.Collections;
using UnityEngine;

public class BombController : MonoBehaviour
{

    [Header("Bomb Settings")]
    public float countdownDuration = 2f;
    public float explosionRadius = 5f;
    private float damage;

    [SerializeField] private int flashCount = 3;


    [Header("References")]
    public GameObject radiusIndicator; 
    public GameObject explosionVFX;

    [Header("Audio")]
    [SerializeField] SoundDef bombCountdownBeep;
    [SerializeField] SoundDef BombThrowEnemyBombExplosionSound;


    private Rigidbody rb;
    private bool isArmed = false;

    [ContextMenu("Arm Bomb (Debug)")]

    public void Initialize(float damageAmount)
    {
        this.damage = damageAmount;
    }

    void Awake() // Changed to Awake to ensure Rigidbody is cached before anything else
    {
        rb = GetComponent<Rigidbody>();

        if (radiusIndicator != null)
        {
            radiusIndicator.transform.localScale = new Vector3(explosionRadius * 2, 0.01f, explosionRadius * 2);
            radiusIndicator.SetActive(false);
        }
        else
        {
            Debug.LogError("Radius Indicator is not assigned in the Inspector!", this);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isArmed)
        {
            rb.isKinematic = true;
            ArmBomb();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ArmBomb()
    {
        if (isArmed) return; 

        isArmed = true;
        StartCoroutine(CountdownAndFlash());
    }

    private IEnumerator CountdownAndFlash()
    {
        float flashInterval = countdownDuration / (flashCount * 2);

        for (int i = 0; i < flashCount; i++)
        {

            if (radiusIndicator != null)
            {
                radiusIndicator.SetActive(true);
                SoundManager.Instance.Play(bombCountdownBeep, transform.position);
            }
            yield return new WaitForSeconds(flashInterval);

            if (radiusIndicator != null)
            {
                radiusIndicator.SetActive(false);
            }
            yield return new WaitForSeconds(flashInterval);
        }

        Explode();
    }

    private void Explode()
    {
        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
            SoundManager.Instance.Play(BombThrowEnemyBombExplosionSound, transform.position);   
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in colliders)
        {
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
               
                break;
            }
        }

        Destroy(gameObject);
    }

    private void DebugArmBomb()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Debug Arm Bomb only works in Play Mode.");
            return;
        }
        ArmBomb();
    }



}
