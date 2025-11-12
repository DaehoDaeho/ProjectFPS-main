using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 머리 위 월드 공간 Slider로 HP 표시.
/// - LateUpdate에서 카메라를 바라보도록 회전(빌보딩)
/// - Health.GetCurrentHealth()를 주기적으로 폴링(이벤트 기반 확장 가능)
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    public Health health;           // 대상 Health(부모에서 자동 탐색 권장)
    public Slider slider;           // 월드 공간 슬라이더.
    public Transform followTarget;  // 이 Transform을 카메라 쪽으로 회전(보통 바의 루트)
    public Camera mainCamera;       // 참조 카메라(일반적으로 Main Camera)

    private float maxValue;         // 최대 체력 캐시(슬라이더 범위 설정)

    private void Awake()
    {
        // 필드 자동 보완.
        if (health == null)
        {
            health = GetComponentInParent<Health>();
        }
        if (slider == null)
        {
            slider = GetComponentInChildren<Slider>();
        }
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // 슬라이더 범위 초기화.
        maxValue = health != null ? health.maxHealth : 100.0f;
        if (slider != null)
        {
            slider.minValue = 0.0f;
            slider.maxValue = maxValue;
            slider.value = maxValue;
        }
    }

    private void LateUpdate()
    {
        // 1) HP 값 반영
        if (health != null && slider != null)
        {
            slider.value = health.GetCurrentHealth();
        }

        // 2) 카메라를 항상 바라보도록 회전(빌보드)
        if (followTarget != null && mainCamera != null)
        {
            Vector3 toCam = mainCamera.transform.position - followTarget.position;
            if (toCam.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(toCam.normalized);
                followTarget.rotation = look;
            }
        }
    }
}
