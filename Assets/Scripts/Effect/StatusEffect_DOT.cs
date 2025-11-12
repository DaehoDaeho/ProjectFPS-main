using UnityEngine;

/// <summary>
/// DoT(초당 피해) 상태효과. 틱 간격마다 IDamageable로 피해 전달.
/// - 스택 최대치 지원: 재적용 시 스택↑, DPS 누적.
/// - refreshOnReapply=true면 재적용 시 남은 시간 갱신.
/// </summary>
public class StatusEffect_DOT : StatusEffectBase
{
    [Header("DOT")]
    public float dpsPerStack = 5.0f;         // 스택당 초당 피해.
    public float tickInterval = 0.5f;        // 피해 틱 간격(초)
    public int maxStacks = 5;                // 최대 스택.
    public bool extendDurationOnReapply = true; // 재적용 시 duration 갱신할지.

    // 내부 상태
    private int stacks;                      // 현재 스택 수.
    private float tickTimer;                 // 다음 틱까지 남은 시간.
    private IDamageable damageable;          // 대상의 IDamageable 참조.

    protected override void OnAttached()
    {
        // 초기화.
        stacks = 1;
        tickTimer = tickInterval;
        damageable = GetComponent<IDamageable>();
    }

    public override void OnReapplied()
    {
        // 1) 스택 증가(상한 캡)
        stacks = stacks + 1;
        if (stacks > maxStacks)
        {
            stacks = maxStacks;
        }

        // 2) 지속시간 갱신 옵션.
        if (extendDurationOnReapply == true)
        {
            timeLeft = duration;
        }
    }

    protected override void OnTick(float dt)
    {
        // 틱 타이머 감소.
        tickTimer = tickTimer - dt;
        if (tickTimer > 0.0f)
        {
            return;
        }

        // 틱 시점 도달: 피해 계산 및 전달.
        tickTimer = tickInterval;

        // 이번 틱의 피해량(초당 피해 × 간격 × 스택)
        float dmg = dpsPerStack * tickInterval * stacks;

        if (damageable != null)
        {
            // 연출 좌표: 간단히 대상 중심.
            Vector3 hp = transform.position;
            Vector3 n = Vector3.up;
            damageable.ApplyDamage(dmg, hp, n, host != null ? host.transform : transform);
        }
    }

    protected override void OnDetached()
    {
        // DoT는 별도 원복 없음(슬로우처럼 배율 조정이 아님)
    }

    /// <summary>현재 스택 수 반환(UI 표시용).</summary>
    public int GetStacks()
    {
        int v = stacks;
        return v;
    }
}
