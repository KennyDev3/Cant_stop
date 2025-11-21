using System.Collections;
using UnityEngine;

public class ChestInteractable : MonoBehaviour, IInteractable
{

    [Header("Config")]
    [SerializeField] private int cost = 25;
    [SerializeField] private LootTableSO lootTable;
    [SerializeField] private Transform spawnPoint;

    [Header("Pop Physics")]
    [SerializeField] private float upForce = 20f;    
    [SerializeField] private float sideForce = 10f;  

    // Weighted chance for chest rarity outcome
    [SerializeField] private float rareChance = 0.3f;     // 30%
    [SerializeField] private float legendaryChance = 0.1f; // 10%

    [SerializeField] private Animator chestAnimator;
    private Outline _outline;


    private bool _isOpen = false;

    void Awake()
    {
        _outline = GetComponent<Outline>();
        if (_outline == null)
        {
            Debug.LogWarning("Outline component missing from GarbageItem.", this);
        }
        else
        {
            _outline.OutlineColor = Color.white;
            _outline.enabled = true;
        }
    }
    public void Interact(PlayerInteractor interactor)
    {
        if (_isOpen) return;

        
        PlayerGarbageHandler wallet = interactor.GetComponent<PlayerGarbageHandler>();


        if (wallet != null)
        {
            // 2. CHECK & SPEND MONEY
            if (wallet.Spend(cost))
            {
                OpenChest();
            }
            else
            {
                // Optional: Play "Error/No Money" Sound
                Debug.Log("Not enough cash!");
            }
        }
    }

    private void OpenChest()
    {
        _isOpen = true;
        Unhighlight();

        // Play VFX and sounds or trigger their events 


        if (chestAnimator) chestAnimator.SetTrigger("Open");

        // Determine Rarity
        float roll = Random.value;
        ItemRarity selectedRarity = ItemRarity.Common;

        if (roll < legendaryChance) selectedRarity = ItemRarity.Legendary;
        else if (roll < legendaryChance + rareChance) selectedRarity = ItemRarity.Rare;

        // Get Item
        ItemSO itemToSpawn = lootTable.GetRandomItem(selectedRarity);

        StartCoroutine(SpawnItemRoutine(itemToSpawn));
    }



    private IEnumerator SpawnItemRoutine(ItemSO item)
    {
        yield return new WaitForSeconds(0.6f); // Delay for animation

        // Instantiate the visual prefab
        GameObject spawnedObj = Instantiate(item.pickupPrefab, spawnPoint.position, Quaternion.identity);

        // Initialize the pickup
        if (spawnedObj.TryGetComponent(out ItemPickup pickup))
        {
            pickup.Initialize(item);

            Rigidbody rb = spawnedObj.GetComponent<Rigidbody>();

            if (rb)
            {
                
                Vector2 randomDir = Random.insideUnitCircle.normalized;
                Vector3 throwDirection = new Vector3(randomDir.x, 0f, randomDir.y);

                rb.AddForce(Vector3.up * upForce, ForceMode.Impulse);

                rb.AddForce(throwDirection * sideForce, ForceMode.Impulse);

              
            }
        }
    }




    public void Highlight()
    {
        if (_outline != null)
        {
            _outline.OutlineColor = Color.yellow;
        }
    }

    public void Unhighlight()
    {
        if (_outline != null)
        {
            _outline.OutlineColor = Color.white;
        }
    }

    public string GetInteractionPrompt()
    {
        return $"Press E to Buy Chest$";
    }







}
