using UnityEngine;

public abstract class AttackBehaviour : MonoBehaviour
{
    // A reference to the main brain to get data if needed
    protected EnemyBrain enemyBrain;

    public bool IsAttacking { get; protected set; }


    public virtual void Initialize(EnemyBrain brain)
    {
        this.enemyBrain = brain;
    }

    public abstract void PerformAttack(Transform target);

    public abstract void AnimationEvent_StartAttack();
    public abstract void AnimationEvent_EndAttack();
}