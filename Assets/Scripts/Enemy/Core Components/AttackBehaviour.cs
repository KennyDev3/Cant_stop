using UnityEngine;

public abstract class AttackBehaviour : MonoBehaviour
{
    protected EnemyBrain enemyBrain;
    public bool IsAttacking { get; protected set; }

    public virtual void Initialize(EnemyBrain brain, EnemyData data)
    {
        this.enemyBrain = brain;
    }

    public abstract void PerformAttack(Transform target);
    public abstract void AnimationEvent_StartAttack();
    public abstract void AnimationEvent_EndAttack();
}