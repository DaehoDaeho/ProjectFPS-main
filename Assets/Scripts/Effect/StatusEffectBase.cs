using UnityEngine;

/// <summary>
/// 모든 상태효과의 공통 기반 클래스(추상).
/// - 수명/갱신/만료를 위한 공통 필드와 수명주기 훅 제공.
/// - StatusEffectHost가 생성/부착/갱신/해제 호출을 담당.
/// </summary>
public abstract class StatusEffectBase : MonoBehaviour
{
    [Header("Common")]
    public string effectName = "Effect";      // 디버그/표시용 이름.
    public Sprite icon;                       // UI 아이콘(선택)
    public float duration = 3.0f;             // 총 지속시간(초)
    public bool refreshOnReapply = true;      // 동일 효과 재적용 시 지속시간 갱신할지.

    // 내부 상태
    protected float timeLeft;                 // 남은 시간(초)
    protected StatusEffectHost host;          // 소유자(대상)의 호스트.

    /// <summary>
    /// 호스트가 부착 시 호출. 파라미터로 호스트 주입 및 남은시간 초기화.
    /// </summary>
    public void Attach(StatusEffectHost h)
    {
        host = h;
        timeLeft = duration;

        OnAttached();
    }

    /// <summary>
    /// 매 프레임(혹은 고정 틱) 갱신. Host.Update 에서 호출됨.
    /// </summary>
    public void Tick(float dt)
    {
        OnTick(dt);

        timeLeft = timeLeft - dt;
        if (timeLeft < 0.0f)
        {
            timeLeft = 0.0f;
        }
    }

    /// <summary>
    /// 만료 여부(남은 시간이 0인지). Host가 이 값으로 제거 시점을 판단.
    /// </summary>
    public bool IsExpired()
    {
        bool expired = timeLeft <= 0.0f;
        return expired;
    }

    /// <summary>
    /// 동일 타입이 재적용될 때 호출. 기본은 '지속시간 갱신'만 수행.
    /// 파생 클래스에서 스택 증가 등 추가 동작을 구현 가능.
    /// </summary>
    public virtual void OnReapplied()
    {
        if (refreshOnReapply == true)
        {
            timeLeft = duration;
        }
    }

    /// <summary>
    /// 호스트에서 제거될 때 호출(정리/원복).
    /// </summary>
    public void Detach()
    {
        OnDetached();
    }

    /// <summary>파생 클래스가 부착 시 훅.</summary>
    protected virtual void OnAttached() { }
    /// <summary>파생 클래스가 매 프레임 갱신 훅.</summary>
    protected virtual void OnTick(float dt) { }
    /// <summary>파생 클래스가 해제 시 훅.</summary>
    protected virtual void OnDetached() { }

    /// <summary>UI 등에 표시할 남은 시간 초를 반환.</summary>
    public float GetTimeLeft()
    {
        float v = timeLeft;
        return v;
    }
}
