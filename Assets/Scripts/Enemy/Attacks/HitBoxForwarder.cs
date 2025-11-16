using UnityEngine;

public class HitboxForwarder : MonoBehaviour
{
    private MeleeAttack meleeAttackScript;

    public void Initialize(MeleeAttack attackScript)
    {
        this.meleeAttackScript = attackScript;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (meleeAttackScript != null)
        {
            meleeAttackScript.ReportHit(other);
        }
    }
}