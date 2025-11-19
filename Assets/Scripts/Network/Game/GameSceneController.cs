using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Game_Main 씬 부팅 담당 컨트롤러(서버/클라 공용).
/// - 서버: ServerGame 생성 -> 로비 참가자 스폰 -> STATE 방송 시작.
/// - 클라: ClientGame 생성 -> 아바타 부모/프리팹 보장 -> STATE 스냅 적용.
/// - 단일 PC에서도 확실히 AvatarsRoot/AvatarPrefab이 준비되도록 부트스트랩을 책임진다.
/// </summary>
public class GameSceneController : MonoBehaviour
{
    [Header("Optional Refs (둘 다 비워도 자동 생성됨)")]
    public Transform avatarsRoot;            // 아바타 부모(없으면 자동 생성)
    public GameObject avatarPrefab;          // 아바타 프리팹(없으면 런타임 캡슐 생성)

    [Header("Server Settings")]
    public Transform[] spawnPoints;          // 서버 스폰 지점들
    public float serverTickRate = 20.0f;     // 서버 틱 주기
    public float serverMoveSpeed = 4.5f;     // 이동 속도

    private void Start()
    {
        // 1) 네트워크 러너 확인
        if (NetworkRunner.instance == null)
        {
            Debug.LogWarning("GameSceneController: NetworkRunner not found. Did you keep it with DontDestroyOnLoad?");
            return;
        }

        bool isServer = NetworkRunner.instance.IsServerRunning();
        bool isClient = NetworkRunner.instance.IsClientConnected();

        // 2) 아바타 부모 확정: 비어 있으면 지금 만든다 (씬 전환 직후에도 100% 준비)
        if (avatarsRoot == null)
        {
            GameObject rootGO = new GameObject("AvatarsRoot");
            avatarsRoot = rootGO.transform;
        }

        // 3) 아바타 프리팹 확정: 비어 있으면 런타임용 캡슐 프리팹을 즉석 생성해 쓴다
        //    (프리팹 에셋이 없어도 동작하도록)
        GameObject runtimeAvatarPrefab = avatarPrefab;
        if (runtimeAvatarPrefab == null)
        {
            runtimeAvatarPrefab = CreateRuntimeCapsulePrefab();
        }

        // 4) 서버 세팅: 중복 추가 방지 후 ServerGame 구성
        if (isServer == true)
        {
            ServerGame sg = gameObject.GetComponent<ServerGame>();
            if (sg == null)
            {
                sg = gameObject.AddComponent<ServerGame>();
            }

            sg.spawnPoints = spawnPoints;
            sg.tickRate = serverTickRate;
            sg.moveSpeed = serverMoveSpeed;

            // 로비 참가자 id 목록을 러너에서 얻어오는 방식이 가장 정확함
            // (임시 유틸 대신, 러너에 안전한 getter가 있다면 그걸 사용)
            //List<int> ids = NetworkUtil.GetCurrentPlayerIds(); // 임시 방식 사용 중이면 후일 교체
            List<int> ids = NetworkRunner.instance.GetCurrentPlayerIdsSnapshot();
            sg.SpawnPlayersInitial(ids);
        }

        // 5) 클라 표시 세팅: 서버/클라 모두 STATE를 보고 싶으면 둘 다 ClientGame 부착
        if (isClient == true || isServer == true)
        {
            ClientGame cg = gameObject.GetComponent<ClientGame>();
            if (cg == null)
            {
                cg = gameObject.AddComponent<ClientGame>();
            }

            cg.avatarsRoot = avatarsRoot;          // 보장된 참조 전달
            cg.avatarPrefab = runtimeAvatarPrefab;  // 보장된 프리팹 전달
            cg.InitAvatarRoot();

            // 로컬 입력 전송자(클라 조작용)
            InputSender input = gameObject.GetComponent<InputSender>();
            if (input == null)
            {
                input = gameObject.AddComponent<InputSender>();
            }
            input.mouseSensitivity = 3.0f;
            input.sendRate = 20.0f;
            input.lockCursorOnStart = true;
        }
    }

    /// <summary>
    /// 런타임에 사용할 "임시 아바타 프리팹"을 코드로 생성한다.
    /// - Capsule + 이름/레이어 기본값.
    /// - 프리팹 에셋이 없어도 동작 보장을 위한 용도.
    /// </summary>
    private GameObject CreateRuntimeCapsulePrefab()
    {
        // 빈 부모 GO를 만든 뒤, 그 아래에 캡슐을 붙여서 하나의 프리팹처럼 사용
        GameObject root = new GameObject("AvatarPrefab_RuntimeCapsule");

        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Body";
        capsule.transform.SetParent(root.transform, false);
        capsule.transform.localPosition = new Vector3(0.0f, 1.0f, 0.0f);

        // 필요하면 머티리얼/색상/스케일을 가볍게 조정 가능(필수는 아님)

        // 프리팹처럼 Instantiate해서 사용하므로 비활성은 필요 없음
        return root;
    }
}
