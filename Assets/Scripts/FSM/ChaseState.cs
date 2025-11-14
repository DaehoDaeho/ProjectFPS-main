using UnityEngine;

/// <summary>
/// Chase: 마지막으로 본 플레이어 위치로 회전/전진.
/// 사거리 이내면 Attack, 시야 상실 후 도착하면 Search.
/// </summary>
public class ChaseState : EnemyState
{
    public ChaseState()
    {

    }

    public ChaseState(EnemyBrain b)
    {
        brain = b;
    }

    public override string Name()
    {
        return "Chase";
    }

    public override void OnEnter()
    {
        // 쿨다운 등은 AttackState가 관리하므로 여기선 없음.
    }

    public override void OnUpdate(float dt)
    {
        // 1) 시야 갱신.
        bool seen = false;
        Vector3 seenPos = Vector3.zero;

        if (brain.senses != null)
        {
            bool can = brain.senses.CanSeeTarget(out seenPos);
            if (can == true)
            {
                seen = true;
                brain.lastKnownPos = seenPos;
            }
        }

        // 2) 회전/전진.
        brain.FacePosition(brain.lastKnownPos, dt);

        float distToLast = Vector3.Distance(brain.transform.position, brain.lastKnownPos);

        if (distToLast > brain.stoppingDistance)
        {
            brain.MoveForward(brain.chaseSpeed, dt);
        }

        // 3) 사거리 판정 -> Attack
        float distToPlayer = brain.DistanceToPlayer();

        if(brain.enemyType == EnemyType.Hybrid || brain.enemyType == EnemyType.Ranged)
        {
            if (distToPlayer <= brain.attackRangedRange)
            {
                brain.RequestStateChange(new AttackRangedState(brain));
                return;
            }
        }


        if (brain.enemyType == EnemyType.Hybrid || brain.enemyType == EnemyType.Melee)
        {
            if (distToPlayer <= brain.attackRange)
            {
                brain.RequestStateChange(new AttackState(brain));
                return;
            }
        }   

        // 4) 시야 상실 + 목적지 도착 -> Search
        if (seen == false)
        {
            if (distToLast <= brain.stoppingDistance)
            {
                brain.RequestStateChange(new SearchState(brain));
                return;
            }
        }
    }

    public override void OnExit()
    {
        // 없음
    }
}
