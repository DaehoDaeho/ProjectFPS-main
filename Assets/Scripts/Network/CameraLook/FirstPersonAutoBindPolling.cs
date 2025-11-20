using System.Collections;
using UnityEngine;

/// <summary>
/// 일정 시간 동안 주기적으로 아바타를 찾다가 발견 즉시 바인딩.
/// </summary>
[DisallowMultipleComponent]
public class FirstPersonAutoBindPolling : MonoBehaviour
{
    public FirstPersonCameraRig rig;
    public LocalAvatarLocator locator;
    public float timeoutSeconds = 5.0f;    // 최대 대기 시간
    public float intervalSeconds = 0.2f;   // 재시도 간격

    private void Start()
    {
        StartCoroutine(CoBind());
    }

    private IEnumerator CoBind()
    {
        float elapsed = 0.0f;

        while (elapsed < timeoutSeconds)
        {
            if (rig == null)
            {
                rig = GetComponent<FirstPersonCameraRig>();
            }
            if (locator == null)
            {
                locator = GetComponent<LocalAvatarLocator>();
            }
            if (rig != null && locator != null)
            {
                Transform me = locator.FindMyAvatar();
                if (me != null)
                {
                    rig.target = me;
                    yield break;
                }
            }

            yield return new WaitForSeconds(intervalSeconds);
            elapsed = elapsed + intervalSeconds;
        }

        Debug.LogWarning("FirstPersonAutoBindPolling: 타임아웃으로 바인딩 실패.");
    }
}
