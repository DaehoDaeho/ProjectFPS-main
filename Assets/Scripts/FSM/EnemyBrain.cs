using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public enum EnemyType
{
    Melee = 0,
    Ranged = 1,
    Hybrid = 2
}

/// <summary>
/// 적 FSM의 컨텍스트이자 전환기.
/// - 공용 데이터(컴포넌트/파라미터/런타임 캐시)를 보관한다.
/// - 현재 상태의 Enter/Update/Exit를 호출한다.
/// - 상태 전환 요청을 안전하게 수행한다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class EnemyBrain : MonoBehaviour
{
    [Header("Modules")]
    public EnemyType enemyType = EnemyType.Hybrid;
    public EnemySenses senses;               // 시야 모듈(거리/각/가림)
    public Health health;                    // 체력(사망 트리거)
    public Transform player;                 // 추격/공격 대상 Transform
    public CharacterController controller;   // 평지 이동용 CC

    [Header("Movement (Flat)")]
    public float chaseSpeed = 3.0f;          // 추격 속도(m/s)
    public float searchSpeed = 1.8f;         // 수색 속도(m/s)
    public float rotateSpeed = 12.0f;        // 회전 보간 속도.
    public float stoppingDistance = 1.2f;    // 멈춤 거리.
    public float gravity = -20.0f;           // 중력 가속도(음수)

    [Header("Attack")]
    public float attackRange = 1.8f;         // 공격 사거리.
    public float attackDamage = 10.0f;       // 공격 데미지.
    public float attackCooldown = 1.2f;      // 공격 간격(초)

    [Header("AttackRanged")]
    public Transform startRay;                  // 원거리 공격 시 레이캐스팅을 시작할 위치.
    public float rayLength = 150.0f;            // 레이의 길이.
    public LayerMask playerLayer;               // 레이 캐스팅으로 감지할 오브젝트의 레이어.
    public float attackRangedRange = 10.0f;         // 원거리 공격 사거리.
    public float attackRangedDamage = 30.0f;       // 원거리 공격 데미지.
    public float attackRangedCooldown = 1.5f;      // 원거리 공격 간격(초)

    [Header("Search")]
    public float searchDuration = 3.0f;      // 수색 시간(초)

    [Header("Debug")]
    public bool drawForward = false;         // 전방 디버그 레이.

    public StatusEffectHost host;

    public Animator animator;

    // ===== 런타임 캐시(상태간 공유) =========================================
    [HideInInspector] public Vector3 lastKnownPos; // 마지막으로 본 플레이어 좌표.
    [HideInInspector] public float attackTimer;    // 공격 쿨다운 남은 시간.
    [HideInInspector] public float searchTimer;    // 수색 남은 시간.
    [HideInInspector] public float attackRangedTimer;   // 원거리 공격 쿨다운 남은 시간.

    // ===== 상태 인스턴스 ====================================================
    private EnemyState currentState;         // 현재 상태.
    //private IdleState idle;                  // Idle 상태 인스턴스.
    //private ChaseState chase;                // Chase 상태 인스턴스.
    //private AttackState attack;              // Attack 상태 인스턴스.
    //private SearchState search;              // Search 상태 인스턴스.
    private DeadState dead;                  // Dead 상태 인스턴스.

    private void Awake()
    {
        // 필수 컴포넌트 자동 참조 보완.
        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }
        if (health == null)
        {
            health = GetComponent<Health>();
        }
        if(player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if(go != null)
            {
                player = go.transform;
            }
        }

        if (startRay != null)
        {
            startRay = transform;
        }

        dead = new DeadState(this);

        // 초기 런타임 캐시값 세팅.
        lastKnownPos = transform.position;
        attackTimer = 0.0f;
        searchTimer = 0.0f;

        // 최초 상태 진입: Idle
        RequestStateChange(new IdleState(this));

        if(health != null)
        {
            health.onChangedHP.AddListener(ChangeStateToRunAway);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 사망 체크는 브레인에서 일괄로 감시(상태 무시)
        if (health != null)
        {
            if (health.GetCurrentHealth() <= 0.0f)
            {
                if (currentState != dead)
                {
                    RequestStateChange(dead);
                    animator.SetBool("IsDead", true);
                    Destroy(gameObject, 3.0f);
                }
                return;
            }
        }

        // 전방 디버그(옵션)
        if (drawForward == true)
        {
            Debug.DrawRay(transform.position + Vector3.up * 1.0f, transform.forward * 1.5f, Color.yellow, 0.02f);
        }

        // 현재 상태 업데이트.
        if (currentState != null)
        {
            currentState.OnUpdate(dt);
        }
    }

    /// <summary>
    /// 안전한 상태 전환. Exit -> 상태 교체 -> Enter 순서로 호출한다.
    /// null 전달은 무시한다.
    /// </summary>
    public void RequestStateChange(EnemyState next)
    {
        if (next == null)
        {
            return;
        }

        if (currentState != null)
        {
            currentState.OnExit();
        }

        currentState = next;
        currentState.OnEnter();

        SyncAnimatorWithState(currentState);
    }

    /// <summary>
    /// 현재 FSM 상태에 맞게 Animator 파라미터를 설정한다.
    /// </summary>
    private void SyncAnimatorWithState(EnemyState st)
    {
        if (animator == null)
        {
            return;
        }

        string n = st.Name();

        if(n == "Dead")
        {
            return;
        }

        // 기본값 초기화
        //animator.SetBool("IsDead", false);
        //animator.SetBool("IsMoving", false);
        animator.SetFloat("Speed", 0.0f);

        if (n == "Idle")
        {
            // Idle은 아무것도 안 해도 됨
        }
        else if (n == "Chase")
        {
            //animator.SetBool("IsMoving", true);
            animator.SetFloat("Speed", 1.0f); // BlendTree에서 Run쪽으로
        }
        else if (n == "Search")
        {
            //animator.SetBool("IsMoving", true);
            animator.SetFloat("Speed", 0.5f); // 천천히
        }
        //else if (n == "Attack")
        //{
            // 공격은 트리거로
        //    animator.SetTrigger("Attack");
        //}
        //else if (n == "Dead")
        //{
        //    animator.SetBool("IsDead", true);
        //}
    }

    // ===== 공용 유틸(상태에서 호출) =========================================

    /// <summary>
    /// 평지 전제에서 목표점을 향해 회전한다(수평 보정).
    /// </summary>
    public void FacePosition(Vector3 target, float dt)
    {
        Vector3 flatTarget = target;
        flatTarget.y = transform.position.y;

        Vector3 to = flatTarget - transform.position; // 수평 방향.
        to.y = 0.0f;

        if (to.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, dt * rotateSpeed);
        }
    }

    /// <summary>
    /// 평지 전제에서 전진 이동(중력 보정 포함).
    /// </summary>
    public void MoveForward(float speed, float dt)
    {
        float speedMul = 1.0f;
        if (host != null)
        {
            speedMul = host.GetSpeedMultiplier();
        }

        Vector3 move = transform.forward * speed * speedMul; // 전진 속도.
        move.y = gravity;                          // 경계 안정화용 중력.

        controller.Move(move * dt);
    }

    /// <summary>
    /// 플레이어와의 현재 거리를 반환한다.
    /// </summary>
    public float DistanceToPlayer()
    {
        if (player == null)
        {
            return float.PositiveInfinity;
        }
        float d = Vector3.Distance(transform.position, player.position);
        return d;
    }

    /// <summary>
    /// 현재 상태명 문자열(디버그/HUD용).
    /// </summary>
    public string GetCurrentStateName()
    {
        if (currentState == null)
        {
            return "None";
        }
        string n = currentState.Name();
        return n;
    }

    /// <summary>
    /// 실제 데미지를 플레이어(IDamageable)에게 전달한다.
    /// (간단 버전: 플레이어 위치를 히트 포인트로 사용)
    /// </summary>
    public void DoAttack()
    {
        if (player == null)
        {
            return;
        }

        // 사거리 유지 체크.
        float dist = DistanceToPlayer();
        if (dist > attackRange)
        {
            return;
        }

        IDamageable id = player.GetComponent<IDamageable>();
        if (id == null)
        {
            return;
        }

        Vector3 hp = player.position; // 히트 포인트(단순화)
        Vector3 n = Vector3.up;             // 노멀(연출용)

        id.ApplyDamage(attackDamage, hp, n, transform);
    }

    public void DoAttackRanged()
    {
        if (player == null)
        {
            return;
        }

        // 사거리 유지 체크.
        float dist = DistanceToPlayer();
        if (dist > attackRangedRange)
        {
            return;
        }

        IDamageable id = player.GetComponent<IDamageable>();
        if (id == null)
        {
            return;
        }

        Vector3 hp = player.position; // 히트 포인트(단순화)
        Vector3 n = Vector3.up;             // 노멀(연출용)

        id.ApplyDamage(attackRangedDamage, hp, n, transform);
    }

    public bool CanHitPlayer()
    {
        Ray ray = new Ray(startRay.position, startRay.forward);
        RaycastHit hit;
        bool got = Physics.Raycast(ray, out hit, rayLength, playerLayer, QueryTriggerInteraction.Ignore);

        return got;
    }

    void ChangeStateToRunAway()
    {
        if(currentState.Name() == "RunAway")
        {
            return;
        }

        RequestStateChange(new RunAwayState(this));
    }
}
