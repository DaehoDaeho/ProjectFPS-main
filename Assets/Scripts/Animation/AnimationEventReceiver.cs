using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    public void OnAttack()
    {
        Debug.Log("Apply Damage!!!!!");
        EnemyBrain enemyBrain = GetComponentInParent<EnemyBrain>();
        if(enemyBrain != null)
        {
            enemyBrain.DoAttack();
        }
    }

    public void OnAttackRanged()
    {
        EnemyBrain enemyBrain = GetComponentInParent<EnemyBrain>();
        if (enemyBrain != null)
        {
            if(enemyBrain.CanHitPlayer() == true)
            {
                enemyBrain.DoAttackRanged();
            }
        }
    }
}
