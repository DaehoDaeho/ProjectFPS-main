using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 서버 권한 이동 시뮬레이션과 STATE 방송을 담당.
/// - 클라이언트의 INPUT을 받아 서버에서 좌표/회전을 계산.
/// - 일정 주기로 STATE를 브로드캐스트.
/// </summary>
public class ServerGame : MonoBehaviour
{
    [Header("Settings")]
    public float tickRate = 20.0f;         // 서버 틱(초당 20회 -> dt=0.05)
    public float moveSpeed = 4.5f;         // 이동 속도(m/s)
    public Transform[] spawnPoints;        // 스폰 위치 목록

    private float tickAccumulator;         // 틱 누적 시간
    private Dictionary<int, PlayerSim> sims;  // 각 플레이어의 시뮬레이션 상태

    // 클라이언트가 보낸 최근 입력을 저장
    private class PendingInput
    {
        public float worldX;   // 월드 기준 이동 X(우+ 좌-)
        public float worldZ;   // 월드 기준 이동 Z(앞+ 뒤-)
        public float yaw;      // 시선 각(도)
        public float pitch;    // 필요 시 사용
    }

    private class PlayerSim
    {
        public int id;                 // 플레이어 ID
        public string name;            // 닉네임(옵션)
        public Vector3 position;       // 월드 좌표
        public float yaw;              // 도(수평)
        public float pitch;            // 도(수직)
        public int hp;                 // 체력
        public PendingInput input;     // 최신 입력(없으면 정지)
    }

    private void Awake()
    {
        sims = new Dictionary<int, PlayerSim>();
    }

    private void OnEnable()
    {
        // 서버만 동작
        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.onServerCommand += OnServerCommand;
        }
    }

    private void OnDisable()
    {
        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.onServerCommand -= OnServerCommand;
        }
    }

    /// <summary>
    /// 게임 씬 시작 시, 로비에 있던 참가자들을 스폰한다.
    /// </summary>
    public void SpawnPlayersInitial(IReadOnlyList<int> playerIds)
    {
        int count = playerIds.Count;
        for (int i = 0; i < count; i = i + 1)
        {
            int id = playerIds[i];

            Vector3 pos = Vector3.zero;
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int idx = i % spawnPoints.Length;
                if (spawnPoints[idx] != null)
                {
                    pos = spawnPoints[idx].position;
                }
            }

            PlayerSim sim = new PlayerSim();
            sim.id = id;
            sim.name = $"Player{id}";
            sim.position = pos;
            sim.yaw = 0.0f;
            sim.pitch = 0.0f;
            sim.hp = 100;
            sim.input = new PendingInput();
            sims.Add(id, sim);
        }
    }

    private void Update()
    {
        // 서버 아닌 경우 동작 안 함
        if (NetworkRunner.instance == null)
        {
            return;
        }
        if (NetworkRunner.instance.IsServerRunning() == false)
        {
            return;
        }

        float dt = Time.deltaTime;
        tickAccumulator = tickAccumulator + dt;

        float tickDelta = 1.0f / tickRate;
        while (tickAccumulator >= tickDelta)
        {
            ServerTick(tickDelta);
            tickAccumulator = tickAccumulator - tickDelta;
        }
    }

    private void ServerTick(float dt)
    {
        // 입력 적용 -> 위치/회전 갱신
        foreach (var kv in sims)
        {
            PlayerSim sim = kv.Value;
            if (sim == null)
            {
                continue;
            }

            PendingInput input = sim.input;
            if (input == null)
            {
                continue;
            }

            // 1) 이미 월드 방향으로 온 값 사용
            Vector3 wish = new Vector3(input.worldX, 0.0f, input.worldZ);

            if (wish.sqrMagnitude > 0.0001f)
            {
                wish = wish.normalized;
                Vector3 delta = wish * moveSpeed * dt;
                sim.position = sim.position + delta;
            }

            // 2) 각도 유지(시야 연출용)
            sim.yaw = input.yaw;
            sim.pitch = input.pitch;
        }

        // 주기적으로 STATE 방송
        BroadcastState();
    }

    private void BroadcastState()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("{\"players\":[");
        bool first = true;
        foreach (var kv in sims)
        {
            PlayerSim p = kv.Value;
            if (p == null)
            {
                continue;
            }

            if (first == false)
            {
                sb.Append(",");
            }
            first = false;

            sb.Append("{");
            sb.AppendFormat("\"id\":{0},\"x\":{1:F3},\"y\":{2:F3},\"z\":{3:F3},\"yaw\":{4:F1},\"hp\":{5}",
                p.id, p.position.x, p.position.y, p.position.z, p.yaw, p.hp);
            sb.Append("}");
        }
        sb.Append("]}");

        string json = sb.ToString();
        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.ServerBroadcastLinePublic("STATE|" + json);
        }
    }

    private void OnServerCommand(int fromClientId, string cmd, string payload)
    {
        if (cmd == "INPUTW")
        {
            HandleInputWorld(fromClientId, payload);
            return;
        }

        // 추후 FIRE 판정 추가 예정
    }

    private void HandleInputWorld(int fromClientId, string payload)
    {
        // payload: "wx,wz,yaw,pitch" (InvariantCulture 로 들어옴)
        string[] parts = payload.Split(',');
        if (parts == null)
        {
            return;
        }
        if (parts.Length < 4)
        {
            return;
        }

        // Culture 안전 파싱
        System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;

        float wx = 0.0f;
        float wz = 0.0f;
        float yawDeg = 0.0f;
        float pitchDeg = 0.0f;

        float.TryParse(parts[0], System.Globalization.NumberStyles.Float, inv, out wx);
        float.TryParse(parts[1], System.Globalization.NumberStyles.Float, inv, out wz);
        float.TryParse(parts[2], System.Globalization.NumberStyles.Float, inv, out yawDeg);
        float.TryParse(parts[3], System.Globalization.NumberStyles.Float, inv, out pitchDeg);

        if (sims.ContainsKey(fromClientId) == true)
        {
            PlayerSim sim = sims[fromClientId];
            if (sim != null && sim.input != null)
            {
                sim.input.worldX = wx;
                sim.input.worldZ = wz;
                sim.input.yaw = yawDeg;
                sim.input.pitch = pitchDeg;
            }
        }
    }
}
