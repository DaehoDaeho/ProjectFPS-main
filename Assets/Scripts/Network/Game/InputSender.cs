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

        int fire = 0; // 추후 사용할 예정
        string payload = $"{mx:F3},{my:F3},{yaw:F1},{pitch:F1},{fire}";

        bool isClient = NetworkRunner.instance.IsClientConnected();
        bool isServer = NetworkRunner.instance.IsServerRunning();

        // 1) 일반 클라이언트: 네트워크로 INPUT 전송
        if (isClient == true)
        {
            string line = "INPUT|" + payload;
            NetworkRunner.instance.ClientSendLine(line);
            return;
        }

        // 2) 호스트 전용 경로: 서버에 직접 주입(fromClientId = 0)
        if (isServer == true && isClient == false)
        {
            NetworkRunner.instance.ServerInjectCommand(0, "INPUT", payload);
            return;
        }

        // 그 외(서버/클라 모두 아님)면 아무 것도 하지 않는다.
    }
}
