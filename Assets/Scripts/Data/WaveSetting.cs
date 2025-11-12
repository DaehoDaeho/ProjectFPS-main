using UnityEngine;

[System.Serializable]
public class WaveEnemyEntry
{
    public EnemySpawnPoint spawnPoint;
    public GameObject enemyPrefab;
    public int count = 3;
    public float spawnInterval = 0.5f;
}

[System.Serializable]
public class WaveDefinition
{
    public string waveName = "Wave 1";
    public WaveEnemyEntry[] enemies;
    public bool waitUntilAllDead = true;
    public float delayAfterWave = 1.0f;
}