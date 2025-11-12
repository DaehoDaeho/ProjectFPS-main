using UnityEngine;

/// <summary>
/// 스턴 상태효과. 지속시간 동안 '행동 금지' 플래그를 제공.
/// - Host.IsStunned()로 외부 시스템이 체크하여 행동을 막는다.
/// </summary>
public class StatusEffect_Stun : StatusEffectBase
{
    protected override void OnAttached()
    {
        // 부착 시 별도 로직 없음. 질의는 Host.IsStunned()로.
    }

    protected override void OnTick(float dt)
    {
        // 수명 감소만 처리(부모 Tick에서 이미 수행)
    }

    protected override void OnDetached()
    {
        // 해제 시 별도 처리 없음.
    }

    public bool IsStunned()
    {
        // 스턴은 살아있는 동안 true로 간주
        bool v = GetTimeLeft() > 0.0f;
        return v;
    }
}
