using UnityEngine;

/// <summary>
/// 플레이어 발밑으로 Raycast를 수행하여 현재 디딘 표면의 타입/좌표/법선을 제공.
/// </summary>
[DisallowMultipleComponent]
public class FootstepSurfaceDetector : MonoBehaviour
{
    public Transform probeOrigin;     // 레이 시작점(보통 CharacterController 중심 약간 위)
    public float probeDistance = 1.2f; // 아래로 쏠 거리(플레이어 키/CC 높이에 맞춰 조정)
    public LayerMask groundMask;      // 바닥 레이어.

    /// <summary>
    /// 현재 발밑 표면의 정보를 가져온다.
    /// </summary>
    public bool TryGetSurface(out SurfaceType surface, out Vector3 point, out Vector3 normal)
    {
        surface = SurfaceType.Concrete;
        point = Vector3.zero;
        normal = Vector3.up;

        if (probeOrigin == null)
        {
            return false;
        }

        RaycastHit hit;
        bool got = Physics.Raycast(
            probeOrigin.position,     // 시작점.
            Vector3.down,             // 아래 방향.
            out hit,                  // 결과.
            probeDistance,            // 거리.
            groundMask,               // 바닥 레이어.
            QueryTriggerInteraction.Ignore // 트리거 무시.
        );

        if (got == true)
        {
            point = hit.point;
            normal = hit.normal;

            SurfaceMaterial s = hit.collider.GetComponent<SurfaceMaterial>();
            if (s != null)
            {
                surface = s.surfaceType;
                return true;
            }
            else
            {
                // 타입이 없으면 기본값(콘크리트) 취급.
                surface = SurfaceType.Concrete;
                return true;
            }
        }

        return false;
    }
}
