using UnityEngine;

/// <summary>
/// 적의 '시야'를 계산하는 모듈.
/// - 거리: viewDistance 미만.
/// - 시야각: viewAngle/2 반각 이내(도트 프로덕트로 판단)
/// - 가림: eye 위치에서 타겟 방향으로 레이캐스트 -> 첫 히트가 타겟 루트라면 '보임'
/// </summary>
public class EnemySenses : MonoBehaviour
{
    [Header("Vision")]
    public Transform eye;                  // 시야의 시작점(머리/눈 위치)
    public float viewDistance = 18.0f;     // 시야 거리(미터)
    public float viewAngle = 110.0f;       // 전체 시야각. 반각 = viewAngle * 0.5
    public LayerMask visionMask;           // 레이캐스트 충돌 마스크(벽/바닥/플레이어 포함)
    public Transform target;               // 감지할 대상(플레이어 루트 Transform)

    [Header("Debug")]
    public bool drawDebug = false;         // 디버그 선 그리기 토글.

    private void Awake()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if(go != null)
        {
            target = go.transform;
        }
    }

    /// <summary>
    /// 타겟이 현재 '보이는지'를 계산.
    /// 보이면 true를 반환하고, lastKnownPos에 현재 타겟 위치를 돌려줍니다.
    /// </summary>
    public bool CanSeeTarget(out Vector3 lastKnownPos)
    {
        // 반환값 초기화.
        lastKnownPos = Vector3.zero;

        // 필수 참조 방어.
        if (eye == null)
        {
            return false;
        }
        if (target == null)
        {
            return false;
        }

        // 1) 거리 판정.
        Vector3 toTarget = target.position - eye.position; // 타겟까지의 벡터(월드)
        float dist = toTarget.magnitude;                   // 거리(미터)
        if (dist > viewDistance)
        {
            return false;
        }

        // 2) 시야각 판정 (도트 프로덕트 = cos(각))
        Vector3 forward = eye.forward;                     // 눈의 전방.
        Vector3 dir = toTarget.normalized;                 // 타겟 방향 단위 벡터.
        float dot = Vector3.Dot(forward, dir);             // cos(theta), 벡터의 내적으로 두 벡터 사이의 각도를 구함.
        float halfRad = (viewAngle * 0.5f) * Mathf.Deg2Rad;// 반각 라디안.
        float cosHalf = Mathf.Cos(halfRad);                // 임계치: cos(반각)

        // dot < cos(반각) 이면 시야 밖.
        if (dot < cosHalf)
        {
            return false;
        }

        // 3) 가림(occlusion) 체크: Raycast로 첫 충돌체 확인.
        Ray ray = new Ray(eye.position, dir);
        RaycastHit hit;
        bool got = Physics.Raycast(ray, out hit, viewDistance, visionMask, QueryTriggerInteraction.Ignore);

        if (got == true)
        {
            // 첫 히트의 루트가 타겟 루트와 동일하면 '가려지지 않음'
            Transform h = hit.collider.transform;
            if (IsSameRoot(h, target) == true)
            {
                lastKnownPos = target.position;

                if (drawDebug == true)
                {
                    Debug.DrawLine(eye.position, hit.point, Color.green, 0.1f);
                }
                return true;
            }
            else
            {
                if (drawDebug == true)
                {
                    Debug.DrawLine(eye.position, hit.point, Color.red, 0.1f);
                }
                return false;
            }
        }

        // 레이가 아무것도 맞지 않으면 '보이지 않음' 처리.
        return false;
    }

    /// <summary>
    /// 두 Transform이 동일한 최상위 루트인지 판정합니다.
    /// (캐릭터 복합 콜라이더 구조에서 '같은 대상인가' 판단용)
    /// </summary>
    private bool IsSameRoot(Transform a, Transform b)
    {
        if (a == null)
        {
            return false;
        }
        if (b == null)
        {
            return false;
        }

        Transform ra = a.root;
        Transform rb = b.root;
        if (ra == rb)
        {
            return true;
        }
        return false;
    }
}
