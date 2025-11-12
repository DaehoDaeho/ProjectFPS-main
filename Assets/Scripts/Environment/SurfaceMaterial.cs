using UnityEngine;

public enum SurfaceType
{
    Concrete = 0,
    Dirt = 1,
    Wood = 2,
    Metal = 3,
    Water = 4
}

/// <summary>
/// 바닥/벽 등 Collider가 붙은 오브젝트에 부착하여
/// '이 표면은 어떤 재질인가'를 노출하는 간단한 태그 컴포넌트.
/// </summary>
[DisallowMultipleComponent]
public class SurfaceMaterial : MonoBehaviour
{
    public SurfaceType surfaceType = SurfaceType.Concrete; // 이 오브젝트의 표면 타입.
}
