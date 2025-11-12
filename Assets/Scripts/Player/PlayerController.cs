using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float walkSpeed = 4.5f;          // 기본 걷기 최고 속도(m/s)
    public float sprintSpeed = 7.5f;        // 달리기 최고 속도(m/s)
    public float acceleration = 15.0f;      // 가속(m/s^2)
    public float deceleration = 20.0f;      // 감속(m/s^2)

    [Header("Jump")]
    public float jumpHeight = 1.2f;         // 점프 높이(m)
    public float gravity = -9.81f * 2.0f;   // 중력(음수)
    public float coyoteTime = 0.12f;        // 코요테 시간(초)
    public float jumpBufferTime = 0.12f;    // 점프 버퍼(초)
    public LayerMask groundMask;            // 접지 레이어.
    public float groundCheckRadius = 0.3f;  // 접지 구체 반경.
    public Transform groundCheck;           // 접지 기준점(없으면 생성)

    [Header("Refs")]
    public Transform cameraPivot;           // 시야 기준(보통 카메라 부모)
    public CharacterController controller;  // CC (없으면 Awake에서 자동 캐시)

    public GroundingStabilizer stabilizer;

    public Animator animator;

    private Vector2 moveInput;              // 이동 입력(스틱/키)
    private bool sprintHeld;                // 달리기 키 눌림.
    private float currentSpeed;             // 현재 목표 수평 속도(입력 크기 미적용)
    private Vector3 velocity;               // 월드 속도(중력 포함)

    private bool isGrounded;                // 접지 여부.
    private float lastGroundedTime;         // 최근 접지 시각.
    private float lastJumpPressTime;        // 최근 점프 입력 시각.

    private void Awake()
    {
        // 방어: CC 자동 캐시.
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        // 접지 기준점 자동 생성.
        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0.0f, 0.1f, 0.0f);
            groundCheck = gc.transform;
        }

        if (animator != null)
        {
            animator.SetBool("Move", false);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        UpdateGrounded();
        UpdateHorizontalSpeed(dt);
        ApplyGravityAndJump(dt);

        Vector3 move = CalculateWorldMove();     // m/s

        // 경사 / 계단에서 보정 처리.
        if(stabilizer != null)
        {
            move = stabilizer.ProjectOnGround(move, controller, this);
            move = stabilizer.ApplyStepSmoothing(move, controller, this);
        }

        controller.Move(move * dt);              // 프레임 변위로 변환 후 이동.

        if(stabilizer != null)
        {
            if(controller.velocity.y <= 0.0f)
            {
                stabilizer.TrySnapDown(controller, this);
            }
        }
    }

    // ===== Input (New Input System) =====

    public void OnMove(InputAction.CallbackContext ctx)
    {
        bool move = false;
        if (ctx.performed == true || ctx.canceled == true)
        {
            moveInput = ctx.ReadValue<Vector2>();

            if(ctx.performed == true)
            {
                move = true;
            }
        }

        if (animator != null)
        {
            animator.SetBool("Move", move);
        }
    }

    public void OnSprint(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == true)
        {
            sprintHeld = true;
        }

        if (ctx.canceled == true)
        {
            sprintHeld = false;
        }
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed == true)
        {
            lastJumpPressTime = Time.time;
        }
    }

    // ===== Grounded =====

    private void UpdateGrounded()
    {
        bool hit = false;

        if (groundCheck != null)
        {
            Collider[] hits = Physics.OverlapSphere(
                groundCheck.position,
                groundCheckRadius,
                groundMask,
                QueryTriggerInteraction.Ignore);

            if (hits != null)
            {
                if (hits.Length > 0)
                {
                    hit = true;
                }
            }
        }

        if (hit == true)
        {
            if (isGrounded == false)
            {
                if (velocity.y < 0.0f)
                {
                    velocity.y = -2.0f; // 접지 전이 시 급가속 방지.
                }
            }

            isGrounded = true;
            lastGroundedTime = Time.time;
        }
        else
        {
            isGrounded = false;
        }
    }

    // ===== Horizontal Speed =====

    private void UpdateHorizontalSpeed(float dt)
    {
        // 변경점 1: 입력 크기에 따라 '목표 속도'를 0~최대까지 설정.
        //  - 아날로그 입력일 때 "이동이 너무 작다" 체감 개선.
        float inputMag = moveInput.magnitude; // 0~1

        float baseTarget = walkSpeed;
        if (sprintHeld == true)
        {
            baseTarget = sprintSpeed;
        }

        float target = baseTarget * inputMag; // 입력 크기 반영.

        // 가감속
        if (currentSpeed < target)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, target, acceleration * dt);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, target, deceleration * dt);
        }
    }

    // ===== Gravity & Jump =====

    private void ApplyGravityAndJump(float dt)
    {
        bool coyote = (Time.time - lastGroundedTime) <= coyoteTime;
        bool buffered = (Time.time - lastJumpPressTime) <= jumpBufferTime;

        if ((buffered == true) && ((isGrounded == true) || (coyote == true)))
        {
            float vJump = Mathf.Sqrt(Mathf.Abs(2.0f * gravity * jumpHeight)); // v = √(2gh)
            velocity.y = vJump;
            lastJumpPressTime = -999.0f;
        }

        velocity.y += gravity * dt;
    }

    // ===== Move Vector =====

    private Vector3 CalculateWorldMove()
    {
        // 변경점 2: 카메라 평면 투영이 '너무 작으면' 플레이어 전방/우측으로 폴백.
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraPivot != null)
        {
            Vector3 camForward = cameraPivot.forward;
            Vector3 camRight = cameraPivot.right;

            camForward.y = 0.0f;
            camRight.y = 0.0f;

            float lenF = camForward.magnitude;
            float lenR = camRight.magnitude;

            if (lenF > 0.0001f)
            {
                camForward /= lenF;
            }

            if (lenR > 0.0001f)
            {
                camRight /= lenR;
            }

            // 임계치 이하(거의 수직 시야 등)면 안전 폴백.
            if (camForward.sqrMagnitude < 0.0001f || camRight.sqrMagnitude < 0.0001f)
            {
                Vector3 bodyF = transform.forward;
                Vector3 bodyR = transform.right;

                bodyF.y = 0.0f;
                bodyR.y = 0.0f;

                if (bodyF.sqrMagnitude > 0.0f)
                {
                    bodyF.Normalize();
                }
                if (bodyR.sqrMagnitude > 0.0f)
                {
                    bodyR.Normalize();
                }

                forward = bodyF;
                right = bodyR;
            }
            else
            {
                forward = camForward;
                right = camRight;
            }
        }

        // 희망 방향.
        Vector3 wish = forward * moveInput.y + right * moveInput.x;

        // 정규화: 키보드 대각선(√2) 보정.
        if (wish.sqrMagnitude > 1.0f)
        {
            wish.Normalize();
        }

        // 수평 속도 벡터 = 방향 × 현재 속도.
        Vector3 horizontal = wish * currentSpeed;

        // 최종 이동 속도(m/s)
        Vector3 move = new Vector3(horizontal.x, velocity.y, horizontal.z);
        return move;
    }
}
