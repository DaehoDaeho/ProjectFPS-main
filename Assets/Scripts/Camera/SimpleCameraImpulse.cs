using UnityEngine;

/// <summary>
/// 스텝 이벤트 등 순간 임펄스에 반응해 카메라 Pivot을 아주 짧게 흔든다.
/// Cinemachine 없이도 간단히 사용 가능(멀미 방지: 진폭/시간을 짧게 유지).
/// </summary>
[DisallowMultipleComponent]
public class SimpleCameraImpulse : MonoBehaviour
{
    public Transform target;          // 흔들 대상(보통 CameraPivot)
    public float positionAmplitude = 0.02f; // 최대 위치 오프셋(m)
    public float rotationAmplitude = 0.6f;  // 최대 회전 오프셋(°)
    public float duration = 0.08f;          // 임펄스 지속 시간(초)

    private float timeLeft;                  // 남은 시간.
    private Vector3 baseLocalPos;            // 시작 로컬 위치 백업.
    private Quaternion baseLocalRot;         // 시작 로컬 회전 백업.
    private bool initialized;                // 초기화 여부.

    private void Awake()
    {
        if (target == null)
        {
            target = transform; // 자기 자신이 Pivot일 수도 있음.
        }
    }

    private void OnEnable()
    {
        if (target != null)
        {
            baseLocalPos = target.localPosition;
            baseLocalRot = target.localRotation;
            initialized = true;
        }
    }

    private void Update()
    {
        if (initialized == false)
        {
            return;
        }

        // 시간이 남아 있으면 감쇠하며 흔든다.
        if (timeLeft > 0.0f)
        {
            float t = timeLeft / duration;        // 1 -> 0
            float falloff = Mathf.SmoothStep(0.0f, 1.0f, t); // 부드러운 감쇠.

            // 간단한 난수 기반 흔들림(프레임마다 달라짐)
            Vector3 posJitter = new Vector3(
                Random.Range(-positionAmplitude, positionAmplitude),
                Random.Range(-positionAmplitude, positionAmplitude),
                Random.Range(-positionAmplitude, positionAmplitude)
            ) * falloff;

            Vector3 rotJitterEuler = new Vector3(
                Random.Range(-rotationAmplitude, rotationAmplitude),
                Random.Range(-rotationAmplitude, rotationAmplitude),
                0.0f
            ) * falloff;

            target.localPosition = baseLocalPos + posJitter;
            target.localRotation = baseLocalRot * Quaternion.Euler(rotJitterEuler);

            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0.0f)
            {
                // 종료 시 원위치 복구
                target.localPosition = baseLocalPos;
                target.localRotation = baseLocalRot;
            }
        }
    }

    /// <summary>
    /// 외부에서 호출: 임펄스를 1회 트리거.
    /// </summary>
    public void Pulse()
    {
        // 상태 설정 후 종료.
        if (initialized == true)
        {
            timeLeft = duration;
        }
    }
}
