using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 이동 속도에 따라 스텝 템포를 계산하고, 왼발/오른발 이벤트를 교대로 발생.
/// PlayerController + CharacterController와 함께 동작.
/// </summary>
[DisallowMultipleComponent]
public class StepEventSource : MonoBehaviour
{
    [Header("Refs")]
    public CharacterController controller; // 현재 속도 조회용.
    public PlayerController player;        // 걷기/달리기 상태 참조(없으면 속도만으로 판단)

    [Header("Step Timing")]
    public float walkStepsPerSecond = 1.8f;   // 걷기 템포(Hz)
    public float sprintStepsPerSecond = 2.6f; // 달리기 템포(Hz)
    public float minSpeedForSteps = 1.0f;     // 이 속도 미만이면 스텝 발생 X(정지/아주 느린 이동 억제)
    public float groundedGraceTime = 0.08f;   // 접지 이탈 후 짧은 시간 내에는 스텝 허용(계단/턱 보정)

    [Header("Events")]
    public UnityEvent onStepLeft;   // 왼발.
    public UnityEvent onStepRight;  // 오른발.

    private bool leftNext = true;   // 다음 발: true=왼발, false=오른발.
    private float nextStepTime;     // 다음 스텝 발생 예정 시각(절대시간)
    private float lastGroundedTime; // 최근 접지 시각(간단한 코요테 용도)

    private void Awake()
    {
        // 참조 자동 캐시(수동 연결도 가능)
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
        if (player == null)
        {
            player = GetComponent<PlayerController>();
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float now = Time.time;

        bool grounded = false;
        if (controller != null)
        {
            grounded = controller.isGrounded;
        }

        if (grounded == true)
        {
            lastGroundedTime = now;
        }

        // 현재 수평 속도(스텝 템포 산정)
        float planarSpeed = 0.0f;
        if (controller != null)
        {
            Vector3 v = controller.velocity;
            v.y = 0.0f;
            planarSpeed = v.magnitude;
        }

        // 최소 속도 미만이면 타이머 리셋(스텝 중지)
        if (planarSpeed < minSpeedForSteps)
        {
            if (now > nextStepTime)
            {
                nextStepTime = now; // 즉시 발행 방지.
            }
            return;
        }

        // 걷기/달리기 템포 결정.
        float stepsHz = walkStepsPerSecond;
        if (player != null)
        {
            // 스프린트 여부는 PlayerController 내부 상태로 판단(필드가 없으면 속도 기준)
            // player의 sprintHeld가 private이면 속도 기준으로만 동작해도 충분.
        }
        if (planarSpeed > (minSpeedForSteps + 2.0f))
        {
            stepsHz = sprintStepsPerSecond;
        }

        // 다음 스텝 시간 초기화(최초 진입 시)
        if (nextStepTime <= 0.0f)
        {
            nextStepTime = now + (1.0f / stepsHz);
        }

        // 접지 또는 코요테 시간 내에서만 스텝을 발생.
        bool canStep = false;
        if (grounded == true)
        {
            canStep = true;
        }
        else
        {
            if ((now - lastGroundedTime) <= groundedGraceTime)
            {
                canStep = true;
            }
        }

        if (canStep == true)
        {
            if (now >= nextStepTime)
            {
                if (leftNext == true)
                {
                    if (onStepLeft != null)
                    {
                        onStepLeft.Invoke();
                    }
                    leftNext = false;
                }
                else
                {
                    if (onStepRight != null)
                    {
                        onStepRight.Invoke();
                    }
                    leftNext = true;
                }

                nextStepTime = now + (1.0f / stepsHz);
            }
        }
    }
}
