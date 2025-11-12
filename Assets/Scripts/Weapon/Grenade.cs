using UnityEngine;

/// <summary>
/// 수류탄: 던지면 굴러다니다가 시간 지연 후 폭발.
/// - 폭발 반경에 감쇠 대미지 적용.
/// - Raycast를 통해 장애물에 막히면 대미지 차단.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Grenade : MonoBehaviour
{
    [Header("Fuse (Delay)")]
    public float fuseTime = 2.5f;             // 지연 시간.

    [Header("Explosion")]
    public float radius = 6.0f;               // 폭발 반경.
    public float damageMax = 120.0f;          // 중심에서의 최대 대미지.
    public float damageMin = 10.0f;           // 가장 자리 최소 대미지.
    public LayerMask damageMask;              // 피해 대상 레이어.
    public LayerMask occlusionMask;           // 차폐 판정 레이어.

    [Header("Effects (Optional)")]
    public GameObject explosionVfxPrefab;     // 폭발 이펙트.
    public AudioSource audioSource;           // 폭발 사운드 재생을 위한 AudioSource.
    public AudioClip explosionClip;           // 폭발 사운드 클립.
    public float vfxUpOffset = 0.1f;          // 지면에 이펙트가 파고드는 것을 방지하기 위한 오프셋.

    private Rigidbody rb;                     // 수류탄의 리지드바디.
    private float timer;                      // 지연 시간까지의 타이머 체크용 변수.

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        timer = fuseTime;
    }

    public void Throw(Vector3 velocity, Vector3 angularVelocity)
    {
        if (rb == null)
        {
            return;
        }
        rb.linearVelocity = velocity;
        rb.angularVelocity = angularVelocity;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        timer = timer - dt;
        if (timer <= 0.0f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        // 1) 이펙트/사운드 출력.
        if (explosionVfxPrefab != null)
        {
            GameObject fx = Instantiate(explosionVfxPrefab, transform.position + Vector3.up * vfxUpOffset, Quaternion.identity);
        }
        if (audioSource != null)
        {
            if (explosionClip != null)
            {
                audioSource.PlayOneShot(explosionClip, 1.0f);
            }
        }

        // 2) 반경 내의 상대에게 대미지 적용.
        Collider[] cols = Physics.OverlapSphere(transform.position, radius, damageMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < cols.Length; i = i + 1)
        {
            Collider c = cols[i];
            if (c == null)
            {
                continue;
            }

            // IDamageable 참조 시도.
            IDamageable id = c.GetComponentInParent<IDamageable>();
            if (id == null)
            {
                continue;
            }

            // 차폐물 체크: 수류탄 위치 -> 대상 중심으로 레이캐스팅.
            Vector3 to = c.bounds.center - transform.position;
            float dist = to.magnitude;
            if (dist <= 0.0001f)
            {
                dist = 0.0001f;
            }
            Vector3 dir = to / dist;

            RaycastHit block;
            bool blocked = Physics.Raycast(transform.position, dir, out block, dist, occlusionMask, QueryTriggerInteraction.Ignore);

            if (blocked == true)
            {
                // 차폐물에 막혔을 경우 처리 생략.
                continue;
            }

            // 거리 기반 선형 감쇠: d=0 -> damageMax, d=radius -> damageMin
            float t = Mathf.Clamp01(dist / radius);
            float dmg = Mathf.Lerp(damageMax, damageMin, t);

            // 히트 지점/노멀(법선) 계산.
            Vector3 hp = c.ClosestPoint(transform.position);
            Vector3 n = (hp - transform.position).normalized;

            id.ApplyDamage(dmg, hp, n, transform);

            // 예: 폭발에 맞으면 3초 DoT 1스택.
            StatusEffect_DOT dotPreset = GameObject.FindAnyObjectByType<StatusEffect_DOT>();
            if(dotPreset != null)
            {
                StatusEffectApplier.ApplyTo(c.gameObject, dotPreset);
            }
        }
        // 3) 파괴.
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // 폭밟 반경 시각화.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
