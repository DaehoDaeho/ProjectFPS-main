using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// uGUI 로비 화면과 NetworkRunner를 연결하는 UI 컨트롤러.
/// - 입력값(Address/Port/Name)
/// - 버튼(Host/Join/Leave/Start)
/// - Toggle(Ready)
/// - 텍스트(Status/RoomList)
/// </summary>
public class LobbyUI : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField inputAddress;      // 주소 입력(기본 127.0.0.1)
    public TMP_InputField inputPort;         // 포트 입력(기본 7777)
    public TMP_InputField inputName;         // 닉네임 입력(기본 Player)

    [Header("Buttons")]
    public Button buttonHost;                // Host 시작
    public Button buttonJoin;                // 클라 접속
    public Button buttonLeave;               // 접속 종료
    public Button buttonStart;               // 게임 시작(호스트 전용)

    [Header("Ready")]
    public Toggle toggleReady;               // 준비 토글

    [Header("Texts")]
    public TMP_Text textStatus;              // 상태 로그 출력
    public TMP_Text textRoom;                // ROOM JSON(간단 표시)

    private string cachedName;               // 최근 전송한 이름 보관

    private void Start()
    {
        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.onStatus += OnStatus;
            NetworkRunner.instance.onRoomText += OnRoomText;
            NetworkRunner.instance.onHostModeChanged += OnHostModeChanged;
            NetworkRunner.instance.onClientConnectedChanged += OnClientConnectedChanged;
            NetworkRunner.instance.onStartSignal += OnStartSignal;
        }

        if (buttonHost != null)
        {
            buttonHost.onClick.AddListener(OnClickHost);
        }
        if (buttonJoin != null)
        {
            buttonJoin.onClick.AddListener(OnClickJoin);
        }
        if (buttonLeave != null)
        {
            buttonLeave.onClick.AddListener(OnClickLeave);
        }
        if (buttonStart != null)
        {
            buttonStart.onClick.AddListener(OnClickStart);
        }
        if (toggleReady != null)
        {
            toggleReady.onValueChanged.AddListener(OnToggleReady);
        }

        // 기본값 채우기
        if (inputAddress != null)
        {
            inputAddress.text = "127.0.0.1";
        }
        if (inputPort != null)
        {
            inputPort.text = "7777";
        }
        if (inputName != null)
        {
            inputName.text = "Player";
        }

        // 초기 버튼 상태
        SetStartButtonInteractable(false);
    }

    private void OnDestroy()
    {
        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.onStatus -= OnStatus;
            NetworkRunner.instance.onRoomText -= OnRoomText;
            NetworkRunner.instance.onHostModeChanged -= OnHostModeChanged;
            NetworkRunner.instance.onClientConnectedChanged -= OnClientConnectedChanged;
            NetworkRunner.instance.onStartSignal -= OnStartSignal;
        }
    }

    // ===== 버튼 핸들러 =====

    private void OnClickHost()
    {
        int port = 7777;
        if (inputPort != null)
        {
            int.TryParse(inputPort.text, out port);
        }

        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.HostStart(port);
        }

        // 호스트 표시 이름을 서버에 바로 반영
        string hostName = "Host";
        if (inputName != null)
        {
            hostName = inputName.text;
        }
        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.HostSetName(hostName);
        }
    }

    private void OnClickJoin()
    {
        string addr = "127.0.0.1";
        if (inputAddress != null)
        {
            addr = inputAddress.text;
        }

        int port = 7777;
        if (inputPort != null)
        {
            int.TryParse(inputPort.text, out port);
        }

        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.ClientConnect(addr, port);
        }

        // 접속 후 바로 JOIN 전송
        cachedName = "Player";
        if (inputName != null)
        {
            cachedName = inputName.text;
        }

        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.ClientSendLine("JOIN|" + cachedName);
        }
    }

    private void OnClickLeave()
    {
        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.ClientSendLine("LEAVE");
            NetworkRunner.instance.ClientDisconnect();
        }
    }

    private void OnClickStart()
    {
        if (NetworkRunner.instance == null)
        {
            return;
        }

        bool isServer = NetworkRunner.instance.IsServerRunning();
        bool isClient = NetworkRunner.instance.IsClientConnected();

        // 호스트 전용 경로(에디터가 서버 역할일 때)
        if (isServer == true && isClient == false)
        {
            NetworkRunner.instance.HostBroadcastStart();
            return;
        }

        // 일반 클라이언트 경로
        NetworkRunner.instance.ClientSendLine("START");
    }

    private void OnToggleReady(bool on)
    {
        if (NetworkRunner.instance == null)
        {
            return;
        }

        bool isServer = NetworkRunner.instance.IsServerRunning();
        bool isClient = NetworkRunner.instance.IsClientConnected();

        // 호스트(서버)인데 클라이언트로 접속하지 않은 경우 → 서버에 직접 반영
        if (isServer == true && isClient == false)
        {
            NetworkRunner.instance.HostSetReady(on);
            return;
        }

        // 일반 클라이언트 경로
        string value = on == true ? "1" : "0";
        NetworkRunner.instance.ClientSendLine("READY|" + value);
    }

    // ===== 이벤트 수신 =====

    private void OnStatus(string msg)
    {
        if (textStatus == null)
        {
            return;
        }

        textStatus.text = msg;
    }

    private void OnRoomText(string json)
    {
        if (textRoom == null)
        {
            return;
        }

        // 초보용: JSON을 그대로 표시(나중에 파싱해서 깔끔하게 보여줘도 됨)
        textRoom.text = json;
    }

    private void OnHostModeChanged(bool isHost)
    {
        SetStartButtonInteractable(isHost);
    }

    private void OnClientConnectedChanged(bool connected)
    {
        // 연결 상태에 따라 UI를 바꿀 수 있다(여기서는 생략)
    }

    private void OnStartSignal()
    {
        // 일단 로그만. 추후 씬 로드를 연동.
        OnStatus("START signal received.");
    }

    private void SetStartButtonInteractable(bool on)
    {
        if (buttonStart != null)
        {
            buttonStart.interactable = on;
        }
    }
}
