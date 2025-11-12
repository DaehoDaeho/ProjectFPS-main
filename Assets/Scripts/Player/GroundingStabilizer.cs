using UnityEngine;

/// <summary>
/// 경사 투영(ProjectOnGround), 스텝 보정(ApplyStepSmoothing), 스냅 다운(TrySnapDown)을 제공하는 안정화 모듈.
/// - 목적: CharacterController 이동에서 계단/경사/엣지 상황의 '멈춤/툭/붕 뜸'을 줄여 체감 품질을 높임.
/// - 핵심 안전장치:
///   1) 공중이거나(접지 X) 레이 미히트면 보정을 하지 않음(잘못된 보정으로 이동 잠김 방지)
///   2) 저속일 때(의미 없는 떨림 영역) 경사 투영을 하지 않음
///   3) 투영 후 길이 손실이 과도하면(원래의 일정 비율 미만) 원본 이동 벡터를 유지
///   4) 스텝 보정은 "낮은 레이 히트 + 높은 레이 미히트"일 때만 수행
///   5) 스냅 다운은 상승 중(y>0)에는 수행하지 않음(점프 억압 방지)
/// </summary>
public class GroundingStabilizer : MonoBehaviour
{
    [Header("Ground Probe")]
    public float probeDistance = 0.6f;      // 바닥 탐사용 레이 길이(플레이어 발보다 약간 긴 값)
    public LayerMask groundMask;            // 바닥으로 인정할 레이어 마스크.

    [Header("Slope Projection")]
    public float slopeStickAngleBias = 2.0f;     // CC.slopeLimit보다 약간 작은 허용 여유각(미끌림 방지용 완충)
    public float minMoveSpeedForProjection = 0.4f;  // 이동 속도가 이 값 미만이면 투영하지 않음(정지 근처 떨림 방지)
    public float maxProjectionLossRatio = 0.5f;     // 투영 후 수평 길이가 원래의 50% 미만이면 원본 유지(멈춤 방지)

    [Header("Step Smoothing")]
    public float stepUpSmoothTime = 0.06f;  // 계단 상승 보간 시간(짧게 부드럽게)
    public float maxStepHeight = 0.4f;      // 스텝으로 상승시킬 수 있는 최대 높이(과상승 방지)
    public float stepProbeForward = 0.25f;  // 전방 탐사 거리(너무 길면 앞벽 오판)

    [Header("Snap Down")]
    public float snapDownDistance = 0.35f;  // 발밑 간극을 메울 최대 거리.
    public float snapDownSpeed = 20.0f;     // 스냅 다운 시 1초당 최대 하강량(부드럽게 붙이기용)

    // 내부 상태(스텝 보정용)
    private float stepUpVelocity;           // SmoothDamp용 내부 속도 캐시.
    private float currentStepOffset;        // 현재까지 누적된 스텝 상승치(y 오프셋)

    /// <summary>
    /// 이동 벡터를 현재 바닥 법선 기준으로 '평면 투영'하여 경사를 따라가게 만든다.
    /// </summary>
    public Vector3 ProjectOnGround(Vector3 move, CharacterController cc, PlayerController pc)
    {
        // 필수 레퍼런스 확인.
        if (cc == null)
        {
            return move; // CC가 없으면 투영 불가 -> 원본 유지.
        }

        // 현재 수평 이동 속도 추정(보정이 의미 있는지 판단)
        float dt = Time.deltaTime; // 프레임 시간(초)
        float speedPlanar = 0f;    // 현재 수평 속도(m/s)
        if (dt > 0f)
        {
            // move는 "이번 프레임 이동량"이 아니라 "초당 속도"로 가정하고 계산.
            speedPlanar = new Vector2(move.x, move.z).magnitude; // 수평 성분의 크기.
        }

        // 접지 상태 판단(접지 아닐 때는 경사 투영 금지)
        bool grounded = false;
        if (pc != null)
        {
            // PlayerController의 내부 접지 여부 공개가 없다면,
            // 안전하게 CC 규칙에 위임하거나 여기서는 보수적으로 true/false 정책 선택.
            // 실습에선 CC.isGrounded에 위임해도 OK.
            grounded = cc.isGrounded;
        }
        else
        {
            grounded = cc.isGrounded;
        }

        if (grounded == false)
        {
            return move; // 공중이면 투영 금지.
        }

        // 너무 느리면 투영하지 않음(정지 근처의 미세 떨림 방지)
        if (speedPlanar < minMoveSpeedForProjection)
        {
            return move;
        }

        // 바닥 법선 구하기(레이 미히트면 투영하지 않음)
        Vector3 normal;
        bool got = TryGetGroundNormal(cc, out normal);
        if (got == false)
        {
            return move;
        }

        // 경사 각도 검사: CC가 오를 수 없는 급경사면 투영을 건너뛰기.
        float slope = Vector3.Angle(normal, Vector3.up); // 법선과 Up의 각도(0=평지, 90=수직)
        float limit = cc.slopeLimit - slopeStickAngleBias;
        if (slope > limit)
        {
            // 급경사에서는 엔진 규칙(CharacterController) 동작에 맡김.
            return move;
        }

        // 평면 투영: move를 바닥 법선에 대해 눕혀 '면을 따라' 이동.
        Vector3 onPlane = Vector3.ProjectOnPlane(move, normal);

        // 투영으로 길이가 과도하게 줄었으면 원본 유지(멈춤 방지)
        float originalLen = new Vector2(move.x, move.z).magnitude;
        float projectedLen = new Vector2(onPlane.x, onPlane.z).magnitude;

        if (originalLen > 0.0001f)
        {
            float ratio = projectedLen / originalLen; // 투영 후 길이 비율.
            if (ratio < maxProjectionLossRatio)
            {
                return move; // 절반 이하로 줄면 원본 유지(측면 벽에 '흡착' 방지)
            }
        }

        // 수직 속도(y)는 원본 유지(오로지 수평만 눕힘)
        onPlane.y = move.y;
        return onPlane;
    }

    /// <summary>
    /// 작은 '턱'을 부드럽게 넘도록 상승 보간을 적용.
    /// '낮은 레이 히트 + 높은 레이 미히트' 조합에서만 스텝으로 판정.
    /// </summary>
    public Vector3 ApplyStepSmoothing(Vector3 move, CharacterController cc, PlayerController pc)
    {
        if (cc == null)
        {
            return move; // CC 없음 → 보정 불가.
        }

        // 수평 이동이 거의 없으면 스텝 보정 대신 '서서히 복귀'만 적용.
        Vector2 planar = new Vector2(move.x, move.z);
        if (planar.sqrMagnitude <= 0.000001f)
        {
            return ReleaseStepGradually(move);
        }

        // 전방 방향 계산(수평 방향만)
        Vector3 dir = GetHorizontalDir(move); // 수평 단위벡터

        // 레이 원점 두 개(낮은/높은) — 낮은 점은 턱에 걸리고 높은 점은 걸리지 않으면 '계단'으로 판정.
        Vector3 originLow = cc.transform.position + Vector3.up * (cc.stepOffset * 0.5f);  // 낮은 원점.
        Vector3 originHigh = cc.transform.position + Vector3.up * (cc.stepOffset + 0.08f); // 높은 원점(조금 더 위)

        // 전방 레이 길이(너무 길면 앞벽을 턱으로 오판)
        float castDist = Mathf.Max(cc.radius + stepProbeForward, 0.15f);

        // 레이캐스트 수행(트리거 제외)
        RaycastHit hitLow;
        bool lowHit = Physics.Raycast(originLow, dir, out hitLow, castDist, groundMask, QueryTriggerInteraction.Ignore);

        RaycastHit hitHigh;
        bool highHit = Physics.Raycast(originHigh, dir, out hitHigh, castDist, groundMask, QueryTriggerInteraction.Ignore);

        // 낮은 레이는 맞고 높은 레이는 비면 '턱'으로 간주하고 상승 보간
        if (lowHit == true && highHit == false)
        {
            // 목표 상승량: 낮은 레이가 찍힌 지점의 높이 차.
            float desiredUp = hitLow.point.y - cc.transform.position.y;
            if (desiredUp < 0f)
            {
                desiredUp = 0f; // 하강 방향은 스텝 상승으로 처리하지 않음.
            }
            if (desiredUp > maxStepHeight)
            {
                desiredUp = maxStepHeight; // 과상승 방지.
            }

            // SmoothDamp로 currentStepOffset을 desiredUp으로 매끈하게 수렴.
            float dt = Time.deltaTime;
            float smoothed = Mathf.SmoothDamp(
                currentStepOffset,     // 현재 값.
                desiredUp,             // 목표 값.
                ref stepUpVelocity,    // 내부 속도 캐시(참조)
                stepUpSmoothTime,      // 보간 시간.
                Mathf.Infinity,        // 최대 속도 무제한.
                dt                     // 프레임 시간.
            );

            // 이번 프레임에서 증가한 상승량(델타) 계산.
            float delta = smoothed - currentStepOffset;
            currentStepOffset = smoothed;

            // 최종 y = 기존 y + 델타(과상승 제한)
            float newY = move.y + delta;
            if (newY > maxStepHeight)
            {
                newY = maxStepHeight;
            }
            move.y = newY;
        }
        else
        {
            // 턱 상황이 아니면 보정값을 점진적으로 0으로 회복.
            move = ReleaseStepGradually(move);
        }

        return move;
    }

    /// <summary>
    /// 발밑 간극이 작게 생겼을 때 '짧게 아래로' 이동시켜 바닥에 착 붙이는 스냅 다운.
    /// 상승 중일 때는 절대 수행하지 않습니다(점프/상승 억압 방지).
    /// </summary>
    public void TrySnapDown(CharacterController cc, PlayerController pc)
    {
        if (cc == null)
        {
            return;
        }

        // 상승 중이면 스냅 다운 금지(점프/오르막 진행 방해 방지)
        if (cc.velocity.y > 0f)
        {
            return;
        }

        // 레이 시작점: 플레이어 발 아래 살짝 위.
        Vector3 start = cc.transform.position + Vector3.up * 0.1f;

        // 아래로 레이 쏴서 가까운 바닥 간격 측정.
        RaycastHit hit;
        bool got = Physics.Raycast(
            start,                          // 시작점.
            Vector3.down,                   // 아래 방향.
            out hit,                        // 히트 결과.
            snapDownDistance + 0.1f,        // 탐사 거리(여유분 포함)
            groundMask,                     // 바닥 레이어.
            QueryTriggerInteraction.Ignore  // 트리거 무시.
        );

        if (got == true)
        {
            // 현재 위치와 바닥 사이의 실제 간격.
            float gap = (start.y - hit.point.y) - 0.1f;

            // 너무 작은 간극이면 무시(미세 떨림 방지)
            if (gap < 0.015f)
            {
                return;
            }

            // 한 프레임에 이동할 하강량(너무 크게 떨어지지 않도록 제한)
            float step = Mathf.Min(gap, snapDownSpeed * Time.deltaTime);

            // 아래로 Move — CharacterController 규칙(충돌/슬로프)을 따름
            Vector3 down = Vector3.down * step;
            cc.Move(down);
        }
    }

    // ===== 내부 유틸 =====

    /// <summary>
    /// 플레이어 발 아래로 레이캐스트하여 바닥 법선을 추정.
    /// </summary>
    private bool TryGetGroundNormal(CharacterController cc, out Vector3 normal)
    {
        normal = Vector3.up; // 실패 시 기본값(평지 가정)

        // 레이 시작점을 발보다 약간 위로 잡아 잡음 감소.
        Vector3 origin = cc.transform.position + Vector3.up * 0.2f;

        RaycastHit hit;
        bool got = Physics.Raycast(
            origin,                         // 시작점.
            Vector3.down,                   // 아래 방향.
            out hit,                        // 히트 결과.
            probeDistance + 0.25f,          // 탐사 거리(여유)
            groundMask,                     // 레이어.
            QueryTriggerInteraction.Ignore  // 트리거 무시.
        );

        if (got == true)
        {
            normal = hit.normal; // 실제 바닥 법선.
            return true;
        }

        return false;
    }

    /// <summary>
    /// 수평 방향 단위벡터를 구한다(y=0, 정규화).
    /// </summary>
    private Vector3 GetHorizontalDir(Vector3 move)
    {
        Vector3 d = move; // 지역 복사: 원본 보존.
        d.y = 0f;         // 수평 성분만 사용.
        if (d.sqrMagnitude > 0f)
        {
            d.Normalize(); // 0이 아닐 때만 정규화
        }
        return d;
    }

    /// <summary>
    /// 스텝 상승 보정치를 서서히 0으로 되돌리며,
    /// 그 변화분(델타)을 이번 프레임 이동에 반영.
    /// </summary>
    private Vector3 ReleaseStepGradually(Vector3 move)
    {
        float dt = Time.deltaTime; // 프레임 시간.
        // currentStepOffset -> 0 으로 부드럽게 수렴.
        float smoothed = Mathf.SmoothDamp(
            currentStepOffset,  // 현재 값.
            0f,                 // 목표 값.
            ref stepUpVelocity, // 내부 속도 캐시(참조)
            stepUpSmoothTime,   // 보간 시간.
            Mathf.Infinity,     // 최대 속도 무제한.
            dt                  // 프레임 시간.
        );

        // 이번 프레임에서 변화한 만큼 y에 더해줌
        float delta = smoothed - currentStepOffset;
        currentStepOffset = smoothed;
        move.y += delta;
        return move;
    }
}
