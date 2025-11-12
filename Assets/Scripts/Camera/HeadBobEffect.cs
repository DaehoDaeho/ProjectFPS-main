using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Head Bob에 속도-진폭 곡선과 스텝 동기 이벤트 추가.
/// </summary>
public class HeadBobEffect : MonoBehaviour, ICameraEffect
{
    [Header("Refs")]
    public LocomotionFeed feed;                          // 속도/접지 데이터.

    [Header("Shape")]
    public float frequencyPerMps = 2.2f;                 // 1 m/s당 주파수 증가량.
    public AnimationCurve amplitudeBySpeed =             // 속도 -> 진폭 스케일 곡선(0~10m/s 가정)
        AnimationCurve.EaseInOut(0f, 1.2f, 6f, 1.0f);    // 저속에서 과감, 고속에서 완만.

    public float baseAmplitudeX = 0.05f;                 // 기본 좌우 진폭(미터)
    public float baseAmplitudeY = 0.035f;                // 기본 상하 진폭(미터)

    public float runSpeedThreshold = 4.5f;               // 달리기 임계.
    public float airborneDamping = 6f;                   // 공중 감쇠 속도.
    public float smooth = 16f;                           // 지상 수렴 속도.

    [Header("Step Events")]
    public UnityEvent onStepLeft;                        // 왼발 지면 접촉 타이밍 이벤트.
    public UnityEvent onStepRight;                       // 오른발 지면 접촉 타이밍 이벤트.

    private float phase;                                 // 진동 위상(라디안)
    private Vector3 offset;                              // Mixer로 전달할 위치 오프셋.
    private bool leftFootNext = true;                    // 다음 스텝이 왼발인지 여부.
    private float lastCycle;                             // 직전 사이클 인덱스(스텝 트리거용)

    public Vector3 CurrentPositionOffset { get { return offset; } }
    public Vector3 CurrentRotationOffsetEuler { get { return Vector3.zero; } }
    public float CurrentFovOffset { get { return 0f; } }

    private void Update()
    {
        if (feed == null)
        {
            return;
        }

        float dt = Time.deltaTime;                       // 프레임 시간.
        float speed = feed.HorizontalSpeed;              // 수평 속도.

        float freq = frequencyPerMps * speed;            // 속도 기반 주파수(Hz 유사)
        phase += freq * dt * Mathf.PI * 2f;              // 위상 업데이트.

        // 사이클 인덱스(0..1..2..): 스텝 타이밍 잡기.
        float cycle = phase / (Mathf.PI * 2f);           // 현재 누적 사이클 수.
        if (Mathf.FloorToInt(cycle) > Mathf.FloorToInt(lastCycle))
        {
            if (leftFootNext == true)
            {
                if (onStepLeft != null)
                {
                    onStepLeft.Invoke();
                }
            }
            else
            {
                if (onStepRight != null)
                {
                    onStepRight.Invoke();
                }
            }
            leftFootNext = !leftFootNext;
        }
        lastCycle = cycle;

        // 속도 기반 진폭 스케일.
        float ampScale = 1f;
        float curveEval = amplitudeBySpeed.Evaluate(speed); // 속도 -> 스케일.
        ampScale *= curveEval;

        if (speed > runSpeedThreshold)
        {
            ampScale *= 1.15f; // 달리기에서 살짝 증폭.
        }

        Vector3 target = Vector3.zero;                   // 목표 오프셋.

        if (feed.IsGrounded == true)
        {
            float x = Mathf.Sin(phase) * baseAmplitudeX * ampScale;               // 좌우.
            float y = Mathf.Abs(Mathf.Sin(phase * 2f)) * baseAmplitudeY * ampScale; // .상하(압박감)
            target = new Vector3(x, -y, 0f);
        }
        else
        {
            target = Vector3.zero;
        }

        if (feed.IsGrounded == true)
        {
            float k = 1f - Mathf.Exp(-smooth * dt);       // 지상 보간 계수.
            offset = Vector3.Lerp(offset, target, k);
        }
        else
        {
            float k = 1f - Mathf.Exp(-airborneDamping * dt); // 공중 감쇠 계수.
            offset = Vector3.Lerp(offset, Vector3.zero, k);
        }
    }
}
