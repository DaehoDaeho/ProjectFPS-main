using UnityEngine;

/// <summary>
/// Dead: 사망 상태. 아무 것도 하지 않음.
/// Health.onDeath 이벤트에서 파괴/드랍/애니를 처리하는 구성을 권장.
/// </summary>
public class DeadState : EnemyState
{
    public DeadState()
    {

    }

    public DeadState(EnemyBrain b)
    {
        brain = b;
    }

    public override string Name()
    {
        return "Dead";
    }

    public override void OnEnter()
    {
        // 사망 상태 진입 시 모든 행동 중지.
        // 이 상태에서는 Update를 해도 아무 동작을 하지 않는다.
    }

    public override void OnUpdate(float dt)
    {
        // 죽은 후에는 로직 없음.
    }

    public override void OnExit()
    {
        // 일반적으로 Dead에서 다른 상태로 나갈 일은 없다.
    }
}
