using System.Globalization;
using UnityEngine;

/// <summary>
/// 로컬 입력(WASD, 마우스 시선)을 읽어 주기적으로 서버에 INPUT 전송.
/// - 단일 PC 시연: 빌드(.exe) 창에서 조작.
/// - yaw/pitch는 누적(간단 카메라 느낌).
/// </summary>
public class InputSender : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 3.0f;     // 마우스 감도(도/픽셀 근사)
    public float sendRate = 20.0f;            // 초당 전송 회수(서버 tick과 비슷하게)
    public bool lockCursorOnStart = true;     // 시작 시 커서 잠금

    private float yaw;                         // 수평 각(도)
    private float pitch;                       // 수직 각(도)
    private float sendAccumulator;             // 전송 주기 누적

    private void Start()
    {
        if (lockCursorOnStart == true)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        // 마우스 시선
        float mdx = Input.GetAxis("Mouse X");
        float mdy = Input.GetAxis("Mouse Y");

        yaw = yaw + (mdx * mouseSensitivity);
        pitch = pitch - (mdy * mouseSensitivity); // 마우스 Y는 반대
        pitch = Mathf.Clamp(pitch, -80.0f, 80.0f);

        // 이동 입력
        float mx = Input.GetAxisRaw("Horizontal"); // A/D: -1/1
        float my = Input.GetAxisRaw("Vertical");   // W/S: -1/1

        // 전송 주기
        float dt = Time.deltaTime;
        sendAccumulator = sendAccumulator + dt;

        float interval = 1.0f / sendRate;
        while (sendAccumulator >= interval)
        {
            SendInput(mx, my);
            sendAccumulator = sendAccumulator - interval;
        }
    }

    private void SendInput(float mx, float my)
    {
        if (NetworkRunner.instance == null)
        {
            return;
        }

        // 1) 내 카메라 yaw로 로컬 입력(mx,my)을 월드 방향으로 회전
        Vector3 moveLocal = new Vector3(mx, 0.0f, my);
        Quaternion rot = Quaternion.Euler(0.0f, yaw, 0.0f);
        Vector3 worldDir = rot * moveLocal;                  // (wx, 0, wz)

        // 2) 정규화(대각선 속도 보정)
        if (worldDir.sqrMagnitude > 0.0001f)
        {
            worldDir = worldDir.normalized;
        }

        // 3) 로케일에 영향을 받지 않도록 InvariantCulture로 문자열 구성
        //    payload: wx,wz,yaw,pitch
        string payload =
            worldDir.x.ToString("F3", CultureInfo.InvariantCulture) + "," +
            worldDir.z.ToString("F3", CultureInfo.InvariantCulture) + "," +
            yaw.ToString("F1", CultureInfo.InvariantCulture) + "," +
            pitch.ToString("F1", CultureInfo.InvariantCulture);

        bool isClient = NetworkRunner.instance.IsClientConnected();
        bool isServer = NetworkRunner.instance.IsServerRunning();

        if (isClient == true)
        {
            NetworkRunner.instance.ClientSendLine("INPUTW|" + payload);
            return;
        }

        if (isServer == true && isClient == false)
        {
            NetworkRunner.instance.ServerInjectCommand(0, "INPUTW", payload);
            return;
        }
    }
}
