using UnityEngine;
using StarterAssets;
using UnityEngine.Events;




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

    void Start()
    {
        currentHealth = maxHealth;
        thirdPersonController = GetComponent<ThirdPersonController>();
        characterController = GetComponent<CharacterController>();
        onHealthChanged.Invoke(currentHealth, maxHealth); // Initial UI update

        
    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        Debug.Log("Player took " + damageAmount + " damage. Current HP: " + currentHealth);
        
        onHealthChanged.Invoke(currentHealth, maxHealth);


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
    }

}


