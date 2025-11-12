using UnityEngine;

/// <summary>
/// 속도 임계 기반이 아닌 '가속도 기반' FOV Kick.
/// - 가속이 +일 때만 FOV를 올리고, 0~음수일 때는 부드럽게 복귀.
/// </summary>
public class FovKickEffect : MonoBehaviour, ICameraEffect
{
    [Header("Refs")]
    public LocomotionFeed feed;                // 속도 데이터.

    [Header("Params")]
    public float accelSensitivity = 0.8f;      // 가속 -> FOV 매핑 계수(도/(m/s^2))
    public float maxKick = 8.0f;               // 최대 FOV 증가 각(도)
    public float riseTime = 0.10f;             // 가속 시 상승 시간.
    public float fallTime = 0.18f;             // 비가속 시 하강 시간.
    public float speedSmoothing = 8f;          // 속도 저역통과(노이즈 억제)

    private float smoothedSpeed;               // 저역통과된 속도(m/s).
    private float lastSmoothedSpeed;           // 직전 프레임 속도.
    private float current;                     // 현재 FOV 오프셋(도)
    private float velocity;                    // SmoothDamp 내부 속도.

    public Vector3 CurrentPositionOffset { get { return Vector3.zero; } }
    public Vector3 CurrentRotationOffsetEuler { get { return Vector3.zero; } }
    public float CurrentFovOffset { get { return current; } }

    private void Update()
    {
        if (feed == null)
        {
            return;
        }

        float dt = Time.deltaTime;                         // 프레임 시간.
        float rawSpeed = feed.HorizontalSpeed;             // 원시 속도.

        // 저역통과로 속도 평활화.
        float alpha = 1f - Mathf.Exp(-speedSmoothing * dt); // EMA 계수.
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawSpeed, alpha);

        // 가속도 근사(미분)
        float accel = 0f;                                   // m/s^2 근사.
        accel = (smoothedSpeed - lastSmoothedSpeed) / Mathf.Max(dt, 0.0001f);
        lastSmoothedSpeed = smoothedSpeed;

        // 목표 FOV: 양의 가속에서만 상승, 아니면 0으로 복귀.
        float target = 0f;

        if (accel > 0f)
        {
            target = Mathf.Clamp(accel * accelSensitivity, 0f, maxKick);
        }
        else
        {
            target = 0f;
        }

        float smoothTime = 0.15f;

        if (target > current)
        {
            smoothTime = riseTime;
        }
        else
        {
            smoothTime = fallTime;
        }

        current = Mathf.SmoothDamp(current, target, ref velocity, smoothTime, Mathf.Infinity, dt);
    }
}
