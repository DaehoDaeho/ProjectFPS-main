using UnityEngine;

/// <summary>
/// Search: 시야를 잃은 후 마지막 좌표 근방을 짧게 수색.
/// 타임아웃 시 Idle 복귀, 다시 보이면 Chase.
/// </summary>
public class SearchState : EnemyState
{
    public SearchState()
    {

    }

    public SearchState(EnemyBrain b)
    {
        brain = b;
    }

    public override string Name()
    {
        return "Search";
    }

    public override void OnEnter()
    {
        // 수색 타이머 초기화.
        brain.searchTimer = brain.searchDuration;
    }

    public override void OnUpdate(float dt)
    {
        // 1) 시야 회복 시 즉시 Chase
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

        if (seen == true)
        {
            brain.RequestStateChange(new ChaseState(brain));
            return;
        }

        // 2) 마지막 좌표 쪽으로 느리게 접근.
        brain.FacePosition(brain.lastKnownPos, dt);

        float dist = Vector3.Distance(brain.transform.position, brain.lastKnownPos);
        if (dist > brain.stoppingDistance)
        {
            brain.MoveForward(brain.searchSpeed, dt);
        }

        // 3) 타임아웃 -> Idle
        brain.searchTimer = brain.searchTimer - dt;
        if (brain.searchTimer <= 0.0f)
        {
            brain.RequestStateChange(new IdleState(brain));
            return;
        }
    }

    public override void OnExit()
    {
        // 없음.
    }
}
