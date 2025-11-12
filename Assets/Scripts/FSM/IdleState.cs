using UnityEngine;

/// <summary>
/// Idle: 대기. 플레이어를 보면 Chase로 전이.
/// </summary>
public class IdleState : EnemyState
{
    // 생성자. 클래스 이름과 동일한 함수. 클래스 객체가 만들어질때 자동으로 호출되는 함수.
    // 데이터 초기화 등을 할 때 유용.
    public IdleState()
    {

    }

    public IdleState(EnemyBrain b)
    {
        brain = b;
    }

    public override string Name()
    {
        return "Idle";
    }

    public override void OnEnter()
    {
        // Idle 진입 시 특별한 초기화는 없음.
        // 필요하면 대기 모션/사운드 트리거 가능.
    }

    public override void OnUpdate(float dt)
    {
        // 시야 판정.
        bool seen = false;
        Vector3 seenPos = Vector3.zero;

        if (brain.senses != null)
        {
            bool can = brain.senses.CanSeeTarget(out seenPos);
            if (can == true)
            {
                seen = true;
            }
        }

        if (seen == true)
        {
            brain.lastKnownPos = seenPos;
            brain.RequestStateChange(new ChaseState(brain)); // 새 인스턴스 혹은 재사용 가능
            return;
        }

        // Idle 유지.
    }

    public override void OnExit()
    {
        // Idle 종료 시 정리할 것 없음.
    }
}
