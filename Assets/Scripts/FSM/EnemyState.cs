using UnityEngine;

/// <summary>
/// 모든 적 상태의 추상 기반 클래스.
/// 각 상태는 Enter/Update/Exit 수명주기를 통해 동작하며,
/// EnemyBrain(컨텍스트)의 공유 데이터를 사용한다.
/// </summary>
public abstract class EnemyState
{
    /// <summary>
    /// 상태가 사용할 컨텍스트(공유 데이터와 유틸성 메서드 제공).
    /// </summary>
    protected EnemyBrain brain; // 현재 적 개체의 브레인(변경/전이/공유 값 접근)

    /// <summary>
    /// 상태 인스턴스가 사용할 컨텍스트를 주입한다.
    /// 모든 상태는 전환 시에 동일 브레인을 공유한다.
    /// </summary>
    public void SetContext(EnemyBrain b)
    {
        brain = b;
    }

    /// <summary>
    /// 상태 진입 시 호출된다. 애니/사운드/초기 타이머 세팅 등을 수행한다.
    /// </summary>
    public virtual void OnEnter()
    {
        // 파생 클래스에서 필요 시 구현.
    }

    /// <summary>
    /// 매 프레임 호출된다. 상태 고유의 로직을 수행하고,
    /// 필요 시 brain.RequestStateChange(...)로 전이 요청을 한다.
    /// </summary>
    public virtual void OnUpdate(float dt)
    {
        // 파생 클래스에서 구현.
    }

    /// <summary>
    /// 상태 종료 직전에 호출된다. 타이머/플래그를 정리한다.
    /// </summary>
    public virtual void OnExit()
    {
        // 파생 클래스에서 필요 시 구현.
    }

    /// <summary>
    /// 디버그용 상태명 반환.
    /// </summary>
    public abstract string Name();
}
