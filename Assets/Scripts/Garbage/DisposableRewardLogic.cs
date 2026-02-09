using System.Collections;
using UnityEngine;

public class DisposableRewardLogic : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int cost = 50;
    [SerializeField] private LootTableSO lootTable;
    [SerializeField] private Transform spawnPoint;

    [Header("Throw Settings")]
    [Tooltip("How many seconds the item stays in the air. Lower = Faster/Zippier.")]
    [SerializeField] private float throwTime = 0.6f;
    [Tooltip("Extra height added to the arc so it doesn't fly in a straight line.")]
    [SerializeField] private float heightOffset = 2f;
    [SerializeField] private string playerTag = "Player";

    [Header("Rarity Odds")]
    [SerializeField] private float rareChance = 0.3f;
    [SerializeField] private float legendaryChance = 0.1f;

    [Header("Effects")]
    [SerializeField] private SoundDef rewardSound;
    [SerializeField] private GameObject rewardVFX;

    public int GetCost() => cost;

    public void TriggerReward(bool isDoubleRoll)
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        // Target player position or a point in front if player is missing
        Vector3 targetPos = player != null ? player.transform.position : transform.position + transform.forward * 5f;

        int rolls = isDoubleRoll ? 2 : 1;

        for (int i = 0; i < rolls; i++)
        {
            float roll = Random.value;
            ItemRarity selectedRarity = ItemRarity.Common;

            if (roll < legendaryChance) selectedRarity = ItemRarity.Legendary;
            else if (roll < legendaryChance + rareChance) selectedRarity = ItemRarity.Rare;

            ItemSO itemToSpawn = lootTable.GetRandomItem(selectedRarity);

            if (itemToSpawn != null)
            {
                // Delay subsequent items slightly for better visual feedback
                StartCoroutine(SpawnItemRoutine(itemToSpawn, targetPos, i * 0.15f));
            }
        }

        if (rewardVFX != null) Instantiate(rewardVFX, spawnPoint.position, Quaternion.identity);
        SoundManager.Instance.Play(rewardSound, transform.position);
    }

    private IEnumerator SpawnItemRoutine(ItemSO item, Vector3 targetPos, float delay)
    {
        yield return new WaitForSeconds(0.2f + delay);

        GameObject spawnedObj = Instantiate(item.pickupPrefab, spawnPoint.position, Quaternion.identity);

        if (spawnedObj.TryGetComponent(out ItemPickup pickup))
        {
            pickup.Initialize(item);

            if (spawnedObj.TryGetComponent(out Rigidbody rb))
            {
                // Reset physics state
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Apply the calculated snappy velocity
                rb.linearVelocity = CalculateSpeedyArc(spawnPoint.position, targetPos, throwTime);
            }
        }
    }

    private Vector3 CalculateSpeedyArc(Vector3 start, Vector3 target, float time)
    {
        Vector3 distance = target - start;
        Vector3 distanceXZ = new Vector3(distance.x, 0, distance.z);

        float sX = distanceXZ.magnitude / time;
        float sY = (distance.y / time) + (0.5f * Mathf.Abs(Physics.gravity.y) * time);

        sY += (heightOffset / time);

        // Combine into final velocity vector
        Vector3 velocity = distanceXZ.normalized * sX;
        velocity.y = sY;

        return velocity;
    }
}