using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 대상(플레이어/적) 쪽 '상태효과 컨테이너'.
/// - Add/Remove/Find/Update를 담당.
/// - 외부 시스템(이동/공격/입력)이 질의할 수 있는 보조 API 제공.
/// </summary>
public class StatusEffectHost : MonoBehaviour
{
    // 현재 활성 효과 리스트.
    private List<StatusEffectBase> effects = new List<StatusEffectBase>(); // 활성 인스턴스 목록.

    // 캐시된 쿼리 값(성능/가독성)
    private float cachedSpeedMul = 1.0f;   // 모든 슬로우/버프를 반영한 최종 이동속도 배율.
    private bool cachedStunned = false;    // 현재 스턴 중인지.

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1) 효과 틱.
        for (int i = 0; i < effects.Count; i = i + 1)
        {
            StatusEffectBase e = effects[i];
            if (e != null)
            {
                e.Tick(dt);
            }
        }

        // 2) 만료 제거(뒤에서 앞으로)
        for (int i = effects.Count - 1; i >= 0; i = i - 1)
        {
            StatusEffectBase e = effects[i];
            if (e == null)
            {
                effects.RemoveAt(i);
                continue;
            }
            bool expired = e.IsExpired();
            if (expired == true)
            {
                e.Detach();
                Destroy(e);
                effects.RemoveAt(i);
            }
        }

        // 3) 쿼리 캐시 갱신.
        RebuildCachedQueryValues();
    }

    /// <summary>
    /// 효과를 추가. 같은 타입이면 OnReapplied 호출(클래스별 재적용 규칙 내장).
    /// 프리팹을 인스턴스화하여 이 호스트 객체에 붙인다.
    /// T : 템플릿(Template) -> 자료형을 미리 정하지 않는다.
    /// </summary>
    public T AddEffect<T>(T effectPrefab) where T : StatusEffectBase
    {
        if (effectPrefab == null)
        {
            return null;
        }

        // 같은 타입의 기존 효과 찾기.
        T found = FindEffect<T>();
        if (found != null)
        {
            found.OnReapplied();
            return found;
        }

        // 새 인스턴스 생성.
        T inst = gameObject.AddComponent<T>();
        CopyFields(effectPrefab, inst); // 프리셋 값 복사(간단 복사)
        inst.Attach(this);
        effects.Add(inst);

        RebuildCachedQueryValues();
        return inst;
    }

    /// <summary>
    /// 같은 타입의 첫 효과를 반환(없으면 null).
    /// </summary>
    public T FindEffect<T>() where T : StatusEffectBase
    {
        for (int i = 0; i < effects.Count; i = i + 1)
        {
            T casted = effects[i] as T;
            if (casted != null)
            {
                return casted;
            }
        }
        return null;
    }

    /// <summary>
    /// 캐시값 재계산: 이동속도 배율, 스턴 여부 등.
    /// </summary>
    private void RebuildCachedQueryValues()
    {
        // 지역 변수들: 계산 누적용
        float speedMul = 1.0f;
        bool stunned = false;

        // 1) 모든 효과를 훑어 최종치 계산.
        for (int i = 0; i < effects.Count; i = i + 1)
        {
            StatusEffectBase e = effects[i];

            // 슬로우: 가장 낮은(가장 강한) 배율만 반영.
            StatusEffect_Slow slow = e as StatusEffect_Slow;
            if (slow != null)
            {
                float m = slow.GetSpeedMultiplier();
                if (m < speedMul)
                {
                    speedMul = m;
                }
            }

            // 스턴: 하나라도 있으면 true
            StatusEffect_Stun stun = e as StatusEffect_Stun;
            if (stun != null)
            {
                if (stun.IsStunned() == true)
                {
                    stunned = true;
                }
            }
        }

        cachedSpeedMul = speedMul;
        cachedStunned = stunned;
    }

    /// <summary>
    /// 외부(이동 코드)에서 호출: 이동속도 최종 배율 반환(0~1).
    /// </summary>
    public float GetSpeedMultiplier()
    {
        float v = cachedSpeedMul;
        return v;
    }

    /// <summary>
    /// 외부(공격/입력)에서 호출: 스턴 상태인지 여부.
    /// </summary>
    public bool IsStunned()
    {
        bool v = cachedStunned;
        return v;
    }

    /// <summary>
    /// 간단한 필드 복사(프리셋 -> 인스턴스). 필요 필드만 복사.
    /// </summary>
    private void CopyFields(StatusEffectBase src, StatusEffectBase dst)
    {
        if (src == null)
        {
            return;
        }
        if (dst == null)
        {
            return;
        }

        dst.effectName = src.effectName;
        dst.icon = src.icon;
        dst.duration = src.duration;
        dst.refreshOnReapply = src.refreshOnReapply;
    }

    /// <summary>
    /// 현재 활성 효과 리스트를 읽기(아이콘 UI 표시 등).
    /// </summary>
    public List<StatusEffectBase> GetActiveEffects()
    {
        List<StatusEffectBase> list = new List<StatusEffectBase>(effects);
        return list;
    }
}
