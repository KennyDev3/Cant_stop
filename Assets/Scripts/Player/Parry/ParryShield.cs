using UnityEngine;

namespace StarterAssets
{
    public class ParryShield : MonoBehaviour
    {
        [SerializeField] private PlayerParryController _parryController;

        private void OnTriggerEnter(Collider other)
        {
            IParriable parriable = other.GetComponent<IParriable>();

            if (parriable != null)
            {
                // Calculate impact point for Particle VFX onSuccesfulParry
                Vector3 impactPoint = other.ClosestPoint(transform.position);

                parriable.OnParried(transform.position);

                if (_parryController != null)
                {
                    _parryController.OnSuccessfulParry(impactPoint);
                }
            }
        }
    }
}