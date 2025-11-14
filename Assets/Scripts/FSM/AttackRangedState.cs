using UnityEngine;

public class AttackRangedState : EnemyState
{
    public AttackRangedState()
    {

    }

    public AttackRangedState(EnemyBrain b)
    {
        brain = b;
    }

    public override string Name()
    {
        return "AttackRanged";
    }

    public override void OnEnter()
    {
        brain.attackRangedTimer = 0.0f;
    }

    public override void OnUpdate(float dt)
    {
        if (brain.host != null)
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

        if(brain.enemyType == EnemyType.Hybrid)
        {
            if (dist <= brain.attackRange)
            {
                brain.RequestStateChange(new AttackState(brain));
                return;
            }
        }

        if (dist > brain.attackRangedRange)
        {
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
        if (brain.attackRangedTimer > 0.0f)
        {
            brain.attackRangedTimer = brain.attackRangedTimer - dt;
            if (brain.attackRangedTimer < 0.0f)
            {
                brain.attackRangedTimer = 0.0f;
            }
        }

        // 5) 쿨다운이 0이면 공격 실행
        if (brain.attackRangedTimer <= 0.0f)
        {
            //DoAttack();
            if (brain.animator != null)
            {
                brain.animator.SetTrigger("AttackRanged");
            }

            brain.attackRangedTimer = brain.attackRangedCooldown;
        }

        // 6) 시각적 정렬: 플레이어를 바라보게 회전
        if (brain.player != null)
        {
            brain.FacePosition(brain.player.position, dt);
        }
    }

    public override void OnExit()
    {
        // 없음
    }
}
