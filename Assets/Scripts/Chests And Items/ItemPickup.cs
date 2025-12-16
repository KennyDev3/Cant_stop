using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    private ItemSO _itemData;
    private bool _canPickup = false;
    private bool _hasLanded = false;

    private Rigidbody _rb;
    private GemIdleAnim _anim;

    [SerializeField] SoundDef itemPickupSound;



    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _anim = GetComponent<GemIdleAnim>();

        
        if (_anim != null)
        {
            _anim.enabled = false;
        }
    }

    public void Initialize(ItemSO itemData)
    {
        _itemData = itemData;

        Invoke(nameof(EnablePickup), 0.5f);
    }

    private void EnablePickup() => _canPickup = true;

    private void OnCollisionEnter(Collision collision)
    {
        if (_hasLanded) return;

        if (!collision.gameObject.CompareTag("Ground")) return;

        _hasLanded = true;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;

        transform.rotation = Quaternion.identity;

        if (_anim != null)
        {
           
            _anim.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_canPickup) return;

        if (other.CompareTag("Player"))
        {
            var inventory = other.GetComponent<InventoryManager>();
            if (inventory != null)
            {
                SoundManager.Instance.Play(itemPickupSound, transform.position);

                inventory.AddItem(_itemData);
                Destroy(gameObject);
            }
        }
    }
}