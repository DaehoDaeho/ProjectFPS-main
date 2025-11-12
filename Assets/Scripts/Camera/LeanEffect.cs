using UnityEngine;

/// <summary>
/// 좌/우 가속도 기반 Lean(롤). 코너링/급선회에서 체감이 확 올라감.
/// </summary>
public class LeanEffect : MonoBehaviour, ICameraEffect
{
    [Header("Refs")]
    public LocomotionFeed feed;                    // 수평 속도 벡터 제공.

    [Header("Params")]
    public float degreesPerMps2 = 0.9f;            // 1 m/s^2 당 기울임 각(도)
    public float maxDegrees = 14.0f;               // 최대 기울임(도)
    public float accelSmooth = 10f;                // 가속도 저역통과.
    public float response = 14f;                   // 최종 각도 EMA 수렴 속도.

    private Vector3 lastVel;                       // 직전 프레임 수평 속도 벡터.
    private Vector3 smoothedAccel;                 // 평활화된 가속도 벡터.
    private float currentDeg;                      // 현재 롤 각(도)

    public Vector3 CurrentPositionOffset { get { return Vector3.zero; } }
    public Vector3 CurrentRotationOffsetEuler { get { return new Vector3(0f, 0f, -currentDeg); } }
    public float CurrentFovOffset { get { return 0f; } }

    private void Update()
    {
        if (feed == null)
        {
            return;
        }

        float dt = Time.deltaTime;                 // 프레임 시간.
        Vector3 v = feed.HorizontalVelocity;       // 수평 속도 벡터.
        Vector3 a = Vector3.zero;                  // 가속도 근사.

        a = (v - lastVel) / Mathf.Max(dt, 0.0001f);
        lastVel = v;

        // 가속도 저역통과.
        float alpha = 1f - Mathf.Exp(-accelSmooth * dt); // EMA 계수.
        smoothedAccel = Vector3.Lerp(smoothedAccel, a, alpha);

        // 우측 축으로 투영
        Vector3 right = feed.PlayerRight;          // 플레이어 우측 축.
        float lateralAccel = Vector3.Dot(smoothedAccel, right); // 좌우 가속 성분.

        float targetDeg = degreesPerMps2 * lateralAccel;        // 가속 -> 각도 매핑.
        targetDeg = Mathf.Clamp(targetDeg, -maxDegrees, maxDegrees);

        float k = 1f - Mathf.Exp(-response * dt);  // EMA 계수.
        currentDeg = Mathf.Lerp(currentDeg, targetDeg, k);
    }
}
