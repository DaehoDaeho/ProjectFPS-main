using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 간단한 게임오브젝트 풀(데칼/스파크 등 재사용).
/// </summary>
public class DecalPool : MonoBehaviour
{
    public int prewarmCount = 50;            // 시작 시 미리 만들 개수.
    public float autoReturnAfter = 10.0f;    // 자동 반환 시간(초)

    private List<GameObject> pooled = new List<GameObject>(); // 풀에서 대기 중.
    private List<GameObject> inUse = new List<GameObject>();  // 현재 사용 중.

    private void Awake()
    {
        // 미리 풀링하지 않음(프리팹이 다양하므로 런타임 최초 요청 때 생성)
    }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        GameObject go = null;

        // 동일 프리팹으로 미리 만든 오브젝트가 있으면 재사용(단순화를 위해 타입 미검증)
        if (pooled.Count > 0)
        {
            go = pooled[pooled.Count - 1];
            pooled.RemoveAt(pooled.Count - 1);
            if (go != null)
            {
                go.transform.position = pos;
                go.transform.rotation = rot;
                go.SetActive(true);
                inUse.Add(go);
                StartCoroutine(AutoReturn(go, autoReturnAfter));
                return go;
            }
        }

        // 없으면 새로 생성.
        go = Instantiate(prefab, pos, rot);
        inUse.Add(go);
        StartCoroutine(AutoReturn(go, autoReturnAfter));
        return go;
    }

    public void Despawn(GameObject go)
    {
        if (go == null)
        {
            return;
        }
        bool removed = inUse.Remove(go);
        if (removed == true)
        {
            go.SetActive(false);
            go.transform.SetParent(transform, true);
            pooled.Add(go);
        }
    }

    private IEnumerator AutoReturn(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        Despawn(go);
    }
}
