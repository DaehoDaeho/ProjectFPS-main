using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

/// <summary>
/// 클라이언트에서 서버 STATE 메시지를 받아 캐릭터 위치/회전을 적용.
/// - 보간/예측 없음: 받은 값을 그대로 쓴다.
/// </summary>
public class ClientGame : MonoBehaviour
{
    [Header("Avatar")]
    public Transform avatarsRoot;          // 캐릭터 부모(없으면 자동 생성)
    public GameObject avatarPrefab;        // 단순 캡슐 프리팹(머티리얼 색만 달라도 OK)

    // id → Transform
    private Dictionary<int, Transform> avatars;

    private Dictionary<int, float> lastYawById = new Dictionary<int, float>();
    private Dictionary<int, float> lastPitchById = new Dictionary<int, float>();
    
    private void Awake()
    {
        avatars = new Dictionary<int, Transform>();
    }

    public void InitAvatarRoot()
    {
        if (avatarsRoot == null)
        {
            GameObject go = new GameObject("AvatarsRoot");
            avatarsRoot = go.transform;
        }
    }

    private void OnEnable()
    {
        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.onClientCommand += OnClientCommand;
        }
    }

    private void OnDisable()
    {
        if (NetworkRunner.instance != null)
        {
            NetworkRunner.instance.onClientCommand -= OnClientCommand;
        }
    }

    private void OnClientCommand(string cmd, string payload)
    {
        if (cmd == "STATE")
        {
            ApplyStateJson(payload);
        }
    }

    private void ApplyStateJson(string json)
    {
        // 아주 단순 파싱: 정규식으로 players 배열에서 id, x,y,z, yaw만 뽑는다.
        // (실전에서는 JSON 파서 사용 권장. 초보 강의라 의존성 줄이기 위해 간단 파싱)
        // 패턴 예: {"id":1,"x":0.0,"y":1.8,"z":2.0,"yaw":90,"hp":100}
        Regex item = new Regex("\\{\"id\":(\\d+),\"x\":([-0-9\\.]+),\"y\":([-0-9\\.]+),\"z\":([-0-9\\.]+),\"yaw\":([-0-9\\.]+),\"hp\":(\\d+)\\}",
                               RegexOptions.Compiled);

        MatchCollection matches = item.Matches(json);
        if (matches == null)
        {
            return;
        }

        for (int i = 0; i < matches.Count; i = i + 1)
        {
            GroupCollection g = matches[i].Groups;

            int id = 0;
            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;
            float yaw = 0.0f;

            int.TryParse(g[1].Value, out id);
            float.TryParse(g[2].Value, out x);
            float.TryParse(g[3].Value, out y);
            float.TryParse(g[4].Value, out z);
            float.TryParse(g[5].Value, out yaw);

            Transform t = GetOrCreateAvatar(id);
            if (t == null)
            {
                continue;
            }

            // 적용(보간 없음)
            t.position = new Vector3(x, y, z);
            t.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);

            // 예: p.id, p.yaw, p.pitch 를 파싱한 직후
            lastYawById[id] = yaw;
            lastPitchById[id] = 0;
        }
    }

    private Transform GetOrCreateAvatar(int id)
    {
        if (avatars.ContainsKey(id) == true)
        {
            return avatars[id];
        }

        if (avatarPrefab == null)
        {
            // 캡슐 기본 생성
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = $"Avatar_{id}";
            capsule.transform.SetParent(avatarsRoot, false);
            avatars.Add(id, capsule.transform);

            //======================================================
            ServerGame sg = GetComponent<ServerGame>();
            if (sg != null)
            {
                sg.SetRoot(id, capsule.transform);
            }
            //======================================================

            return capsule.transform;
        }
        else
        {
            GameObject go = GameObject.Instantiate(avatarPrefab, avatarsRoot);
            go.name = $"Avatar_{id}";
            avatars.Add(id, go.transform);

            //======================================================
            ServerGame sg = GetComponent<ServerGame>();
            if (sg != null)
            {
                sg.SetRoot(id, go.transform);
            }
            //======================================================

            return go.transform;
        }
    }

    public bool TryGetLastAngles(int id, out float yaw, out float pitch)
    {
        yaw = 0.0f;
        pitch = 0.0f;
        bool ok = false;

        if (lastYawById.ContainsKey(id) == true)
        {
            yaw = lastYawById[id];
            ok = true;
        }
        if (lastPitchById.ContainsKey(id) == true)
        {
            pitch = lastPitchById[id];
            // ok는 그대로 유지(둘 다 있으면 true, 하나만 있어도 true)
            ok = true;
        }
        return ok;
    }
}
