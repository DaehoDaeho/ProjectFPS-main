using UnityEngine;
using TMPro;
using System.Text;

public class DebugHudUGUI : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI hudText; // 화면 표시용 TMP 텍스트;
    public Transform playerTransform; // 선택: 플레이어(없으면 Main Camera);


    [Header("Toggle")]
    public bool showHud = true; // HUD 표시 여부.
    public KeyCode toggleKey = KeyCode.BackQuote; // 큰 따옴표 키.


    [Header("Smoothing")]
    public float emaFactor = 0.1f; // 0~1, 클수록 최근 프레임 가중치 큼;.

    private float smoothedDt = 0.0f; // EMA 내부 상태.
    private StringBuilder sb; // 문자열 누적 버퍼.


    private void Awake()
    {
        if (hudText == null)
        {
            Debug.LogWarning("[DebugHudUGUI] hudText is not set.");
        }


        if (playerTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                playerTransform = cam.transform;
            }
        }


        sb = new StringBuilder(256);


        if (hudText != null)
        {
            hudText.gameObject.SetActive(showHud);
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(toggleKey) == true)
        {
            showHud = (showHud == false) ? true : false;
            if (hudText != null)
            {
                hudText.gameObject.SetActive(showHud);
            }
        }


        float dt = Time.unscaledDeltaTime;
        if (smoothedDt <= 0.0f)
        {
            smoothedDt = dt; // 초기화;
        }
        else
        {
            smoothedDt = Mathf.Lerp(smoothedDt, dt, emaFactor);
        }


        if (showHud == true && hudText != null)
        {
            float fps = 1.0f / Mathf.Max(smoothedDt, 0.0001f);
            sb.Length = 0; // 버퍼 재사용으로 GC 절감.
            sb.Append("FPS: ").Append(fps.ToString("F1")).AppendLine();
            sb.Append("Frame Time: ").Append((smoothedDt * 1000.0f).ToString("F2")).Append(" ms").AppendLine();


            if (playerTransform != null)
            {
                Vector3 p = playerTransform.position;
                Vector3 e = playerTransform.eulerAngles;
                sb.Append("Pos (m): X ").Append(p.x.ToString("F2"))
                .Append(", Y ").Append(p.y.ToString("F2"))
                .Append(", Z ").Append(p.z.ToString("F2")).AppendLine();
                sb.Append("Rot (deg): X ").Append(e.x.ToString("F1"))
                .Append(", Y ").Append(e.y.ToString("F1"))
                .Append(", Z ").Append(e.z.ToString("F1"));
            }

            hudText.text = sb.ToString();
        }
    }
}