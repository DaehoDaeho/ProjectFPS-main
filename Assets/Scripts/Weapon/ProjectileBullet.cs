using UnityEngine;

/// <summary>
/// 고속 탄환.
/// - '연속 충돌 체크'를 자체 구현: 지난 위치 -> 현재 위치로 Raycast(Sweep) 후 이동
///   -> 높은 속도에서도 벽/얇은 콜라이더를 관통하지 않음.
/// - 히트 시 IDamageable로 데미지 전달, 임팩트 이펙트(선택)
/// - 일정 시간 후 자동 파괴.
/// </summary>
public class ProjectileBullet : MonoBehaviour
{
    [Header("Ballistics")]
    public float speed = 120.0f;             // 탄 속도(m/s). 매우 빠름.
    public bool useGravity = false;          // 낙차 적용 여부.
    public float gravity = -9.81f;           // 중력 가속도(옵션)
    public float maxLifeTime = 4.0f;         // 최대 생존 시간(초)
    public LayerMask hitMask;                // 충돌 대상으로 볼 레이어(벽/지형/캐릭터 등)

    [Header("Damage")]
    public float damage = 20.0f;             // 기본 데미지.
    public float headshotMultiplier = 2.0f;  // Hitbox가 있으면 곱.

    [Header("Effects (Optional)")]
    public GameObject impactVfxPrefab;       // 피격 지점 VFX
    public GameObject decalPrefab;           // 데칼(선택)
    public float decalOffset = 0.01f;        // 표면 파고듦 방지 오프셋.

    // 내부 상태
    private Vector3 velocity;                // 현재 속도(중력 포함)
    private Vector3 lastPosition;            // 지난 프레임 위치(스윕 시작점)
    private float life;                      // 수명 누적.

    private void Awake()
    {
        // 초기화는 외부에서 Spawn 직후 SetInitialVelocity 등으로 조정해도 됨.
        velocity = transform.forward * speed;
        lastPosition = transform.position;
        life = 0.0f;
    }

    /// <summary>
    /// 런처가 발사 직후 호출하여 초기 속도를 세팅(ADS/스프레드 반영 후 방향 사용 권장)
    /// </summary>
    public void SetInitialVelocity(Vector3 v)
    {
        velocity = v;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 1) 중력 적용(옵션)
        if (useGravity == true)
        {
            velocity.y = velocity.y + gravity * dt;
        }

        // 2) 이동 거리 계산.
        Vector3 start = transform.position;            // 현재 위치(스윕 시작점으로도 사용 가능)
        Vector3 displacement = velocity * dt;          // 이번 프레임 이동 예정 벡터.
        Vector3 end = start + displacement;            // 이동 후 위치(이상적)

        // 3) 연속 충돌 체크(Sweep): 지난 위치->이번 위치 '둘 다'를 고려.
        //    - FixedUpdate 누락/프레임 점프 방지를 위해 lastPosition에서 end까지도 스윕.
        Vector3 sweepStart = lastPosition;
        Vector3 sweepDir = (end - sweepStart);
        float sweepDist = sweepDir.magnitude;

        bool hitSomething = false;
        RaycastHit hitInfo = new RaycastHit();

        if (sweepDist > 0.0001f)
        {
            Vector3 dir = sweepDir / sweepDist;
            bool got = Physics.Raycast(sweepStart, dir, out hitInfo, sweepDist, hitMask, QueryTriggerInteraction.Ignore);
            if (got == true)
            {
                hitSomething = true;
            }
        }

        // 4) 히트 처리 또는 이동.
        if (hitSomething == true)
        {
            OnHit(hitInfo);
            // 히트 즉시 파괴(관통 버전이 필요하면 확장)
            Destroy(gameObject);
        }
        else
        {
            // 충돌 없음: 위치 갱신.
            transform.position = end;
            // 탄두가 진행 방향을 바라보도록 회전(시각연출)
            if (velocity.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(velocity.normalized, Vector3.up);
                transform.rotation = look;
            }
        }

        // 5) 생존 시간 초과 시 파괴.
        life = life + dt;
        if (life >= maxLifeTime)
        {
            Destroy(gameObject);
        }

        // 6) 다음 스윕을 위한 위치 기억.
        lastPosition = transform.position;
    }

    private void OnHit(RaycastHit hit)
    {
        // 1) 데미지 라우팅(IDamageable)
        float finalDamage = damage;

        // Hitbox가 있으면 멀티플라이어.
        Hitbox hb = hit.collider.GetComponent<Hitbox>();
        if (hb != null)
        {
            finalDamage = finalDamage * hb.damageMultiplier;
            if (hb.owner != null)
            {
                hb.owner.ApplyDamage(finalDamage, hit.point, hit.normal, transform);                
            }
            else
            {
                IDamageable id = hit.collider.GetComponentInParent<IDamageable>();
                if (id != null)
                {
                    id.ApplyDamage(finalDamage, hit.point, hit.normal, transform);
                }
            }
        }
        else
        {
            IDamageable id = hit.collider.GetComponentInParent<IDamageable>();
            if (id != null)
            {
                id.ApplyDamage(finalDamage, hit.point, hit.normal, transform);
            }
        }

        // 2) 임팩트 VFX/데칼(선택)
        if (impactVfxPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(hit.normal);
            GameObject vfx = Instantiate(impactVfxPrefab, hit.point + hit.normal * decalOffset, rot);
        }
        if (decalPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(-hit.normal);
            GameObject decal = Instantiate(decalPrefab, hit.point + hit.normal * decalOffset, rot);
        }

        // 예: 피탄 대상에게 2초짜리 0.7배 슬로우 부여.
        StatusEffect_Slow slowPreset = GameObject.FindAnyObjectByType<StatusEffect_Slow>();
        if (slowPreset != null)
        {
            StatusEffectApplier.ApplyTo(hit.collider.gameObject, slowPreset);
        }
    }
}
