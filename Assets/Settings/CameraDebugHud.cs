using UnityEngine;
using TMPro;

/// <summary>
/// [용도] 카메라 튜닝 중 핵심 지표 HUD(속도, 접지, FOV, Lean 각) 표시.
/// [UI] Canvas에 'CamHudLabel'(TextMeshProUGUI) 생성 후 label에 연결.
/// </summary>
public class CameraDebugHud : MonoBehaviour
{
    public TextMeshProUGUI label;     // label: 출력용 텍스트 UI.
    public LocomotionFeed feed;       // feed: 속도/접지 데이터.
    public Camera targetCamera;       // targetCamera: FOV 조회.
    public LeanEffect lean;           // lean: Lean 상태 확인(선택)

    private void Awake()
    {
        if (label == null)
        {
            GameObject t = GameObject.Find("CamHudLabel"); // t: 씬에서 찾은 TMP 오브젝트.
            if (t != null)
            {
                label = t.GetComponent<TextMeshProUGUI>();
            }
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (label == null)
        {
            return;
        }

        float speed = 0f;        // speed: 수평 속도(m/s)
        bool grounded = false;   // grounded: 접지 여부.
        float fov = 0f;          // fov: 카메라 시야각(도)
        float leanDeg = 0f;      // leanDeg: 좌우 기울임 각(도)

        if (feed != null)
        {
            speed = feed.HorizontalSpeed;
            grounded = feed.IsGrounded;
        }

        if (targetCamera != null)
        {
            fov = targetCamera.fieldOfView;
        }

        if (lean != null && targetCamera != null)
        {
            float z = targetCamera.transform.localRotation.eulerAngles.z; // z: 로컬 Z 오일러(0~360)
            if (z > 180f)
            {
                z -= 360f;
            }
            leanDeg = -z; // LeanEffect에서 -currentDeg 적용했으므로 부호 반전.
        }

        label.text = $"Speed {speed:0.00} m/s | Grounded {(grounded == true ? "Y" : "N")} | FOV {fov:0.0}° | Lean {leanDeg:0.0}°";
    }
}
