using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스로 시야를 회전시키는 스크립트(3일차 'MouseLook' 패치).
/// - Yaw(좌우)는 playerBody(Y축 회전), Pitch(상하)는 cameraPivot(로컬 X회전).
/// - 시작 시 현재 Transform에서 yaw/pitch를 읽어 초기 튐 방지.
/// - ESC로 커서 잠금/해제 토글(입력 0 현상 방지).
/// - New Input System의 PlayerInput → OnLook 이벤트에 연결해 사용.
/// </summary>
public class MouseLook : MonoBehaviour
{
    [Header("Refs")]
    public Transform playerBody;     // Yaw 적용(플레이어 본체)
    public Transform cameraPivot;    // Pitch 적용(카메라 부모; 자식에 Main Camera 존재)

    [Header("Look")]
    public float mouseSensitivity = 2.5f; // 기본 감도(상황에 맞게 1.5~3.5 범위 권장)
    public float pitchMin = -89f;         // 아래 클램프
    public float pitchMax = 89f;          // 위 클램프

    private Vector2 lookInput;            // 최신 마우스 입력(프레임 간 유지)
    private float yaw;                    // 현재 Yaw(도)
    private float pitch;                  // 현재 Pitch(도)

    public float sensitivityMultiplier = 1.0f;

    private void Awake()
    {
        // 필수 참조 확인
        if (playerBody == null)
        {
            Debug.LogError("MouseLook: playerBody 할당이 필요합니다(플레이어 루트 Transform).");
        }
        if (cameraPivot == null)
        {
            Debug.LogError("MouseLook: cameraPivot 할당이 필요합니다(카메라 부모 Transform).");
        }

        // 커서 잠금(초기)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        // 시작 시점의 실제 Transform 값을 읽어 초기 yaw/pitch 설정(초기 스냅 방지)
        if (playerBody != null)
        {
            Vector3 e = playerBody.eulerAngles; // 월드 회전
            yaw = e.y;
        }

        if (cameraPivot != null)
        {
            Vector3 eLocal = cameraPivot.localEulerAngles; // 로컬 회전
            float px = eLocal.x;
            if (px > 180f)
            {
                px -= 360f;
            }
            pitch = Mathf.Clamp(px, pitchMin, pitchMax);
        }
    }

    private void Update()
    {
        // ESC로 커서 잠금/해제 토글: 포커스/입력 문제 회피
        if (Input.GetKeyDown(KeyCode.Escape) == true)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // 잠금 상태에서만 회전 처리
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float dt = Time.deltaTime;

            // 3일차 구조 유지: lookInput(프레임당 축 값) × 감도 × 10 × dt
            float dx = lookInput.x * mouseSensitivity * 10f * dt * sensitivityMultiplier; // 좌우
            float dy = lookInput.y * mouseSensitivity * 10f * dt * sensitivityMultiplier; // 상하

            yaw += dx;      // 우측 이동 시 +yaw
            pitch -= dy;    // 위로 이동 시 pitch 감소(고개 위로)

            // Pitch 클램프
            if (pitch < pitchMin)
            {
                pitch = pitchMin;
            }
            if (pitch > pitchMax)
            {
                pitch = pitchMax;
            }

            // 적용
            if (playerBody != null)
            {
                Quaternion yRot = Quaternion.Euler(0f, yaw, 0f);
                playerBody.rotation = yRot;
            }

            if (cameraPivot != null)
            {
                Quaternion xRot = Quaternion.Euler(pitch, 0f, 0f);
                cameraPivot.localRotation = xRot;
            }
        }
    }

    // PlayerInput → Events에서 연결되는 콜백(New Input System)
    public void OnLook(InputAction.CallbackContext ctx)
    {
        // performed: 읽기, canceled: (패드/키) 좌표 0으로 복귀
        if (ctx.performed == true || ctx.canceled == true)
        {
            lookInput = ctx.ReadValue<Vector2>();
        }
    }

    public void SetSensitivityMultiplier(float m)
    {
        // 요청된 배율 값 사본.
        float requested = m;

        // 최소값 보정(너무 작으면 입력이 거의 멈춘 것처럼 느껴짐)
        if (requested < 0.01f)
        {
            requested = 0.01f;
        }

        // 최대값 보정(과도한 배율은 예기치 않은 급회전 유발)
        if (requested > 5.0f)
        {
            requested = 5.0f;
        }

        // 내부 상태 갱신.
        sensitivityMultiplier = requested;
    }
}
