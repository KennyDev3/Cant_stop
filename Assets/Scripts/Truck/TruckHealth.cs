using UnityEngine;

public class TruckHealth : MonoBehaviour
{

    public TruckData truckData;
    private float currentHealth;
    private bool isDead = false;

    private TruckMovement truckMovement;




    void Start()
    {
        currentHealth = truckData.maxHealth;
        truckMovement = GetComponent<TruckMovement>();

    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return; // Don't take damage if already dead

        currentHealth -= damageAmount;
        Debug.Log("Truck took " + damageAmount + " damage. Current HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            isDead = true;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Truck is dead!");
        
        // Disable the movement script
        if (truckMovement != null)
        {
            truckMovement.enabled = false;
        }
        // We no longer destroy the GameObject
        // Destroy(gameObject);
    }
    
    
   
}
