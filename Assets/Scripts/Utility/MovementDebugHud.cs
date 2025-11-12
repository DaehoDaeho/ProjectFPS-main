using UnityEngine;
using TMPro;

/// <summary>
/// 속도, 접지, 경사각 등 이동 관련 디버그 정보를 표시.
/// </summary>
public class MovementDebugHud : MonoBehaviour
{
    public TextMeshProUGUI label;
    public LocomotionFeed feed;
    public CharacterController controller;
    public GroundingStabilizer stabilizer;

    private void Awake()
    {
        if (label == null)
        {
            GameObject t = GameObject.Find("MoveHudLabel");
            if (t != null)
            {
                label = t.GetComponent<TextMeshProUGUI>();
            }
        }
        if (controller == null)
        {
            controller = FindAnyObjectByType<CharacterController>();
        }
    }

    private void Update()
    {
        if (label == null)
        {
            return;
        }

        float speed = 0f;
        bool grounded = false;
        float slopeDeg = 0f;

        if (feed != null)
        {
            speed = feed.HorizontalSpeed;
            grounded = feed.IsGrounded;
        }

        if (controller != null && stabilizer != null)
        {
            Vector3 n = Vector3.up;
            bool got = false;

            // 내부 비공개지만, 유사 계산으로 대체(아래 레이)
            RaycastHit hit;
            Vector3 origin = controller.transform.position + Vector3.up * 0.2f;
            if (Physics.Raycast(origin, Vector3.down, out hit, stabilizer.probeDistance + 0.2f, stabilizer.groundMask, QueryTriggerInteraction.Ignore) == true)
            {
                got = true;
                n = hit.normal;
            }

            if (got == true)
            {
                slopeDeg = Vector3.Angle(n, Vector3.up);
            }
        }

        label.text = $"Speed {speed:0.00} m/s | Grounded {(grounded == true ? "Y" : "N")} | Slope {slopeDeg:0.0}°";
    }
}
