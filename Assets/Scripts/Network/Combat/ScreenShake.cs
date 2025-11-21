using UnityEngine;

/// <summary>
/// 짧은 진폭으로 화면을 흔드는 간단한 셰이크.
/// - 카메라 피벗의 localPosition을 약간 흔들었다가 원위치.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    public Transform shakePivot;        // 흔들 기준 피벗(카메라 위 부모 노드 권장)
    public float shakeReturnSpeed = 10.0f; // 복귀 속도

    private float shakeStrength;        // 현재 셰이크 세기
    private float shakeTimeRemain;      // 남은 셰이크 시간
    private Vector3 baseLocalPos;       // 원래 로컬 위치

    private void Start()
    {
        if (shakePivot != null)
        {
            baseLocalPos = shakePivot.localPosition;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (shakeTimeRemain > 0.0f)
        {
            shakeTimeRemain = shakeTimeRemain - dt;
            if (shakePivot != null)
            {
                // 간단 잡음
                Vector3 offset = new Vector3(
                    (Random.value - 0.5f) * shakeStrength,
                    (Random.value - 0.5f) * shakeStrength,
                    0.0f
                );
                shakePivot.localPosition = baseLocalPos + offset;
            }
        }
        else
        {
            // 원위치로 복귀
            if (shakePivot != null)
            {
                shakePivot.localPosition = Vector3.Lerp(shakePivot.localPosition, baseLocalPos, dt * shakeReturnSpeed);
            }
        }
    }

    public void ShakeOnce(float strength, float duration)
    {
        shakeStrength = strength;
        shakeTimeRemain = duration;
    }
}
