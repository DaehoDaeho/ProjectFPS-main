using UnityEngine;

/// <summary>
/// STATE가 도착해 아바타가 생성된 뒤, 내 아바타 Transform을 찾아
/// FirstPersonCameraRig.target에 바인딩하고 전방 정렬까지 수행.
/// </summary>
[DisallowMultipleComponent]
public class FirstPersonAutoBind : MonoBehaviour
{
    public FirstPersonCameraRig rig;
    public LocalAvatarLocator locator;

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

    private void Start()
    {
        TryBindOnce();
    }

    private void OnClientCommand(string cmd, string payload)
    {
        if (cmd == "STATE")
        {
            TryBindOnce();
        }
    }

    private void TryBindOnce()
    {
        if (rig == null)
        {
            rig = GetComponent<FirstPersonCameraRig>();
        }
        if (locator == null)
        {
            locator = GetComponent<LocalAvatarLocator>();
        }
        if (rig == null)
        {
            return;
        }
        if (locator == null)
        {
            return;
        }

        Transform me = locator.FindMyAvatar();
        if (me != null)
        {
            rig.target = me;

            // 1) 아바타 전방에 카메라를 정렬
            if (rig.alignYawOnBind == true)
            {
                rig.AlignYawToTargetForward();
            }

            // 2) 서버 STATE 각도로 1회 동기화(있다면)
            ClientGame cg = GameObject.FindAnyObjectByType<ClientGame>();
            if (cg != null)
            {
                int myId = locator != null ? locator.myId : 0;
                float syaw = 0.0f;
                float spitch = 0.0f;
                bool ok = cg.TryGetLastAngles(myId, out syaw, out spitch);
                if (ok == true)
                {
                    rig.SetAnglesFromState(syaw, spitch);
                }
            }

            // 구독 해제(중복 바인딩 방지)
            if (NetworkRunner.instance != null)
            {
                NetworkRunner.instance.onClientCommand -= OnClientCommand;
            }
        }
    }
}
