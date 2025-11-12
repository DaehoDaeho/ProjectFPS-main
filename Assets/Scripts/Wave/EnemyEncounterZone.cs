using NUnit.Framework;
using System.Collections.Generic;
using TMPro.Examples;
using UnityEditor.U2D;
using UnityEngine;

/// <summary>
/// 플레이어가 이벤트 트리거에 진입했을 시 데이터에 세팅된 스폰 포인트에 세팅된 수만큼의 적을 스폰하는 클래스.
/// </summary>
public class EnemyEncounterZone : MonoBehaviour
{
    public string encounterName = "Encounter A";
    public bool startOnPlayerEnter = true;
    public string playerTag = "player";

    public WaveDefinition[] waves;

    public int maxAliveEnemies = 10;
    public bool endEncounterWhenDone = true;    // 모든 웨이브 종료 시 자동 종료 표시 여부.

    // 당장 필수는 아님.
    public GameObject[] lockOnStart;
    public GameObject[] unlockOnEnd;

    private bool activated; // 이미 한 번 활성화 되었는지 여부.
    private int currentWaveIndex;
    private float waveCooldownTimer;

    private int aliveEnemies;
    private int totalSpawnedThisWave;
    private int totalToSpawnThisWave;

    private class RuntimeWaveEntryState
    {
        public WaveEnemyEntry config;
        public int spawnedCount;
        public float nextSpawnTime;
    }

    private List<RuntimeWaveEntryState> entryStates = new List<RuntimeWaveEntryState>();

    private Collider zoneCollider;
        
    // Update is called once per frame
    void Update()
    {
        if(activated == false)
        {
            return;
        }

        if(waves == null)
        {
            return;
        }

        if(currentWaveIndex < 0)
        {
            return;
        }

        if(currentWaveIndex >= waves.Length)
        {
            return;
        }

        WaveDefinition wave = waves[currentWaveIndex];

        UpdateWaveSpawning(wave);
        CheckWaveCompletion(wave);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(startOnPlayerEnter == false)
        {
            return;
        }

        if(other == null)
        {
            return;
        }

        if(other.CompareTag(playerTag) == true)
        {
            StartEncounter();
        }
    }

    public void StartEncounter()
    {
        if(activated == true)
        {
            return;
        }

        activated = true;
        currentWaveIndex = 0;
        waveCooldownTimer = 0.0f;
        aliveEnemies = 0;

        SetupWaveRuntimeState();

        SetObjectsActive(lockOnStart, true);
        SetObjectsActive(unlockOnEnd, false);
    }

    // 현재 웨이브의 WaveDefinition을 기반으로 런타임 상태를 준비한다.
    void SetupWaveRuntimeState()
    {
        entryStates.Clear();
        totalSpawnedThisWave = 0;
        totalToSpawnThisWave = 0;

        if(waves == null)
        {
            return;
        }

        if(currentWaveIndex < 0)
        {
            return;
        }

        if(currentWaveIndex >= waves.Length)
        {
            return;
        }

        WaveDefinition wave = waves[currentWaveIndex];
        if(wave.enemies == null)
        {
            return;
        }

        float now = Time.time;

        for(int i=0; i<wave.enemies.Length; ++i)
        {
            WaveEnemyEntry e = wave.enemies[i];
            if(e == null)
            {
                continue;
            }

            if(e.spawnPoint == null)
            {
                continue;
            }

            if(e.enemyPrefab == null)
            {
                continue;
            }

            if(e.count <= 0)
            {
                continue;
            }

            RuntimeWaveEntryState state = new RuntimeWaveEntryState();
            state.config = e;
            state.spawnedCount = 0;
            state.nextSpawnTime = now;

            entryStates.Add(state);
            totalToSpawnThisWave += e.count;
        }
    }

    void UpdateWaveSpawning(WaveDefinition wave)
    {
        if(entryStates.Count <= 0)
        {
            return;
        }

        if(waveCooldownTimer > 0.0f)
        {
            waveCooldownTimer -= Time.deltaTime;
            if(waveCooldownTimer < 0.0f)
            {
                waveCooldownTimer = 0.0f;
            }
            return;
        }

        //==================================================
        if(aliveEnemies >= maxAliveEnemies)
        {
            return;
        }
        //==================================================

        float now = Time.time;

        for(int i=0; i<entryStates.Count; ++i)
        {
            if(aliveEnemies >= maxAliveEnemies)
            {
                break;
            }

            RuntimeWaveEntryState st = entryStates[i];
            if(st == null)
            {
                continue;
            }

            if(st.spawnedCount >= st.config.count)
            {
                continue;
            }

            if(now < st.nextSpawnTime)
            {
                continue;
            }

            GameObject spawned = st.config.spawnPoint.SpawnOne(this);
            if(spawned != null)
            {
                ++st.spawnedCount;
                st.nextSpawnTime = now + st.config.spawnInterval;

                ++aliveEnemies;
                ++totalSpawnedThisWave;
            }
        }
    }

    // 웨이브 완료 조건을 확인하고, 다음 웨이브로 전환하거나 Encounter 종료를 처리한다.
    void CheckWaveCompletion(WaveDefinition wave)
    {
        if(totalSpawnedThisWave < totalToSpawnThisWave)
        {
            return;
        }

        if(wave.waitUntilAllDead == true)
        {
            if(aliveEnemies > 0)
            {
                return;
            }
        }

        ++currentWaveIndex;

        if (currentWaveIndex >= waves.Length)
        {
            OnEncounterCompleted();
        }
        else
        {
            SetupWaveRuntimeState();
        }
    }

    public void OnEnemyDead(EnemyLifetimeReporter reporter)
    {
        if(aliveEnemies > 0)
        {
            --aliveEnemies;
        }
    }

    void OnEncounterCompleted()
    {
        if(endEncounterWhenDone == true)
        {
            activated = false;
        }

        SetObjectsActive(lockOnStart, false);
        SetObjectsActive(unlockOnEnd, true);
    }

    void SetObjectsActive(GameObject[] objs, bool active)
    {
        for(int i=0; i<objs.Length; ++i)
        {
            if (objs[i] != null)
            {
                objs[i].SetActive(active);
            }
        }
    }
}
