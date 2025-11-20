using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 간단 헬퍼: 현재 클라이언트(또는 호스트)의 '내 아바타' Transform을 찾아서 돌려준다.
/// 데모 기준으로 아바타 이름이 "Avatar_{id}" 라고 가정.
/// 실제로는 NetworkRunner에 '내 id'를 반환하는 getter가 있으면 그걸 쓰는 게 가장 좋다.
/// </summary>
public class LocalAvatarLocator : MonoBehaviour
{
    public int myId = 0;                      // 호스트=0, 첫 클라=1 ...
    public Transform avatarsRoot;             // 아바타들이 모여있는 루트

    public Transform FindMyAvatar()
    {
        if (avatarsRoot == null)
        {
            return null;
        }

        string targetName = $"Avatar_{myId}";
        for (int i = 0; i < avatarsRoot.childCount; i = i + 1)
        {
            Transform c = avatarsRoot.GetChild(i);
            if (c == null)
            {
                continue;
            }
            if (c.name == targetName)
            {
                return c;
            }
        }
        return null;
    }
}
