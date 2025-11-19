using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// 서버/클라이언트 네트워킹을 총괄하는 런타임 매니저.
/// - Host(서버) 시작/중지
/// - Client(클라이언트) 접속/종료
/// - Update에서 수신 폴링(논블로킹)
/// - 간단 이벤트(Action)로 UI에 알림
///
/// 라인 프로토콜(LineProtocol)을 이용해 문자열 메시지를 주고받는다.
/// </summary>
public class NetworkRunner : MonoBehaviour
{
    public static NetworkRunner instance;                 // 전역 접근(씬에 1개)

    [Header("Defaults")]
    public int defaultPort = 7777;                        // 기본 포트
    public float roomBroadcastInterval = 0.5f;            // 서버의 ROOM 방송 주기(초)

    // ==== 서버 측 필드 ====
    private TcpListener serverListener;                   // 서버 리스너 소켓
    private bool serverRunning;                           // 서버 동작 여부
    private Dictionary<int, ClientConn> clients;          // 접속 중 클라이언트 맵
    private int nextClientId;                             // 부여할 다음 클라이언트 ID
    private float roomBroadcastTimer;                     // ROOM 주기 타이머

    // ==== 클라이언트 측 필드 ====
    private TcpClient client;                             // 클라이언트 소켓
    private NetworkStream clientStream;                   // 클라 스트림
    private LineProtocol clientLP;                        // 클라 라인 프로토콜
    private bool clientConnected;                         // 클라 접속 여부

    // === Host(서버 자신)도 ROOM에 표시하기 위한 플레이어 엔트리 ===
    private PlayerInfo hostPlayer;              // id = 0으로 사용할 호스트 플레이어
    public bool includeHostInRoom = true;       // 호스트를 ROOM 목록에 포함할지 여부

    // ==== 로비 상태(서버 authoritative) ====
    private class PlayerInfo
    {
        public int id;            // 고유 ID
        public string name;       // 닉네임
        public bool ready;        // 준비 여부
    }

    private class ClientConn
    {
        public int id;                        // 클라 ID
        public TcpClient socket;              // TCP 소켓
        public NetworkStream stream;          // 스트림
        public LineProtocol lp;               // 라인 프로토콜
        public PlayerInfo info;               // 플레이어 정보
    }

    private Dictionary<int, PlayerInfo> players;          // 서버가 관리하는 플레이어 목록
    private List<int> pendingRemoveClientIds = new List<int>(); // 클라이언트 제거 지연 처리용(열거 중 수정 방지)

    // ==== UI 연동 이벤트 ====
    public Action<string> onStatus;                       // 상태 로그 출력용
    public Action<string> onRoomText;                     // ROOM 스냅샷 텍스트
    public Action<bool> onHostModeChanged;                // 호스트 여부 알림
    public Action<bool> onClientConnectedChanged;         // 클라 접속 여부 알림
    public Action onStartSignal;                          // START 수신 알림

    // 게임 단계에서 사용할 서버/클라 커맨드 콜백
    // 서버: (fromClientId, cmd, payload)
    // 클라: (cmd, payload)
    public Action<int, string, string> onServerCommand;
    public Action<string, string> onClientCommand;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        clients = new Dictionary<int, ClientConn>();
        players = new Dictionary<int, PlayerInfo>();
        nextClientId = 1;

        // Runner를 씬 전환 후에도 유지
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // 서버 수신/수락
        if (serverRunning == true)
        {
            Server_AcceptPending();
            Server_PollReceive();
            Server_RoomBroadcastTick();
        }

        // 클라이언트 수신
        if (clientConnected == true)
        {
            Client_PollReceive();
        }
    }

    // ===== 서버: 시작/중지 =====

    public void HostStart(int port)
    {
        if (serverRunning == true)
        {
            if (onStatus != null)
            {
                onStatus.Invoke("Server already running.");
            }
            return;
        }

        try
        {
            serverListener = new TcpListener(IPAddress.Any, port);
            serverListener.Start();
            serverRunning = true;

            roomBroadcastTimer = 0.0f;

            // 호스트를 id=0 플레이어로 등록
            if (includeHostInRoom == true)
            {
                hostPlayer = new PlayerInfo();
                hostPlayer.id = 0;
                hostPlayer.name = "Host";
                hostPlayer.ready = false;

                if (players.ContainsKey(0) == false)
                {
                    players.Add(0, hostPlayer);
                }
            }

            if (onStatus != null)
            {
                onStatus.Invoke($"Server listening on port {port}");
            }
            if (onHostModeChanged != null)
            {
                onHostModeChanged.Invoke(true);
            }
        }
        catch (Exception e)
        {
            if (onStatus != null)
            {
                onStatus.Invoke($"Server start failed: {e.Message}");
            }
        }
    }

    public void HostStop()
    {
        if (serverRunning == false)
        {
            return;
        }

        foreach (var kv in clients)
        {
            try
            {
                kv.Value.socket.Close();
            }
            catch
            {
            }
        }
        clients.Clear();
        players.Clear();

        try
        {
            serverListener.Stop();
        }
        catch
        {
        }

        serverRunning = false;

        if (onHostModeChanged != null)
        {
            onHostModeChanged.Invoke(false);
        }

        if (onStatus != null)
        {
            onStatus.Invoke("Server stopped.");
        }

        // 서버를 끄면 클라도 끊어지는 상황이 많으므로 추가 정리
        if (clientConnected == true)
        {
            ClientDisconnect();
        }
    }

    // ===== 서버: 수락/수신/방송 =====

    private void Server_AcceptPending()
    {
        if (serverListener == null)
        {
            return;
        }

        bool pending = serverListener.Pending();
        if (pending == false)
        {
            return;
        }

        try
        {
            TcpClient sock = serverListener.AcceptTcpClient();
            sock.NoDelay = true;

            ClientConn cc = new ClientConn();
            cc.id = nextClientId;
            nextClientId = nextClientId + 1;
            cc.socket = sock;
            cc.stream = sock.GetStream();
            cc.lp = new LineProtocol(cc.stream);

            PlayerInfo pi = new PlayerInfo();
            pi.id = cc.id;
            pi.name = $"Player{cc.id}";
            pi.ready = false;
            cc.info = pi;

            clients.Add(cc.id, cc);
            players.Add(cc.id, pi);

            if (onStatus != null)
            {
                onStatus.Invoke($"Client {cc.id} connected.");
            }
        }
        catch (Exception e)
        {
            if (onStatus != null)
            {
                onStatus.Invoke($"Accept failed: {e.Message}");
            }
        }
    }

    private void Server_PollReceive()
    {
        foreach (var kv in clients)
        {
            ClientConn cc = kv.Value;
            if (cc == null)
            {
                continue;
            }
            if (cc.socket == null)
            {
                continue;
            }

            NetworkStream st = cc.stream;
            if (st == null)
            {
                continue;
            }

            List<string> lines = cc.lp.ReadAvailableLines();
            if (lines == null)
            {
                continue;
            }

            for (int i = 0; i < lines.Count; i = i + 1)
            {
                string line = lines[i];
                Server_HandleLine(cc, line);
            }

            // 연결 확인(간단): 소켓이 닫혔는지 체크 불가 -> 예외 발생 시 제거 로직으로 처리
            // 여기서는 생략
        }

        // === 루프가 끝난 뒤에 한꺼번에 제거 적용 ===
        if (pendingRemoveClientIds != null && pendingRemoveClientIds.Count > 0)
        {
            for (int i = 0; i < pendingRemoveClientIds.Count; i = i + 1)
            {
                int id = pendingRemoveClientIds[i];
                Server_RemoveClient(id);
            }
            pendingRemoveClientIds.Clear();

            // 실제 삭제가 반영된 상태를 한 번만 브로드캐스트
            Server_BroadcastRoom();
        }

    }

    private void Server_HandleLine(ClientConn cc, string line)
    {
        if (string.IsNullOrEmpty(line) == true)
        {
            return;
        }

        // "CMD|payload" 형태
        int bar = line.IndexOf('|');
        string cmd;
        string payload;

        if (bar >= 0)
        {
            cmd = line.Substring(0, bar);
            payload = line.Substring(bar + 1);
        }
        else
        {
            cmd = line;
            payload = string.Empty;
        }

        if (cmd == "JOIN")
        {
            if (string.IsNullOrEmpty(payload) == false)
            {
                cc.info.name = payload;
            }
            cc.info.ready = false;

            if (onStatus != null)
            {
                onStatus.Invoke($"JOIN from {cc.id} as '{cc.info.name}'");
            }

            Server_BroadcastRoom();
        }
        else if (cmd == "READY")
        {
            if (payload == "1")
            {
                cc.info.ready = true;
            }
            else
            {
                cc.info.ready = false;
            }

            if (onStatus != null)
            {
                onStatus.Invoke($"READY {cc.id} = {cc.info.ready}");
            }

            Server_BroadcastRoom();
        }
        else if (cmd == "LEAVE")
        {
            // 열거 중 컬렉션 수정 에러를 막기 위해, 즉시 삭제하지 않고 대기열에 넣는다.
            pendingRemoveClientIds.Add(cc.id);
            // ROOM 방송은 실제 삭제가 적용된 뒤에 한 번만 하자(아래에서 처리됨).
        }
        else if (cmd == "START")
        {
            // 호스트 전용이라고 가정(실습에서는 에디터=호스트 버튼으로만 전송)
            Server_BroadcastLine("START");
            if (onStatus != null)
            {
                onStatus.Invoke("START broadcasted.");
            }
        }
        else
        {
            // 로비 외 커맨드는 게임 모듈로 전달
            if (onServerCommand != null)
            {
                onServerCommand.Invoke(cc.id, cmd, payload);
                return;
            }

            if (onStatus != null)
            {
                onStatus.Invoke($"Unknown cmd from {cc.id}: {cmd}");
            }
        }
    }

    private void Server_RemoveClient(int id)
    {
        if (clients.ContainsKey(id) == true)
        {
            try
            {
                clients[id].socket.Close();
            }
            catch
            {
            }
            clients.Remove(id);
        }

        if (players.ContainsKey(id) == true)
        {
            players.Remove(id);
        }

        if (onStatus != null)
        {
            onStatus.Invoke($"Client {id} removed.");
        }
    }

    private void Server_RoomBroadcastTick()
    {
        roomBroadcastTimer = roomBroadcastTimer + Time.deltaTime;
        if (roomBroadcastTimer < roomBroadcastInterval)
        {
            return;
        }
        roomBroadcastTimer = 0.0f;
        Server_BroadcastRoom();
    }

    private void Server_BroadcastRoom()
    {
        // 간단 JSON 구성
        // {"players":[{"id":1,"name":"Alice","ready":true}, ...]}
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("{\"players\":[");
        bool first = true;
        foreach (var kv in players)
        {
            PlayerInfo p = kv.Value;
            if (first == false)
            {
                sb.Append(",");
            }
            first = false;
            sb.Append("{");
            sb.AppendFormat("\"id\":{0},\"name\":\"{1}\",\"ready\":{2}",
                p.id, EscapeJson(p.name), p.ready == true ? "true" : "false");
            sb.Append("}");
        }
        sb.Append("]}");
        string json = sb.ToString();

        Server_BroadcastLine("ROOM|" + json);
    }

    private string EscapeJson(string s)
    {
        if (s == null)
        {
            return "";
        }
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private void Server_BroadcastLine(string line)
    {
        foreach (var kv in clients)
        {
            ClientConn cc = kv.Value;
            if (cc == null)
            {
                continue;
            }
            if (cc.lp == null)
            {
                continue;
            }
            cc.lp.WriteLine(line);
        }
    }

    // ===== 클라이언트: 접속/끊기/송수신 =====

    public void ClientConnect(string address, int port)
    {
        if (clientConnected == true)
        {
            if (onStatus != null)
            {
                onStatus.Invoke("Client already connected.");
            }
            return;
        }

        try
        {
            client = new TcpClient();
            client.NoDelay = true;
            client.Connect(address, port);
            clientStream = client.GetStream();
            clientLP = new LineProtocol(clientStream);
            clientConnected = true;

            if (onClientConnectedChanged != null)
            {
                onClientConnectedChanged.Invoke(true);
            }

            if (onStatus != null)
            {
                onStatus.Invoke($"Connected to {address}:{port}");
            }
        }
        catch (Exception e)
        {
            if (onStatus != null)
            {
                onStatus.Invoke($"Connect failed: {e.Message}");
            }
        }
    }

    public void ClientDisconnect()
    {
        if (clientConnected == false)
        {
            return;
        }

        try
        {
            if (client != null)
            {
                client.Close();
            }
        }
        catch
        {
        }

        client = null;
        clientStream = null;
        clientLP = null;
        clientConnected = false;

        if (onClientConnectedChanged != null)
        {
            onClientConnectedChanged.Invoke(false);
        }

        if (onStatus != null)
        {
            onStatus.Invoke("Disconnected.");
        }
    }

    public void ClientSendLine(string line)
    {
        if (clientConnected == false)
        {
            return;
        }
        if (clientLP == null)
        {
            return;
        }
        clientLP.WriteLine(line);
    }

    private void Client_PollReceive()
    {
        if (clientConnected == false)
        {
            return;
        }
        if (clientLP == null)
        {
            return;
        }

        List<string> lines = clientLP.ReadAvailableLines();
        if (lines == null)
        {
            return;
        }

        for (int i = 0; i < lines.Count; i = i + 1)
        {
            string line = lines[i];
            Client_HandleLine(line);
        }
    }

    private void Client_HandleLine(string line)
    {
        if (string.IsNullOrEmpty(line) == true)
        {
            return;
        }

        int bar = line.IndexOf('|');
        string cmd;
        string payload;

        if (bar >= 0)
        {
            cmd = line.Substring(0, bar);
            payload = line.Substring(bar + 1);
        }
        else
        {
            cmd = line;
            payload = string.Empty;
        }

        if (cmd == "ROOM")
        {
            // UI 텍스트로 그대로 보여준다(파싱 생략 가능)
            if (onRoomText != null)
            {
                onRoomText.Invoke(payload);
            }
        }
        else if (cmd == "START")
        {
            if (onStartSignal != null)
            {
                onStartSignal.Invoke();
            }
        }
        else
        {
            // 게임 단계 커맨드는 클라 모듈로 전달
            if (onClientCommand != null)
            {
                onClientCommand.Invoke(cmd, payload);
                return;
            }

            // 기타 메시지는 상태 로그로 출력
            if (onStatus != null)
            {
                onStatus.Invoke("SAYS: " + line);
            }
        }
    }

    public bool IsServerRunning()
    {
        return serverRunning == true;
    }

    public bool IsClientConnected()
    {
        return clientConnected == true;
    }

    public void HostBroadcastStart()
    {
        if (serverRunning == false)
        {
            return;
        }

        Server_BroadcastLine("START");

        // 호스트 자신에게도 즉시 START 신호 발생 (중요!)
        if (onStartSignal != null)
        {
            onStartSignal.Invoke();
        }

        if (onStatus != null)
        {
            onStatus.Invoke("START broadcasted by host.");
        }
    }

    public void HostSetName(string name)
    {
        if (serverRunning == false)
        {
            return;
        }
        if (includeHostInRoom == false)
        {
            return;
        }
        if (hostPlayer == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(name) == false)
        {
            hostPlayer.name = name;
        }
        Server_BroadcastRoom();
    }

    public void HostSetReady(bool ready)
    {
        if (serverRunning == false)
        {
            return;
        }
        if (includeHostInRoom == false)
        {
            return;
        }
        if (hostPlayer == null)
        {
            return;
        }

        hostPlayer.ready = ready;
        Server_BroadcastRoom();
    }

    public void ServerBroadcastLinePublic(string line)
    {
        // 1) 네트워크로 클라이언트들에게 방송
        if (serverRunning == true)
        {
            Server_BroadcastLine(line);
        }

        // 2) 호스트(서버) 로컬에도 같은 내용을 전달해 호스트 화면에서도 적용되게 함
        //    (ClientGame.OnClientCommand -> ApplyStateJson 이 호출되도록)
        int bar = line.IndexOf('|');
        string cmd = bar >= 0 ? line.Substring(0, bar) : line;
        string payload = bar >= 0 ? line.Substring(bar + 1) : string.Empty;

        // STATE 외에도 필요하면 다른 메시지도 로컬 반영 가능
        if (cmd == "STATE")
        {
            if (onClientCommand != null)
            {
                onClientCommand.Invoke(cmd, payload);
            }
        }
    }

    // 실제 참가자 id를 반환
    public List<int> GetCurrentPlayerIdsSnapshot()
    {
        List<int> ids = new List<int>();

        // players 딕셔너리는: id=0 (호스트; includeHostInRoom==true일 때) + 클라들(1..N)
        if (players != null)
        {
            foreach (var kv in players)
            {
                ids.Add(kv.Key);
            }
        }

        // 정렬은 선택 사항(보기 좋게)
        ids.Sort();

        return ids;
    }

    public void ServerInjectCommand(int fromClientId, string cmd, string payload)
    {
        // 서버가 켜져 있을 때, 네트워크 경유 없이
        // 서버 콜백(onServerCommand)을 직접 호출해 로컬 입력을 주입한다.
        if (serverRunning == false)
        {
            return;
        }

        if (onServerCommand != null)
        {
            onServerCommand.Invoke(fromClientId, cmd, payload);
        }
    }
}
