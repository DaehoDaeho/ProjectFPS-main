using UnityEngine;

/// <summary>
/// NavMesh 없이 '평지 직선 이동'만으로 추격/공격/수색을 수행하는 간단 FSM.
/// - Idle: 대기(발견 시 Chase 전환)
/// - Chase: 마지막으로 본 위치를 향해 전진(수평 회전 보간)
/// - Attack: 사거리 이내일 때 쿨다운을 지키며 플레이어에 데미지 부여.
/// - Search: 시야 상실 뒤 마지막 위치 근방에서 잠시 탐색 후 Idle 복귀.
/// - Dead: Health 0 이하일 때 정지(추후 애니/파괴는 Health.onDeath로 처리 권장)
/// </summary>
public class EnemyAIFlat : MonoBehaviour
{
    public enum State { Idle, Chase, Attack, Search, Dead }

    [Header("Modules")]
    public EnemySenses senses;            // 시야 모듈(발견/상실 판정)
    public Health health;                 // 체력(사망 상태 전이 트리거)
    public Transform player;              // 추격/공격 대상 Transform
    public PlayerHealth playerDamageable;  // 대상의 IDamageable(공격 데미지 전달)
    public CharacterController controller;// 단순 이동을 위한 CC(충돌/경사 안정화)

    [Header("Movement (Flat)")]
    public float chaseSpeed = 3.0f;       // 추격 속도(m/s). 평지 전진 속도.
    public float rotateSpeed = 12.0f;     // 회전 보간 속도(높을수록 빠름)
    public float stoppingDistance = 1.2f; // 이 거리 이내면 전진을 멈춤(들쭉거림 방지)
    public float gravity = -20.0f;        // 미세한 접지 안정화를 위한 중력 가속도(음수)

    [Header("Attack")]
    public float attackRange = 1.8f;      // 공격 발동 사거리(미터)
    public float attackDamage = 10.0f;    // 공격 데미지량.
    public float attackCooldown = 1.2f;   // 공격 간격(초). 0이면 연타가 되므로 권장 X

    [Header("Search")]
    public float searchDuration = 3.0f;   // 시야 상실 후 마지막 위치 근방에서 탐색하는 시간(초)

    [Header("Debug")]
    public bool drawForward = false;      // 전방 디버그 레이 표기.

    public StatusEffectHost host;

    // ===== 내부 상태 필드(역할 주석) =========================================
    private State state;                  // 현재 FSM 상태.
    private float attackTimer;            // 공격 쿨다운 카운터(0이면 가능)
    private float searchTimer;            // 수색 잔여 시간.
    private Vector3 lastKnownPos;         // 마지막으로 '봤던' 플레이어 좌표(시야 상실 대비)

    private void Awake()
    {
        // 필수 컴포넌트 자동 연결 보조.
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        // 초기 상태/타이머 설정.
        state = State.Idle;
        attackTimer = 0.0f;
        searchTimer = 0.0f;
        lastKnownPos = transform.position;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1) 공격 쿨다운 감소.
        if (attackTimer > 0.0f)
        {
            attackTimer = attackTimer - dt;
            if (attackTimer < 0.0f)
            {
                attackTimer = 0.0f;
            }
        }

        // 2) 사망 상태로 전이(Health 0 이하)
        if (health != null)
        {
            if (health.GetCurrentHealth() <= 0.0f)
            {
                if (state != State.Dead)
                {
                    EnterDead();
                }
                return; // Dead는 더 처리하지 않음.
            }
        }

        // 3) 시야 체크: 가능하면 lastKnownPos 갱신.
        bool seen = false;
        Vector3 seenPos = Vector3.zero;
        if (senses != null)
        {
            bool can = senses.CanSeeTarget(out seenPos);
            if (can == true)
            {
                seen = true;
                lastKnownPos = seenPos;
            }
        }

        switch(state)
        {
            case State.Idle:
                {
                    UpdateIdle(seen);
                }
                break;
            case State.Chase:
                {
                    UpdateChase(seen);
                }
                break;
            case State.Attack:
                {
                    UpdateAttack(seen);
                }
                break;
            case State.Search:
                {
                    UpdateSearch(seen);
                }
                break;
        }

        // 5) 전방 디버그 레이(시각 확인용)
        if (drawForward == true)
        {
            Debug.DrawRay(transform.position + Vector3.up * 1.0f, transform.forward * 1.5f, Color.yellow, 0.02f);
        }
    }

    // ===== 상태 진입/업데이트 ================================================

    private void EnterIdle()
    {
        state = State.Idle;
        // 평지 데모: Idle에서 하는 일은 없음(발견 시 바로 Chase)
    }

    private void UpdateIdle(bool seen)
    {
        // 발견 즉시 추격 시작.
        if (seen == true)
        {
            EnterChase();
            return;
        }
        // Idle 유지.
    }

    private void EnterChase()
    {
        state = State.Chase;
        // 별도 초기화는 없음. UpdateChase에서 회전/전진 처리.
    }

    private void UpdateChase(bool seen)
    {
        // 1) 회전: 수평면에서 마지막 좌표를 바라보도록 보간.
        Vector3 targetPos = lastKnownPos;
        Vector3 flatTarget = targetPos;
        flatTarget.y = transform.position.y; // 평지 가정: 수평 Y 동일화.

        Vector3 to = flatTarget - transform.position; // 이동/회전 기준 벡터.
        to.y = 0.0f;                                  // 수평만 고려.

        if (to.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * rotateSpeed);
        }

        // 2) 정지거리/전진.
        float dist = Vector3.Distance(transform.position, flatTarget);

        if (dist > stoppingDistance)
        {
            float speedMul = 1.0f;
            if (host != null)
            {
                speedMul = host.GetSpeedMultiplier();
            }

            Vector3 move = transform.forward * (chaseSpeed * speedMul);
            // 전진 벡터(월드 전방 기준)
            //Vector3 move = transform.forward * chaseSpeed;

            // CC 안정화를 위한 중력 보정(평지에서도 경계에서 떨림을 줄임)
            move.y = gravity;

            controller.Move(move * Time.deltaTime);
        }

        // 3) 공격 사거리 진입 -> Attack
        if (player != null)
        {
            float d2 = Vector3.Distance(transform.position, player.position);
            if (d2 <= attackRange)
            {
                EnterAttack();
                return;
            }
        }

        // 4) 시야 상실 상태에서 마지막 좌표 도달 -> Search
        if (seen == false)
        {
            if (dist <= stoppingDistance)
            {
                EnterSearch();
                return;
            }
        }
    }

    private void EnterAttack()
    {
        state = State.Attack;
        // 공격 상태에서는 정지/회전/쿨다운 기반 데미지 적용.
    }

    private void UpdateAttack(bool seen)
    {
        // 0) 스턴 중이면 리턴.
        //if (host != null)
        //{
        //    if (host.IsStunned() == true)
        //    {
        //        // 이동/공격/마우스입력 처리 중단.
        //        return;
        //    }
        //}

        // 1) 사거리 유지가 깨지면 Chase로 복귀.
        if (player != null)
        {
            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > attackRange)
            {
                EnterChase();
                return;
            }
        }

        // 2) 시야가 완전히 끊기면 Search로.
        if (seen == false)
        {
            EnterSearch();
            return;
        }

        // 3) 공격 수행(쿨다운)
        if (attackTimer <= 0.0f)
        {
            DoAttack();
            attackTimer = attackCooldown;
        }

        // 4) 공격 중에도 천천히 플레이어를 향해 회전(시각적 자연스러움)
        if (player != null)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0.0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * rotateSpeed);
            }
        }
    }

    private void EnterSearch()
    {
        state = State.Search;
        searchTimer = searchDuration;
    }

    private void UpdateSearch(bool seen)
    {
        // 1) 마지막 위치를 향해 천천히 접근(회전 + 낮은 속도 전진)
        Vector3 flatTarget = lastKnownPos;
        flatTarget.y = transform.position.y;

        Vector3 to = flatTarget - transform.position;
        to.y = 0.0f;

        float dist = to.magnitude;

        if (dist > stoppingDistance)
        {
            Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * rotateSpeed);

            Vector3 move = transform.forward * chaseSpeed * 0.6f; // 수색은 추격보다 느리게
            move.y = gravity;
            controller.Move(move * Time.deltaTime);
        }

        // 2) 다시 보이면 즉시 Chase로.
        if (seen == true)
        {
            EnterChase();
            return;
        }

        // 3) 타임아웃 시 Idle 복귀.
        searchTimer = searchTimer - Time.deltaTime;
        if (searchTimer <= 0.0f)
        {
            EnterIdle();
        }
    }

    private void EnterDead()
    {
        state = State.Dead;
        // 여기서는 정지만. 실제 파괴/드랍/애니는 Health.onDeath에서 처리 권장.
    }

    /// <summary>
    /// 실제 데미지를 플레이어(IDamageable)에 전달..
    /// </summary>
    private void DoAttack()
    {
        if (playerDamageable == null)
        {
            return;
        }

        // 연출용 인자: hitPoint는 플레이어 위치, 법선은 Vector3.up 사용.
        Vector3 hp = player.position;
        Vector3 n = Vector3.up;

        playerDamageable.ApplyDamage(attackDamage, hp, n, transform);
    }
}
