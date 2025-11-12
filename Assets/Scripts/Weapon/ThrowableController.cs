using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 투척 컨트롤러.
/// - 버튼 탭: 기본 세기로 즉시 던지기
/// - 버튼 홀드: 파워 축적(옵션)을 거쳐 던지기
/// </summary>
public class ThrowableController : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCamera;         // 던질 방향 기준.
    public Transform hand;              // 손/던지는 원점.
    public Grenade grenadePrefab;       // 수류탄 프리팹.

    [Header("Throw")]
    public float baseThrowSpeed = 14.0f;     // 기본 던지기 속력.
    public float maxThrowSpeed = 22.0f;      // 홀드 시 최대 속력.
    public float chargeTime = 1.0f;          // 최대 세기까지 걸리는 시간(초)
    public float angularSpin = 20.0f;        // 회전(시각)

    private bool charging;                   // 파워 충전 중.
    private float charge;                    // 0~1 누적.
    private bool fireRequested;              // 입력 릴리즈 감지.

    public void OnThrow(InputAction.CallbackContext ctx)
    {
        if (ctx.started == true)
        {
            charging = true;
            charge = 0.0f;
        }
        else if (ctx.canceled == true || ctx.performed == true)
        {
            charging = false;
            fireRequested = true;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (charging == true)
        {
            // 파워 충전.
            float inc = 0.0f;
            if (chargeTime > 0.0001f)
            {
                inc = dt / chargeTime;
            }
            charge = charge + inc;
            if (charge > 1.0f)
            {
                charge = 1.0f;
            }
        }

        if (fireRequested == true)
        {
            fireRequested = false;
            ThrowOne();
        }
    }

    private void ThrowOne()
    {
        if (grenadePrefab == null)
        {
            return;
        }
        if (playerCamera == null)
        {
            return;
        }

        // 1) 던질 원점/방향.
        Vector3 origin = hand != null ? hand.position : playerCamera.transform.position;
        Vector3 dir = playerCamera.transform.forward;

        // 2) 속력 결정(충전값 반영)
        float spd = Mathf.Lerp(baseThrowSpeed, maxThrowSpeed, charge);
        Vector3 vel = dir * spd;

        // 3) 각속도(회전 연출)
        Vector3 ang = Random.onUnitSphere * angularSpin;

        // 4) 인스턴스 생성 후 Throw 호출.
        Grenade g = Instantiate(grenadePrefab, origin, Quaternion.identity);
        g.Throw(vel, ang);

        // 5) 충전 값 초기화.
        charge = 0.0f;
    }
}
