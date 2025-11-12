using UnityEngine;

/// <summary>
/// 슬로우 상태효과. 이동속도 배율(0~1)을 제공.
/// - Host는 모든 슬로우 중 '가장 낮은 배율'만 반영(가장 강한 슬로우 우선)
/// </summary>
public class StatusEffect_Slow : StatusEffectBase
{
    [Header("Slow")]
    [Range(0.0f, 1.0f)]
    public float speedMultiplier = 0.6f; // 적용 배율(예: 0.6이면 40% 감소)

    protected override void OnAttached()
    {
        // 원복 필요 없음: 호스트가 '최종 배율'을 매 프레임 재계산.
    }

    protected override void OnTick(float dt)
    {
        // 슬로우는 틱 처리 필요 없음(수명 감소만 처리됨)
    }

    protected override void OnDetached()
    {
        // 별도 원복 없음(호스트가 매 프레임 최종 배율을 재계산하므로 자동 원복)
    }

    public float GetSpeedMultiplier()
    {
        float v = speedMultiplier;
        return v;
    }
}
