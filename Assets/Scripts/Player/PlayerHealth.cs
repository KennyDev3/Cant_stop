using UnityEngine;
using StarterAssets;
using UnityEngine.Events;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using Unity.Cinemachine;



public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 1000f;
    private float currentHealth;
    private bool isDead = false;


    private ThirdPersonController thirdPersonController;
    private CharacterController characterController;

    [System.Serializable]
    public class HealthChangeEvent : UnityEvent<float, float> { }
    public HealthChangeEvent onHealthChanged;

    [Header("Damage Feedback")]
    // 2. Assign the 'Global Volume' object from your scene to this slot in the Inspector
    public Volume globalVolume;
    public float flashDuration = 0.5f;
    public float flashIntensity = 0.4f;

    [Header("Camera Shake")]
    public CinemachineCamera playerCamera;
    public float shakeIntensity = 2f;
    public float shakeTime = 0.2f;

    private float _startingIntensity;
    private CinemachineBasicMultiChannelPerlin _perlin;

    private Vignette _vignette;

    private Coroutine _flashCoroutine;
    private Coroutine _shakeCoroutine;



    void Start()
    {
        currentHealth = maxHealth;
        thirdPersonController = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
        onHealthChanged.Invoke(currentHealth, maxHealth); // Initial UI update

        if (globalVolume != null && globalVolume.profile.TryGet(out _vignette))
        {
            // Ensure it starts invisible
            _vignette.intensity.value = 0f;
            _vignette.color.value = Color.red;
        }

        if (playerCamera != null)
        {
            _perlin = playerCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }
        else
        {
            Debug.LogWarning("Global Volume or Vignette override missing!");
        }

    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead || (thirdPersonController != null && thirdPersonController.IsInvulnerable()))
        {
            return;
        }

        currentHealth -= damageAmount;
        Debug.Log("Player took " + damageAmount + " damage. Current HP: " + currentHealth);

        onHealthChanged.Invoke(currentHealth, maxHealth);

        TriggerDamageFlash();



        if (currentHealth <= 0)
        {
            isDead = true;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player is dead!");

        // Disable both controller components to stop all movement and input
        if (thirdPersonController != null)
        {
            thirdPersonController.enabled = false;
        }
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    private void TriggerDamageFlash()
    {
        if (_vignette == null) return;

        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);

        _flashCoroutine = StartCoroutine(DoFlash());

        if (_perlin != null)
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(DoShake());
        }
    }

    private IEnumerator DoFlash()
    {
        _vignette.intensity.value = flashIntensity;

        float elapsedTime = 0f;

        while (elapsedTime < flashDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = elapsedTime / flashDuration;

            _vignette.intensity.value = Mathf.Lerp(flashIntensity, 0f, t);

            yield return null;
        }

        // 4. Ensure it ends at exactly 0
        _vignette.intensity.value = 0f;
    }

    private IEnumerator DoShake()
    {

        _perlin.FrequencyGain = 20f;

        _perlin.AmplitudeGain = shakeIntensity;

        yield return new WaitForSeconds(shakeTime);

        _perlin.AmplitudeGain = 0f;
        _perlin.FrequencyGain = 1f;

    }
}


