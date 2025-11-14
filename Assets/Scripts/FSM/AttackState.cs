using UnityEngine;

/// <summary>
/// Attack: 사거리 내에서 쿨다운을 지키며 플레이어를 공격.
/// 사거리 이탈 시 Chase, 시야 완전 상실 시 Search.
/// </summary>
public class AttackState : EnemyState
{
    public AttackState()
    {

    }

    public AttackState(EnemyBrain b)
    {
        brain = b;
    }

    public override string Name()
    {
        return "Attack";
    }

    public override void OnEnter()
    {
        // 공격 상태 진입 시 쿨다운을 즉시 0으로 두면 바로 한 번 공격 가능.
        brain.attackTimer = 0.0f;
    }

    public override void OnUpdate(float dt)
    {
        if(brain.host != null)
        {
            if (brain.host.IsStunned() == true)
            {
                // 이동/공격/마우스입력 처리 중단.
                return;
            }
        }

        // 1) 시야 판단 + lastKnownPos 갱신
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

        // 2) 사거리 유지 체크: 이탈 시 Chase 복귀
        float dist = brain.DistanceToPlayer();        
        if (dist > brain.attackRange)
        {
            if(brain.enemyType == EnemyType.Hybrid)
            {
                if (dist <= brain.attackRangedRange)
                {
                    brain.RequestStateChange(new AttackRangedState(brain));
                    return;
                }
            }

            brain.RequestStateChange(new ChaseState(brain));
            return;
        }

        // 3) 시야 완전히 끊기면 Search
        if (seen == false)
        {
            brain.RequestStateChange(new SearchState(brain));
            return;
        }

        // 4) 공격 쿨다운 타이머
        if (brain.attackTimer > 0.0f)
        {
            brain.attackTimer = brain.attackTimer - dt;
            if (brain.attackTimer < 0.0f)
            {
                brain.attackTimer = 0.0f;
            }
        }

        // 5) 쿨다운이 0이면 공격 실행
        if (brain.attackTimer <= 0.0f)
        {
            //DoAttack();
            if (brain.animator != null)
            {
                brain.animator.SetTrigger("Attack");
            }

            brain.attackTimer = brain.attackCooldown;
        }

        // 6) 시각적 정렬: 플레이어를 바라보게 회전
        if (brain.player != null)
        {
            brain.FacePosition(brain.player.position, dt);
        }
    }

    public override void OnExit()
    {
        // 없음.
    }
}
