using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 4분할 이미지(상/하/좌/우)를 사용해 확산을 시각화.
/// 이동 속도와 사격 이벤트에 반응해 확산값을 증가/감쇠.
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [Header("Refs")]
    public CharacterController controller; // 이동 속도 참조.
    public RectTransform topPart;
    public RectTransform bottomPart;
    public RectTransform leftPart;
    public RectTransform rightPart;

    [Header("Spread")]
    public float baseSpread = 6f;          // 기본 간격(px)
    public float moveSpreadFactor = 10f;   // 이동 속도당 가산(px per m/s)
    public float fireKick = 12f;           // 발사 시 즉시 가산(px)
    public float decaySpeed = 40f;         // 초당 감쇠(px/s)
    public float maxSpread = 40f;          // 최대 확산(px)

    private float currentSpread;           // 현재 확산(px)

    private void Awake()
    {
        if (controller == null)
        {
            controller = FindAnyObjectByType<CharacterController>();
        }
        currentSpread = baseSpread;
    }

    private void Update()
    {
        // 1) 이동 속도 기반 목표 확산.
        float planarSpeed = 0.0f;
        if (controller != null)
        {
            Vector3 v = controller.velocity;
            v.y = 0.0f;
            planarSpeed = v.magnitude;
        }

        float targetSpread = baseSpread + moveSpreadFactor * planarSpeed;

        // 2) 현재 확산을 targetSpread 쪽으로 감쇠 이동.
        if (currentSpread > targetSpread)
        {
            currentSpread -= decaySpeed * Time.deltaTime;
            if (currentSpread < targetSpread)
            {
                currentSpread = targetSpread;
            }
        }
        else
        {
            currentSpread += decaySpeed * Time.deltaTime;
            if (currentSpread > targetSpread)
            {
                currentSpread = targetSpread;
            }
        }

        if (currentSpread > maxSpread)
        {
            currentSpread = maxSpread;
        }

        // 3) UI 반영.
        ApplySpread(currentSpread);
    }

    public void PulseFireSpread()
    {
        currentSpread += fireKick;
        if (currentSpread > maxSpread)
        {
            currentSpread = maxSpread;
        }
    }

    private void ApplySpread(float s)
    {
        if (topPart != null)
        {
            topPart.anchoredPosition = new Vector2(0.0f, s);
        }
        if (bottomPart != null)
        {
            bottomPart.anchoredPosition = new Vector2(0.0f, -s);
        }
        if (leftPart != null)
        {
            leftPart.anchoredPosition = new Vector2(-s, 0.0f);
        }
        if (rightPart != null)
        {
            rightPart.anchoredPosition = new Vector2(s, 0.0f);
        }
    }
}
