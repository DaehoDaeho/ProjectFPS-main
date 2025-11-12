using UnityEngine;

/// <summary>
/// 충돌 지점에서 대상의 StatusEffectHost를 찾아 '효과 프리팹'을 부여하는 간단 유틸.
/// - 발사체/폭발/트랩 등이 히트 시점에 호출하면 된다.
/// - 프리셋 값을 가진 효과 컴포넌트를 '복제'하여 대상에 붙인다.
/// </summary>
public static class StatusEffectApplier
{
    /// <summary>
    /// 대상에 효과를 한 종류 추가. 동일 타입 존재 시 OnReapplied만 호출.
    /// </summary>
    public static T ApplyTo<T>(GameObject target, T effectPreset) where T : StatusEffectBase
    {
        Debug.Log("Apply Status Effect : " + effectPreset.GetType());
        if (target == null)
        {
            return null;
        }
        if (effectPreset == null)
        {
            return null;
        }

        StatusEffectHost host = target.GetComponent<StatusEffectHost>();
        if (host == null)
        {
            // 대상에 호스트가 없으면 효과를 붙일 수 없다(디자인 상 '효과를 받을 수 없는' 대상).
            return null;
        }

        T inst = host.AddEffect(effectPreset);
        return inst;
    }
}
