using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace StarterAssets

{ 
public class PlayerParryController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How long the parry bubble stays active")]
    public float ParryDuration = 0.6f;

    [Tooltip("Time before you can parry again (unless successful)")]
    public float Cooldown = 1.5f;

    [Tooltip("Invincibility duration after a successful parry")]
    public float GracePeriod = 0.5f;

    [Header("Game Feel")]
    [Tooltip("Duration of the slow motion effect in real-time seconds")]
    [SerializeField] private float _hitStopDuration = 0.2f;

    [Tooltip("Target time scale (0.0 = Stopped, 1.0 = Normal)")]
    [SerializeField][Range(0f, 1f)] private float _hitStopTimeScale = 0.1f;

        [SerializeField] private StarterAssetsInputs _input;
    [SerializeField] private GameObject _parryShieldVisual; 
    [SerializeField] private ParticleSystem _parryParticles;
    [SerializeField] private GameObject _successVFXPrefab;


        [Header("Events")]
    public UnityEvent OnParryStart;
    public UnityEvent OnParrySuccess;

    private bool _isParrying = false;
    private bool _isOnCooldown = false;
    private bool _isInvincible = false;

    public bool IsInvincible => _isInvincible;

    private void Start()
    {
        // Ensure shield is off at start
        if (_parryShieldVisual != null) _parryShieldVisual.SetActive(false);
    }

    private void Update()
    {
        if (_input.parry)
        {
            _input.parry = false;
            AttemptParry();
        }
    }

    private void AttemptParry()
    {
        if (_isParrying || _isOnCooldown) return;

        StartCoroutine(ParryRoutine());
    }

    private IEnumerator ParryRoutine()
    {
        _isParrying = true;
        _isOnCooldown = true;

        // Activate Visuals and Hitbox
        if (_parryShieldVisual != null) _parryShieldVisual.SetActive(true);

        // Adjust particle speed to match window if needed
        if (_parryParticles != null)
        {
            var main = _parryParticles.main;
            main.simulationSpeed = 6f; 
            _parryParticles.Play();
        }

        OnParryStart?.Invoke();

        // Wait for the active parry window
        yield return new WaitForSeconds(ParryDuration);

        // Deactivate Shield
        if (_parryShieldVisual != null) _parryShieldVisual.SetActive(false);
        _isParrying = false;

        // Handle Cooldown
      
        yield return new WaitForSeconds(Cooldown - ParryDuration);

        _isOnCooldown = false;
    }

    public void OnSuccessfulParry(Vector3 impactPosition)
    {
            Debug.Log("Parry Successful!");

            StopAllCoroutines();
            _isParrying = false;
            _isOnCooldown = false;
            if (_parryShieldVisual != null) _parryShieldVisual.SetActive(false);

            if (_successVFXPrefab != null)
            {
                Vector3 directionOut = (impactPosition - transform.position).normalized;
                Quaternion rotation = Quaternion.LookRotation(directionOut);

                Instantiate(_successVFXPrefab, impactPosition, rotation);
            }

            // Trigger hit stop
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerHitStop(_hitStopDuration, _hitStopTimeScale);
            }

            StartCoroutine(InvincibilityRoutine());
            OnParrySuccess?.Invoke();
        }

    private IEnumerator InvincibilityRoutine()
    {
        _isInvincible = true;
        yield return new WaitForSeconds(GracePeriod);
        _isInvincible = false;
    }





    }
}