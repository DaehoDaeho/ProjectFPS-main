using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 지정된 포인트 배열을 순환하며 적 프리팹을 생성.
/// - 최대 동시 적 수(maxAlive)를 유지.
/// - respawnInterval 간격으로 비어 있는 슬롯을 채움.
/// - 파괴된 적 레퍼런스를 주기적으로 정리(Cleanup)
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;      // 스폰할 적 프리팹.
    public Transform[] spawnPoints;     // 스폰 지점들(순환)
    public int maxAlive = 5;            // 동시에 존재할 수 있는 최대 적 수.
    public float respawnInterval = 3.0f;// 리스폰 간격(초)

    private List<GameObject> alive = new List<GameObject>(); // 현재 살아 있는 적 목록.
    private float timer;                // 리스폰 타이머.
    private int nextIndex;              // 다음 스폰 포인트 인덱스(순환)

    private void Awake()
    {
        timer = respawnInterval;
        nextIndex = 0;
    }

    private void Update()
    {
        // 동시 수가 꽉 찼으면 리스트 정리만 수행.
        if (alive.Count >= maxAlive)
        {
            CleanupList();
            return;
        }

        // 타이머 경과 시 스폰 시도.
        timer = timer - Time.deltaTime;
        if (timer <= 0.0f)
        {
            SpawnOne();
            timer = respawnInterval;
        }

        // 파괴된 오브젝트 레퍼런스 제거.
        CleanupList();
    }

    private void SpawnOne()
    {
        if (enemyPrefab == null)
        {
            return;
        }
        if (spawnPoints == null)
        {
            return;
        }
        if (spawnPoints.Length <= 0)
        {
            return;
        }

        Transform p = spawnPoints[nextIndex];
        if (p != null)
        {
            GameObject e = Instantiate(enemyPrefab, p.position, p.rotation);
            alive.Add(e);
        }

        // 순환 인덱스 증가.
        nextIndex = nextIndex + 1;
        if (nextIndex >= spawnPoints.Length)
        {
            nextIndex = 0;
        }
    }

    private void CleanupList()
    {
        // null(파괴된) 항목 제거: 뒤에서 앞으로 순회하며 RemoveAt
        for (int i = alive.Count - 1; i >= 0; i = i - 1)
        {
            if (alive[i] == null)
            {
                alive.RemoveAt(i);
            }
        }
    }
}
